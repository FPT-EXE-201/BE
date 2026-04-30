using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class GenerateMealPlanDtoValidator : AbstractValidator<GenerateMealPlanDto>
{
    public GenerateMealPlanDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .Must(date => !date.HasValue || date.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date must be today or in the future.");

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(500).WithMessage("Additional notes cannot exceed 500 characters.");
    }
}
