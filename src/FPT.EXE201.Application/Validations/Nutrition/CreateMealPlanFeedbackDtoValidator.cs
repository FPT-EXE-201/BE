using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateMealPlanFeedbackDtoValidator : AbstractValidator<CreateMealPlanFeedbackDto>
{
    public CreateMealPlanFeedbackDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
    }
}
