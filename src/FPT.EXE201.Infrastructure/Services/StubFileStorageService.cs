using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

public class StubFileStorageService : IFileStorageService
{
    private const string UploadsDirectory = "uploads";

    public Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        // Ensure uploads directory exists
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), UploadsDirectory);
        Directory.CreateDirectory(uploadsDir);

        // Generate unique object key
        var extension = Path.GetExtension(fileName);
        var objectKey = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), objectKey.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Save file
        using (var fileStreamDest = new FileStream(filePath, FileMode.Create))
        {
            fileStream.CopyTo(fileStreamDest);
        }

        var result = new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: $"/{objectKey}", 
            OriginalFileName: fileName,
            MimeType: contentType,
            FileSizeBytes: sizeBytes,
            ChecksumSha256: null
        );

        return Task.FromResult(result);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), objectKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), objectKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string objectKey)
    {
        return $"/{objectKey}";
    }
}
