namespace FPT.EXE201.Application.DTOs.Common;

/// <summary>
/// Standard API response wrapper for consistent response format.
/// Instances are created by BaseApiController (for success) or GlobalExceptionFilter (for errors).
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Collection of error messages (if any)
    /// </summary>
    public IList<string>? Errors { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; }

    public ApiResponse()
    {
        Timestamp = DateTime.UtcNow;
        Message = string.Empty;
    }

    public ApiResponse(bool success, string message, int statusCode = 200, IList<string>? errors = null)
    {
        Success = success;
        Message = message;
        StatusCode = statusCode;
        Errors = errors;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Standard API response wrapper with data payload.
/// Instances are created by BaseApiController (for success) or GlobalExceptionFilter (for errors).
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// The response data payload
    /// </summary>
    public T? Data { get; set; }

    public ApiResponse() : base()
    {
    }

    public ApiResponse(bool success, string message, T? data = default, int statusCode = 200, IList<string>? errors = null)
        : base(success, message, statusCode, errors)
    {
        Data = data;
    }
}
