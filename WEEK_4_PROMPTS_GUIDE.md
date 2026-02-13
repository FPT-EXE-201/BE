# WEEK 4 PROMPTS GUIDE — File Storage + Medical Documents (FINAL v2)

> ⚠️ **Database Convention**: Project sử dụng **CHAR(36)** để lưu Guid, KHÔNG dùng BINARY(16).  
> ⚠️ **Enum Convention**: Dùng `JsonStringEnumConverter` — enum serialize thành string.  
> ⚠️ **Naming Convention**: Property names phải self-documenting + có XML comment giải thích.  
> ⚠️ **Exception Handling**: Services throw exceptions (`NotFoundException`, `BadRequestException`, `ConflictException`...), `GlobalExceptionFilter` xử lý thành `ApiResponse`.  
> ⚠️ **RBAC**: Dùng `[RequirePermission("permission.code")]` trong Controller.  
> ⚠️ **Soft Delete**: Dùng `deleted_at` timestamp + global query filter trong `AppDbContext.OnModelCreating`. KHÔNG hard delete.  
> ⚠️ **Seed Data**: Dùng anonymous type + fixed DateTime, KHÔNG dùng entity instance.  
> ⚠️ **LangCode**: Lowercase match Week 1 (`"vi"`, `"en"`).  
> ⚠️ **Column Mapping**: C# property dùng tên rõ nghĩa (`OriginalFileName`), DB column dùng tên ngắn (`original_name`). Mapping qua `.HasColumnName()`.  
> ⚠️ **Controller**: Kế thừa `BaseApiController`, dùng `Success()`, `Created()`, `GetCurrentUserId()`. KHÔNG tự viết `ApiResponse`.  
> ⚠️ **Repository**: Kế thừa `GenericRepository<T>`, lazy init trong `UnitOfWork` qua `??=` pattern.  
> ⚠️ **DbContext**: Dùng `AppDbContext`, KHÔNG phải `ApplicationDbContext`. Configurations auto-apply qua `ApplyConfigurationsFromAssembly`.  

---

## 📋 CONTEXT

### Week 4 Overview

**Mục tiêu**: File Storage + Medical Documents — upload ảnh phiếu khám, OCR stub, tagging.

**⚠️ Week 3 → Week 4 Integration**:
- `medical_documents.pregnancy_id` → FK to Week 3's `pregnancies.id`
- `medical_documents.visit_id` → FK to Week 3's `prenatal_visits.id` (nullable, populated sau khi OCR/AI parse)
- Flow: Upload ảnh → OCR text → Gemini AI → auto-create PrenatalVisit + PrenatalTest (Week 3) → update `medical_documents.visit_id`

**Database Tables** (7 tables):
1. `storage_files` — Lưu trữ file vật lý (local/S3)
2. `ref_document_types` — Danh mục loại tài liệu (master)
3. `ref_document_type_translations` — Tên loại tài liệu đa ngôn ngữ
4. `medical_documents` — Metadata tài liệu + FK trực tiếp → storage_files
5. `ocr_results` — Kết quả OCR/AI processing
6. `tags` — Tag tự định nghĩa bởi user
7. `medical_document_tags` — Many-to-many join table

**Property Naming Rule**:

```
C# Property (rõ nghĩa)          │ DB Column (ngắn gọn)
─────────────────────────────────┼──────────────────────
OwnerUserId                      │ owner_user_id
StorageProvider                  │ provider
BucketName                       │ bucket
ObjectKey                        │ object_key
PublicUrl                        │ public_url
OriginalFileName                 │ original_name
MimeType                         │ mime_type
FileSizeBytes                    │ size_bytes
ChecksumSha256                   │ checksum_sha256
UploadedAt                       │ uploaded_at
LanguageCode                     │ lang_code
DisplayName                      │ name
StorageFileId                    │ storage_file_id
DocumentDate                     │ document_date
CapturedAt                       │ captured_at
MetadataJson                     │ metadata_json
DocumentId                       │ document_id
OcrRunNumber                     │ ocr_run_no
OcrEngine                        │ engine
LanguageHint                     │ language_hint
RawText                          │ raw_text
StructuredJson                   │ structured_json
ConfidenceScore                  │ confidence
ErrorMessage                     │ error_message
```

**API Endpoints**:
```
# Documents (upload + CRUD)
POST   /api/pregnancies/{id}/documents           → Upload ảnh + tạo document (multipart/form-data)
GET    /api/pregnancies/{id}/documents           → List documents của thai kỳ
GET    /api/documents/{id}                       → Chi tiết 1 document
PUT    /api/documents/{id}                       → Update metadata (title, notes, visit link)
DELETE /api/documents/{id}                       → Soft delete document

# Timeline
GET    /api/pregnancies/{id}/timeline            → Xem dòng thời gian (documents + visits)

# Tags
POST   /api/tags                                 → Tạo tag mới
GET    /api/tags                                 → List tags của user
DELETE /api/tags/{id}                            → Soft delete tag
POST   /api/documents/{docId}/tags/{tagId}       → Gắn tag vào document
DELETE /api/documents/{docId}/tags/{tagId}       → Gỡ tag khỏi document

# OCR
POST   /api/documents/{id}/ocr/rerun            → Chạy lại OCR
GET    /api/ocr/{id}/status                      → Kiểm tra trạng thái OCR

# Reference Data (public, no auth required)
GET    /api/ref/document-types?lang=vi           → Danh mục loại tài liệu
```

**Business Rules**:
- ⚠️ **File Size Limit**: 10MB per file (nhưng tính toán hợp lý vì user có thể upload nhiều ảnh vì phiếu khám có thể dài)
- ⚠️ **Allowed MIME Types**: image/jpeg, image/png, application/pdf
- ⚠️ **Unique Tags**: Tag name unique per user (`user_id + name` unique)
- ⚠️ **Storage Strategy**: Week 4 dùng `StubFileStorageService` (chỉ lưu metadata + placeholder URL). Week 5 sẽ thay bằng `SupabaseStorageService` (để upload file thật lên Supabase Storage)
- ⚠️ **Ownership**: User chỉ được access own documents (via pregnancy ownership)
- ⚠️ **OCR Status Flow**: `Pending → Processing → Succeeded / Failed` (stub trong Week 4, full pipeline trong Week 5)
- ⚠️ **Visit Link**: `medical_documents.visit_id` ban đầu NULL, được populate sau khi OCR/AI tạo PrenatalVisit

**Existing Codebase Patterns** (PHẢI follow):
- `BaseEntity` → `{ Id, CreatedAt, UpdatedAt, DeletedAt, IsDeleted }` (IsDeleted = computed, ignored by EF)
- `GenericRepository<T>` → constraint `where T : BaseEntity`. Methods: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `SoftDeleteAsync`...
- `AppDbContext` → global soft delete filter + auto timestamps in `SaveChangesAsync`
- `UnitOfWork` → lazy `??=` pattern
- `BaseApiController` → `Success()`, `Created()`, `GetCurrentUserId()`
- `GlobalExceptionFilter` → catches `NotFoundException`, `BadRequestException`, `ConflictException`, `ForbiddenException`

**Development Workflow**:
1. Prompt 1: Domain entities (StorageFile, RefDocumentType, RefDocumentTypeTranslation)
2. Prompt 2: Domain entities (MedicalDocument, OcrResult, Tag, MedicalDocumentTag, Enums)
3. Prompt 3: EF Core Configurations (ALL 7 entities)
4. Prompt 4: Migration + Seed Data (8 document types)
5. Prompt 5: DTOs + FluentValidation
6. Prompt 6: Repository Interfaces + Service Interfaces
7. Prompt 7: Repository Implementations + UnitOfWork update
8. Prompt 8: StubFileStorageService (Infrastructure) + OcrService (stub)
9. Prompt 9: MedicalDocumentService + TagService
10. Prompt 10: Controllers + AutoMapper + Permissions + Ref Data endpoint

---

## 🎯 PROMPT 1/10 — Domain Entities: StorageFile + RefDocumentType

**Nhiệm vụ**: Tạo 3 entities cho File Storage và Document Type reference.

**Reference SQL**:
```sql
CREATE TABLE storage_files (
    id CHAR(36) PRIMARY KEY,
    owner_user_id CHAR(36) NULL,
    provider VARCHAR(20) NOT NULL DEFAULT 'local',
    bucket VARCHAR(100) NULL,
    object_key VARCHAR(500) NOT NULL,
    public_url VARCHAR(1000) NULL,
    original_name VARCHAR(255) NULL,
    mime_type VARCHAR(100) NOT NULL,
    size_bytes BIGINT NOT NULL,
    checksum_sha256 BINARY(32) NULL,
    uploaded_at DATETIME NOT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (owner_user_id) REFERENCES users(id),
    INDEX idx_storage_files_owner (owner_user_id),
    INDEX idx_storage_files_uploaded (uploaded_at)
);

CREATE TABLE ref_document_types (
    id CHAR(36) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    INDEX idx_ref_document_types_code (code)
);

CREATE TABLE ref_document_type_translations (
    document_type_id CHAR(36) NOT NULL,
    lang_code VARCHAR(5) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT NULL,
    PRIMARY KEY (document_type_id, lang_code),
    FOREIGN KEY (document_type_id) REFERENCES ref_document_types(id) ON DELETE CASCADE,
    FOREIGN KEY (lang_code) REFERENCES languages(code)
);
```

