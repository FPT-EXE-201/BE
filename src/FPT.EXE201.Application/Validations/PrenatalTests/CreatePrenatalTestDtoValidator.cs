using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.Validations.PrenatalTests;

public class CreatePrenatalTestDtoValidator : AbstractValidator<CreatePrenatalTestDto>
{
    public CreatePrenatalTestDtoValidator()
    {
        RuleFor(x => x.TestTypeId).NotEmpty();
        RuleFor(x => x.TestDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
