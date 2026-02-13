using FluentValidation;
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.Validations.Pregnancies;

public class CreatePregnancyDtoValidator : AbstractValidator<CreatePregnancyDto>
{
    public CreatePregnancyDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.LastMenstrualPeriodDate.HasValue || x.EstimatedConceptionDate.HasValue)
            .WithMessage("Either Last Menstrual Period date or conception date must be provided");

        When(x => x.LastMenstrualPeriodDate.HasValue, () =>
        {
            RuleFor(x => x.LastMenstrualPeriodDate!.Value)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Last Menstrual Period date cannot be in the future")
                .Must(d => d > DateOnly.FromDateTime(DateTime.Today.AddDays(-315)))
                .WithMessage("Last Menstrual Period date cannot be more than 45 weeks ago");
        });

        When(x => x.EstimatedConceptionDate.HasValue, () =>
        {
            RuleFor(x => x.EstimatedConceptionDate!.Value)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Conception date cannot be in the future");
        });

        RuleFor(x => x.Notes).MaximumLength(2000);

        // Nhóm 1
        RuleFor(x => x.BabyNickname).MaximumLength(100);
        RuleFor(x => x.BabyGender).IsInEnum();
        RuleFor(x => x.PregnancyType).IsInEnum();

        // Nhóm 2
        RuleFor(x => x.MotherBloodType).MaximumLength(10);
        When(x => x.PrePregnancyWeightKg.HasValue, () =>
        {
            RuleFor(x => x.PrePregnancyWeightKg!.Value)
                .InclusiveBetween(30m, 300m)
                .WithMessage("Pre-pregnancy weight must be between 30 and 300 kg");
        });
        When(x => x.HeightCm.HasValue, () =>
        {
            RuleFor(x => x.HeightCm!.Value)
                .InclusiveBetween(100m, 250m)
                .WithMessage("Height must be between 100 and 250 cm");
        });

        // Nhóm 3
        RuleFor(x => x.DueDateSource).IsInEnum();
        When(x => x.Gravida.HasValue, () =>
        {
            RuleFor(x => x.Gravida!.Value)
                .InclusiveBetween(1, 20)
                .WithMessage("Gravida must be between 1 and 20");
        });
        When(x => x.Para.HasValue, () =>
        {
            RuleFor(x => x.Para!.Value)
                .InclusiveBetween(0, 20)
                .WithMessage("Para must be between 0 and 20");
        });
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);
    }
}
