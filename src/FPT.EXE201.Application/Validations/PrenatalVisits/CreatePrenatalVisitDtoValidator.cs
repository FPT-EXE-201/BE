using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.Validations.PrenatalVisits;

public class CreatePrenatalVisitDtoValidator : AbstractValidator<CreatePrenatalVisitDto>
{
    public CreatePrenatalVisitDtoValidator()
    {
        RuleFor(x => x.VisitDateTime).NotEmpty();
        RuleFor(x => x.VisitType).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
