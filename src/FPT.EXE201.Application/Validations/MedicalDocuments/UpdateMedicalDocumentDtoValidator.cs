using FluentValidation;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.Validations.MedicalDocuments;

public class UpdateMedicalDocumentDtoValidator : AbstractValidator<UpdateMedicalDocumentDto>
{
    public UpdateMedicalDocumentDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.DocumentDate)
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DocumentDate.HasValue)
            .WithMessage("Document date cannot be in the future.");
    }
}
