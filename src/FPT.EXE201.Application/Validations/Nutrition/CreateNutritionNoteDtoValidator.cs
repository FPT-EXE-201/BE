using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateNutritionNoteDtoValidator : AbstractValidator<CreateNutritionNoteDto>
{
    public CreateNutritionNoteDtoValidator()
    {
        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Note type must be Diet, Note, or Other.");

        RuleFor(x => x.ValueText)
            .NotEmpty().WithMessage("Value text is required.")
            .MaximumLength(200).WithMessage("Value text cannot exceed 200 characters.");
    }
}
