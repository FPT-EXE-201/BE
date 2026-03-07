using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class UpdateFoodPreferenceDtoValidator : AbstractValidator<UpdateFoodPreferenceDto>
{
    public UpdateFoodPreferenceDtoValidator()
    {
        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Severity must be Low, Medium, or High.")
            .When(x => x.Severity.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters.");
    }
}
