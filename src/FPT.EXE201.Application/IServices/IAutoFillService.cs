using FPT.EXE201.Application.DTOs.AutoFill;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý "Review & Confirm" flow:
/// 1. ReviewAsync: parse StructuredJson → ExtractionReviewDto (cho FE render form)
/// 2. ConfirmAsync: nhận user-edited data → auto-create PrenatalVisit/Test
/// </summary>
public interface IAutoFillService
{
    /// <summary>
    /// Lấy dữ liệu AI đã extract, parse thành review form.
    /// Chỉ cho phép khi Status = Succeeded.
    /// </summary>
    Task<ExtractionReviewDto> ReviewAsync(
        Guid ocrResultId, Guid currentUserId,
        string langCode = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// User confirm extracted data → auto-create entities dựa vào document type.
    /// Chỉ cho phép khi Status = Succeeded (chưa confirm trước đó).
    /// </summary>
    Task<AutoFillResultDto> ConfirmAsync(
        Guid ocrResultId, ConfirmExtractionDto dto,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
