namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when authentication fails (401 Unauthorized)
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base()
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