**Code**:

```csharp
// File: FPT.EXE201.Domain/Entities/StorageFile.cs
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

// File: FPT.EXE201.Domain/Entities/RefDocumentType.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục loại tài liệu y tế (reference/master data).
/// Seed sẵn bởi hệ thống. User chọn từ danh sách khi upload.
/// Cũng dùng làm vocabulary cho Gemini AI classification.
/// 
/// Ví dụ: PRENATAL_CHECKUP, ULTRASOUND, BLOOD_TEST, PRESCRIPTION...
/// </summary>
public class RefDocumentType : BaseEntity
{
    /// <summary>
    /// Mã định danh duy nhất. Convention: UPPER_SNAKE_CASE.
    /// Ví dụ: "PRENATAL_CHECKUP", "ULTRASOUND".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Còn sử dụng hay đã ngưng.
    /// false = ẩn khỏi dropdown nhưng giữ data cũ.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    /// <summary>Tên hiển thị theo từng ngôn ngữ (VI, EN...).</summary>
    public ICollection<RefDocumentTypeTranslation> Translations { get; set; }
        = new List<RefDocumentTypeTranslation>();

    /// <summary>Các tài liệu thuộc loại này.</summary>
    public ICollection<MedicalDocument> Documents { get; set; }
        = new List<MedicalDocument>();
}

// File: FPT.EXE201.Domain/Entities/RefDocumentTypeTranslation.cs
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho loại tài liệu.
/// Composite key: (DocumentTypeId + LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity — entity này dùng composite primary key.
/// </summary>
public class RefDocumentTypeTranslation
{
    /// <summary>FK → RefDocumentType.Id</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị cho user. Ví dụ: "Phiếu khám thai".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional).</summary>
    public string? Description { get; set; }

    // Navigation
    public RefDocumentType DocumentType { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
```

**✅ Checkpoint**: Build thành công (MedicalDocument warning vì chưa tạo — OK, sẽ tạo ở Prompt 2).

---

## 🎯 PROMPT 2/10 — Domain Entities: MedicalDocument + OcrResult + Tag + Enums

**Nhiệm vụ**: Tạo entities chính, join table, và 2 enums.

**⚠️ Simplified Design**: KHÔNG có `DocumentFile` entity. `MedicalDocument` trỏ trực tiếp tới `StorageFile` qua `StorageFileId`. Upload 1 ảnh = 2 records (StorageFile + MedicalDocument). Đơn giản hơn, đủ dùng cho EXE-201.

**Reference SQL**:
```sql
CREATE TABLE medical_documents (
    id CHAR(36) PRIMARY KEY,
    pregnancy_id CHAR(36) NOT NULL,
    visit_id CHAR(36) NULL,
    document_type_id CHAR(36) NULL,
    storage_file_id CHAR(36) NOT NULL,
    title VARCHAR(200) NULL,
    document_date DATE NULL,
    captured_at DATETIME NOT NULL,
    source VARCHAR(20) NOT NULL,
    notes TEXT NULL,
    metadata_json TEXT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    FOREIGN KEY (visit_id) REFERENCES prenatal_visits(id) ON DELETE SET NULL,
    FOREIGN KEY (document_type_id) REFERENCES ref_document_types(id),
    FOREIGN KEY (storage_file_id) REFERENCES storage_files(id),
    INDEX idx_medical_documents_pregnancy (pregnancy_id),
    INDEX idx_medical_documents_visit (visit_id),
    INDEX idx_medical_documents_captured (captured_at)
);

CREATE TABLE ocr_results (
    id CHAR(36) PRIMARY KEY,
    document_id CHAR(36) NOT NULL,
    ocr_run_no INT NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL,
    engine VARCHAR(50) NULL,
    language_hint VARCHAR(10) NULL,
    raw_text TEXT NULL,
    structured_json TEXT NULL,
    confidence DECIMAL(5,2) NULL,
    error_message TEXT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (document_id) REFERENCES medical_documents(id) ON DELETE CASCADE,
    INDEX idx_ocr_results_document (document_id),
    INDEX idx_ocr_results_status (status)
);

CREATE TABLE tags (
    id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    name VARCHAR(50) NOT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uk_tags_user_name (user_id, name)
);

CREATE TABLE medical_document_tags (
    document_id CHAR(36) NOT NULL,
    tag_id CHAR(36) NOT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (document_id, tag_id),
    FOREIGN KEY (document_id) REFERENCES medical_documents(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);
```

**Code**:

```csharp
// File: FPT.EXE201.Domain/Enums/DocumentSource.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Nguồn gốc của tài liệu y tế.
/// </summary>
public enum DocumentSource
{
    /// <summary>User tự chụp/upload từ thiết bị</summary>
    Upload = 1,

    /// <summary>Được chia sẻ từ người khác (bác sĩ, người thân)</summary>
    Share = 2,

    /// <summary>Import từ hệ thống khác</summary>
    Import = 3
}

// File: FPT.EXE201.Domain/Enums/OcrStatus.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái xử lý OCR.
/// Flow: Pending → Processing → Succeeded / Failed
/// </summary>
public enum OcrStatus
{
    /// <summary>Đang chờ xử lý</summary>
    Pending = 0,

    /// <summary>Đang được OCR engine xử lý</summary>
    Processing = 1,

    /// <summary>OCR thành công, có kết quả</summary>
    Succeeded = 2,

    /// <summary>OCR thất bại</summary>
    Failed = 3
}
```

