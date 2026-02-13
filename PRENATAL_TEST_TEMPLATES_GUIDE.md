# Prenatal Test — Image Upload Architecture Guide

> **Scope**: PrenatalTest — server-side image upload + storage.  
> **Status**: ✅ IMPLEMENTED (Stub). `StubFileStorageService` → sẽ thay bằng `SupabaseStorageService`.

---

## 1. Upload Flow

**Flutter gửi file ảnh → Backend nhận `IFormFile` → Backend upload lên Supabase → Backend lưu URL vào DB.**

```
┌──────────┐     multipart/form-data       ┌──────────────┐     Upload Stream     ┌─────────────────┐
│  Flutter  │ ─────────────────────────────▶│   Backend    │ ───────────────────▶  │ IFileStorage    │
│   App     │   images[] + metadata        │  Controller  │    UploadAsync()     │ Service         │
└──────────┘                               └──────┬───────┘                      └────────┬────────┘
                                                  │                                       │
                                                  │  StorageFileResult                     │
                                                  │  { PublicUrl, ObjectKey }              │
                                                  │◀──────────────────────────────────────│
                                                  │
                                           ┌──────▼───────┐
                                           │   Service    │  Serialize URLs → ImageUrlsJson
                                           │  (save DB)   │
                                           └──────────────┘
```

**Hiện tại**: `StubFileStorageService` chỉ tạo placeholder URL, không upload thật.  
**Sau này**: Thay bằng `SupabaseStorageService` — upload file thật lên Supabase Storage.

---

## 2. 10 Test Types (Seeded)

| # | Code                  | Category | Ví dụ                              |
|---|-----------------------|----------|-------------------------------------|
| 1 | BIOCHEMISTRY          | LAB      | Hoá sinh máu                       |
| 2 | ULTRASOUND            | IMAGING  | Siêu âm thai                       |
| 3 | BLOOD_PRESSURE        | OTHER    | Huyết áp                           |
| 4 | COMPLETE_BLOOD_COUNT  | LAB      | Công thức máu toàn phần            |
| 5 | URINE_TEST            | LAB      | Tổng phân tích nước tiểu           |
| 6 | HEPATITIS_B           | LAB      | Sàng lọc viêm gan B               |
| 7 | HIV_SCREEN            | LAB      | Sàng lọc HIV                      |
| 8 | TSH                   | LAB      | Hormone tuyến giáp                 |
| 9 | NT_SCAN               | IMAGING  | Đo độ mờ da gáy                   |
| 10| OGTT                  | LAB      | Nghiệm pháp dung nạp glucose      |

---

## 3. Architecture Layers

### Interface (Application Layer)
```csharp
// IServices/IFileStorageService.cs
public interface IFileStorageService
{
    Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
    string GetPublicUrl(string objectKey);
}

public record StorageFileResult(
    string ObjectKey, string PublicUrl,
    string OriginalFileName, string MimeType, long FileSizeBytes);
```

### FileUploadItem (Application Layer — tách IFormFile)
```csharp
// IServices/IPrenatalTestService.cs
public record FileUploadItem(Stream Stream, string FileName, string ContentType, long Length);
```

### StubFileStorageService (Infrastructure Layer)
```csharp
// Services/StubFileStorageService.cs
public class StubFileStorageService : IFileStorageService
{
    public Task<StorageFileResult> UploadAsync(...) 
    {
        var objectKey = $"prenatal-tests/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{ext}";
        return Task.FromResult(new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: $"https://placeholder.storage/{objectKey}", ...));
    }
}
```

---

## 4. Domain Entity

```csharp
public class PrenatalTest : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public Guid? VisitId { get; set; }           // Nullable
    public Guid TestTypeId { get; set; }
    public DateOnly TestDate { get; set; }
    public string? ImageUrlsJson { get; set; }   // JSON array URLs (Supabase)
    public string? Notes { get; set; }           // Ghi chú tuỳ chọn
    public bool IsAbnormalResult { get; set; }
}
```

---

## 5. Database Columns (prenatal_tests)

| Column             | Type          | Note                           |
|--------------------|---------------|--------------------------------|
| id                 | CHAR(36) PK   | Guid                           |
| pregnancy_id       | CHAR(36) FK   | → pregnancies.id               |
| visit_id           | CHAR(36) FK?  | → prenatal_visits.id (nullable)|
| test_type_id       | CHAR(36) FK   | → ref_test_types.id            |
| test_date          | DATE          |                                |
| image_urls_json    | JSON          | Serialized list URLs           |
| notes              | TEXT          | Ghi chú tuỳ chọn              |
| is_abnormal_result | TINYINT(1)    |                                |
| created_at         | DATETIME(6)   |                                |
| updated_at         | DATETIME(6)   |                                |
| deleted_at         | DATETIME(6)?  |                                |

---

## 6. DTOs

### CreatePrenatalTestDto (metadata only — file qua IFormFile)
```csharp
public record CreatePrenatalTestDto(
    Guid TestTypeId,
    Guid? VisitId,
    DateOnly TestDate,
    string? Notes,
    bool IsAbnormalResult = false
);
```

### UpdatePrenatalTestDto
```csharp
public record UpdatePrenatalTestDto(
    List<string>? ExistingImageUrls,  // URLs ảnh cũ muốn giữ lại
    string? Notes,
    bool IsAbnormalResult
);
```

