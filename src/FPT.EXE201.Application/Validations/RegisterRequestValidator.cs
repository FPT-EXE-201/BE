using FluentValidation;
using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.Validations;

/// <summary>
/// Validator for user registration requests
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[@$!%*?&#^()_+=\-\[\]{}|\\:;""'<>,.\/]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .Matches(@"^[\p{L}\s'-]+$").WithMessage("Full name can only contain letters, spaces, hyphens and apostrophes");

        RuleFor(x => x.Phone)
            .Matches(@"^(\+84|0)[1-9]\d{8}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid Vietnamese phone number format (e.g., 0912345678 or +84912345678)");

        RuleFor(x => x.PreferredLanguage)
            .Must(lang => lang == null || lang == "vi" || lang == "en")
            .WithMessage("Preferred language must be 'vi' or 'en'");
    }
}
