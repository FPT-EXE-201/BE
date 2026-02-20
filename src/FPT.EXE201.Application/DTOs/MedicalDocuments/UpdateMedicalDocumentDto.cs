namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

public record UpdateMedicalDocumentDto(
    Guid? VisitId,
    Guid? DocumentTypeId,
    string? Title,
    DateOnly? DocumentDate,
    string? Notes
);
