using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Stub implementation — chỉ tạo metadata + placeholder URL, không upload file thật.
/// Week 5 sẽ thay bằng SupabaseStorageService (upload thật lên Supabase Storage).
/// </summary>
public class StubFileStorageService : IFileStorageService
{
    public Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        // Generate unique object key (same format sẽ dùng cho Supabase)
        var extension = Path.GetExtension(fileName);
        var objectKey = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        var result = new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: $"https://placeholder.storage/{objectKey}",
            OriginalFileName: fileName,
            MimeType: contentType,
            FileSizeBytes: sizeBytes,
            ChecksumSha256: null
        );

        return Task.FromResult(result);
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        // Stub: không có file vật lý để download — Week 5 sẽ dùng SupabaseStorageService
        throw new NotSupportedException(
            "StubFileStorageService does not support download. Use SupabaseStorageService (Week 5).");
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        // Stub: không có file vật lý để xóa
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string objectKey)
    {
        return $"https://placeholder.storage/{objectKey}";
    }
}
