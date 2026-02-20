namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Thông tin 1 file trong document (multi-file support).
/// </summary>
public class DocumentFileDto
{
    public Guid Id { get; set; }
    public Guid StorageFileId { get; set; }

    /// <summary>Tên file gốc. Ví dụ: "phieu-kham-trang1.jpg".</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>MIME type. Ví dụ: "image/jpeg".</summary>
    public string MimeType { get; set; } = null!;

    /// <summary>Kích thước file (bytes).</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>URL download file.</summary>
    public string? FileUrl { get; set; }

    /// <summary>Thứ tự file trong document (1, 2, 3...).</summary>
    public int SortOrder { get; set; }

    /// <summary>Nhãn trang (tùy chọn).</summary>
    public string? PageLabel { get; set; }
}
