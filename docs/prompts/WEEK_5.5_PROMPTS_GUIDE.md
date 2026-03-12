# WEEK 5.5 PROMPTS GUIDE — Auto-Fill: AI Extraction → Entity Creation

> ⚠️ **Database Convention**: Project sử dụng **CHAR(36)** để lưu Guid, KHÔNG dùng BINARY(16).  
> ⚠️ **Exception Handling**: Services throw exceptions, GlobalExceptionFilter xử lý thành ApiResponse.  
> ⚠️ **RBAC**: Dùng `[RequirePermission("permission.name")]` trong Controller, KHÔNG try-catch.  
> ⚠️ **EF Core Tracking**: Khi cần UPDATE entity, dùng `GetByIdTrackedAsync` (WITH tracking). `GetByIdAsync` dùng `AsNoTracking()` → `SaveChangesAsync` SẼ KHÔNG LƯU gì.  
> ⚠️ **Prerequisite**: Week 5 (OCR + AI Extraction pipeline) phải hoàn thành trước.

---

## 📋 CONTEXT — Đọc trước khi bắt đầu

### WEEK 5.5 Overview

**Mục tiêu**: Implement "Review & Confirm" flow — cho phép user xem dữ liệu AI đã extract, chỉnh sửa nếu cần, rồi confirm để hệ thống tự động tạo PrenatalVisit / PrenatalTest từ `OcrResult.StructuredJson`.

**Vấn đề Week 5 để lại**:
- Week 5 chỉ extract dữ liệu → lưu JSON thô vào `OcrResult.StructuredJson`
- Chưa có flow tự động tạo Visit/Test từ extracted data
- User phải tạo thủ công Visit (POST /visits) và Test (POST /tests) riêng biệt
- Không có UI flow "Review → Confirm → Auto-create"

**WEEK 5.5 giải quyết**:

```
Week 5 pipeline:  Upload → OCR → AI → StructuredJson (lưu JSON thô)
                                            ↓
WEEK 5.5 flow:      GET review data → User chỉnh sửa → POST confirm
                                            ↓
                  Auto-create: PrenatalVisit (+ VitalsJson) và/hoặc PrenatalTest(s)
                  Auto-link:   MedicalDocument.VisitId ← created visit
```

### StructuredJson Actual Format (Week 5 Output)

```
OcrResult.StructuredJson được lưu dưới dạng MedicalRecordExtractionResult (2 fields):
{
  "vitalsData": { ... VitalsJsonDto schema ... },   ← Maps 1:1 với PrenatalVisit.VitalsJson
  "overallConfidence": 0.7
}
```

**C# Models đã có** (file `Application/AI/ExtractionModels/MedicalRecordExtractionResult.cs`):

```csharp
public class MedicalRecordExtractionResult
{
    [JsonPropertyName("vitalsData")]
    public VitalsJsonDto? VitalsData { get; set; }    // ← 1:1 với VitalsJsonDto

    [JsonPropertyName("overallConfidence")]
    public double OverallConfidence { get; set; }
}
```

### VitalsJsonDto Schema (đã có đầy đủ)

```
VitalsJsonDto (top-level, nested objects):
├── generalInfo      : GeneralInfoDto       — facility, fullName, dateOfBirth, age, phone, address, ...
├── previousVisit    : PreviousVisitInfoDto  — visitDate, diagnosis, treatment
├── interview        : InterviewDto          — reasonForVisit, pregnancyNumber, gestationalWeek, ...
├── medicalHistory   : MedicalHistoryDto     — personal, obstetric, gynecology, family
├── examination      : ExaminationDto
│   ├── vitalSigns   : VitalSignsDto         — pulseBpm, temperatureCelsius, bloodPressureSystolic/Diastolic, weightKg, heightCm, respiratoryRateBpm
│   ├── general      : GeneralExaminationDto  — mentalStatus, urineProtein, edema, ...
│   └── obstetric    : ObstetricExaminationDto — fundusHeightCm, fetalHeartRateBpm, fetalPresentation, amnioticFluid, cervix, ...
├── diagnosis        : DiagnosisDto           — text, icdCode
├── treatmentPlan    : TreatmentPlanDto       — medication, nextSteps, healthEducation
├── prognosis        : string?
└── nextAppointment  : NextAppointmentDto     — date, notes, examinerType
```

### Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                     WEEK 5.5 — Auto-Fill Flow                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️ Review + Confirm chỉ áp dụng cho:                          │
│     - PRENATAL_CHECKUP (có OCR data → tạo Visit)               │
│     - Test types (user nhập metadata → tạo PrenatalTest)       │
│  ⚠️ Others (PRESCRIPTION, VACCINATION...) là archive only,     │
│     KHÔNG cần review/confirm — DONE tại Phase 1 Upload.        │
│                                                                 │
│  1. GET /api/ocr/{id}/review                                   │
│     → Parse StructuredJson → trả về ExtractionReviewDto        │
│     → Flutter hiển thị form để user review/edit                │
│                                                                 │
│  2. POST /api/ocr/{id}/confirm                                 │
│     → Nhận ConfirmExtractionDto (user-edited data)             │
│     → Dựa vào documentType → chọn strategy:                   │
│                                                                 │
│     ┌──────────────────┬────────────────────────────────────┐  │
│     │ Document Type     │ Auto-create                       │  │
│     ├──────────────────┼────────────────────────────────────┤  │
│     │ PRENATAL_CHECKUP  │ PrenatalVisit + VitalsJson        │  │
│     │ ULTRASOUND        │ PrenatalTest (IMAGING)            │  │
│     │ BLOOD_TEST        │ PrenatalTest(s) (LAB)             │  │
│     │ URINE_TEST        │ PrenatalTest (LAB)                │  │
│     │ HIV_TEST etc.     │ PrenatalTest (direct match)       │  │
│     └──────────────────┴────────────────────────────────────┘  │
│     ⚠️ Others (PRESCRIPTION, VACCINATION, MEDICAL_REPORT,      │
│        OTHER) KHÔNG đi qua confirm flow — đã DONE ở Phase 1.   │
│        HandleNotesOnly giữ lại như defensive fallback.         │
│                                                                 │
│  3. Auto-link: MedicalDocument.VisitId ← created visit ID     │
│     PrenatalTest.DocumentId ← source document (test types)    │
│                                                                 │
│  4. Update OcrResult.Status = "Confirmed"                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Cấu trúc folders mới WEEK 5.5

```
FPT.EXE201.Application/
├── DTOs/
│   └── AutoFill/
│       ├── ExtractionReviewDto.cs          — Response: extracted data for review
│       ├── ConfirmExtractionDto.cs          — Request: user-confirmed data
│       └── AutoFillResultDto.cs             — Response: what was created
├── IServices/
│   └── IAutoFillService.cs                  — Interface
├── Services/
│   └── AutoFillService.cs                   — Main orchestrator
│
FPT.EXE201.Domain/
├── Enums/
│   └── OcrStatus.cs                         — Thêm: Confirmed
│
FPT.EXE201.Api/
├── Controllers/
│   └── AutoFillController.cs                — 2 endpoints
```

### Conventions đã có (PHẢI tuân thủ)

- AutoMapper cho entity→DTO (Week 4 pattern) khi mapping 1:1
- Manual mapping khi có logic phức tạp (Week 3 pattern)
- Exception-based flow: throw `NotFoundException`, `BadRequestException`, `ForbiddenException`
- `[RequirePermission("x")]` trên controller actions
- UnitOfWork pattern: `_unitOfWork.{Repository}.{Method}Async()`
- FluentValidation cho `[FromBody]` DTOs
- `CancellationToken` trên tất cả async methods
- **`GetByIdAsync`** = read-only (AsNoTracking) — dùng cho query/read
- **`GetByIdTrackedAsync`** = with tracking — dùng khi cần UPDATE entity

