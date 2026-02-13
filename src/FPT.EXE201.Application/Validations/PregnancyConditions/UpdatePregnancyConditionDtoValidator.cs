using FluentValidation;
using FPT.EXE201.Application.DTOs.PregnancyConditions;

namespace FPT.EXE201.Application.Validations.PregnancyConditions;

public class UpdatePregnancyConditionDtoValidator : AbstractValidator<UpdatePregnancyConditionDto>
{
    public UpdatePregnancyConditionDtoValidator()
    {
        When(x => x.DiagnosedDate.HasValue, () =>
        {
            RuleFor(x => x.DiagnosedDate!.Value)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Diagnosed date cannot be in the future");
        });
        When(x => x.Severity.HasValue, () =>
        {
            RuleFor(x => x.Severity!.Value).IsInEnum()
                .WithMessage("Severity must be Mild, Moderate, or Severe");
        });
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
