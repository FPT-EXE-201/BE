using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class CreateWeightGoalDtoValidator : AbstractValidator<CreateWeightGoalDto>
{
    public CreateWeightGoalDtoValidator()
    {
        RuleFor(x => x.HeightCm)
            .GreaterThan(50).When(x => x.HeightCm.HasValue)
            .WithMessage("Height must be greater than 50 cm.")
            .LessThan(250).When(x => x.HeightCm.HasValue)
            .WithMessage("Height must be less than 250 cm.");

        RuleFor(x => x.PrePregnancyWeightKg)
            .GreaterThan(0).When(x => x.PrePregnancyWeightKg.HasValue)
            .WithMessage("Pre-pregnancy weight must be greater than 0.")
            .LessThan(500).When(x => x.PrePregnancyWeightKg.HasValue)
            .WithMessage("Pre-pregnancy weight must be less than 500 kg.");

        RuleFor(x => x.RecommendedTotalGainMin)
            .GreaterThanOrEqualTo(0).When(x => x.RecommendedTotalGainMin.HasValue)
            .WithMessage("Minimum gain must be >= 0.");

        RuleFor(x => x.RecommendedTotalGainMax)
            .GreaterThanOrEqualTo(x => x.RecommendedTotalGainMin ?? 0)
            .When(x => x.RecommendedTotalGainMax.HasValue && x.RecommendedTotalGainMin.HasValue)
            .WithMessage("Maximum gain must be >= minimum gain.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
