using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Upload file lên Supabase Storage — thay thế StubFileStorageService (Week 4).
/// Sử dụng Supabase Storage REST API.
/// </summary>
public class SupabaseStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _bucketName = configuration["Supabase:Storage:BucketName"]
            ?? throw new InvalidOperationException("Supabase:Storage:BucketName is required.");
        _publicBaseUrl = configuration["Supabase:Storage:PublicBaseUrl"]
            ?? throw new InvalidOperationException("Supabase:Storage:PublicBaseUrl is required.");
    }

    public async Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedContentType = NormalizeContentType(contentType, fileName);

        // 1. Generate unique object key (same format as StubFileStorageService)
        var extension = Path.GetExtension(fileName);
        var objectKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        // 2. Calculate SHA-256 checksum
        byte[] checksum;
        using (var sha256 = SHA256.Create())
        {
            checksum = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            fileStream.Position = 0; // Reset for upload
        }

        // 3. Upload to Supabase Storage
        var uploadUrl = $"object/{_bucketName}/{objectKey}";
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(normalizedContentType);

        var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.UnsupportedMediaType
                || error.Contains("invalid_mime_type", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    $"Unsupported file type '{contentType}'. Please use JPEG, PNG, or WEBP images.");
            }

            throw new InvalidOperationException(
                $"Supabase upload failed ({response.StatusCode}): {error}");
        }

        // 4. Return result with public URL
        return new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: GetPublicUrl(objectKey),
            OriginalFileName: fileName,
            MimeType: normalizedContentType,
            FileSizeBytes: sizeBytes,
            ChecksumSha256: checksum
        );
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var downloadUrl = $"object/{_bucketName}/{objectKey}";
        var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new FileNotFoundException($"File not found in Supabase: {objectKey}");

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var deleteUrl = $"object/{_bucketName}/{objectKey}";
        var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        // Ignore 404 — file already deleted
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Supabase delete failed ({response.StatusCode}): {error}");
        }
    }

    public Task DeleteByPublicUrlIfKnownAsync(string? publicUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(publicUrl))
            return Task.CompletedTask;

        var prefix = $"{_publicBaseUrl.TrimEnd('/')}/{_bucketName}/";
        if (!publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var objectKey = publicUrl[prefix.Length..];
        return DeleteAsync(objectKey, cancellationToken);
    }

    public string GetPublicUrl(string objectKey)
    {
        return $"{_publicBaseUrl.TrimEnd('/')}/{_bucketName}/{objectKey}";
    }

    private static string NormalizeContentType(string? contentType, string fileName)
    {
        var normalized = (contentType ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized == "image/jpg")
            return "image/jpeg";

        if (!string.IsNullOrWhiteSpace(normalized) && normalized != "application/octet-stream")
            return normalized;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => string.IsNullOrWhiteSpace(normalized) ? "application/octet-stream" : normalized
        };
    }
}