### PrenatalTestDto (Response)
```csharp
public record PrenatalTestDto(
    Guid Id, Guid PregnancyId, Guid? VisitId,
    Guid TestTypeId, string TestTypeCode, string TestTypeDisplayName,
    DateOnly TestDate,
    List<string>? ImageUrls,
    string? Notes,
    bool IsAbnormalResult,
    DateTime CreatedAt
);
```

---

## 7. Controller — multipart/form-data

```csharp
[HttpPost("api/pregnancies/{pregnancyId:guid}/tests")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Create(
    Guid pregnancyId,
    [FromForm] CreatePrenatalTestDto dto,
    [FromForm] List<IFormFile>? images,    // File ảnh từ Flutter
    [FromQuery] string lang = "vi", CancellationToken ct = default)
{
    var uploadItems = MapToUploadItems(images);
    var result = await _testService.CreateAsync(pregnancyId, userId, dto, uploadItems, lang, ct);
    return Created(result, "Test created successfully");
}

// Convert IFormFile → FileUploadItem (tách ASP.NET khỏi Application layer)
private static List<FileUploadItem>? MapToUploadItems(List<IFormFile>? files)
{
    if (files == null || files.Count == 0) return null;
    return files.Select(f => new FileUploadItem(
        f.OpenReadStream(), f.FileName, f.ContentType, f.Length
    )).ToList();
}
```

---

## 8. Service — Upload + Save

```csharp
// PrenatalTestService.CreateAsync()
var imageUrls = await UploadImagesAsync(images, userId, ct);
test.ImageUrlsJson = SerializeImageUrls(imageUrls);

// PrenatalTestService.UpdateAsync()
var newImageUrls = await UploadImagesAsync(newImages, userId, ct);
var finalUrls = new List<string>();
if (dto.ExistingImageUrls != null) finalUrls.AddRange(dto.ExistingImageUrls);
if (newImageUrls != null) finalUrls.AddRange(newImageUrls);
test.ImageUrlsJson = SerializeImageUrls(finalUrls.Count > 0 ? finalUrls : null);

// Helper
private async Task<List<string>?> UploadImagesAsync(List<FileUploadItem>? images, Guid userId, CancellationToken ct)
{
    if (images == null || images.Count == 0) return null;
    var urls = new List<string>();
    foreach (var img in images)
    {
        var result = await _fileStorageService.UploadAsync(
            img.Stream, img.FileName, img.ContentType, img.Length, userId, ct);
        urls.Add(result.PublicUrl);
    }
    return urls;
}
```

---

## 9. API Examples (multipart/form-data)

### Create Test (cURL)
```bash
curl -X POST "https://api.example.com/api/pregnancies/{id}/tests?lang=vi" \
  -H "Authorization: Bearer {jwt}" \
  -F "testTypeId=b0000001-..." \
  -F "testDate=2025-01-15" \
  -F "notes=Bác sĩ nói kết quả bình thường" \
  -F "isAbnormalResult=false" \
  -F "images=@/path/to/photo1.jpg" \
  -F "images=@/path/to/photo2.jpg"
```

### Update Test (cURL)
```bash
curl -X PUT "https://api.example.com/api/tests/{id}?lang=vi" \
  -H "Authorization: Bearer {jwt}" \
  -F "existingImageUrls=https://supabase.co/.../old1.jpg" \
  -F "notes=Cập nhật: cần tái khám sau 2 tuần" \
  -F "isAbnormalResult=true" \
  -F "newImages=@/path/to/new_photo.jpg"
```

### Flutter (Dart — multipart)
```dart
var request = http.MultipartRequest('POST', Uri.parse('$baseUrl/api/pregnancies/$id/tests'));
request.headers['Authorization'] = 'Bearer $token';
request.fields['testTypeId'] = testTypeId;
request.fields['testDate'] = '2025-01-15';
request.fields['notes'] = 'Kết quả bình thường';
for (var file in imageFiles) {
  request.files.add(await http.MultipartFile.fromPath('images', file.path));
}
var response = await request.send();
```

---

## 10. Files Changed

| File | Layer | Change |
|------|-------|--------|
| `Application/IServices/IFileStorageService.cs` | App | **NEW** — Interface + StorageFileResult record |
| `Application/IServices/IPrenatalTestService.cs` | App | Added `List<FileUploadItem>?` param + FileUploadItem record |
| `Application/DTOs/PrenatalTests/CreatePrenatalTestDto.cs` | App | Removed ImageUrls (file via IFormFile now) |
| `Application/DTOs/PrenatalTests/UpdatePrenatalTestDto.cs` | App | `ImageUrls` → `ExistingImageUrls` (URLs to keep) |
| `Application/Services/PrenatalTestService.cs` | App | Inject IFileStorageService, upload via UploadImagesAsync() |
| `Infrastructure/Services/StubFileStorageService.cs` | Infra | **NEW** — Stub implementation (placeholder URLs) |
| `Infrastructure/DependencyInjection.cs` | Infra | Register IFileStorageService → StubFileStorageService |
| `Api/Controllers/PrenatalTestsController.cs` | Api | `[FromBody]` → `[FromForm]` + `IFormFile` + `[Consumes("multipart/form-data")]` |
