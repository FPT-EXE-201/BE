using FluentValidation;
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.Validations.Pregnancies;

public class ChangePregnancyStatusDtoValidator : AbstractValidator<ChangePregnancyStatusDto>
{
    public ChangePregnancyStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid pregnancy status");

        // Khi status = Delivered, bắt buộc có ActualDeliveryDate
        When(x => x.Status == Domain.Enums.PregnancyStatus.Delivered, () =>
        {
            RuleFor(x => x.ActualDeliveryDate)
                .NotNull().WithMessage("Actual delivery date is required when status is Delivered")
                .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Actual delivery date cannot be in the future");

            When(x => x.DeliveryMethod.HasValue, () =>
            {
                RuleFor(x => x.DeliveryMethod!.Value).IsInEnum()
                    .WithMessage("Invalid delivery method");
            });
        });
    }
}
