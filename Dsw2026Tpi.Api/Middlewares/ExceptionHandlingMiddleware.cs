using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is AppException appEx)
            {
                var details = appEx.Error.Details != null && appEx.Error.Details.Any()
                    ? string.Join(';', appEx.Error.Details.Select(d => $"{d.Field}:{d.Issue}"))
                    : string.Empty;
                _logger.LogWarning("Error de aplicación: {ErrorCode} - {Message} {Details}", appEx.Error.ErrorCode, appEx.Error.Message, details);
            }
            else
            {
                _logger.LogError(ex, "Se produjo un error no controlado durante el procesamiento de la solicitud: {Message}", ex.Message);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        ErrorResponse error = ex is AppException exApp ?
            exApp.Error :
            new ErrorResponse(nameof(ErrorCodes.UNHANDLED_ERROR), ErrorCodes.UNHANDLED_ERROR);
        var status = ex switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            EntityNotFoundException => HttpStatusCode.NotFound,
            ConflictException or AuthenticationException => HttpStatusCode.Conflict,
            AuthorizationException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError,
        };
        var result = JsonSerializer.Serialize(error, Dsw2026Tpi.Data.Options.JsonOptions.JsonSerializerOptions);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(result);
    }
}
