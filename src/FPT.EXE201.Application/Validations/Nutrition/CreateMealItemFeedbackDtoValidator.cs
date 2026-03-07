using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateMealItemFeedbackDtoValidator : AbstractValidator<CreateMealItemFeedbackDto>
{
    public CreateMealItemFeedbackDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(300).WithMessage("Comment cannot exceed 300 characters.");
    }
}
