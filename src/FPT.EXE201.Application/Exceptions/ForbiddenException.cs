namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when access to a resource is forbidden
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException() : base()
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
