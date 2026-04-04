using FluentValidation;
using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.Validations;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequestDto>
{
    public const string RequiredConfirmationPhrase = "XOA_TAI_KHOAN";

    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.ConfirmationPhrase)
            .NotEmpty().WithMessage("Confirmation phrase is required")
            .Must(p => string.Equals(p.Trim(), RequiredConfirmationPhrase, StringComparison.Ordinal))
            .WithMessage($"Confirmation phrase must be exactly '{RequiredConfirmationPhrase}'");
    }
}