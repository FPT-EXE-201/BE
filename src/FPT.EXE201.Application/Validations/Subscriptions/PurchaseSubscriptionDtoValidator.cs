using FluentValidation;
using FPT.EXE201.Application.DTOs.Subscriptions;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Validations.Subscriptions;

public class PurchaseSubscriptionDtoValidator : AbstractValidator<PurchaseSubscriptionDto>
{
    public PurchaseSubscriptionDtoValidator()
    {
        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan is required.")
            .Must(plan => Enum.TryParse<SubscriptionPlan>(plan, true, out _))
            .WithMessage("Plan must be one of: Monthly, SixMonths, Yearly.");
    }
}
