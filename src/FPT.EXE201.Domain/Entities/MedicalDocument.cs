using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tài liệu y tế — kết nối file ảnh (StorageFile) với thai kỳ (Pregnancy).
/// 
/// Flow: User chụp phiếu khám → upload 1-N ảnh → tạo N StorageFile + N DocumentFile
///       → OCR chạy background trên từng file → ghép raw text
///       → Gemini AI parse → auto-create PrenatalVisit + PrenatalTest
///       → update VisitId để link document ↔ visit
/// 
/// Hỗ trợ multi-file: 1 document có thể chứa nhiều file (phiếu khám dài → chụp nhiều tấm).
/// Files được quản lý qua DocumentFile junction entity.
/// </summary>
public class MedicalDocument : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ mà tài liệu này thuộc về.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>
    /// FK → PrenatalVisit (Week 3). Buổi khám liên quan.
    /// Nullable: ban đầu NULL khi upload, được populate sau khi OCR/AI
    /// tự động tạo PrenatalVisit từ nội dung tài liệu.
    /// </summary>
    public Guid? VisitId { get; set; }

    /// <summary>FK → RefDocumentType. Loại tài liệu (từ danh mục master).</summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>Tiêu đề tài liệu. Ví dụ: "Khám thai tuần 28".</summary>
    public string? Title { get; set; }

    /// <summary>Ngày của tài liệu (ngày khám, ngày xét nghiệm). Dùng DateOnly vì chỉ cần ngày, khớp DB column DATE.</summary>
    public DateOnly? DocumentDate { get; set; }

    /// <summary>Thời điểm user chụp/upload tài liệu vào app.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>
    /// Nguồn gốc: Upload (user tự chụp), Share (từ bác sĩ), Import.
    /// </summary>
    public DocumentSource Source { get; set; }

    /// <summary>Ghi chú tự do của user.</summary>
    public string? Notes { get; set; }

    /// <summary>Đánh dấu yêu thích / quan trọng. Thay thế hệ thống Tag phức tạp.</summary>
    public bool IsFavorite { get; set; }
    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Thai kỳ sở hữu tài liệu này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;

    /// <summary>Buổi khám liên quan (nullable, populated bởi OCR/AI).</summary>
    public PrenatalVisit? Visit { get; set; }

    /// <summary>Loại tài liệu từ danh mục master.</summary>
    public RefDocumentType? DocumentType { get; set; }

    /// <summary>
    /// Danh sách files đã upload (1-N). Ordered by SortOrder.
    /// Hỗ trợ multi-file: phiếu khám dài → chụp nhiều tấm.
    /// </summary>
    public ICollection<DocumentFile> Files { get; set; } = new List<DocumentFile>();

    /// <summary>Danh sách kết quả OCR (có thể chạy lại nhiều lần).</summary>
    public ICollection<OcrResult> OcrResults { get; set; } = new List<OcrResult>();
}
