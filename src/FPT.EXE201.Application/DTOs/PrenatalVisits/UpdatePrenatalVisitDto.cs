using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record UpdatePrenatalVisitDto(
    DateTime VisitDateTime,
    VisitType VisitType,
    Guid? DoctorId,
    string? Location,
    string? Notes,

    /// <summary>
    /// Dữ liệu phiếu khám thai theo chuẩn MS: 51/BV2 Bộ Y tế.
    /// </summary>
    VitalsJsonDto? Vitals
);
