using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.Validations.PrenatalTests;

public class UpdatePrenatalTestDtoValidator : AbstractValidator<UpdatePrenatalTestDto>
{
    public UpdatePrenatalTestDtoValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