```csharp
// File: FPT.EXE201.Domain/Entities/MedicalDocument.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tài liệu y tế — kết nối file ảnh (StorageFile) với thai kỳ (Pregnancy).
/// 
/// Flow: User chụp phiếu khám → upload → tạo StorageFile + MedicalDocument
///       → OCR chạy background → kết quả lưu vào OcrResult
///       → Gemini AI parse → auto-create PrenatalVisit + PrenatalTest
///       → update VisitId để link document ↔ visit
/// 
/// ⚠️ Simplified: KHÔNG có DocumentFile trung gian.
///    MedicalDocument trỏ trực tiếp tới StorageFile qua StorageFileId.
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

    /// <summary>
    /// FK → StorageFile. File vật lý đã upload.
    /// Mỗi MedicalDocument gắn trực tiếp với 1 StorageFile.
    /// </summary>
    public Guid StorageFileId { get; set; }

    /// <summary>Tiêu đề tài liệu. Ví dụ: "Khám thai tuần 28".</summary>
    public string? Title { get; set; }

    /// <summary>Ngày của tài liệu (ngày khám, ngày xét nghiệm).</summary>
    public DateTime? DocumentDate { get; set; }

    /// <summary>Thời điểm user chụp/upload tài liệu vào app.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>
    /// Nguồn gốc: Upload (user tự chụp), Share (từ bác sĩ), Import.
    /// </summary>
    public DocumentSource Source { get; set; }

    /// <summary>Ghi chú tự do của user.</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// JSON metadata bổ sung (flexible schema).
    /// Ví dụ: {"hospital": "BV Từ Dũ", "floor": "3"}
    /// </summary>
    public string? MetadataJson { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Thai kỳ sở hữu tài liệu này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;

    /// <summary>Buổi khám liên quan (nullable, populated bởi OCR/AI).</summary>
    public PrenatalVisit? Visit { get; set; }

    /// <summary>Loại tài liệu từ danh mục master.</summary>
    public RefDocumentType? DocumentType { get; set; }

    /// <summary>File vật lý đã upload.</summary>
    public StorageFile StorageFile { get; set; } = null!;

    /// <summary>Danh sách kết quả OCR (có thể chạy lại nhiều lần).</summary>
    public ICollection<OcrResult> OcrResults { get; set; } = new List<OcrResult>();

    /// <summary>Tags gắn vào tài liệu.</summary>
    public ICollection<MedicalDocumentTag> Tags { get; set; } = new List<MedicalDocumentTag>();
}

// File: FPT.EXE201.Domain/Entities/OcrResult.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Kết quả một lần chạy OCR trên tài liệu y tế.
/// Mỗi document có thể chạy OCR nhiều lần (user retry khi kết quả sai).
/// 
/// Flow:
///   1. Upload ảnh → status = Pending
///   2. OCR engine xử lý → status = Processing
///   3. Thành công → status = Succeeded, lưu raw_text + structured_json
///      Thất bại → status = Failed, lưu error_message
///   4. structured_json chứa output từ Gemini AI (visit + test data parsed)
/// 
/// StructuredJson example:
/// {
///   "visit": {"visitDate": "2026-06-15", "doctorName": "BS Nguyễn Văn A"},
///   "tests": [{"testTypeCode": "BLOOD_GLUCOSE", "result": "95 mg/dL"}]
/// }
/// </summary>
public class OcrResult : BaseEntity
{
    /// <summary>FK → MedicalDocument. Tài liệu được chạy OCR.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Số lần chạy OCR (1, 2, 3...). Tăng mỗi lần user rerun.
    /// </summary>
    public int OcrRunNumber { get; set; }

    /// <summary>Trạng thái: Pending → Processing → Succeeded / Failed.</summary>
    public OcrStatus Status { get; set; }

    /// <summary>
    /// Tên OCR engine đã dùng. Ví dụ: "google-vision", "tesseract", "stub-v1".
    /// </summary>
    public string? OcrEngine { get; set; }

    /// <summary>
    /// Gợi ý ngôn ngữ cho OCR. Ví dụ: "vi" cho tiếng Việt.
    /// </summary>
    public string? LanguageHint { get; set; }

    /// <summary>
    /// Văn bản thô trích xuất từ ảnh (raw OCR output, chưa parse).
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// JSON có cấu trúc sau khi Gemini AI parse raw text.
    /// Chứa visit info + test results đã phân loại theo ref_test_types.
    /// </summary>
    public string? StructuredJson { get; set; }

    /// <summary>
    /// Độ tin cậy của kết quả OCR (0.00 - 100.00).
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu OCR thất bại.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    /// <summary>Tài liệu được chạy OCR.</summary>
    public MedicalDocument Document { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/Tag.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tag do user tự định nghĩa để phân loại tài liệu.
/// Mỗi user có danh sách tags riêng (unique: user_id + name).
/// Ví dụ: "Quan trọng", "BV Từ Dũ", "Tam cá nguyệt 3"
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>FK → User. User sở hữu tag này.</summary>
    public Guid UserId { get; set; }

    /// <summary>Tên tag. Unique per user. Tối đa 50 ký tự.</summary>
    public string Name { get; set; } = string.Empty;

    // Navigation
    /// <summary>User sở hữu tag.</summary>
    public User User { get; set; } = null!;

    /// <summary>Các document-tag assignments.</summary>
    public ICollection<MedicalDocumentTag> DocumentTags { get; set; }
        = new List<MedicalDocumentTag>();
}

// File: FPT.EXE201.Domain/Entities/MedicalDocumentTag.cs
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Join table: gắn tag vào tài liệu (many-to-many).
/// Composite key: (DocumentId + TagId).
/// ⚠️ KHÔNG kế thừa BaseEntity — join table dùng composite primary key.
/// </summary>
public class MedicalDocumentTag
{
    /// <summary>FK → MedicalDocument.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>FK → Tag.</summary>
    public Guid TagId { get; set; }

    /// <summary>Thời điểm gắn tag.</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation
    public MedicalDocument Document { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 3/10 — EF Core Configurations (ALL 7 Entities)

**Nhiệm vụ**: Map C# property names → DB column names (snake_case). Tạo 7 configuration files.

**⚠️ IMPORTANT**:
- `builder.Ignore(e => e.IsDeleted)` — computed property, KHÔNG map vào DB.
- Enum dùng `.HasConversion<string>()` cho consistency.
- Language FK dùng `.HasPrincipalKey(l => l.Code)` vì Language PK là `string Code`.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Configurations/StorageFileConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class StorageFileConfiguration : IEntityTypeConfiguration<StorageFile>
{
    public void Configure(EntityTypeBuilder<StorageFile> builder)
    {
        builder.ToTable("storage_files");

        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(s => s.OwnerUserId).HasColumnName("owner_user_id").HasColumnType("CHAR(36)");
        builder.Property(s => s.StorageProvider).IsRequired().HasColumnName("provider").HasMaxLength(20)
            .HasDefaultValue("local");
        builder.Property(s => s.BucketName).HasColumnName("bucket").HasMaxLength(100);
        builder.Property(s => s.ObjectKey).IsRequired().HasColumnName("object_key").HasMaxLength(500);
        builder.Property(s => s.PublicUrl).HasColumnName("public_url").HasMaxLength(1000);
        builder.Property(s => s.OriginalFileName).HasColumnName("original_name").HasMaxLength(255);
        builder.Property(s => s.MimeType).IsRequired().HasColumnName("mime_type").HasMaxLength(100);
        builder.Property(s => s.FileSizeBytes).IsRequired().HasColumnName("size_bytes");
        builder.Property(s => s.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("BINARY(32)");
        builder.Property(s => s.UploadedAt).IsRequired().HasColumnName("uploaded_at").HasColumnType("DATETIME");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(s => s.IsDeleted);

        builder.HasIndex(s => s.OwnerUserId).HasDatabaseName("idx_storage_files_owner");
        builder.HasIndex(s => s.UploadedAt).HasDatabaseName("idx_storage_files_uploaded");

        builder.HasOne(s => s.Owner)
            .WithMany().HasForeignKey(s => s.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefDocumentTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefDocumentTypeConfiguration : IEntityTypeConfiguration<RefDocumentType>
{
    public void Configure(EntityTypeBuilder<RefDocumentType> builder)
    {
        builder.ToTable("ref_document_types");

        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Code).IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("uk_ref_document_types_code");
        builder.Property(r => r.IsActive).IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(r => r.IsDeleted);

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.DocumentType).HasForeignKey(t => t.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefDocumentTypeTranslationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefDocumentTypeTranslationConfiguration
    : IEntityTypeConfiguration<RefDocumentTypeTranslation>
{
    public void Configure(EntityTypeBuilder<RefDocumentTypeTranslation> builder)
    {
        builder.ToTable("ref_document_type_translations");

        builder.HasKey(t => new { t.DocumentTypeId, t.LanguageCode });

        builder.Property(t => t.DocumentTypeId).HasColumnName("document_type_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("lang_code").HasMaxLength(5);
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        builder.HasOne(t => t.DocumentType)
            .WithMany(d => d.Translations).HasForeignKey(t => t.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/MedicalDocumentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.ToTable("medical_documents");

        builder.Property(m => m.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(m => m.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.VisitId).HasColumnName("visit_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.DocumentTypeId).HasColumnName("document_type_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.StorageFileId).IsRequired().HasColumnName("storage_file_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(m => m.DocumentDate).HasColumnName("document_date").HasColumnType("DATE");
        builder.Property(m => m.CapturedAt).IsRequired().HasColumnName("captured_at").HasColumnType("DATETIME");
        builder.Property(m => m.Source).IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(m => m.MetadataJson).HasColumnName("metadata_json").HasColumnType("TEXT");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(m => m.IsDeleted);

        builder.HasIndex(m => m.PregnancyId).HasDatabaseName("idx_medical_documents_pregnancy");
        builder.HasIndex(m => m.VisitId).HasDatabaseName("idx_medical_documents_visit");
        builder.HasIndex(m => m.CapturedAt).HasDatabaseName("idx_medical_documents_captured");

        // Relationships
        builder.HasOne(m => m.Pregnancy)
            .WithMany().HasForeignKey(m => m.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Visit)
            .WithMany().HasForeignKey(m => m.VisitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.DocumentType)
            .WithMany(d => d.Documents).HasForeignKey(m => m.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.StorageFile)
            .WithMany().HasForeignKey(m => m.StorageFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.OcrResults)
            .WithOne(o => o.Document).HasForeignKey(o => o.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/OcrResultConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class OcrResultConfiguration : IEntityTypeConfiguration<OcrResult>
{
    public void Configure(EntityTypeBuilder<OcrResult> builder)
    {
        builder.ToTable("ocr_results");

        builder.Property(o => o.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(o => o.DocumentId).IsRequired().HasColumnName("document_id").HasColumnType("CHAR(36)");
        builder.Property(o => o.OcrRunNumber).IsRequired().HasColumnName("ocr_run_no").HasDefaultValue(1);
        builder.Property(o => o.Status).IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.OcrEngine).HasColumnName("engine").HasMaxLength(50);
        builder.Property(o => o.LanguageHint).HasColumnName("language_hint").HasMaxLength(10);
        builder.Property(o => o.RawText).HasColumnName("raw_text").HasColumnType("TEXT");
        builder.Property(o => o.StructuredJson).HasColumnName("structured_json").HasColumnType("TEXT");
        builder.Property(o => o.ConfidenceScore).HasColumnName("confidence").HasColumnType("DECIMAL(5,2)");
        builder.Property(o => o.ErrorMessage).HasColumnName("error_message").HasColumnType("TEXT");

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(o => o.IsDeleted);

        builder.HasIndex(o => o.DocumentId).HasDatabaseName("idx_ocr_results_document");
        builder.HasIndex(o => o.Status).HasDatabaseName("idx_ocr_results_status");

        builder.HasOne(o => o.Document)
            .WithMany(m => m.OcrResults).HasForeignKey(o => o.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/TagConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.Property(t => t.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(t => t.UserId).IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.Name).IsRequired().HasColumnName("name").HasMaxLength(50);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(t => t.IsDeleted);

        builder.HasIndex(t => new { t.UserId, t.Name })
            .IsUnique().HasDatabaseName("uk_tags_user_name");

        builder.HasOne(t => t.User)
            .WithMany().HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/MedicalDocumentTagConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MedicalDocumentTagConfiguration : IEntityTypeConfiguration<MedicalDocumentTag>
{
    public void Configure(EntityTypeBuilder<MedicalDocumentTag> builder)
    {
        builder.ToTable("medical_document_tags");

        builder.HasKey(m => new { m.DocumentId, m.TagId });

        builder.Property(m => m.DocumentId).HasColumnName("document_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.TagId).HasColumnName("tag_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.CreatedAt).IsRequired().HasColumnName("created_at").HasColumnType("DATETIME(6)");

        builder.HasOne(m => m.Document)
            .WithMany(d => d.Tags).HasForeignKey(m => m.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Tag)
            .WithMany(t => t.DocumentTags).HasForeignKey(m => m.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Update `AppDbContext` — thêm DbSets**:

```csharp
// Add to AppDbContext.cs (after existing DbSets)

