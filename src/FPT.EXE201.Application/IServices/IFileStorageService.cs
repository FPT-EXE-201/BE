namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Abstraction cho file storage.
/// Week 4: StubFileStorageService (metadata only, placeholder URL).
/// Week 5: SupabaseStorageService (upload thật lên Supabase Storage).
/// ⚠️ Interface ở Application, Implementation ở Infrastructure.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Upload file vào storage, trả về thông tin file đã lưu.</summary>
    Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Download file từ storage (dùng cho OCR pipeline — Week 5).</summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Xóa file khỏi storage.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort delete using the public URL returned from <see cref="UploadAsync"/> (stub or Supabase).
    /// No-op if URL format is unknown.
    /// </summary>
    Task DeleteByPublicUrlIfKnownAsync(string? publicUrl, CancellationToken cancellationToken = default);

    /// <summary>Tạo URL công khai cho file.</summary>
    string GetPublicUrl(string objectKey);
}

/// <summary>Kết quả sau khi upload file thành công.</summary>
public record StorageFileResult(
    string ObjectKey,
    string PublicUrl,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    byte[]? ChecksumSha256
);
