namespace FPT.EXE201.Application.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public IList<ValidationError>? Errors { get; }

    public ValidationException() : base("One or more validation errors occurred")
    {
        Errors = new List<ValidationError>();
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<ValidationError>();
    }

    public ValidationException(IList<ValidationError> errors) : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public ValidationException(string message, IList<ValidationError> errors) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new List<ValidationError>();
    }
}

/// <summary>
/// Represents a validation error for a specific field
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The name of the property that failed validation
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// The error message
    /// </summary>
    public string ErrorMessage { get; set; }

    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}