// Week 4 — File Storage + Medical Documents
public DbSet<StorageFile> StorageFiles => Set<StorageFile>();
public DbSet<RefDocumentType> RefDocumentTypes => Set<RefDocumentType>();
public DbSet<RefDocumentTypeTranslation> RefDocumentTypeTranslations => Set<RefDocumentTypeTranslation>();
public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
public DbSet<OcrResult> OcrResults => Set<OcrResult>();
public DbSet<Tag> Tags => Set<Tag>();
public DbSet<MedicalDocumentTag> MedicalDocumentTags => Set<MedicalDocumentTag>();
```

**⚠️ NOTE**: Configurations auto-applied via `ApplyConfigurationsFromAssembly`. Translation + MedicalDocumentTag (không kế thừa BaseEntity) KHÔNG có global soft-delete filter — đúng ý muốn.

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 4/10 — Migration + Seed Reference Data

**Nhiệm vụ**: Tạo migration + Seed 8 document types (mỗi cái 2 translations = 16 records).

**⚠️ CRITICAL**:
- Dùng **anonymous type** cho `HasData()`, KHÔNG dùng entity instance.
- Fixed `DateTime` — KHÔNG dùng `DateTime.UtcNow`.
- Lang code lowercase: `"vi"`, `"en"`.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Persistence/Seeders/DocumentTypeSeeder.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class DocumentTypeSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid PrenatalCheckup = Guid.Parse("d0000001-0000-0000-0000-000000000001");
    private static readonly Guid Ultrasound      = Guid.Parse("d0000001-0000-0000-0000-000000000002");
    private static readonly Guid BloodTest       = Guid.Parse("d0000001-0000-0000-0000-000000000003");
    private static readonly Guid UrineTest       = Guid.Parse("d0000001-0000-0000-0000-000000000004");
    private static readonly Guid Prescription    = Guid.Parse("d0000001-0000-0000-0000-000000000005");
    private static readonly Guid Vaccination     = Guid.Parse("d0000001-0000-0000-0000-000000000006");
    private static readonly Guid MedicalReport   = Guid.Parse("d0000001-0000-0000-0000-000000000007");
    private static readonly Guid OtherDoc        = Guid.Parse("d0000001-0000-0000-0000-000000000008");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefDocumentType>().HasData(
            new { Id = PrenatalCheckup, Code = "PRENATAL_CHECKUP",    IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Ultrasound,      Code = "ULTRASOUND",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = BloodTest,       Code = "BLOOD_TEST",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = UrineTest,       Code = "URINE_TEST",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Prescription,    Code = "PRESCRIPTION",        IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Vaccination,     Code = "VACCINATION_RECORD",  IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = MedicalReport,   Code = "MEDICAL_REPORT",      IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = OtherDoc,        Code = "OTHER",               IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        // ⚠️ Property names PHẢI match C# entity: DocumentTypeId, LanguageCode, DisplayName, Description
        modelBuilder.Entity<RefDocumentTypeTranslation>().HasData(
            // Vietnamese
            new { DocumentTypeId = PrenatalCheckup, LanguageCode = "vi", DisplayName = "Phiếu khám thai",            Description = (string?)"Phiếu kết quả khám thai định kỳ" },
            new { DocumentTypeId = Ultrasound,      LanguageCode = "vi", DisplayName = "Kết quả siêu âm",            Description = (string?)"Hình ảnh siêu âm thai nhi" },
            new { DocumentTypeId = BloodTest,       LanguageCode = "vi", DisplayName = "Xét nghiệm máu",             Description = (string?)"Kết quả xét nghiệm máu" },
            new { DocumentTypeId = UrineTest,       LanguageCode = "vi", DisplayName = "Xét nghiệm nước tiểu",       Description = (string?)"Kết quả xét nghiệm nước tiểu" },
            new { DocumentTypeId = Prescription,    LanguageCode = "vi", DisplayName = "Đơn thuốc",                   Description = (string?)"Đơn thuốc kê toa" },
            new { DocumentTypeId = Vaccination,     LanguageCode = "vi", DisplayName = "Phiếu tiêm chủng",            Description = (string?)"Hồ sơ tiêm chủng" },
            new { DocumentTypeId = MedicalReport,   LanguageCode = "vi", DisplayName = "Báo cáo y tế",               Description = (string?)"Báo cáo y tế tổng hợp" },
            new { DocumentTypeId = OtherDoc,        LanguageCode = "vi", DisplayName = "Tài liệu khác",              Description = (string?)"Tài liệu y tế không thuộc loại trên" },
            // English
            new { DocumentTypeId = PrenatalCheckup, LanguageCode = "en", DisplayName = "Prenatal Checkup Report",    Description = (string?)"Regular prenatal checkup results" },
            new { DocumentTypeId = Ultrasound,      LanguageCode = "en", DisplayName = "Ultrasound",                  Description = (string?)"Fetal ultrasound images" },
            new { DocumentTypeId = BloodTest,       LanguageCode = "en", DisplayName = "Blood Test",                  Description = (string?)"Blood test results" },
            new { DocumentTypeId = UrineTest,       LanguageCode = "en", DisplayName = "Urine Test",                  Description = (string?)"Urine test results" },
            new { DocumentTypeId = Prescription,    LanguageCode = "en", DisplayName = "Prescription",                Description = (string?)"Medical prescription" },
            new { DocumentTypeId = Vaccination,     LanguageCode = "en", DisplayName = "Vaccination Record",          Description = (string?)"Vaccination history record" },
            new { DocumentTypeId = MedicalReport,   LanguageCode = "en", DisplayName = "Medical Report",              Description = (string?)"Comprehensive medical report" },
            new { DocumentTypeId = OtherDoc,        LanguageCode = "en", DisplayName = "Other",                       Description = (string?)"Other medical documents" }
        );
    }
}
```

