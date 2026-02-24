using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class CreateWeightLogDtoValidator : AbstractValidator<CreateWeightLogDto>
{
    public CreateWeightLogDtoValidator()
    {
        RuleFor(x => x.LoggedOn)
            .NotEmpty().WithMessage("Logged date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Logged date cannot be in the future.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThan(500).WithMessage("Weight must be less than 500 kg.");

        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("Note cannot exceed 255 characters.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Source must be a valid WeightSource value (Manual, OCR).");
    }
}
