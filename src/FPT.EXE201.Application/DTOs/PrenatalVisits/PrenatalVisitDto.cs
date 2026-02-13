using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record PrenatalVisitDto(
    Guid Id,
    Guid PregnancyId,
    Guid? DoctorId,
    DateTime VisitDateTime,
    string VisitType,
    string? Location,
    string? Notes,

    /// <summary>Dữ liệu phiếu khám thai (structured object, không phải raw JSON string).</summary>
    VitalsJsonDto? Vitals,

    /// <summary>Số xét nghiệm đã thực hiện trong buổi khám này.</summary>
    int TestCount,

    DateTime CreatedAt
);
