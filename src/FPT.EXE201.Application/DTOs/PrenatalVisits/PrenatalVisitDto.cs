using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record PrenatalVisitDto(
    Guid Id,
    Guid PregnancyId,
    Guid? DoctorId,
    DateOnly VisitDate,
    string VisitType,
    string? Location,
    string? Notes,

    /// <summary>Dữ liệu phiếu khám thai (structured object, không phải raw JSON string).</summary>
    VitalsJsonDto? Vitals,

    /// <summary>Số xét nghiệm đã thực hiện trong buổi khám này.</summary>
    int TestCount,

    /// <summary>ID các tài liệu y tế gắn với buổi khám.</summary>
    List<Guid> LinkedDocumentIds,

    /// <summary>Ảnh tài liệu gắn với buổi khám (publicUrl từ MedicalDocument.Files).</summary>
    List<string> LinkedDocumentImages,

    DateTime CreatedAt
);
