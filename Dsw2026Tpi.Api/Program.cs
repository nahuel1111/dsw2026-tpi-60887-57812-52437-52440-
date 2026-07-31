using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.Data.Options;

using Serilog;
using Dsw2026Tpi.Api.Seed;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;


namespace Dsw2026Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Inicializar con un logger simple antes de construir el host
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Iniciando aplicación Dsw2026Tpi.Api");

            var builder = WebApplication.CreateBuilder(args);

            builder.AddSerilogConfiguration();
            builder.Services.AddAppIdentity();
            builder.Services.AddAppAuthentication(builder.Configuration);
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddApplicationPersistence(builder.Configuration);
            builder.Services.AddAppCors(builder.Configuration);
            builder.Services.AddAppDependencies();
            var rlSection = builder.Configuration.GetSection("RateLimiting");
            var periodMinutes = rlSection.GetValue<int>("PeriodMinutes");
            var adminPer = rlSection.GetValue<int>("AdminPerPeriod");
            var patientPer = rlSection.GetValue<int>("PatientPerPeriod");
            var appointmentPer = rlSection.GetValue<int>("AppointmentPerPeriod");
            var generalPer = rlSection.GetValue<int>("GeneralPerPeriod");

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, ct) =>
                {
                    var http = context.HttpContext;
                    var loggerFactory = http.RequestServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("RateLimiter") ?? NullLogger.Instance;
                    logger.LogWarning("Rechazo por límite de peticiones para {Path} desde {RemoteIp}", http.Request.Path, http.Connection.RemoteIpAddress);

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    http.Response.ContentType = "application/json";
                    var err = new ErrorResponse("TOO_MANY_REQUESTS", "demasiadas_solicitudes");
                    err.AddDetail("limite", "se_excedio_el_limite_de_solicitudes");
                    var json = System.Text.Json.JsonSerializer.Serialize(err, JsonOptions.JsonSerializerOptions);
                    await http.Response.WriteAsync(json, ct);
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    if (path.Equals("/auth/admin/login", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith("/auth/admin/login", StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/api/auth/admin/login", StringComparison.OrdinalIgnoreCase))
                    {
                        var partitionKey = "admin-ip-" + ip;
                        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = adminPer,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(periodMinutes),
                            TokensPerPeriod = adminPer,
                            AutoReplenishment = true
                        });
                    }

                    if (path.Equals("/auth/patient/login", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith("/auth/patient/login", StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/api/auth/patient/login", StringComparison.OrdinalIgnoreCase))
                    {
                        var partitionKey = "patient-ip-" + ip;
                        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = patientPer,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(periodMinutes),
                            TokensPerPeriod = patientPer,
                            AutoReplenishment = true
                        });
                    }

                    if ((path.StartsWith("/api/appointments", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/appointments", StringComparison.OrdinalIgnoreCase))
                        && httpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var key = "appointments-user-" + (userId ?? ip);
                        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = appointmentPer,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(periodMinutes),
                            TokensPerPeriod = appointmentPer,
                            AutoReplenishment = true
                        });
                    }

                    var user = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var generalKey = "general-" + (user ?? ip);
                    return RateLimitPartition.GetTokenBucketLimiter(generalKey, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = generalPer,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(periodMinutes),
                        TokensPerPeriod = generalPer,
                        AutoReplenishment = true
                    });
                });
            });
            builder.Services.AddControllers();
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseSerilogRequestLogging();
            app.UseRateLimiter();

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();
            app.MapHealthChecks("/health-check");

            await AdminSeeder.SeedAsync(app.Services);


            Log.Information("Aplicación iniciada correctamente");

            await app.RunAsync();
        }
        catch (HostAbortedException)
        {
            Log.Information("El host fue abortado (normal durante migraciones de EF Core)");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación falló al iniciar");
            throw;
        }
        finally
        {
            Log.Information("Cerrando aplicación");
            await Log.CloseAndFlushAsync();
        }
    }
}

