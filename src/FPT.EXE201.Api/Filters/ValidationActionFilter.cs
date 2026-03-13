using FPT.EXE201.Application.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FPT.EXE201.Api.Filters;

/// <summary>
/// Converts ModelState validation errors into ValidationException
/// so they are handled consistently by GlobalExceptionFilter with ApiResponse format.
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
                    new ValidationError(ms.Key, e.ErrorMessage)))
                .ToList();

            throw new ValidationException(errors);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