**Update `AppDbContext.OnModelCreating`** — add seeder call:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing code ...

    // Week 4 Seeders
    DocumentTypeSeeder.Seed(modelBuilder);
}
```

**Commands to run** (từ thư mục `src/FPT.EXE201.Api`):
```bash
dotnet ef migrations add Week4_StorageDocuments --project ../FPT.EXE201.Infrastructure --startup-project .
dotnet ef database update --project ../FPT.EXE201.Infrastructure --startup-project .
```

**✅ Checkpoint**:
- Migration tạo thành công
- Database update OK
- Verify: 7 tables mới, 8 document types, 16 translation records

---

## 🎯 PROMPT 5/10 — DTOs + FluentValidation

**Nhiệm vụ**: Tạo DTOs cho tất cả modules + FluentValidation validators.

**⚠️ DTO Rules**: Dùng `record` type. Property names match C# entities. Enum properties trong response dùng `string` (AutoMapper tự convert enum → string).

**Code — MedicalDocument DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/MedicalDocuments/CreateMedicalDocumentDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Metadata khi tạo tài liệu. File upload riêng qua IFormFile trong Controller.
/// </summary>
public record CreateMedicalDocumentDto(
    /// <summary>ID loại tài liệu từ danh mục (ref_document_types).</summary>
    Guid? DocumentTypeId,

    /// <summary>Tiêu đề tài liệu.</summary>
    string? Title,

    /// <summary>Ngày của tài liệu (ngày khám, ngày xét nghiệm).</summary>
    DateTime? DocumentDate,

    /// <summary>Nguồn gốc: Upload / Share / Import.</summary>
    DocumentSource Source,

    /// <summary>Ghi chú.</summary>
    string? Notes
);

// File: FPT.EXE201.Application/DTOs/MedicalDocuments/UpdateMedicalDocumentDto.cs
namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

public record UpdateMedicalDocumentDto(
    Guid? VisitId,
    Guid? DocumentTypeId,
    string? Title,
    DateTime? DocumentDate,
    string? Notes
);

// File: FPT.EXE201.Application/DTOs/MedicalDocuments/MedicalDocumentDto.cs
namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Response trả về thông tin tài liệu y tế + file info.
/// </summary>
public record MedicalDocumentDto(
    Guid Id,
    Guid PregnancyId,
    Guid? VisitId,
    Guid? DocumentTypeId,

    /// <summary>Tên loại tài liệu theo ngôn ngữ. Ví dụ: "Phiếu khám thai".</summary>
    string? DocumentTypeDisplayName,

    Guid StorageFileId,

    /// <summary>Tên file gốc. Ví dụ: "phieu-kham-28-tuan.jpg".</summary>
    string? OriginalFileName,

    /// <summary>MIME type. Ví dụ: "image/jpeg".</summary>
    string MimeType,

    /// <summary>Kích thước file (bytes).</summary>
    long FileSizeBytes,

    /// <summary>URL download file.</summary>
    string? FileUrl,

    string? Title,
    DateTime? DocumentDate,
    DateTime CapturedAt,

    /// <summary>Nguồn gốc: "Upload", "Share", "Import".</summary>
    string Source,

    string? Notes,
    List<TagDto> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// File: FPT.EXE201.Application/DTOs/MedicalDocuments/OcrResultDto.cs
namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Response trả về trạng thái + kết quả OCR.
/// </summary>
public record OcrResultDto(
    Guid Id,
    Guid DocumentId,
    int OcrRunNumber,
    string Status,
    string? OcrEngine,
    string? RawText,
    string? StructuredJson,
    decimal? ConfidenceScore,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

**Code — Tag DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/Tags/CreateTagDto.cs
namespace FPT.EXE201.Application.DTOs.Tags;

public record CreateTagDto(
    /// <summary>Tên tag. Unique per user. Tối đa 50 ký tự.</summary>
    string Name
);

// File: FPT.EXE201.Application/DTOs/Tags/TagDto.cs
namespace FPT.EXE201.Application.DTOs.Tags;

public record TagDto(
    Guid Id,
    string Name,
    DateTime CreatedAt
);
```

**Code — Reference Data DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/RefData/RefDocumentTypeDto.cs
namespace FPT.EXE201.Application.DTOs.RefData;

public record RefDocumentTypeDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description
);
```

**Code — Timeline DTO**:

```csharp
// File: FPT.EXE201.Application/DTOs/Timeline/TimelineEventDto.cs
namespace FPT.EXE201.Application.DTOs.Timeline;

/// <summary>
/// Một sự kiện trên dòng thời gian thai kỳ.
/// EventType: "Document", "Visit".
/// </summary>
public record TimelineEventDto(
    /// <summary>Loại sự kiện: "Document", "Visit".</summary>
    string EventType,

    /// <summary>ID của entity (document ID hoặc visit ID).</summary>
    Guid EventId,

    /// <summary>Ngày xảy ra sự kiện.</summary>
    DateTime EventDate,

    /// <summary>Tiêu đề hiển thị.</summary>
    string? Title,

    /// <summary>Mô tả ngắn.</summary>
    string? Description
);
```

**Code — FluentValidation Validators**:

```csharp
// File: FPT.EXE201.Application/Validations/MedicalDocuments/CreateMedicalDocumentDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.Validations.MedicalDocuments;

public class CreateMedicalDocumentDtoValidator : AbstractValidator<CreateMedicalDocumentDto>
{
    public CreateMedicalDocumentDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Tiêu đề không được quá 200 ký tự.");

        RuleFor(x => x.DocumentDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.DocumentDate.HasValue)
            .WithMessage("Ngày tài liệu không được ở tương lai.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Nguồn gốc không hợp lệ (Upload, Share, Import).");
    }
}

// File: FPT.EXE201.Application/Validations/MedicalDocuments/UpdateMedicalDocumentDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.Validations.MedicalDocuments;

public class UpdateMedicalDocumentDtoValidator : AbstractValidator<UpdateMedicalDocumentDto>
{
    public UpdateMedicalDocumentDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Tiêu đề không được quá 200 ký tự.");

        RuleFor(x => x.DocumentDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.DocumentDate.HasValue)
            .WithMessage("Ngày tài liệu không được ở tương lai.");
    }
}

// File: FPT.EXE201.Application/Validations/Tags/CreateTagDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.Tags;

namespace FPT.EXE201.Application.Validations.Tags;

public class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên tag không được trống.")
            .MaximumLength(50).WithMessage("Tên tag không được quá 50 ký tự.");
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 6/10 — Repository Interfaces + Service Interfaces

**Nhiệm vụ**: Tạo 5 repository interfaces + 4 service interfaces.

**⚠️ NOTE**:
- `IStorageFileRepository`, `IMedicalDocumentRepository`, `IOcrResultRepository`, `ITagRepository` → kế thừa `IGenericRepository<T>`.
- `IMedicalDocumentTagRepository` → custom interface (join table, không kế thừa BaseEntity).
- `IFileStorageService`, `IOcrService` → implementation ở **Infrastructure** layer.
- `IMedicalDocumentService`, `ITagService` → implementation ở **Application** layer.

**Code — Repository Interfaces**:

```csharp
// File: FPT.EXE201.Application/IRepositories/IStorageFileRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IStorageFileRepository : IGenericRepository<StorageFile>
{
}

// File: FPT.EXE201.Application/IRepositories/IMedicalDocumentRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMedicalDocumentRepository : IGenericRepository<MedicalDocument>
{
    /// <summary>List documents theo pregnancy, include StorageFile + DocumentType + Tags.</summary>
    Task<List<MedicalDocument>> GetByPregnancyIdWithDetailsAsync(
        Guid pregnancyId, CancellationToken cancellationToken = default);

    /// <summary>Lấy 1 document với toàn bộ details (StorageFile, Tags, OCR, Pregnancy).</summary>
    Task<MedicalDocument?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IOcrResultRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IOcrResultRepository : IGenericRepository<OcrResult>
{
    /// <summary>Lấy OCR result mới nhất của document.</summary>
    Task<OcrResult?> GetLatestByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách OCR đang pending để xử lý batch.</summary>
    Task<List<OcrResult>> GetPendingAsync(
        int limit = 10, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/ITagRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface ITagRepository : IGenericRepository<Tag>
{
    /// <summary>List tags của user (sorted by name).</summary>
    Task<List<Tag>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tìm tag theo user + name (cho unique check).</summary>
    Task<Tag?> GetByUserIdAndNameAsync(
        Guid userId, string name, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IMedicalDocumentTagRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

/// <summary>
/// Repository cho join table MedicalDocumentTag.
/// ⚠️ KHÔNG kế thừa IGenericRepository — entity này dùng composite PK.
/// </summary>
public interface IMedicalDocumentTagRepository
{
    Task AddAsync(MedicalDocumentTag documentTag, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);
}
```

**Code — Service Interfaces**:

