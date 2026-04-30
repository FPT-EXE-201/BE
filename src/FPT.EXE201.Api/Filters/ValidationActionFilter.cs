using FPT.EXE201.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FPT.EXE201.Api.Filters;

/// <summary>
/// Converts ModelState validation errors into the standard ApiResponse format.
/// </summary>
public class ValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .SelectMany(ms => ms.Value!.Errors.Select(e =>
                    e.ErrorMessage))
                .ToList();

            context.Result = new BadRequestObjectResult(
                new ApiResponse(false, "Validation failed", 400, errors));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
