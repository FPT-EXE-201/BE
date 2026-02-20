using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Request body khi user confirm extracted data.
/// Chứa dữ liệu user đã review + chỉnh sửa.
/// Gửi từ Flutter sau khi user xem ExtractionReviewDto và edit.
/// </summary>
public record ConfirmExtractionDto(
    /// <summary>
    /// Loại document (bắt buộc nếu ban đầu chưa chọn).
    /// Xác định strategy auto-fill: PRENATAL_CHECKUP → Visit, BLOOD_TEST → Test.
    /// </summary>
    Guid DocumentTypeId,

    /// <summary>Ngày khám/xét nghiệm (user có thể chỉnh lại từ extracted date).</summary>
    DateOnly EventDate,

    /// <summary>Gắn vào Visit có sẵn (nullable). Nếu null → tạo Visit mới (cho PRENATAL_CHECKUP).</summary>
    Guid? ExistingVisitId,

    /// <summary>
    /// VitalsJson đã chỉnh sửa (cho PRENATAL_CHECKUP).
    /// Null nếu document không phải checkup.
    /// </summary>
    VitalsJsonDto? Vitals,

    /// <summary>Tên cơ sở y tế (user có thể chỉnh).</summary>
    string? Location,

    /// <summary>Ghi chú user muốn lưu vào Visit/Test.</summary>
    string? Notes
);