### Existing Key Entities (Reference)

**PrenatalVisit** (`FPT.EXE201.Domain/Entities/PrenatalVisit.cs`):

| Field | Type | Notes |
|-------|------|-------|
| `PregnancyId` | `Guid` | FK → Pregnancy |
| `DoctorId` | `Guid?` | Nullable |
| `VisitDate` | **`DateOnly`** | NOT DateTime |
| `VisitType` | **`VisitType`** enum | `Routine`, `Emergency`, `FollowUp`, `LabOnly`, `Other` |
| `Location` | `string?` | |
| `Notes` | `string?` | |
| `VitalsJson` | `string?` | Flexible JSON — stores VitalsJsonDto |
| Navigation | `Pregnancy`, `Tests` (ICollection\<PrenatalTest\>) | |

**PrenatalTest** (`FPT.EXE201.Domain/Entities/PrenatalTest.cs`):

| Field | Type | Notes |
|-------|------|-------|
| `PregnancyId` | `Guid` | FK → Pregnancy |
| `VisitId` | `Guid?` | FK → PrenatalVisit (nullable) |
| `TestTypeId` | `Guid` | FK → RefTestType |
| `TestDate` | **`DateOnly`** | NOT DateTime |
| `ImageUrlsJson` | `string?` | JSON array of URLs |
| `Notes` | `string?` | |
| `IsAbnormalResult` | `bool` | |
| **`DocumentId`** | **`Guid?`** | FK → MedicalDocument (nullable, ON DELETE SET NULL) |
| Navigation | `Pregnancy`, `Visit?`, `TestType`, `Document?` | |

**MedicalDocument** (`FPT.EXE201.Domain/Entities/MedicalDocument.cs`):

| Field | Type | Notes |
|-------|------|-------|
| `PregnancyId` | `Guid` | FK → Pregnancy |
| `VisitId` | `Guid?` | FK → PrenatalVisit (nullable, populated by OCR/AI) |
| `DocumentTypeId` | `Guid?` | FK → RefDocumentType (nullable) |
| `Title` | `string?` | |
| `DocumentDate` | **`DateOnly?`** | NOT DateTime |
| `CapturedAt` | `DateTime` | |
| `Source` | `DocumentSource` enum | Upload, Share, Import |
| `Notes` | `string?` | |
| `IsFavorite` | `bool` | |
| Navigation | `Pregnancy`, `Visit?`, `DocumentType?`, `Files`, `OcrResults` | |

### API Endpoints WEEK 5.5

```
GET  /api/ocr/{ocrResultId}/review     — Xem extracted data để review
POST /api/ocr/{ocrResultId}/confirm    — Confirm & auto-create entities
```

### Permissions cần thêm

| Permission | Description | Roles |
|------------|-------------|-------|
| `ocr.review` | Xem extracted data để review | USER, DOCTOR, ADMIN |
| `ocr.confirm` | Confirm extraction → auto-create entities | USER, DOCTOR, ADMIN |

### Business Rules

1. Chỉ confirm được OcrResult có `Status = Succeeded` (đã extract xong)
2. Sau confirm: `Status` → `Confirmed` (không confirm lại)
3. PRENATAL_CHECKUP → tạo PrenatalVisit + set VitalsJson + link document (`MedicalDocument.VisitId`)
4. BLOOD_TEST → fuzzy match testName → tạo PrenatalTest(s) cho mỗi lab result + set `PrenatalTest.DocumentId`
5. ULTRASOUND, URINE_TEST, HIV_TEST, HEPATITIS_B_TEST, THYROID_TEST, GLUCOSE_TEST, CBC_TEST, NT_SCAN → direct match → tạo PrenatalTest + set `PrenatalTest.DocumentId`
6. **Others (PRESCRIPTION/VACCINATION_RECORD/MEDICAL_REPORT/OTHER) KHÔNG đi qua confirm flow** — đã DONE tại Phase 1 Upload. HandleNotesOnly giữ lại như defensive fallback.
7. Ownership check: document → pregnancy → userId must match
8. Nếu document không có `documentTypeId` → user phải chọn khi confirm
9. Auto-link: `MedicalDocument.VisitId` ← visit được tạo (nếu có)
10. Auto-link: `PrenatalTest.DocumentId` ← document nguồn (test types, ON DELETE SET NULL)

### Existing RefDocumentType Codes (đã seed sẵn)

| Code | Guid |
|------|------|
| `PRENATAL_CHECKUP` | `b0000001-...-000000000001` |
| `ULTRASOUND` | `b0000001-...-000000000002` |
| `BLOOD_TEST` | `b0000001-...-000000000003` |
| `URINE_TEST` | `b0000001-...-000000000004` |
| `PRESCRIPTION` | `b0000001-...-000000000005` |
| `VACCINATION_RECORD` | `b0000001-...-000000000006` |
| `MEDICAL_REPORT` | `b0000001-...-000000000007` |
| `OTHER` | `b0000001-...-000000000008` |
| `HIV_TEST` | `b0000001-...-000000000009` |
| `HEPATITIS_B_TEST` | `b0000001-...-00000000000a` |
| `THYROID_TEST` | `b0000001-...-00000000000b` |
| `GLUCOSE_TEST` | `b0000001-...-00000000000c` |
| `CBC_TEST` | `b0000001-...-00000000000d` |
| `NT_SCAN` | `b0000001-...-00000000000e` |

### Existing RefTestType Codes (đã seed sẵn)

| Code | Category |
|------|----------|
| `BIOCHEMISTRY` | LAB |
| `ULTRASOUND` | IMAGING |
| `BLOOD_PRESSURE` | OTHER |
| `COMPLETE_BLOOD_COUNT` | LAB |
| `URINE_TEST` | LAB |
| `HEPATITIS_B` | LAB |
| `HIV_SCREEN` | LAB |
| `TSH` | LAB |
| `NT_SCAN` | IMAGING |
| `OGTT` | LAB |
| `BLOOD_TEST` | LAB |
| `CBC_TEST` | LAB |

### IUnitOfWork Repositories Available

```csharp
// Week 1-2
IUserRepository Users;
IUserProfileRepository UserProfiles;
ILanguageRepository Languages;
IRefreshTokenRepository RefreshTokens;
IRoleRepository Roles;
IPermissionRepository Permissions;
IUserRoleRepository UserRoles;

// Week 3
IPregnancyRepository Pregnancies;
IPregnancyConditionRepository PregnancyConditions;
IPrenatalVisitRepository PrenatalVisits;
IPrenatalTestRepository PrenatalTests;
IRefPregnancyConditionRepository RefPregnancyConditions;
IRefTestTypeRepository RefTestTypes;            // Has GetActiveWithTranslationsAsync(langCode, category?, ct)

// Week 4
IStorageFileRepository StorageFiles;
IMedicalDocumentRepository MedicalDocuments;    // Has GetByIdWithDetailsAsync(id, ct)
IDocumentFileRepository DocumentFiles;
IOcrResultRepository OcrResults;
IRefDocumentTypeRepository RefDocumentTypes;

// Week 5
IAiPromptTemplateRepository AiPromptTemplates;
```

### GetByIdWithDetailsAsync — Include Chain (Reference)

```csharp
// MedicalDocumentRepository.GetByIdWithDetailsAsync includes:
.Include(m => m.Files.OrderBy(f => f.SortOrder))
    .ThenInclude(f => f.StorageFile)
.Include(m => m.DocumentType)
    .ThenInclude(dt => dt!.Translations)
.Include(m => m.OcrResults.OrderByDescending(o => o.OcrRunNumber).Take(1))
.Include(m => m.Pregnancy)
```

