using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.Validations.PrenatalVisits;

public class UpdatePrenatalVisitDtoValidator : AbstractValidator<UpdatePrenatalVisitDto>
{
    public UpdatePrenatalVisitDtoValidator()
    {
        RuleFor(x => x.VisitDate).NotEmpty();
        RuleFor(x => x.VisitType).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
