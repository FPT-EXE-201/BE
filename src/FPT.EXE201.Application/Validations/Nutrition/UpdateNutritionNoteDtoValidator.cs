using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class UpdateNutritionNoteDtoValidator : AbstractValidator<UpdateNutritionNoteDto>
{
    public UpdateNutritionNoteDtoValidator()
    {
        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Note type must be Diet, Note, or Other.")
            .When(x => x.NoteType.HasValue);

        RuleFor(x => x.ValueText)
            .MaximumLength(200).WithMessage("Value text cannot exceed 200 characters.")
            .When(x => x.ValueText != null);
    }
}