### BaseApiController Helpers

```csharp
Success<T>(data, message)      → 200 OK
Created<T>(data, message)      → 201 Created
Accepted<T>(data, message)     → 202 Accepted
NoContentResponse()            → 204 No Content
GetCurrentUserId()             → Guid from JWT NameIdentifier claim
```

---

## 📊 DATABASE CHANGES

```sql
-- ══════════════════════════════════════════════════════════════
-- ║  WEEK 5.5 — Auto-Fill: Database Changes                  ║
-- ══════════════════════════════════════════════════════════════

-- 1. Expand OcrStatus enum: thêm 'Confirmed'
-- (Trong C# enum, thêm Confirmed = new value sau Failed)

-- 2. Thêm columns vào ocr_results
ALTER TABLE ocr_results
    ADD COLUMN confirmed_at       DATETIME(6)  NULL AFTER error_message,
    ADD COLUMN confirmed_by       CHAR(36)     NULL AFTER confirmed_at,
    ADD COLUMN confirmed_json     JSON         NULL AFTER confirmed_by,
    ADD COLUMN auto_fill_result   JSON         NULL AFTER confirmed_json;

-- confirmed_json  = dữ liệu user đã review+edit (có thể khác structured_json)
-- auto_fill_result = kết quả auto-fill: {"visitId": "...", "testIds": ["..."], "notes": "..."}

-- 3. Thêm permissions
INSERT INTO permissions (id, permission_name, description) VALUES
    (UUID(), 'ocr.review', 'Review AI extracted data'),
    (UUID(), 'ocr.confirm', 'Confirm extraction and auto-create entities');

-- 4. Gán permissions cho roles (USER, DOCTOR, ADMIN)
```

---

## 🎯 PROMPT 1/7 — Expand OcrStatus Enum + OcrResult Entity

**Nhiệm vụ**: Thêm status `Confirmed` vào `OcrStatus` enum. Thêm 4 fields mới vào `OcrResult` entity.

**⚠️ CRITICAL**:
- `Confirmed` phải là value SAU `Succeeded` và `Failed`
- OcrResult Week 5 đã có AI fields — chỉ thêm confirm fields
- KHÔNG đổi existing values, chỉ append

**Code — Updated OcrStatus Enum**:

```csharp
// File: FPT.EXE201.Domain/Enums/OcrStatus.cs
// ⚠️ REPLACE toàn bộ — thêm Confirmed status

namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái pipeline OCR + AI Extraction.
/// Flow: Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded → Confirmed
/// Bất kỳ bước nào fail → Failed
///
/// ⚠️ MIGRATION NOTE: Week 4 dùng "Processing" → Week 5 rename thành "OcrProcessing".
///    Nếu DB đã có rows với status = "Processing", cần chạy SQL update trước:
///    UPDATE ocr_results SET status = 'OcrProcessing' WHERE status = 'Processing';
/// </summary>
public enum OcrStatus
{
    /// <summary>Đang chờ xử lý.</summary>
    Pending,

    /// <summary>Azure Document Intelligence đang chạy OCR.</summary>
    OcrProcessing,

    /// <summary>OCR hoàn tất, raw text đã có. Chờ AI extraction.</summary>
    OcrCompleted,

    /// <summary>Gemini đang trích xuất structured data từ raw text.</summary>
    AiExtracting,

    /// <summary>Pipeline hoàn tất: cả OCR + AI extraction thành công.</summary>
    Succeeded,

    /// <summary>Pipeline thất bại ở bất kỳ bước nào.</summary>
    Failed,

    /// <summary>WEEK 5.5: User đã review + confirm extracted data → entities created.</summary>
    Confirmed
}
```

**Code — Updated OcrResult Entity** (thêm confirm fields):

```csharp
// File: FPT.EXE201.Domain/Entities/OcrResult.cs
// ⚠️ THÊM 4 properties mới vào cuối — TRƯỚC navigation properties
// KHÔNG xóa fields existing

    // ═══ WEEK 5.5: Confirm & Auto-Fill Fields ═══

    /// <summary>Thời điểm user confirm extracted data.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>User ID đã confirm.</summary>
    public Guid? ConfirmedBy { get; set; }

    /// <summary>JSON dữ liệu user đã review + chỉnh sửa (có thể khác StructuredJson).</summary>
    public string? ConfirmedJson { get; set; }

    /// <summary>JSON kết quả auto-fill: {"visitId":"...","testIds":["..."],"summary":"..."}.</summary>
    public string? AutoFillResultJson { get; set; }
```

**Full OcrResult.cs** sau khi sửa:

