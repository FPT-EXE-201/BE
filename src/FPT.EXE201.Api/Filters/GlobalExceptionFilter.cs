using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FPT.EXE201.Api.Filters;

/// <summary>
/// Global exception filter to handle exceptions consistently across all controllers
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        // Extract request context for structured logging
        var httpContext = context.HttpContext;
        var requestPath = httpContext.Request.Path;
        var requestMethod = httpContext.Request.Method;
        var userId = httpContext.User.FindFirst("sub")?.Value 
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var requestId = httpContext.TraceIdentifier;

        ApiResponse apiResponse;
        int statusCode;

        switch (context.Exception)
        {
            case UnauthorizedException ex:
                statusCode = 401;
                apiResponse = new ApiResponse(false, ex.Message, statusCode);
                _logger.LogWarning(ex, 
                    "Unauthorized access attempt. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, requestId);
                break;

            case ForbiddenException ex:
                statusCode = 403;
                apiResponse = new ApiResponse(false, ex.Message, statusCode);
                _logger.LogWarning(ex, 
                    "Forbidden access attempt. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, requestId);
                break;

            case NotFoundException ex:
                statusCode = 404;
                apiResponse = new ApiResponse(false, ex.Message, statusCode);
                _logger.LogInformation(ex, 
                    "Resource not found. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, requestId);
                break;

            case ConflictException ex:
                statusCode = 409;
                apiResponse = new ApiResponse(false, ex.Message, statusCode);
                _logger.LogWarning(ex, 
                    "Conflict occurred. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, requestId);
                break;

            case BadRequestException ex:
                statusCode = 400;
                apiResponse = new ApiResponse(false, ex.Message, statusCode, ex.Errors);
                _logger.LogWarning(ex, 
                    "Bad request. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, Errors: {@Errors}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, ex.Errors, requestId);
                break;

            case ValidationException ex:
                statusCode = 400;
                var errors = ex.Errors?.Select(e => e.ErrorMessage).ToList();
                apiResponse = new ApiResponse(false, "Validation failed", statusCode, errors);
                _logger.LogWarning(ex, 
                    "Validation failed. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, ValidationErrors: {@ValidationErrors}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, ex.Errors, requestId);
                break;

            default:
                statusCode = 500;
                var message = _environment.IsDevelopment() 
                    ? context.Exception.Message 
                    : "An internal server error occurred";
                apiResponse = new ApiResponse(false, message, statusCode);
                
                // Critical errors get full structured logging
                _logger.LogError(context.Exception, 
                    "Unhandled exception occurred. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, ExceptionType: {ExceptionType}, TraceId: {TraceId}",
                    userId ?? "Anonymous", requestPath, requestMethod, context.Exception.GetType().Name, requestId);
                break;
        }

        context.Result = new ObjectResult(apiResponse)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
