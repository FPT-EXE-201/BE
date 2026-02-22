using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class UpdateWeightLogDtoValidator : AbstractValidator<UpdateWeightLogDto>
{
    public UpdateWeightLogDtoValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be greater than 0.")
            .LessThan(500).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be less than 500 kg.");

        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("Note cannot exceed 255 characters.");

        RuleFor(x => x.Source)
            .IsInEnum().When(x => x.Source.HasValue)
            .WithMessage("Source must be a valid WeightSource value (Manual, OCR).");
    }
}
