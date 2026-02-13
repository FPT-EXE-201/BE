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
    DateTime VisitDateTime,
    string VisitType,
    string? Location,
    string? Notes,

    /// <summary>Dữ liệu phiếu khám thai (structured object).</summary>
    VitalsJsonDto? Vitals,

    /// <summary>Danh sách xét nghiệm trong buổi khám này, kèm tên loại xét nghiệm theo ngôn ngữ.</summary>
    List<PrenatalTestDto> Tests,

    DateTime CreatedAt
);
