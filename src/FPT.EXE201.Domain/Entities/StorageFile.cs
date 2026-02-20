using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Bản ghi lưu trữ file vật lý (local filesystem hoặc S3/Azure).
/// Dùng chung cho mọi module cần upload file: medical documents,
/// profile photos, chat attachments...
/// 
/// Mỗi file upload → 1 record ở đây, objectKey xác định vị trí file.
/// </summary>
public class StorageFile : BaseEntity
{
    /// <summary>
    /// ID của user đã upload file này.
    /// Nullable vì system-generated files (thumbnails) không có owner.
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// Nhà cung cấp lưu trữ: "local" (dev), "s3" (production), "azure".
    /// Default "local" cho development.
    /// </summary>
    public string StorageProvider { get; set; } = "local";

    /// <summary>
    /// Tên bucket/container (S3/Azure). Null cho local storage.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// Đường dẫn file trong storage. Ví dụ: "2026/02/11/{guid}.jpg".
    /// Unique identifier cho file vật lý.
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// URL công khai để download file. Ví dụ: "/uploads/2026/02/11/{guid}.jpg".
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// Tên file gốc user đã upload. Ví dụ: "phieu-kham-28-tuan.jpg".
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// MIME type của file. Ví dụ: "image/jpeg", "application/pdf".
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file tính bằng bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// SHA-256 checksum để verify tính toàn vẹn file.
    /// </summary>
    public byte[]? ChecksumSha256 { get; set; }

    /// <summary>
    /// Thời điểm file được upload thành công.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>User đã upload file này.</summary>
    public User? Owner { get; set; }
}
