using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Metadata khi tạo tài liệu. File upload riêng qua IFormFile trong Controller.
/// </summary>
public record CreateMedicalDocumentDto(
    /// <summary>ID loại tài liệu từ danh mục (ref_document_types).</summary>
    Guid? DocumentTypeId,

    /// <summary>Tiêu đề tài liệu.</summary>
    string? Title,

    /// <summary>Ngày của tài liệu (ngày khám, ngày xét nghiệm).</summary>
    DateOnly? DocumentDate,

    /// <summary>Nguồn gốc: Upload / Share / Import.</summary>
    DocumentSource Source,

    /// <summary>Ghi chú.</summary>
    string? Notes
);