```csharp
// File: FPT.EXE201.Application/IServices/IFileStorageService.cs
namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Abstraction cho file storage.
/// Week 4 = StubFileStorageService (metadata only).
/// Week 5 = SupabaseStorageService (upload thật lên Supabase).
/// ⚠️ Interface ở Application, Implementation ở Infrastructure.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Upload file vào storage, trả về thông tin file đã lưu.</summary>
    Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Download file từ storage.</summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Xóa file khỏi storage.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

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

// File: FPT.EXE201.Application/IServices/IOcrService.cs
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý OCR. Week 4 = stub. Full implementation in later weeks.
/// ⚠️ Interface ở Application, Implementation ở Infrastructure.
/// </summary>
public interface IOcrService
{
    /// <summary>Tạo OcrResult mới với status=Pending, queue để xử lý.</summary>
    Task<Guid> QueueOcrAsync(
        Guid documentId, string? languageHint = null,
        CancellationToken cancellationToken = default);

    /// <summary>Chạy lại OCR cho document (tăng OcrRunNumber).</summary>
    Task<Guid> RerunOcrAsync(
        Guid documentId, Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy kết quả OCR theo ID.</summary>
    Task<OcrResultDto> GetResultAsync(
        Guid ocrResultId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/IMedicalDocumentService.cs
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.DTOs.Timeline;

namespace FPT.EXE201.Application.IServices;

public interface IMedicalDocumentService
{
    /// <summary>Upload file + tạo document trong 1 bước.</summary>
    Task<MedicalDocumentDto> CreateWithFileAsync(
        Guid pregnancyId, CreateMedicalDocumentDto dto,
        Stream fileStream, string fileName, string contentType, long fileSize,
        Guid currentUserId, CancellationToken cancellationToken = default);

    Task<List<MedicalDocumentDto>> GetByPregnancyIdAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<MedicalDocumentDto> GetByIdAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<MedicalDocumentDto> UpdateAsync(
        Guid id, UpdateMedicalDocumentDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy timeline (documents + visits) của thai kỳ.</summary>
    Task<List<TimelineEventDto>> GetTimelineAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/ITagService.cs
using FPT.EXE201.Application.DTOs.Tags;

namespace FPT.EXE201.Application.IServices;

public interface ITagService
{
    Task<TagDto> CreateAsync(CreateTagDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<List<TagDto>> GetByUserIdAsync(Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task AddTagToDocumentAsync(Guid documentId, Guid tagId, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 7/10 — Repository Implementations + UnitOfWork Update

**Nhiệm vụ**: Implement 5 repositories + update UnitOfWork với lazy init.

**⚠️ Constructor**: Dùng `AppDbContext context`, KHÔNG phải `ApplicationDbContext`.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Repositories/StorageFileRepository.cs
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class StorageFileRepository : GenericRepository<StorageFile>, IStorageFileRepository
{
    public StorageFileRepository(AppDbContext context) : base(context) { }
}

// File: FPT.EXE201.Infrastructure/Repositories/MedicalDocumentRepository.cs
using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MedicalDocumentRepository : GenericRepository<MedicalDocument>, IMedicalDocumentRepository
{
    public MedicalDocumentRepository(AppDbContext context) : base(context) { }

    public async Task<List<MedicalDocument>> GetByPregnancyIdWithDetailsAsync(
        Guid pregnancyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.PregnancyId == pregnancyId)
            .Include(m => m.StorageFile)
            .Include(m => m.DocumentType)
                .ThenInclude(dt => dt!.Translations)
            .Include(m => m.Tags)
                .ThenInclude(t => t.Tag)
            .OrderByDescending(m => m.CapturedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalDocument?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.StorageFile)
            .Include(m => m.DocumentType)
                .ThenInclude(dt => dt!.Translations)
            .Include(m => m.Tags)
                .ThenInclude(t => t.Tag)
            .Include(m => m.OcrResults.OrderByDescending(o => o.OcrRunNumber).Take(1))
            .Include(m => m.Pregnancy)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/OcrResultRepository.cs
using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class OcrResultRepository : GenericRepository<OcrResult>, IOcrResultRepository
{
    public OcrResultRepository(AppDbContext context) : base(context) { }

    public async Task<OcrResult?> GetLatestByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.DocumentId == documentId)
            .OrderByDescending(o => o.OcrRunNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<OcrResult>> GetPendingAsync(
        int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.Status == OcrStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/TagRepository.cs
using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class TagRepository : GenericRepository<Tag>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context) { }

    public async Task<List<Tag>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByUserIdAndNameAsync(
        Guid userId, string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == name, cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/MedicalDocumentTagRepository.cs
using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

/// <summary>
/// Repository cho join table — KHÔNG kế thừa GenericRepository.
/// </summary>
public class MedicalDocumentTagRepository : IMedicalDocumentTagRepository
{
    private readonly AppDbContext _context;

    public MedicalDocumentTagRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MedicalDocumentTag documentTag, CancellationToken cancellationToken = default)
    {
        await _context.Set<MedicalDocumentTag>().AddAsync(documentTag, cancellationToken);
    }

    public async Task RemoveAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<MedicalDocumentTag>()
            .FirstOrDefaultAsync(m => m.DocumentId == documentId && m.TagId == tagId, cancellationToken);

        if (entity != null)
            _context.Set<MedicalDocumentTag>().Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MedicalDocumentTag>()
            .AnyAsync(m => m.DocumentId == documentId && m.TagId == tagId, cancellationToken);
    }
}
```

**Update `IUnitOfWork` — thêm properties**:

```csharp
// Add to IUnitOfWork.cs

// Week 4 — File Storage + Medical Documents
IStorageFileRepository StorageFiles { get; }
IMedicalDocumentRepository MedicalDocuments { get; }
IOcrResultRepository OcrResults { get; }
ITagRepository Tags { get; }
IMedicalDocumentTagRepository MedicalDocumentTags { get; }
```

**Update `UnitOfWork` — lazy init**:

```csharp
// Add to UnitOfWork.cs

// Week 4
private IStorageFileRepository? _storageFiles;
private IMedicalDocumentRepository? _medicalDocuments;
private IOcrResultRepository? _ocrResults;
private ITagRepository? _tags;
private IMedicalDocumentTagRepository? _medicalDocumentTags;

public IStorageFileRepository StorageFiles => _storageFiles ??= new StorageFileRepository(_context);
public IMedicalDocumentRepository MedicalDocuments => _medicalDocuments ??= new MedicalDocumentRepository(_context);
public IOcrResultRepository OcrResults => _ocrResults ??= new OcrResultRepository(_context);
public ITagRepository Tags => _tags ??= new TagRepository(_context);
public IMedicalDocumentTagRepository MedicalDocumentTags => _medicalDocumentTags ??= new MedicalDocumentTagRepository(_context);
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 8/10 — StubFileStorageService (Infrastructure) + OcrService (Stub)

**Nhiệm vụ**: Implement StubFileStorageService (chỉ lưu metadata, chưa lưu file vật lý) + OcrService (stub cho Week 4).

> **⚠️ Tại sao dùng Stub?**: Week 4 tập trung vào entities + CRUD. Việc upload file thật lên Supabase Storage sẽ implement ở Week 5 cùng với Azure OCR + Gemini AI. StubFileStorageService lưu metadata vào DB và trả placeholder URL — đủ để test flow end-to-end.

**⚠️ Cả 2 service đặt ở `Infrastructure/Services/`** vì chúng tương tác với external systems (storage, future AI API).

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Services/StubFileStorageService.cs
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Stub implementation — chỉ lưu metadata, không lưu file vật lý.
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
        var objectKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        // Stub: trả placeholder URL — Week 5 sẽ trả Supabase public URL thật
        var result = new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: $"/api/files/{objectKey}/placeholder",
            OriginalFileName: fileName,
            MimeType: contentType,
            FileSizeBytes: sizeBytes,
            ChecksumSha256: null // Week 5 sẽ tính checksum khi upload thật
        );

        return Task.FromResult(result);
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        // Stub: chưa có file thật để download
        throw new NotSupportedException(
            "StubFileStorageService không hỗ trợ download. "
            + "Hãy chuyển sang SupabaseStorageService (Week 5).");
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        // Stub: không có file vật lý để xóa, chỉ return OK
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string objectKey)
    {
        return $"/api/files/{objectKey}/placeholder";
    }
}

// File: FPT.EXE201.Infrastructure/Services/OcrService.cs
using AutoMapper;
using FPT.EXE201.Application;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OcrService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> QueueOcrAsync(
        Guid documentId, string? languageHint = null,
        CancellationToken cancellationToken = default)
    {
        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = 1,
            Status = OcrStatus.Pending,
            LanguageHint = languageHint ?? "vi"
        };

        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ocrResult.Id;
    }

    public async Task<Guid> RerunOcrAsync(
        Guid documentId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Verify document exists + ownership
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền chạy OCR cho tài liệu này.");

        // Get next run number
        var latestOcr = await _unitOfWork.OcrResults.GetLatestByDocumentIdAsync(documentId, cancellationToken);
        var nextRunNo = latestOcr != null ? latestOcr.OcrRunNumber + 1 : 1;

        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = nextRunNo,
            Status = OcrStatus.Pending,
            LanguageHint = "vi"
        };

        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // STUB: In production, queue background job here
        // await _backgroundJobService.EnqueueOcrProcessingAsync(ocrResult.Id);

        return ocrResult.Id;
    }

    public async Task<OcrResultDto> GetResultAsync(
        Guid ocrResultId, CancellationToken cancellationToken = default)
    {
        var ocr = await _unitOfWork.OcrResults.GetByIdAsync(ocrResultId, cancellationToken: cancellationToken);
        if (ocr == null)
            throw new NotFoundException("Kết quả OCR không tồn tại.");

        return _mapper.Map<OcrResultDto>(ocr);
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 9/10 — MedicalDocumentService + TagService

**Nhiệm vụ**: Implement business logic cho Documents + Tags + Timeline.

**⚠️ Đặt ở `Application/Services/`** — đây là business logic layer.

**Code**:

```csharp
// File: FPT.EXE201.Application/Services/MedicalDocumentService.cs
using AutoMapper;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.DTOs.Timeline;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MedicalDocumentService : IMedicalDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOcrService _ocrService;

    public MedicalDocumentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService fileStorageService,
        IOcrService ocrService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
        _ocrService = ocrService;
    }

    public async Task<MedicalDocumentDto> CreateWithFileAsync(
        Guid pregnancyId, CreateMedicalDocumentDto dto,
        Stream fileStream, string fileName, string contentType, long fileSize,
        Guid currentUserId, CancellationToken cancellationToken = default)
    {
        // 1. Verify pregnancy ownership
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Thai kỳ không tồn tại.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền thêm tài liệu vào thai kỳ này.");

        // 2. Upload file to storage
        var storageResult = await _fileStorageService.UploadAsync(
            fileStream, fileName, contentType, fileSize, currentUserId, cancellationToken);

        // 3. Create StorageFile record
        var storageFile = new StorageFile
        {
            OwnerUserId = currentUserId,
            StorageProvider = "stub",
            ObjectKey = storageResult.ObjectKey,
            PublicUrl = storageResult.PublicUrl,
            OriginalFileName = storageResult.OriginalFileName,
            MimeType = storageResult.MimeType,
            FileSizeBytes = storageResult.FileSizeBytes,
            ChecksumSha256 = storageResult.ChecksumSha256,
            UploadedAt = DateTime.UtcNow
        };
        await _unitOfWork.StorageFiles.AddAsync(storageFile, cancellationToken);

        // 4. Create MedicalDocument record
        var document = new MedicalDocument
        {
            PregnancyId = pregnancyId,
            DocumentTypeId = dto.DocumentTypeId,
            StorageFileId = storageFile.Id,
            Title = dto.Title,
            DocumentDate = dto.DocumentDate,
            CapturedAt = DateTime.UtcNow,
            Source = dto.Source,
            Notes = dto.Notes
        };
        await _unitOfWork.MedicalDocuments.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Queue OCR for images/PDFs
        if (contentType.StartsWith("image/") || contentType == "application/pdf")
        {
            await _ocrService.QueueOcrAsync(document.Id, "vi", cancellationToken);
        }

        // 6. Reload with details for response
        var result = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(document.Id, cancellationToken);
        return _mapper.Map<MedicalDocumentDto>(result!);
    }

    public async Task<List<MedicalDocumentDto>> GetByPregnancyIdAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Thai kỳ không tồn tại.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền xem tài liệu của thai kỳ này.");

        var documents = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, cancellationToken);
        return _mapper.Map<List<MedicalDocumentDto>>(documents);
    }

    public async Task<MedicalDocumentDto> GetByIdAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền xem tài liệu này.");

        return _mapper.Map<MedicalDocumentDto>(document);
    }

    public async Task<MedicalDocumentDto> UpdateAsync(
        Guid id, UpdateMedicalDocumentDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền cập nhật tài liệu này.");

        // Verify visit belongs to same pregnancy (if provided)
        if (dto.VisitId.HasValue)
        {
            var visit = await _unitOfWork.PrenatalVisits.GetByIdAsync(dto.VisitId.Value,
                cancellationToken: cancellationToken);
            if (visit == null || visit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Buổi khám không tồn tại hoặc không thuộc thai kỳ này.");
        }

        _mapper.Map(dto, document);
        _unitOfWork.MedicalDocuments.Update(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<MedicalDocumentDto>(result!);
    }

    public async Task DeleteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền xóa tài liệu này.");

        await _unitOfWork.MedicalDocuments.SoftDeleteAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TimelineEventDto>> GetTimelineAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Thai kỳ không tồn tại.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền xem timeline của thai kỳ này.");

        var events = new List<TimelineEventDto>();

        // Documents
        var documents = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, cancellationToken);
        foreach (var doc in documents)
        {
            events.Add(new TimelineEventDto(
                EventType: "Document",
                EventId: doc.Id,
                EventDate: doc.DocumentDate ?? doc.CapturedAt,
                Title: doc.Title ?? "Tài liệu y tế",
                Description: doc.Notes
            ));
        }

        // Visits (from Week 3)
        var visits = await _unitOfWork.PrenatalVisits.GetAllAsync(
            v => v.PregnancyId == pregnancyId, cancellationToken: cancellationToken);
        foreach (var visit in visits)
        {
            events.Add(new TimelineEventDto(
                EventType: "Visit",
                EventId: visit.Id,
                EventDate: visit.VisitDateTime,
                Title: $"Khám thai — {visit.VisitType}",
                Description: visit.Notes
            ));
        }

        // TODO: Future weeks — add weight logs, nutrition logs, etc.

        return events.OrderByDescending(e => e.EventDate).ToList();
    }
}

