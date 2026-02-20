using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Junction entity giữa MedicalDocument và StorageFile.
/// Hỗ trợ nhiều file per document (ví dụ: phiếu khám dài → chụp 2-3 tấm ảnh).
/// 
/// Flow: User chụp phiếu khám → upload 1-N ảnh → tạo N StorageFile + N DocumentFile
///       → OCR chạy trên từng file → ghép raw text → AI extract structured data.
/// </summary>
public class DocumentFile : BaseEntity
{
    /// <summary>FK → MedicalDocument. Tài liệu chứa file này.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>FK → StorageFile. File vật lý đã upload.</summary>
    public Guid StorageFileId { get; set; }

    /// <summary>
    /// Thứ tự file trong document (1, 2, 3...).
    /// Dùng để xác định thứ tự ghép OCR text.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Nhãn trang (tùy chọn). Ví dụ: "Trang 1", "Mặt trước", "Mặt sau".
    /// </summary>
    public string? PageLabel { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Tài liệu sở hữu file này.</summary>
    public MedicalDocument Document { get; set; } = null!;

    /// <summary>File vật lý đã upload.</summary>
    public StorageFile StorageFile { get; set; } = null!;
}
