using FluentValidation;
using FPT.EXE201.Application.DTOs.AutoFill;

namespace FPT.EXE201.Application.Validations.AutoFill;

public class ConfirmExtractionDtoValidator : AbstractValidator<ConfirmExtractionDto>
{
    public ConfirmExtractionDtoValidator()
    {
        RuleFor(x => x.DocumentTypeId)
            .NotEmpty()
            .WithMessage("Document type is required.");

        RuleFor(x => x.EventDate)
            .NotEmpty()
            .WithMessage("Event date is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date cannot be in the future.");

        RuleFor(x => x.Location)
            .MaximumLength(255)
            .WithMessage("Location must not exceed 255 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters.");
    }
}
