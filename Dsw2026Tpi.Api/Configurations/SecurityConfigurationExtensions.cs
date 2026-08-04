using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Options;
using Dsw2026Tpi.CrossCutting.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dsw2026Tpi.Api.Configurations;

public static class SecurityConfigurationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener parámetros para creación del JWT desde appsettings.json
        var jwtConfig = configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var issuer = jwtConfig["Issuer"] ?? throw new ArgumentNullException("JWT Issuer");
        var audience = jwtConfig["Audience"] ?? throw new ArgumentNullException("JWT Audience");
        var key = Encoding.UTF8.GetBytes(keyText);

        //Agregar autenticación
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                //Definir parámetros para la generación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var http = context.HttpContext;
                        var loggerFactory = http.RequestServices.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("AuthEvents");
                        logger?.LogWarning("Acceso no autenticado a {Path}", http.Request.Path);

                        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        http.Response.ContentType = "application/json";
                        var err = new ErrorResponse(nameof(ErrorCodes.AUTHENTICATION_FAILED), "no tiene permisos para usar este endpoint");
                        err.AddDetail("auth", "no_autenticado");
                        var json = System.Text.Json.JsonSerializer.Serialize(err, JsonOptions.JsonSerializerOptions);
                        await http.Response.WriteAsync(json);
                    },
                    OnForbidden = async context =>
                    {
                        var http = context.HttpContext;
                        var loggerFactory = http.RequestServices.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("AuthEvents");
                        logger?.LogWarning("Acceso no autorizado a {Path} por {User}", http.Request.Path, http.User?.Identity?.Name ?? "anonymous");

                        http.Response.StatusCode = StatusCodes.Status403Forbidden;
                        http.Response.ContentType = "application/json";
                        var err = new ErrorResponse(nameof(ErrorCodes.AUTHORIZATION_FAILED), ErrorCodes.AUTHORIZATION_FAILED);
             
                        err.AddDetail("auth", "sin_permisos");
                        var json = System.Text.Json.JsonSerializer.Serialize(err, JsonOptions.JsonSerializerOptions);
                        await http.Response.WriteAsync(json);
                    }
                };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminPolicy, policy =>
                policy.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.PatientPolicy, policy =>
                policy.RequireRole(Roles.Patient));
        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        //Obtener configuración para CORS desde appsettings.json
        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()?
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => origin.TrimEnd('/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

        //Si no se definió configuración en el archivo, utilizar la que se define
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost",
                "https://localhost"
            ];
        }

        //Agregar CORS con la política por defecto a partir de las URLs definidas
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true
            };

        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<AuthenticationDbContext>()
          .AddSignInManager()
          .AddDefaultTokenProviders();
        return services;
    }
}
