using System.Security.Claims;
using FPT.EXE201.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

/// <summary>
/// Base controller with common functionality for all API controllers.
/// Provides helper methods to create standardized ApiResponse objects.
/// All exceptions are automatically handled by GlobalExceptionFilter.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Create a successful response with data (200 OK)
    /// </summary>
    /// <typeparam name="T">Type of response data</typeparam>
    /// <param name="data">The data to return</param>
    /// <param name="message">Success message</param>
    protected IActionResult Success<T>(T data, string message = "Operation completed successfully")
    {
        var apiResponse = new ApiResponse<T>(true, message, data, 200);
        return Ok(apiResponse);
    }

    /// <summary>
    /// Create a created response with data (201 Created)
    /// </summary>
    /// <typeparam name="T">Type of response data</typeparam>
    /// <param name="data">The created resource data</param>
    /// <param name="message">Success message</param>
    protected IActionResult Created<T>(T data, string message = "Resource created successfully")
    {
        var apiResponse = new ApiResponse<T>(true, message, data, 201);
        return StatusCode(201, apiResponse);
    }

    /// <summary>
    /// Create a no content response (204 No Content) - for DELETE operations
    /// </summary>
    protected IActionResult NoContentResponse()
    {
        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user's ID from JWT claims
    /// </summary>
    /// <returns>Current user's Guid ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or claim is invalid</exception>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userId;
    }

    // ============================================================================
    // ERROR RESPONSES - Chỉ sử dụng khi cần custom logic đặc biệt
    // Thông thường, throw Exception và để GlobalExceptionFilter xử lý
    // ============================================================================

    /// <summary>
    /// Create a bad request response (400) - Use only when you need custom logic.
    /// Prefer throwing BadRequestException instead.
    /// </summary>
    protected IActionResult BadRequestResponse(string message, IList<string>? errors = null)
    {
        var apiResponse = new ApiResponse(false, message, 400, errors);
        return BadRequest(apiResponse);
    }

    /// <summary>
    /// Create an unauthorized response (401) - Use only when you need custom logic.
    /// Prefer throwing UnauthorizedException instead.
    /// </summary>
    protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
    {
        var apiResponse = new ApiResponse(false, message, 401);
        return Unauthorized(apiResponse);
    }

    /// <summary>
    /// Create a forbidden response (403) - Use only when you need custom logic.
    /// Prefer throwing ForbiddenException instead.
    /// </summary>
    protected IActionResult ForbiddenResponse(string message = "Access forbidden")
    {
        var apiResponse = new ApiResponse(false, message, 403);
        return StatusCode(403, apiResponse);
    }

    /// <summary>
    /// Create a not found response (404) - Use only when you need custom logic.
    /// Prefer throwing NotFoundException instead.
    /// </summary>
    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        var apiResponse = new ApiResponse(false, message, 404);
        return NotFound(apiResponse);
    }

    /// <summary>
    /// Create a conflict response (409) - Use only when you need custom logic.
    /// Prefer throwing ConflictException instead.
    /// </summary>
    protected IActionResult ConflictResponse(string message = "Resource conflict")
    {
        var apiResponse = new ApiResponse(false, message, 409);
        return Conflict(apiResponse);
    }
}
