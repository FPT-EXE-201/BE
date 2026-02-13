using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Kết quả một xét nghiệm trong thai kỳ.
/// 
/// Có thể gắn vào 1 buổi khám (VisitId) hoặc độc lập (VisitId = null).
/// Ví dụ: User tự đi xét nghiệm máu ở phòng lab không qua buổi khám.
/// 
/// Cả 10 loại xét nghiệm đều dùng chung flow:
///   - Chụp ảnh kết quả → upload Supabase → lưu URLs vào ImageUrlsJson
///   - Ghi chú tuỳ chọn (Notes) cho quick annotation
/// </summary>
public class PrenatalTest : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ mà xét nghiệm này thuộc về.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>
    /// FK → PrenatalVisit. Buổi khám mà xét nghiệm này được thực hiện.
    /// Nullable: xét nghiệm có thể không gắn với buổi khám nào.
    /// </summary>
    public Guid? VisitId { get; set; }

    /// <summary>FK → RefTestType. Loại xét nghiệm (từ danh mục master).</summary>
    public Guid TestTypeId { get; set; }

    /// <summary>Ngày thực hiện xét nghiệm.</summary>
    public DateOnly TestDate { get; set; }

    /// <summary>
    /// JSON array chứa URLs ảnh kết quả xét nghiệm (Supabase storage).
    /// Ví dụ: ["https://xxx.supabase.co/storage/.../img1.jpg", "..."]
    /// </summary>
    public string? ImageUrlsJson { get; set; }

    /// <summary>
    /// Ghi chú tuỳ chọn. User hoặc bác sĩ có thể ghi nhanh.
    /// Ví dụ: "Bác sĩ nói chỉ số bình thường", "Cần tái khám sau 2 tuần"
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Có bất thường hay không.
    /// true = kết quả ngoài giới hạn bình thường, cần theo dõi.
    /// Có thể do user tự đánh dấu hoặc bác sĩ xác nhận.
    /// </summary>
    public bool IsAbnormalResult { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public PrenatalVisit? Visit { get; set; }
    public RefTestType TestType { get; set; } = null!;
}