// File: FPT.EXE201.Application/Services/TagService.cs
using AutoMapper;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.Tags;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TagService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TagDto> CreateAsync(
        CreateTagDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Check duplicate tag name
        var existing = await _unitOfWork.Tags.GetByUserIdAndNameAsync(
            currentUserId, dto.Name, cancellationToken);
        if (existing != null)
            throw new ConflictException($"Tag '{dto.Name}' đã tồn tại.");

        var tag = new Tag
        {
            UserId = currentUserId,
            Name = dto.Name
        };

        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TagDto>(tag);
    }

    public async Task<List<TagDto>> GetByUserIdAsync(
        Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tags = await _unitOfWork.Tags.GetByUserIdAsync(currentUserId, cancellationToken);
        return _mapper.Map<List<TagDto>>(tags);
    }

    public async Task DeleteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (tag == null)
            throw new NotFoundException("Tag không tồn tại.");
        if (tag.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền xóa tag này.");

        await _unitOfWork.Tags.SoftDeleteAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTagToDocumentAsync(
        Guid documentId, Guid tagId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Verify document ownership
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền gắn tag cho tài liệu này.");

        // Verify tag ownership
        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken: cancellationToken);
        if (tag == null)
            throw new NotFoundException("Tag không tồn tại.");
        if (tag.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền sử dụng tag này.");

        // Check if already tagged
        var exists = await _unitOfWork.MedicalDocumentTags.ExistsAsync(documentId, tagId, cancellationToken);
        if (exists)
            throw new ConflictException("Tag đã được gắn vào tài liệu này.");

        var documentTag = new MedicalDocumentTag
        {
            DocumentId = documentId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.MedicalDocumentTags.AddAsync(documentTag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveTagFromDocumentAsync(
        Guid documentId, Guid tagId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken);
        if (document == null)
            throw new NotFoundException("Tài liệu không tồn tại.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("Bạn không có quyền gỡ tag cho tài liệu này.");

        await _unitOfWork.MedicalDocumentTags.RemoveAsync(documentId, tagId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 10/10 — Controllers + AutoMapper + Permissions + RefData Endpoint

**Nhiệm vụ**: Tạo Controllers (kế thừa `BaseApiController`), AutoMapper profiles, permission constants, ref data endpoint.

**⚠️ Controller Rules**:
- Kế thừa `BaseApiController` → dùng `Success()`, `Created()`, `GetCurrentUserId()`
- KHÔNG tự viết `ApiResponse<T>.SuccessResponse()`
- `[RequirePermission(...)]` cho protected endpoints

**Code — AutoMapper Profiles**:

```csharp
// File: FPT.EXE201.Application/MapperProfiles/MedicalDocumentProfile.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.DTOs.Tags;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.MapperProfiles;

public class MedicalDocumentProfile : Profile
{
    public MedicalDocumentProfile()
    {
        // MedicalDocument → MedicalDocumentDto
        CreateMap<MedicalDocument, MedicalDocumentDto>()
            .ForMember(dest => dest.DocumentTypeDisplayName,
                opt => opt.MapFrom(src => src.DocumentType != null
                    ? src.DocumentType.Translations.FirstOrDefault()?.DisplayName
                    : null))
            .ForMember(dest => dest.OriginalFileName,
                opt => opt.MapFrom(src => src.StorageFile.OriginalFileName))
            .ForMember(dest => dest.MimeType,
                opt => opt.MapFrom(src => src.StorageFile.MimeType))
            .ForMember(dest => dest.FileSizeBytes,
                opt => opt.MapFrom(src => src.StorageFile.FileSizeBytes))
            .ForMember(dest => dest.FileUrl,
                opt => opt.MapFrom(src => src.StorageFile.PublicUrl))
            .ForMember(dest => dest.Tags,
                opt => opt.MapFrom(src => src.Tags.Select(t => t.Tag)));

        // UpdateMedicalDocumentDto → MedicalDocument (partial update)
        CreateMap<UpdateMedicalDocumentDto, MedicalDocument>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PregnancyId, opt => opt.Ignore())
            .ForMember(dest => dest.StorageFileId, opt => opt.Ignore())
            .ForMember(dest => dest.CapturedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Source, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // OcrResult → OcrResultDto
        CreateMap<OcrResult, OcrResultDto>();

        // Tag → TagDto
        CreateMap<Tag, TagDto>();
    }
}
```

**Code — Controllers**:

```csharp
// File: FPT.EXE201.Api/Controllers/MedicalDocumentsController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
public class MedicalDocumentsController : BaseApiController
{
    private readonly IMedicalDocumentService _documentService;

    public MedicalDocumentsController(IMedicalDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>Upload ảnh + tạo document trong 1 bước (multipart/form-data).</summary>
    [HttpPost("pregnancies/{pregnancyId}/documents")]
    [RequirePermission("document.create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        Guid pregnancyId,
        IFormFile file,
        [FromForm] Guid? documentTypeId = null,
        [FromForm] string? title = null,
        [FromForm] DateTime? documentDate = null,
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var dto = new CreateMedicalDocumentDto(documentTypeId, title, documentDate, DocumentSource.Upload, notes);

        using var stream = file.OpenReadStream();
        var result = await _documentService.CreateWithFileAsync(
            pregnancyId, dto, stream, file.FileName, file.ContentType, file.Length,
            userId, cancellationToken);

        return Created(result, "Tài liệu đã được tạo thành công.");
    }

    /// <summary>List documents của thai kỳ.</summary>
    [HttpGet("pregnancies/{pregnancyId}/documents")]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetByPregnancy(
        Guid pregnancyId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetByPregnancyIdAsync(pregnancyId, userId, cancellationToken);
        return Success(result);
    }

    /// <summary>Chi tiết 1 document.</summary>
    [HttpGet("documents/{id}")]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetByIdAsync(id, userId, cancellationToken);
        return Success(result);
    }

    /// <summary>Update metadata (title, notes, visit link).</summary>
    [HttpPut("documents/{id}")]
    [RequirePermission("document.update")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateMedicalDocumentDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.UpdateAsync(id, dto, userId, cancellationToken);
        return Success(result, "Tài liệu đã được cập nhật.");
    }

    /// <summary>Soft delete document.</summary>
    [HttpDelete("documents/{id}")]
    [RequirePermission("document.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _documentService.DeleteAsync(id, userId, cancellationToken);
        return Success<object?>(null, "Tài liệu đã được xóa.");
    }
}

// File: FPT.EXE201.Api/Controllers/TimelineController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/pregnancies/{pregnancyId}/timeline")]
public class TimelineController : BaseApiController
{
    private readonly IMedicalDocumentService _documentService;

    public TimelineController(IMedicalDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>Dòng thời gian thai kỳ (documents + visits).</summary>
    [HttpGet]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetTimeline(
        Guid pregnancyId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetTimelineAsync(pregnancyId, userId, cancellationToken);
        return Success(result);
    }
}

// File: FPT.EXE201.Api/Controllers/TagsController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.Tags;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/tags")]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpPost]
    [RequirePermission("tag.create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTagDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _tagService.CreateAsync(dto, userId, cancellationToken);
        return Created(result, "Tag đã được tạo.");
    }

    [HttpGet]
    [RequirePermission("tag.view")]
    public async Task<IActionResult> GetByUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _tagService.GetByUserIdAsync(userId, cancellationToken);
        return Success(result);
    }

    [HttpDelete("{id}")]
    [RequirePermission("tag.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _tagService.DeleteAsync(id, userId, cancellationToken);
        return Success<object?>(null, "Tag đã được xóa.");
    }
}

// File: FPT.EXE201.Api/Controllers/DocumentTagsController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/documents/{documentId}/tags")]
public class DocumentTagsController : BaseApiController
{
    private readonly ITagService _tagService;

    public DocumentTagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpPost("{tagId}")]
    [RequirePermission("tag.assign")]
    public async Task<IActionResult> AddTag(
        Guid documentId, Guid tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _tagService.AddTagToDocumentAsync(documentId, tagId, userId, cancellationToken);
        return Success<object?>(null, "Tag đã được gắn vào tài liệu.");
    }

    [HttpDelete("{tagId}")]
    [RequirePermission("tag.unassign")]
    public async Task<IActionResult> RemoveTag(
        Guid documentId, Guid tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _tagService.RemoveTagFromDocumentAsync(documentId, tagId, userId, cancellationToken);
        return Success<object?>(null, "Tag đã được gỡ khỏi tài liệu.");
    }
}

// File: FPT.EXE201.Api/Controllers/OcrController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
public class OcrController : BaseApiController
{
    private readonly IOcrService _ocrService;

    public OcrController(IOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    /// <summary>Chạy lại OCR cho document.</summary>
    [HttpPost("documents/{documentId}/ocr/rerun")]
    [RequirePermission("ocr.trigger")]
    public async Task<IActionResult> RerunOcr(
        Guid documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var ocrResultId = await _ocrService.RerunOcrAsync(documentId, userId, cancellationToken);
        return Created(new { OcrResultId = ocrResultId }, "OCR đã được đưa vào hàng đợi.");
    }

    /// <summary>Kiểm tra trạng thái OCR.</summary>
    [HttpGet("ocr/{id}/status")]
    [RequirePermission("ocr.view")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ocrService.GetResultAsync(id, cancellationToken);
        return Success(result);
    }
}
```

**Code — RefData Endpoint** (thêm vào existing RefDataController hoặc tạo mới):

```csharp
// Thêm vào Controller RefData (nếu đã có từ Week 3) hoặc tạo mới:
// File: FPT.EXE201.Api/Controllers/RefDataController.cs (thêm method)

using Microsoft.EntityFrameworkCore;

/// <summary>Danh mục loại tài liệu (public, no auth).</summary>
[HttpGet("ref/document-types")]
[AllowAnonymous]
public async Task<IActionResult> GetDocumentTypes(
    [FromQuery] string lang = "vi", CancellationToken cancellationToken = default)
{
    var types = await _unitOfWork.RefDocumentTypes.GetAllAsync(
        r => r.IsActive,
        include: q => q.Include(r => r.Translations),
        cancellationToken: cancellationToken);

    var result = types.Select(r =>
    {
        var translation = r.Translations
            .FirstOrDefault(t => t.LanguageCode == lang)
            ?? r.Translations.FirstOrDefault();

        return new RefDocumentTypeDto(
            r.Id, r.Code,
            translation?.DisplayName ?? r.Code,
            translation?.Description);
    }).ToList();

    return Success(result);
}
```

**Update `IUnitOfWork` — thêm ref data access**:

```csharp
// Add to IUnitOfWork.cs
IGenericRepository<RefDocumentType> RefDocumentTypes { get; }
```

```csharp
// Add to UnitOfWork.cs
private IGenericRepository<RefDocumentType>? _refDocumentTypes;
public IGenericRepository<RefDocumentType> RefDocumentTypes
    => _refDocumentTypes ??= new GenericRepository<RefDocumentType>(_context);
```

**Update DependencyInjection** — đăng ký services:

```csharp
// Add to FPT.EXE201.Infrastructure/DependencyInjection.cs (AddInfrastructure method)

// Week 4 — Infrastructure Services
services.AddScoped<IFileStorageService, StubFileStorageService>();
services.AddScoped<IOcrService, OcrService>();
```

```csharp
// Add to FPT.EXE201.Application/DependencyInjection.cs (AddApplication method)

// Week 4 — Application Services
services.AddScoped<IMedicalDocumentService, MedicalDocumentService>();
services.AddScoped<ITagService, TagService>();
```

**Permissions to seed** (thêm vào PermissionSeeder nếu có):

```
document.create
document.view
document.update
document.delete
tag.create
tag.view
tag.delete
tag.assign
tag.unassign
ocr.trigger
ocr.view
```

**✅ Final Checkpoint**:
1. Build project thành công
2. Run migration
3. Test API endpoints:
   - `POST /api/pregnancies/{id}/documents` (multipart upload)
   - `GET /api/pregnancies/{id}/documents`
   - `GET /api/documents/{id}`
   - `PUT /api/documents/{id}`
   - `DELETE /api/documents/{id}`
   - `POST /api/tags` + `GET /api/tags`
   - `POST /api/documents/{docId}/tags/{tagId}`
   - `GET /api/pregnancies/{id}/timeline`
   - `POST /api/documents/{id}/ocr/rerun`
   - `GET /api/ocr/{id}/status`
   - `GET /api/ref/document-types?lang=vi`
4. Verify StorageFile record được tạo với `StorageProvider = "stub"` và placeholder URL
5. Verify OCR queue (status=Pending trong ocr_results)
6. ⚠️ Lưu ý: File chưa được upload thật — Week 5 sẽ thay StubFileStorageService bằng SupabaseStorageService

---

## 🎉 Week 4 Complete!

**✅ Implemented**:
- ✅ 7 database tables (bỏ document_files — simplified)
- ✅ Stub file storage service (metadata only, placeholder URL — Week 5 thay bằng Supabase)
- ✅ Document management (upload metadata + CRUD)
- ✅ Tag system (user-defined tags, assign/unassign)
- ✅ OCR integration (stub — full implementation in Week 5)
- ✅ Timeline view (documents + visits from Week 3)
- ✅ Ref data endpoint (document types with i18n)
- ✅ 11 permissions with RBAC
- ✅ Full ownership validation

**Architecture (simplified)**:
```
StorageFile (1) ←── (1) MedicalDocument ──→ (1) Pregnancy (Week 3)
                           │
                           ├──→ (1) PrenatalVisit (nullable, Week 3)
                           ├──→ (1) RefDocumentType (nullable)
                           ├── (N) OcrResult
                           └── (N) MedicalDocumentTag ──→ (1) Tag ──→ (1) User
```

**Next Steps**: Week 5 sẽ thay StubFileStorageService bằng SupabaseStorageService + implement OCR/AI pipeline (Azure Document Intelligence + Google Gemini).