```csharp
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
///   2. OCR engine xử lý → status = OcrProcessing
///   3. AI extract → status = AiExtracting
///   4. Thành công → status = Succeeded, lưu raw_text + structured_json
///      Thất bại → status = Failed, lưu error_message
///   5. User confirm → status = Confirmed, lưu confirmed_json + auto_fill_result
///
/// StructuredJson format (MedicalRecordExtractionResult):
///   { "vitalsData": VitalsJsonDto, "overallConfidence": 0.7 }
/// </summary>
public class OcrResult : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int OcrRunNumber { get; set; }
    public OcrStatus Status { get; set; }
    public string? OcrEngine { get; set; }
    public string? LanguageHint { get; set; }
    public string? RawText { get; set; }
    public string? StructuredJson { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? ErrorMessage { get; set; }

    // ═══ Week 5: AI Processing Fields ═══
    public int? OcrProcessingTimeMs { get; set; }
    public string? AiModelUsed { get; set; }
    public int? AiTokensUsed { get; set; }
    public int? AiProcessingTimeMs { get; set; }
    public Guid? AiPromptTemplateId { get; set; }

    // ═══ WEEK 5.5: Confirm & Auto-Fill Fields ═══
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? ConfirmedJson { get; set; }
    public string? AutoFillResultJson { get; set; }

    // ═══ Navigation ═══
    public MedicalDocument Document { get; set; } = null!;
    public AiPromptTemplate? AiPromptTemplate { get; set; }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 2/7 — EF Configuration + Migration

**Nhiệm vụ**: Update `OcrResultConfiguration` (thêm 4 columns), seed permissions, tạo migration.

**Code — Updated OcrResultConfiguration** (thêm vào file existing):

```csharp
// ⚠️ THÊM vào OcrResultConfiguration.cs — trong method Configure(), SAU dòng:
//     builder.Property(o => o.AiPromptTemplateId).HasColumnName("ai_prompt_template_id").HasColumnType("CHAR(36)");

        // WEEK 5.5: Confirm & Auto-Fill columns
        builder.Property(o => o.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("DATETIME(6)");

        builder.Property(o => o.ConfirmedBy)
            .HasColumnName("confirmed_by")
            .HasColumnType("CHAR(36)");

        builder.Property(o => o.ConfirmedJson)
            .HasColumnName("confirmed_json")
            .HasColumnType("JSON");

        builder.Property(o => o.AutoFillResultJson)
            .HasColumnName("auto_fill_result")
            .HasColumnType("JSON");
```

**Seed Permissions** — thêm vào `DatabaseSeeder.cs`:

```csharp
// ⚠️ Thêm 2 dòng permission, sau dòng:
//   await SeedPermissionIfNotExists(context, "ai.admin", "AI Admin", "Admin can manage AI prompt templates");

await SeedPermissionIfNotExists(context, "ocr.review", "Review OCR Extraction", "User can review AI-extracted data before confirming");
await SeedPermissionIfNotExists(context, "ocr.confirm", "Confirm OCR Extraction", "User can confirm extraction and auto-create entities");

// ⚠️ Thêm vào mảng userPermissionCodes (đã có "ocr.trigger", "ocr.view"):
var userPermissionCodes = new[]
{
    // ... existing ...
    "ocr.trigger", "ocr.view",
    "ocr.review", "ocr.confirm",    // ← THÊM
    // ... existing ...
};

// ⚠️ Thêm vào mảng doctorPermissionCodes (đã có "ocr.trigger", "ocr.view"):
var doctorPermissionCodes = new[]
{
    // ... existing ...
    "ocr.trigger", "ocr.view",
    "ocr.review", "ocr.confirm",    // ← THÊM
    // ... existing ...
};

// Admin đã tự động có tất cả permissions → không cần thêm.
```

**Migration Command**:

```bash
cd src/FPT.EXE201.Api
dotnet ef migrations add AddAutoFillConfirmFields --project ../FPT.EXE201.Infrastructure
dotnet ef database update
```

**✅ Checkpoint**: Migration tạo thành công, database updated, build OK.

---

## 🎯 PROMPT 3/7 — DTOs (Request + Response)

**Nhiệm vụ**: Tạo 3 DTOs cho auto-fill flow: Review response, Confirm request, Result response.

**⚠️ CRITICAL**:
- `ExtractionReviewDto` = response class → dùng `class` với `{ get; set; }`
- `ConfirmExtractionDto` = input DTO từ `[FromBody]` → dùng `record` positional (FluentValidation sẽ chạy)
- `AutoFillResultDto` = response class → dùng `class` với `{ get; set; }`
- VitalsData từ StructuredJson ĐÃ LÀ VitalsJsonDto — parse trực tiếp, KHÔNG cần mapping thủ công

**Code — ExtractionReviewDto**:

```csharp
// File: FPT.EXE201.Application/DTOs/AutoFill/ExtractionReviewDto.cs

using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Response trả về dữ liệu AI đã extract cho user review.
/// Flutter sẽ hiển thị form pre-filled từ data này.
/// User edit rồi gửi lại qua ConfirmExtractionDto.
/// </summary>
public class ExtractionReviewDto
{
    public Guid OcrResultId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid PregnancyId { get; set; }

    /// <summary>Document type code: PRENATAL_CHECKUP, BLOOD_TEST, etc.</summary>
    public string? DocumentTypeCode { get; set; }

    /// <summary>Document type display name (theo ngôn ngữ).</summary>
    public string? DocumentTypeDisplayName { get; set; }

    /// <summary>OCR status hiện tại.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Confidence score từ OCR (0-100%).</summary>
    public decimal? ConfidenceScore { get; set; }

    // ═══ Extracted Data (parsed từ StructuredJson → MedicalRecordExtractionResult) ═══

    /// <summary>
    /// Dữ liệu VitalsJson (cho PRENATAL_CHECKUP).
    /// Parse trực tiếp từ MedicalRecordExtractionResult.VitalsData — ĐÃ LÀ VitalsJsonDto.
    /// Null nếu AI không extract được.
    /// </summary>
    public VitalsJsonDto? Vitals { get; set; }

    /// <summary>Độ tin cậy tổng thể của AI extraction (0.0 - 1.0).</summary>
    public double? OverallConfidence { get; set; }

    /// <summary>Raw StructuredJson (cho debug/advanced users).</summary>
    public string? RawStructuredJson { get; set; }

    /// <summary>Có thể auto-fill hay không (dựa vào documentType + extraction quality).</summary>
    public bool CanAutoFill { get; set; }

    /// <summary>Lý do không thể auto-fill (nếu CanAutoFill = false).</summary>
    public string? CannotAutoFillReason { get; set; }
}
```

**Code — ConfirmExtractionDto**:

```csharp
// File: FPT.EXE201.Application/DTOs/AutoFill/ConfirmExtractionDto.cs

using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Request body khi user confirm extracted data.
/// Chứa dữ liệu user đã review + chỉnh sửa.
/// Gửi từ Flutter sau khi user xem ExtractionReviewDto và edit.
/// </summary>
public record ConfirmExtractionDto(
    /// <summary>
    /// Loại document (bắt buộc nếu ban đầu chưa chọn).
    /// Xác định strategy auto-fill: PRENATAL_CHECKUP → Visit, BLOOD_TEST → Test.
    /// </summary>
    Guid DocumentTypeId,

    /// <summary>Ngày khám/xét nghiệm (user có thể chỉnh lại từ extracted date).</summary>
    DateOnly EventDate,

    /// <summary>Gắn vào Visit có sẵn (nullable). Nếu null → tạo Visit mới (cho PRENATAL_CHECKUP).</summary>
    Guid? ExistingVisitId,

    /// <summary>
    /// VitalsJson đã chỉnh sửa (cho PRENATAL_CHECKUP).
    /// Null nếu document không phải checkup.
    /// </summary>
    VitalsJsonDto? Vitals,

    /// <summary>Tên cơ sở y tế (user có thể chỉnh).</summary>
    string? Location,

    /// <summary>Ghi chú user muốn lưu vào Visit/Test.</summary>
    string? Notes
);

```

**Code — AutoFillResultDto**:

```csharp
// File: FPT.EXE201.Application/DTOs/AutoFill/AutoFillResultDto.cs

namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Response sau khi confirm: tóm tắt entities đã được tạo.
/// </summary>
public class AutoFillResultDto
{
    /// <summary>OcrResult ID đã confirm.</summary>
    public Guid OcrResultId { get; set; }

    /// <summary>Document type đã dùng để auto-fill.</summary>
    public string DocumentTypeCode { get; set; } = "";

    /// <summary>Visit ID đã tạo hoặc đã link (cho PRENATAL_CHECKUP). Null nếu không tạo visit.</summary>
    public Guid? CreatedVisitId { get; set; }

    /// <summary>Danh sách Test IDs đã tạo (cho BLOOD_TEST, URINE_TEST, ULTRASOUND).</summary>
    public List<Guid> CreatedTestIds { get; set; } = new();

    /// <summary>Document đã được auto-link vào visit hay chưa.</summary>
    public bool DocumentLinkedToVisit { get; set; }

    /// <summary>Tóm tắt cho user (hiển thị snackbar/toast).</summary>
    public string Summary { get; set; } = "";
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 4/7 — Validation

**Nhiệm vụ**: Tạo FluentValidation cho `ConfirmExtractionDto`.

**Code — ConfirmExtractionDtoValidator**:

```csharp
// File: FPT.EXE201.Application/Validations/AutoFill/ConfirmExtractionDtoValidator.cs

using FluentValidation;
using FPT.EXE201.Application.DTOs.AutoFill;

namespace FPT.EXE201.Application.Validations.AutoFill;

public class ConfirmExtractionDtoValidator : AbstractValidator<ConfirmExtractionDto>
{
    public ConfirmExtractionDtoValidator()
    {
        RuleFor(x => x.DocumentTypeId)
            .NotEmpty()
            .WithMessage("Document type is required.");

        RuleFor(x => x.EventDate)
            .NotEmpty()
            .WithMessage("Event date is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date cannot be in the future.");

        RuleFor(x => x.Location)
            .MaximumLength(255)
            .WithMessage("Location must not exceed 255 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters.");

    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 5/7 — IAutoFillService Interface

**Nhiệm vụ**: Tạo interface cho AutoFill service.

**Code — IAutoFillService**:

```csharp
// File: FPT.EXE201.Application/IServices/IAutoFillService.cs

using FPT.EXE201.Application.DTOs.AutoFill;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý "Review & Confirm" flow:
/// 1. ReviewAsync: parse StructuredJson → ExtractionReviewDto (cho FE render form)
/// 2. ConfirmAsync: nhận user-edited data → auto-create PrenatalVisit/Test
/// </summary>
public interface IAutoFillService
{
    /// <summary>
    /// Lấy dữ liệu AI đã extract, parse thành review form.
    /// Chỉ cho phép khi Status = Succeeded.
    /// </summary>
    Task<ExtractionReviewDto> ReviewAsync(
        Guid ocrResultId, Guid currentUserId,
        string langCode = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// User confirm extracted data → auto-create entities dựa vào document type.
    /// Chỉ cho phép khi Status = Succeeded (chưa confirm trước đó).
    /// </summary>
    Task<AutoFillResultDto> ConfirmAsync(
        Guid ocrResultId, ConfirmExtractionDto dto,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 6/7 — AutoFillService Implementation

**Nhiệm vụ**: Implement `AutoFillService` — core logic auto-fill dựa trên document type.

**⚠️ CRITICAL NOTES**:
- Parse `OcrResult.StructuredJson` → `MedicalRecordExtractionResult` (model đã có trong `Application/AI/ExtractionModels/`)
- `MedicalRecordExtractionResult.VitalsData` **ĐÃ LÀ** `VitalsJsonDto` → KHÔNG cần mapping thủ công
- Dùng `GetByIdTrackedAsync` khi cần UPDATE entity (OcrResult, PrenatalVisit)
- Dùng `GetByIdAsync` khi chỉ READ (RefDocumentType, RefTestType)
- Dùng `DocumentType.Code` để xác định auto-fill strategy
- All changes saved in 1 `SaveChangesAsync` call
- Ownership check: document → pregnancy → userId

**Code — AutoFillService**:

```csharp
// File: FPT.EXE201.Application/Services/AutoFillService.cs

using System.Text.Json;
using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.DTOs.AutoFill;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Application.Services;

public class AutoFillService : IAutoFillService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AutoFillService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Document type codes → auto-fill strategy
    private static readonly HashSet<string> VisitCreatingTypes = new() { "PRENATAL_CHECKUP" };
    private static readonly HashSet<string> TestCreatingTypes = new()
    {
        "BLOOD_TEST", "URINE_TEST", "ULTRASOUND",
        "HIV_TEST", "HEPATITIS_B_TEST", "THYROID_TEST",
        "GLUCOSE_TEST", "CBC_TEST", "NT_SCAN"
    };
    private static readonly HashSet<string> NotesOnlyTypes = new()
    {
        "PRESCRIPTION", "VACCINATION_RECORD", "MEDICAL_REPORT", "OTHER"
    };

    // Mapping: DocumentType code → RefTestType code (direct match)
    private static readonly Dictionary<string, string> DocTypeToTestTypeCode = new()
    {
        ["BLOOD_TEST"] = "BLOOD_TEST",
        ["ULTRASOUND"] = "ULTRASOUND",
        ["URINE_TEST"] = "URINE_TEST",
        ["HIV_TEST"] = "HIV_SCREEN",
        ["HEPATITIS_B_TEST"] = "HEPATITIS_B",
        ["THYROID_TEST"] = "TSH",
        ["GLUCOSE_TEST"] = "OGTT",
        ["CBC_TEST"] = "CBC_TEST",
        ["NT_SCAN"] = "NT_SCAN",
    };

    public AutoFillService(IUnitOfWork unitOfWork, ILogger<AutoFillService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ═══════════════════════════════════════
    // ReviewAsync — Parse + return review form
    // ═══════════════════════════════════════

    public async Task<ExtractionReviewDto> ReviewAsync(
        Guid ocrResultId, Guid currentUserId,
        string langCode = "vi",
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy OcrResult (read-only — chỉ đọc, không cần tracking)
        var ocrResult = await _unitOfWork.OcrResults.GetByIdAsync(
            ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        // 2. Lấy Document + Pregnancy (ownership check)
        //    GetByIdWithDetailsAsync includes: Files, DocumentType + Translations, OcrResults (latest 1), Pregnancy
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(
            ocrResult.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        // 3. Check status
        if (ocrResult.Status == OcrStatus.Confirmed)
            throw new BadRequestException("This extraction has already been confirmed.");

        if (ocrResult.Status != OcrStatus.Succeeded)
            throw new BadRequestException(
                $"Cannot review extraction when status is '{ocrResult.Status}'. Must be 'Succeeded'.");

        // 4. Parse StructuredJson → MedicalRecordExtractionResult
        //    VitalsData ĐÃ LÀ VitalsJsonDto — không cần mapping
        var extraction = ParseStructuredJson(ocrResult.StructuredJson);

        // 5. Get document type info (already loaded via GetByIdWithDetailsAsync includes)
        string? docTypeCode = null;
        string? docTypeDisplayName = null;
        if (document.DocumentType != null)
        {
            docTypeCode = document.DocumentType.Code;
            var translation = document.DocumentType.Translations
                ?.FirstOrDefault(t => t.LanguageCode == langCode);
            docTypeDisplayName = translation?.Name ?? document.DocumentType.Code;
        }

        // 6. Build review DTO
        var review = new ExtractionReviewDto
        {
            OcrResultId = ocrResultId,
            DocumentId = document.Id,
            PregnancyId = document.PregnancyId,
            DocumentTypeCode = docTypeCode,
            DocumentTypeDisplayName = docTypeDisplayName,
            Status = ocrResult.Status.ToString(),
            ConfidenceScore = ocrResult.ConfidenceScore,
            RawStructuredJson = ocrResult.StructuredJson,
        };

        if (extraction != null)
        {
            review.OverallConfidence = extraction.OverallConfidence;

            // ⚠️ VitalsData ĐÃ LÀ VitalsJsonDto — assign trực tiếp
            // Chỉ trả vitals cho PRENATAL_CHECKUP (tạo Visit)
            if (docTypeCode == null || VisitCreatingTypes.Contains(docTypeCode))
            {
                review.Vitals = extraction.VitalsData;
            }

            // Lab results → không còn dùng, tất cả test types đều dùng direct mapping
        }

        // 7. Determine if auto-fill is possible
        review.CanAutoFill = DetermineCanAutoFill(review, docTypeCode);
        if (!review.CanAutoFill)
        {
            review.CannotAutoFillReason = GetCannotAutoFillReason(review, docTypeCode);
        }

        return review;
    }

    // ═══════════════════════════════════════
    // ConfirmAsync — Create entities
    // ═══════════════════════════════════════

    public async Task<AutoFillResultDto> ConfirmAsync(
        Guid ocrResultId, ConfirmExtractionDto dto,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy OcrResult — PHẢI dùng GetByIdTrackedAsync vì sẽ UPDATE status
        var ocrResult = await _unitOfWork.OcrResults.GetByIdTrackedAsync(
            ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        if (ocrResult.Status != OcrStatus.Succeeded)
            throw new BadRequestException(ocrResult.Status == OcrStatus.Confirmed
                ? "This extraction has already been confirmed."
                : $"Can only confirm when status is 'Succeeded'. Current: '{ocrResult.Status}'.");

        // 2. Lấy Document + Pregnancy (ownership check)
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(
            ocrResult.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        // 3. Validate DocumentTypeId
        var docType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
            dto.DocumentTypeId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Document type not found.");

        // 4. Update document's DocumentTypeId nếu chưa có hoặc khác
        if (!document.DocumentTypeId.HasValue || document.DocumentTypeId != dto.DocumentTypeId)
        {
            document.DocumentTypeId = dto.DocumentTypeId;
            _unitOfWork.MedicalDocuments.Update(document);
        }

        // 5. Execute strategy dựa vào document type code
        var result = new AutoFillResultDto
        {
            OcrResultId = ocrResultId,
            DocumentTypeCode = docType.Code
        };

        if (VisitCreatingTypes.Contains(docType.Code))
        {
            await HandlePrenatalCheckup(document, dto, result, cancellationToken);
        }
        else if (TestCreatingTypes.Contains(docType.Code))
        {
            await HandleTestCreation(document, dto, docType.Code, result, cancellationToken);
        }
        else
        {
            // PRESCRIPTION, VACCINATION_RECORD, MEDICAL_REPORT, OTHER
            HandleNotesOnly(document, dto, result);
        }

        // 6. Update OcrResult → Confirmed
        //    Entity đã tracked từ GetByIdTrackedAsync — chỉ set properties
        ocrResult.Status = OcrStatus.Confirmed;
        ocrResult.ConfirmedAt = DateTime.UtcNow;
        ocrResult.ConfirmedBy = currentUserId;
        ocrResult.ConfirmedJson = JsonSerializer.Serialize(dto, JsonOptions);
        ocrResult.AutoFillResultJson = JsonSerializer.Serialize(result, JsonOptions);

        // 7. Save all changes in one transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-fill confirmed for OcrResult {OcrId}, DocType {DocType}. " +
            "Visit: {VisitId}, Tests: {TestCount}",
            ocrResultId, docType.Code, result.CreatedVisitId, result.CreatedTestIds.Count);

        return result;
    }

    // ═══════════════════════════════════════
    // Strategy: PRENATAL_CHECKUP → Visit
    // ═══════════════════════════════════════

    private async Task HandlePrenatalCheckup(
        MedicalDocument document, ConfirmExtractionDto dto,
        AutoFillResultDto result, CancellationToken cancellationToken)
    {
        PrenatalVisit visit;

        if (dto.ExistingVisitId.HasValue)
        {
            // Link vào visit có sẵn — dùng TrackedAsync vì sẽ update
            visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(
                dto.ExistingVisitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Visit not found.");

            if (visit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Visit does not belong to this pregnancy.");

            // Update vitals nếu có
            if (dto.Vitals != null)
            {
                visit.VitalsJson = JsonSerializer.Serialize(dto.Vitals, JsonOptions);
                visit.Location = dto.Location ?? visit.Location;
                visit.Notes = CombineNotes(visit.Notes, dto.Notes);
                // Entity tracked → EF auto-detects changes
            }
        }
        else
        {
            // Tạo Visit mới
            visit = new PrenatalVisit
            {
                PregnancyId = document.PregnancyId,
                VisitDate = dto.EventDate,          // DateOnly
                VisitType = VisitType.Routine,
                Location = dto.Location,
                Notes = dto.Notes,
                VitalsJson = dto.Vitals != null
                    ? JsonSerializer.Serialize(dto.Vitals, JsonOptions)
                    : null
            };
            await _unitOfWork.PrenatalVisits.AddAsync(visit, cancellationToken);
        }

        // Auto-link document → visit
        document.VisitId = visit.Id;
        _unitOfWork.MedicalDocuments.Update(document);

        result.CreatedVisitId = visit.Id;
        result.DocumentLinkedToVisit = true;
        result.Summary = dto.ExistingVisitId.HasValue
            ? $"Updated prenatal visit for {dto.EventDate:dd/MM/yyyy}."
            : $"Created prenatal visit for {dto.EventDate:dd/MM/yyyy}.";
    }

    // ═══════════════════════════════════════
    // Strategy: Test-creating document types → Test(s)
    // ═══════════════════════════════════════

    private async Task HandleTestCreation(
        MedicalDocument document, ConfirmExtractionDto dto,
        string docTypeCode, AutoFillResultDto result,
        CancellationToken cancellationToken)
    {
        Guid? visitId = dto.ExistingVisitId;

        if (visitId.HasValue)
        {
            // Validate visit nếu FE truyền lên
            var existingVisit = await _unitOfWork.PrenatalVisits.GetByIdAsync(
                visitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Visit not found.");
            if (existingVisit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Visit does not belong to this pregnancy.");
        }
        else
        {
            // Không có visit → auto-create một Routine visit
            var newVisit = new PrenatalVisit
            {
                PregnancyId = document.PregnancyId,
                VisitDate = dto.EventDate,
                VisitType = VisitType.Routine,
                Location = dto.Location,
                Notes = dto.Notes,
            };
            await _unitOfWork.PrenatalVisits.AddAsync(newVisit, cancellationToken);
            visitId = newVisit.Id;
            result.CreatedVisitId = newVisit.Id;
        }

        // All test types use direct mapping → 1 PrenatalTest
        if (!DocTypeToTestTypeCode.TryGetValue(docTypeCode, out var directTestTypeCode))
        {
            result.Summary = $"No direct test type mapping for '{docTypeCode}'.";
            document.VisitId = visitId;
            _unitOfWork.MedicalDocuments.Update(document);
            result.DocumentLinkedToVisit = true;
            return;
        }

        var testType = await FindTestTypeByCode(directTestTypeCode, cancellationToken);

        var test = new PrenatalTest
        {
            PregnancyId = document.PregnancyId,
            VisitId = visitId,
            TestTypeId = testType.Id,
            TestDate = dto.EventDate,
            Notes = dto.Notes,
            IsAbnormalResult = false,
            ImageUrlsJson = BuildImageUrlsJson(document),
            DocumentId = document.Id
        };
        await _unitOfWork.PrenatalTests.AddAsync(test, cancellationToken);
        result.CreatedTestIds.Add(test.Id);
        result.Summary = $"Created {directTestTypeCode} test result for {dto.EventDate:dd/MM/yyyy}.";

        // Always link document → visit
        document.VisitId = visitId;
        _unitOfWork.MedicalDocuments.Update(document);
        result.DocumentLinkedToVisit = true;
    }

    // ═══════════════════════════════════════
    // Strategy: Notes-only (PRESCRIPTION, VACCINATION, etc.)
    // ═══════════════════════════════════════

    private void HandleNotesOnly(
        MedicalDocument document, ConfirmExtractionDto dto,
        AutoFillResultDto result)
    {
        bool updated = false;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            document.Notes = CombineNotes(document.Notes, dto.Notes);
            updated = true;
        }

        if (dto.EventDate != default && !document.DocumentDate.HasValue)
        {
            document.DocumentDate = dto.EventDate;  // DateOnly
            updated = true;
        }

        if (updated)
            _unitOfWork.MedicalDocuments.Update(document);

        result.Summary = "Document notes updated.";
    }

    // ═══════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════

    /// <summary>Parse StructuredJson → MedicalRecordExtractionResult.</summary>
    private MedicalRecordExtractionResult? ParseStructuredJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<MedicalRecordExtractionResult>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse StructuredJson");
            return null;
        }
    }

    private bool DetermineCanAutoFill(ExtractionReviewDto review, string? docTypeCode)
    {
        if (review.Status != OcrStatus.Succeeded.ToString()) return false;
        if (string.IsNullOrEmpty(docTypeCode)) return false;

        // Notes-only types luôn có thể "auto-fill" (chỉ save notes)
        if (NotesOnlyTypes.Contains(docTypeCode)) return true;

        // Test types luôn có thể auto-fill
        if (TestCreatingTypes.Contains(docTypeCode)) return true;

        // PRENATAL_CHECKUP — BE tự lấy vitals từ StructuredJson nếu FE không gửi
        if (docTypeCode == "PRENATAL_CHECKUP") return true;

        return review.OverallConfidence >= 0.3;
    }

    private string? GetCannotAutoFillReason(ExtractionReviewDto review, string? docTypeCode)
    {
        if (review.Status != OcrStatus.Succeeded.ToString())
            return "Extraction process has not completed.";
        if (string.IsNullOrEmpty(docTypeCode))
            return "Please select a document type before confirming.";
        return "Extracted data quality is insufficient.";
    }

    private async Task<RefTestType> FindTestTypeByCode(string code, CancellationToken cancellationToken)
    {
        var allTypes = await _unitOfWork.RefTestTypes
            .GetActiveWithTranslationsAsync("vi", null, cancellationToken);
        return allTypes.FirstOrDefault(t => t.Code == code)
            ?? throw new NotFoundException($"Test type '{code}' not found in seed data.");
    }

    private static string? BuildImageUrlsJson(MedicalDocument document)
    {
        var urls = document.Files?
            .OrderBy(f => f.SortOrder)
            .Select(f => f.StorageFile?.PublicUrl)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToArray();
        if (urls == null || urls.Length == 0) return null;
        return JsonSerializer.Serialize(urls, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string? CombineNotes(string? existing, string? additional)
    {
        if (string.IsNullOrWhiteSpace(additional)) return existing;
        if (string.IsNullOrWhiteSpace(existing)) return additional;
        return $"{existing}\n---\n{additional}";
    }
}
```

**⚠️ KEY POINTS Summary**:
1. `extraction.VitalsData` **ĐÃ LÀ** `VitalsJsonDto` → assign trực tiếp `review.Vitals = extraction.VitalsData`
2. KHÔNG cần `MapExtractionToVitals()` helper — data đã đúng format
3. `GetByIdTrackedAsync` cho OcrResult (ConfirmAsync), PrenatalVisit (khi update existing)
4. `GetByIdAsync` cho RefDocumentType, RefTestType, PrenatalVisit (read-only validate)
5. `GetByIdWithDetailsAsync` cho MedicalDocument (includes Pregnancy, Files, DocumentType.Translations)

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 7/7 — Controller + DI Registration + Permissions + Updated OcrResultDto

**Nhiệm vụ**: Tạo controller, đăng ký DI, seed permissions, update DTO.

**Code — AutoFillController**:

```csharp
// File: FPT.EXE201.Api/Controllers/AutoFillController.cs

using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.AutoFill;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/ocr")]
public class AutoFillController : BaseApiController
{
    private readonly IAutoFillService _autoFillService;

    public AutoFillController(IAutoFillService autoFillService)
    {
        _autoFillService = autoFillService;
    }

    /// <summary>
    /// Xem dữ liệu AI đã extract — cho user review trước khi confirm.
    /// Trả về ExtractionReviewDto với form data pre-filled.
    /// </summary>
    [HttpGet("{ocrResultId}/review")]
    [RequirePermission("ocr.review")]
    public async Task<IActionResult> Review(
        Guid ocrResultId,
        [FromQuery] string lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _autoFillService.ReviewAsync(ocrResultId, userId, lang, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// Confirm extracted data → auto-create PrenatalVisit/Test.
    /// User gửi dữ liệu đã review + chỉnh sửa.
    /// </summary>
    [HttpPost("{ocrResultId}/confirm")]
    [RequirePermission("ocr.confirm")]
    public async Task<IActionResult> Confirm(
        Guid ocrResultId,
        [FromBody] ConfirmExtractionDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _autoFillService.ConfirmAsync(ocrResultId, dto, userId, cancellationToken);
        return Created(result, result.Summary);
    }
}
```

**DI Registration** — thêm vào `DependencyInjection.cs`:

```csharp
// ⚠️ THÊM vào Application/DependencyInjection.cs, sau dòng:
//   services.AddScoped<IMedicalRecordAiService, MedicalRecordAiService>();

        // Week 5.5 — Auto-Fill
        services.AddScoped<IAutoFillService, AutoFillService>();
```

**Seed Permissions** — thêm vào `DatabaseSeeder.cs`:

```csharp
// 1️⃣ Thêm 2 dòng permission mới, sau dòng:
//    await SeedPermissionIfNotExists(context, "ai.admin", "AI Admin", "Admin can manage AI prompt templates");

await SeedPermissionIfNotExists(context, "ocr.review", "Review OCR Extraction", "User can review AI-extracted data before confirming");
await SeedPermissionIfNotExists(context, "ocr.confirm", "Confirm OCR Extraction", "User can confirm extraction and auto-create entities");

// 2️⃣ Thêm vào mảng userPermissionCodes (đã có "ocr.trigger", "ocr.view"):
var userPermissionCodes = new[]
{
    // ... giữ nguyên các dòng cũ ...
    "ocr.trigger", "ocr.view",
    "ocr.review", "ocr.confirm",    // ← THÊM 2 dòng này
    // ... giữ nguyên phần còn lại ...
};

// 3️⃣ Thêm vào mảng doctorPermissionCodes (đã có "ocr.trigger", "ocr.view"):
var doctorPermissionCodes = new[]
{
    // ... giữ nguyên các dòng cũ ...
    "ocr.trigger", "ocr.view",
    "ocr.review", "ocr.confirm",    // ← THÊM 2 dòng này
    // ... giữ nguyên phần còn lại ...
};

// Admin tự động có ALL permissions → không cần thêm.
```

**Update OcrResultDto** — thêm WEEK 5.5 fields:

```csharp
// ⚠️ THÊM vào cuối class OcrResultDto
// File: FPT.EXE201.Application/DTOs/MedicalDocuments/OcrResultDto.cs
// SAU dòng:   public DateTime UpdatedAt { get; set; }

    // WEEK 5.5: Confirm fields
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? ConfirmedJson { get; set; }
    public string? AutoFillResultJson { get; set; }
```

**✅ Checkpoint**: Build thành công, tất cả endpoints hoạt động.

---

## 📊 API FLOW — Hướng dẫn test

### Flow hoàn chỉnh (End-to-End):

```
1. Upload document (Week 4):
   POST /api/pregnancies/{id}/documents
   → documentId
   → PRENATAL_CHECKUP: auto-queue OCR (non-blocking, background processing)
   → Test types: NO OCR
   → Others: DONE (archive only, no confirm needed)
   → Response trả về ngay (<1s)

2. OCR + AI extract (Week 5) — PRENATAL_CHECKUP ONLY:
   → OcrBackgroundService chạy ngầm (10-30s)
   → Flutter polls GET /api/ocr/{id}/status mỗi 3-5s
   → Pending → OcrProcessing → AiExtracting → Succeeded
   → OcrResult.StructuredJson = {vitalsData: {...}, overallConfidence: 0.7}

3. Review (WEEK 5.5):
   GET /api/ocr/{ocrResultId}/review
     → ExtractionReviewDto {
         vitals: VitalsJsonDto (1:1 từ AI extraction, chỉ cho PRENATAL_CHECKUP),
         canAutoFill: true/false,
         overallConfidence: 0.7,
         ...
       }
     → Flutter hiển thị form pre-filled để user review/edit

4. Confirm & auto-fill (WEEK 5.5):
   POST /api/ocr/{ocrResultId}/confirm
   Body: ConfirmExtractionDto {
     documentTypeId, eventDate,
     vitals: VitalsJsonDto (edited),       // cho PRENATAL_CHECKUP
     existingVisitId?, location?, notes?
   }
   → AutoFillResultDto { createdVisitId, createdTestIds, summary }
   → OcrResult.Status = "Confirmed"

5. Verify created entities:
   GET /api/pregnancies/{id}/visits     → new visit appears (with VitalsJson)
   GET /api/pregnancies/{id}/tests      → new tests appear (with DocumentId linked)
   GET /api/documents/{id}              → visitId linked
```

### Test Scenarios:

| # | Scenario | DocumentType | Expected Result |
|---|----------|-------------|-----------------|
| 1 | Phiếu khám thai → auto-create Visit | PRENATAL_CHECKUP | PrenatalVisit created + VitalsJson + DocumentLinked |
| 2 | Kết quả xét nghiệm máu → auto-create Tests | BLOOD_TEST | PrenatalTest(s) created + DocumentId linked |
| 3 | Kết quả siêu âm → auto-create Test | ULTRASOUND | 1 PrenatalTest (IMAGING) created + DocumentId linked |
| 4 | Xét nghiệm nước tiểu → auto-create Test | URINE_TEST | PrenatalTest created + DocumentId linked |
| 5 | Đơn thuốc → defensive fallback | PRESCRIPTION | Document notes updated, NO entities created |
| 6 | Link vào visit có sẵn | Any + existingVisitId | Document linked, visit updated (not duplicated) |
| 7 | Confirm lần 2 | Any | 400 → "This extraction has already been confirmed." |
| 8 | Status chưa Succeeded | Pending/Failed | 400 → "Can only confirm when status is Succeeded." |
| 9 | Document user khác | Any | 403 Forbidden |
| 10 | Delete document sau confirm | Test types | PrenatalTest.DocumentId = NULL (ON DELETE SET NULL), Test preserved |

---

## 📊 MAPPING REFERENCE

### StructuredJson → Review DTO (KHÔNG cần mapping thủ công)

```
MedicalRecordExtractionResult          ExtractionReviewDto
─────────────────────────────          ────────────────────
.VitalsData (VitalsJsonDto)       →    .Vitals (trực tiếp, 1:1, chỉ PRENATAL_CHECKUP)
.OverallConfidence                →    .OverallConfidence
```

**⚠️ VitalsData ĐÃ LÀ VitalsJsonDto** — chứa đầy đủ:
- `generalInfo.facility`, `generalInfo.fullName`, `generalInfo.dateOfBirth`, ...
- `interview.gestationalWeek`, `interview.reasonForVisit`, ...
- `examination.vitalSigns.bloodPressureSystolic`, `examination.vitalSigns.weightKg`, ...
- `examination.obstetric.fetalHeartRateBpm`, `examination.obstetric.fundusHeightCm`, ...
- `diagnosis.text`, `treatmentPlan.medication`, `nextAppointment.date`, ...

### DocumentType → RefTestType Mapping

```
RefDocumentType.Code    →    RefTestType.Code         Category     Match Type
─────────────────────        ──────────────────        ────────     ──────────
BLOOD_TEST              →    BLOOD_TEST               LAB          Direct
ULTRASOUND              →    ULTRASOUND               IMAGING      Direct
URINE_TEST              →    URINE_TEST               LAB          Direct
HIV_TEST                →    HIV_SCREEN               LAB          Direct
HEPATITIS_B_TEST        →    HEPATITIS_B              LAB          Direct
THYROID_TEST            →    TSH                      LAB          Direct
GLUCOSE_TEST            →    OGTT                     LAB          Direct
CBC_TEST                →    CBC_TEST                 LAB          Direct
NT_SCAN                 →    NT_SCAN                  IMAGING      Direct
PRENATAL_CHECKUP        →    (tạo Visit, không Test)  N/A          Strategy
PRESCRIPTION            →    (notes only)             N/A          —
VACCINATION_RECORD      →    (notes only)             N/A          —
MEDICAL_REPORT          →    (notes only)             N/A          —
OTHER                   →    (notes only)             N/A          —
```

---

## ⚠️ EF CORE TRACKING RULES (Quan trọng!)

```
┌──────────────────────────────────────────────────────────────────┐
│  RULE: Dùng đúng method cho đúng use case                       │
│                                                                  │
│  GetByIdAsync(...)          → AsNoTracking() → CHỈ ĐỌC          │
│  GetByIdTrackedAsync(...)   → WITH tracking → CHO UPDATE         │
│                                                                  │
│  ⚠️  Nếu dùng GetByIdAsync rồi set properties + SaveChanges     │
│      → EF KHÔNG BIẾT entity đã thay đổi → KHÔNG LƯU GÌ!        │
│                                                                  │
│  ⚠️  Nếu entity ĐÃ tracked (từ GetByIdTrackedAsync),            │
│      KHÔNG gọi _unitOfWork.*.Update(entity)                     │
│      → Causes tracking conflict nếu related entities cũng load  │
│      → Chỉ set properties + SaveChangesAsync                    │
│                                                                  │
│  ⚠️  GetByIdWithDetailsAsync (MedicalDocument) loads nhiều       │
│      navigations → khi cần update document, gọi Update()        │
│      vì entity có thể không tracked (check implementation)      │
└──────────────────────────────────────────────────────────────────┘
```

---

## ⚠️ KNOWN LIMITATIONS (MVP)

1. **Fuzzy matching** test names dựa vào Vietnamese keywords — có thể miss edge cases
2. **Không rollback** nếu user muốn hủy confirm → cần manual delete entities
3. **1 document = 1 confirm** — không support partial confirm
4. **VitalsJsonDto** đã map 1:1 từ AI → không cần thêm mapping layer
5. **Không auto-detect documentType** từ AI — phụ thuộc user chọn hoặc đã set khi upload
6. **PRESCRIPTION → chưa tạo entity thuốc** — chỉ lưu notes (future: Medication entity)
7. **VACCINATION_RECORD → chưa tạo entity tiêm chủng** — chỉ lưu notes (future: Vaccination entity)

---

## 📋 CHECKLIST

- [ ] OcrStatus enum expanded (thêm `Confirmed`)
- [ ] OcrResult entity + config (4 fields mới: ConfirmedAt, ConfirmedBy, ConfirmedJson, AutoFillResultJson)
- [ ] Migration created + applied
- [ ] Permissions seeded (ocr.review, ocr.confirm) + gán cho USER, DOCTOR
- [ ] ExtractionReviewDto created (DTOs/AutoFill/)
- [ ] ConfirmExtractionDto created (DTOs/AutoFill/)
- [ ] AutoFillResultDto created (DTOs/AutoFill/)
- [ ] ConfirmExtractionDtoValidator created (English messages)
- [ ] IAutoFillService interface created
- [ ] AutoFillService implemented — dùng `GetByIdTrackedAsync` cho UPDATE
- [ ] AutoFillService.VitalsData is VitalsJsonDto — NO manual mapping
- [ ] AutoFillService sets `PrenatalTest.DocumentId` in HandleTestCreation
- [ ] AutoFillController created (GET review, POST confirm)
- [ ] DI registration `IAutoFillService → AutoFillService`
- [ ] OcrResultDto updated (4 fields: ConfirmedAt, ConfirmedBy, ConfirmedJson, AutoFillResultJson)
- [ ] Build succeeded (0 warnings, 0 errors)
- [ ] Test: PRENATAL_CHECKUP → Visit created + VitalsJson + DocumentLinked
- [ ] Test: BLOOD_TEST → Test(s) created + DocumentId linked
- [ ] Test: ULTRASOUND → Test created + DocumentId linked
- [ ] Test: PRESCRIPTION → notes only (defensive fallback)
- [ ] Test: Double confirm → 400 error
- [ ] Test: Wrong user → 403 error
- [ ] Test: Delete document after confirm → PrenatalTest.DocumentId = NULL, Test preserved
