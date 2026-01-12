namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when a resource is not found (404 Not Found)
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException() : base()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
