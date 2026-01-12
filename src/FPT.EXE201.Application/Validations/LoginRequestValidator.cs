using FluentValidation;
using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.Validations;

/// <summary>
/// Validator for login requests
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrPhone)
            .NotEmpty().WithMessage("Email or phone number is required")
            .MaximumLength(255).WithMessage("Email or phone must not exceed 255 characters")
            .Must(BeValidEmailOrPhone).WithMessage("Must be a valid email address or Vietnamese phone number");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");
    }

    private bool BeValidEmailOrPhone(string emailOrPhone)
    {
        if (string.IsNullOrWhiteSpace(emailOrPhone))
            return false;

        // Check if it's a valid email
        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (System.Text.RegularExpressions.Regex.IsMatch(emailOrPhone, emailPattern))
            return true;

        // Check if it's a valid Vietnamese phone number
        var phonePattern = @"^(\+84|0)[1-9]\d{8}$";
        return System.Text.RegularExpressions.Regex.IsMatch(emailOrPhone, phonePattern);
    }
}
