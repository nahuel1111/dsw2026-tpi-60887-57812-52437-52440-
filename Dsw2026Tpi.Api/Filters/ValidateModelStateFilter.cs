using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dsw2026Tpi.Api.Filters;

public class ValidateModelStateFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        foreach (var model in context.ModelState)
        {
            foreach (var modelError in model.Value!.Errors)
            {
                var message = modelError.ErrorMessage ?? string.Empty;
                if (message.Contains("The JSON value could not be converted", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("System.Guid", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Guid", StringComparison.OrdinalIgnoreCase))
                {
                    var fieldKey = model.Key ?? string.Empty;
                    var field = fieldKey.StartsWith("$.") || fieldKey.StartsWith("$")
                        ? fieldKey
                        : "$." + fieldKey;

                    var err = new ErrorResponse("INVALID_GUID", "GUID inválido");
                    err.AddDetail(field, "guid_invalido");
                    context.Result = new BadRequestObjectResult(err);
                    return;
                }
            }
        }

        // Fallback: construir response genérica de validación
        var error = new ErrorResponse(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        foreach (var model in context.ModelState)
        {
            foreach (var modelError in model.Value!.Errors)
            {
                var message = modelError.ErrorMessage ?? string.Empty;
                error.AddDetail(model.Key, message);
            }
        }

        context.Result = new BadRequestObjectResult(error);
    }
}
