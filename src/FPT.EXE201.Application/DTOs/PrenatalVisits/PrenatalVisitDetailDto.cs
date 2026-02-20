using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

/// <summary>
/// Chi tiết 1 buổi khám, bao gồm danh sách xét nghiệm.
/// Dùng cho GET /api/visits/{id} — FE hiển thị trang chi tiết buổi khám.
/// </summary>
public record PrenatalVisitDetailDto(
    Guid Id,
    Guid PregnancyId,
    Guid? DoctorId,
    DateOnly VisitDate,
    string VisitType,
    string? Location,
    string? Notes,

    /// <summary>Dữ liệu phiếu khám thai (structured object).</summary>
    VitalsJsonDto? Vitals,

    /// <summary>Danh sách xét nghiệm trong buổi khám này, kèm tên loại xét nghiệm theo ngôn ngữ.</summary>
    List<PrenatalTestDto> Tests,

    /// <summary>ID các tài liệu y tế gắn với buổi khám.</summary>
    List<Guid> LinkedDocumentIds,

    /// <summary>Ảnh tài liệu gắn với buổi khám (publicUrl từ MedicalDocument.Files).</summary>
    List<string> LinkedDocumentImages,

    DateTime CreatedAt
);
