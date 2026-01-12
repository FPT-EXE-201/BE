namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when a resource already exists (409 Conflict)
/// </summary>
public class ConflictException : Exception
{
    public ConflictException() : base()
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
