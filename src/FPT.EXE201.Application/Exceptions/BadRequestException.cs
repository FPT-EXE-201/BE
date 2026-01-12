namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when a request is invalid or malformed
/// </summary>
public class BadRequestException : Exception
{
    public IList<string>? Errors { get; }

    public BadRequestException() : base()
    {
    }

    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, IList<string> errors) : base(message)
    {
        Errors = errors;
    }

    public BadRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
