using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class GenerateMealPlanDtoValidator : AbstractValidator<GenerateMealPlanDto>
{
    public GenerateMealPlanDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date must be today or in the future.");

        RuleFor(x => x.DurationWeeks)
            .InclusiveBetween(1, 4)
            .WithMessage("Duration must be between 1 and 4 weeks.");

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(500).WithMessage("Additional notes cannot exceed 500 characters.");
    }
}
