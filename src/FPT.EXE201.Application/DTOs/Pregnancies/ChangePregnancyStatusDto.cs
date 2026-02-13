using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Request body khi thay đổi trạng thái thai kỳ.
/// Chỉ cho phép: Active → Delivered / Ended / Miscarriage.
/// Nếu status = Delivered, có thể gửi kèm ActualDeliveryDate + DeliveryMethod.
/// </summary>
public record ChangePregnancyStatusDto(
    PregnancyStatus Status,

    /// <summary>Ngày sinh thực tế. Bắt buộc khi status = Delivered.</summary>
    DateOnly? ActualDeliveryDate = null,

    /// <summary>Phương pháp sinh. Optional khi status = Delivered.</summary>
    DeliveryMethod? DeliveryMethod = null
);
