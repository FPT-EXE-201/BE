# 📋 WEEK 5 — Third-party Services: Supabase Storage + Azure OCR + Gemini AI (FINAL v2)

> **Prerequisite**: Week 3 (Pregnancy Core) + Week 4 (Medical Documents + StubFileStorageService) đã implement xong.
>
> **Mục tiêu**: (1) Thay StubFileStorageService bằng **SupabaseStorageService** (upload file thật lên Supabase Storage). (2) Xây dựng AI Infrastructure theo kiến trúc **RAG + Rule Layer** — dùng Azure Document Intelligence cho OCR và Google Gemini cho trích xuất dữ liệu có cấu trúc. (3) Thiết kế **tái sử dụng** cho Nutrition Planning AI (Week sau).

---

## 🏗️ KIẾN TRÚC TỔNG QUAN — RAG + RULE LAYER

### Architecture Diagram

```
┌─ API Layer ──────────────────────────────────────────────────────┐
│  OcrController          MedicalDocumentsController               │
│  AiAdminController      (PRENATAL_CHECKUP upload → trigger OCR)  │
├─ Application Layer ──────────────────────────────────────────────┤
│                                                                   │
│  ┌─ Application/AI/ ──────────────────────────────────────────┐  │
│  │  IAiProvider           — Abstract AI model calls            │  │
│  │  IOcrProvider          — Abstract OCR calls                 │  │
│  │  PromptBuilder         — Fluent builder, lắp ráp rule layers│  │
│  │  Models/               — AiPrompt, AiResponse, OcrModels    │  │
│  │  ExtractionModels/     — Structured medical record data     │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  IServices/                                                       │
│    IMedicalRecordAiService — Full pipeline: OCR → RAG → AI       │
│    IFileStorageService     — (interface from Week 4)              │
│                                                                   │
│  Services/                                                        │
│    MedicalRecordAiService  — Orchestrate: context + prompt + AI  │
│                                                                   │
├─ Infrastructure Layer ───────────────────────────────────────────┤
│                                                                   │
│  Infrastructure/AI/                                               │
│    GeminiAiProvider        — Google Gemini REST API client        │
│    AzureOcrProvider        — Azure Document Intelligence client  │
│                                                                   │
│  Infrastructure/Services/                                         │
│    SupabaseStorageService  — Replaces Week 4 StubFileStorageService│
│    OcrService (enhanced)   — Replaces Week 4 OcrService stub     │
│    OcrJobQueue             — Channel<T>-based in-process job queue│
│    OcrBackgroundService    — BackgroundService consumes queue     │
│                                                                   │
├─ Domain Layer ───────────────────────────────────────────────────┤
│  AiPromptTemplate entity                                          │
│  OcrResult entity (updated)                                       │
│  OcrStatus enum (expanded)                                        │
└──────────────────────────────────────────────────────────────────┘
```

### Rule Layer System (Layered Prompt Construction)

```
┌──────────────────────────────────────────────────┐
│ Layer 1: SYSTEM RULES                             │
│   - Language: Vietnamese medical terminology      │
│   - Format: Always output valid JSON              │
│   - Safety: No diagnosis, data extraction only    │
├──────────────────────────────────────────────────┤
│ Layer 2: DOMAIN RULES                             │
│   - Pregnancy medicine domain knowledge           │
│   - Vietnamese ↔ English medical terminology      │
│   - Standard ranges for pregnancy metrics         │
│   - ⚡ REUSABLE across features                   │
├──────────────────────────────────────────────────┤
│ Layer 3: FEATURE RULES                            │
│   - Medical Record: extraction schema, fields     │
│   - Nutrition (future): dietary guidelines        │
│   - Chat (future): conversation style             │
├──────────────────────────────────────────────────┤
│ Layer 4: USER CONTEXT (RAG — from Database)       │
│   - Current gestational week                      │
│   - Known conditions (PregnancyConditions)        │
│   - Previous records (for consistency)            │
│   - User preferences (future: nutrition)          │
└──────────────────────────────────────────────────┘
```

### RAG Pattern: Structured Context Retrieval

Thay vì vector embeddings (full RAG), chúng ta dùng **Structured Context Retrieval** — truy vấn dữ liệu có cấu trúc từ MySQL và inject vào prompt:

```
1. User upload ảnh medical record (DocumentType = PRENATAL_CHECKUP)
   ⚠️ OCR + AI extraction CHỈ chạy cho PRENATAL_CHECKUP.
   Test types và Others KHÔNG chạy OCR.
   ✅ Upload trả về ngay. OCR job được enqueue và chạy background.
2. OcrBackgroundService dequeues job và gọi ProcessDocumentAsync:
   Azure Document Intelligence → OCR raw text
3. ContextRetriever queries DB:
   - Pregnancy (gestational week, status)
   - PregnancyConditions (known conditions)
   - Recent MedicalDocuments (for consistency)
4. PromptBuilder assembles:
   Layer 1 (System) + Layer 2 (Domain) + Layer 3 (Feature) + Layer 4 (Context) + OCR text
5. Gemini processes → structured JSON
6. Save to OcrResult.StructuredJson
```

### Tái sử dụng cho Nutrition Planning AI (Week sau)

```
Same infrastructure, different feature rules:

Medical Record:  PromptBuilder.FromTemplate("medical_record.extraction")
                   .WithContext("pregnancy", pregnancyData)
                   .WithUserMessage(ocrText)

Nutrition Plan:  PromptBuilder.FromTemplate("nutrition.meal_planning")
                   .WithContext("pregnancy", pregnancyData)
                   .WithContext("nutrition_profile", nutritionData)
                   .WithUserMessage("Lên thực đơn tuần 28")

Nutrition Chat:  PromptBuilder.FromTemplate("nutrition.chat")
                   .WithContext("pregnancy", pregnancyData)
                   .WithContext("conversation_history", messages)
                   .WithUserMessage(userQuestion)
```

---

## 🎯 PROMPT 1/10 — Context + SQL Schema

### CONTEXT CHO AI ASSISTANT

Paste đoạn context này vào đầu conversation với AI assistant:

```
BẠN ĐANG LÀM VIỆC TRÊN DỰ ÁN .NET 8 Clean Architecture cho ứng dụng theo dõi thai kỳ.

PROJECT STRUCTURE:
├── FPT.EXE201.Domain          — Entities, Enums
├── FPT.EXE201.Application     — DTOs, Interfaces, Services, AI abstractions
├── FPT.EXE201.Infrastructure  — EF Core, External API clients, Repositories
└── FPT.EXE201.Api             — Controllers, Filters, Middlewares

DATABASE: MySQL 8, CHAR(36) cho GUID, utf8mb4, snake_case columns.
ORM: EF Core 8 + Pomelo MySQL.

EXISTING PATTERNS:
- BaseEntity: { Id (Guid), CreatedAt, UpdatedAt, DeletedAt, IsDeleted (computed, NOT mapped) }
- GenericRepository<T>: where T : BaseEntity, constructor nhận DbContext (abstract), protected _context, _dbSet
- Specific repos: constructor nhận AppDbContext, gọi base(context)
- UnitOfWork: lazy init với ??= pattern, field _context là AppDbContext
- BaseApiController: Success<T>(), Created<T>(), GetCurrentUserId()
- GlobalExceptionFilter: catches NotFoundException, BadRequestException, ConflictException, ForbiddenException
- EF Configurations: snake_case table/column, Ignore(IsDeleted), enum .HasConversion<string>()
- AutoMapper profiles in Application/MapperProfiles
- FluentValidation auto-registered via AddValidatorsFromAssembly
- Services: AddScoped<IXxxService, XxxService> in DependencyInjection.cs
- GetByIdAsync signature: (Guid id, Func<IQueryable<T>, IQueryable<T>>? include = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
  ⚠️ PHẢI dùng named parameter: GetByIdAsync(id, cancellationToken: cancellationToken)

WEEK 3 ENTITIES (đã implement): Pregnancy, PregnancyCondition, RefPregnancyCondition, PrenatalVisit, PrenatalTest, RefTestType + Translations
WEEK 4 ENTITIES (đã implement): StorageFile, MedicalDocument, OcrResult, RefDocumentType + Translations

WEEK 5 MỤC TIÊU:
- Thay StubFileStorageService (Week 4) bằng SupabaseStorageService (upload file thật lên Supabase Storage)
- Thêm AiPromptTemplate entity (lưu prompt templates có version)
- Mở rộng OcrResult (thêm AI processing fields)
- Mở rộng OcrStatus enum (multi-phase pipeline)
- Tạo AI Provider abstraction (IAiProvider, IOcrProvider)
- Tạo PromptBuilder (Rule Layer system)
- Implement GeminiAiProvider (Google Gemini REST API)
- Implement AzureOcrProvider (Azure Document Intelligence REST API)
- Implement SupabaseStorageService (Supabase Storage REST API)
- Implement MedicalRecordAiService (full pipeline: OCR → RAG context → AI extraction)
- Enhance OcrService (replace Week 4 stub with production-ready background queue)
- Background OCR processing via Channel + BackgroundService (non-blocking upload)
- Kiến trúc tái sử dụng cho Nutrition Planning AI (Week sau)
```

### SQL SCHEMA

```sql
-- ╔══════════════════════════════════════════════════════════════╗
-- ║  WEEK 5 — AI Infrastructure + Medical Record Extraction     ║
-- ║  1 new table + 1 altered table                              ║
-- ╚══════════════════════════════════════════════════════════════╝

-- ────────────────────────────────────────────
-- Table 1: ai_prompt_templates
-- Lưu versioned prompt templates cho các AI features.
-- Mỗi template chứa 3 rule layers + output schema + model config.
-- ────────────────────────────────────────────
CREATE TABLE ai_prompt_templates (
    id              CHAR(36) NOT NULL PRIMARY KEY,
    template_key    VARCHAR(100) NOT NULL,         -- e.g. 'medical_record.extraction'
    version         INT NOT NULL DEFAULT 1,
    display_name    VARCHAR(200) NOT NULL,
    description     TEXT NULL,

    -- Rule Layers (lưu dạng text, inject vào prompt)
    system_rules    TEXT NOT NULL,                  -- Layer 1: System rules
    domain_rules    TEXT NULL,                      -- Layer 2: Domain rules (shared)
    feature_rules   TEXT NOT NULL,                  -- Layer 3: Feature-specific rules
    output_schema   TEXT NULL,                      -- JSON schema cho expected output

    -- Model Configuration
    model_name      VARCHAR(50) NOT NULL DEFAULT 'gemini-2.5-flash',
    temperature     DECIMAL(3,2) NOT NULL DEFAULT 0.10,
    max_output_tokens INT NOT NULL DEFAULT 8192,

    is_active       TINYINT(1) NOT NULL DEFAULT 1,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at      DATETIME NULL,

    UNIQUE KEY uk_template_key_version (template_key, version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ────────────────────────────────────────────
-- ALTER: ocr_results — thêm AI processing fields
-- Hỗ trợ 2-phase pipeline: OCR → AI Extraction
-- ────────────────────────────────────────────
ALTER TABLE ocr_results
    ADD COLUMN ocr_processing_time_ms   INT NULL AFTER error_message,
    ADD COLUMN ai_model_used            VARCHAR(50) NULL AFTER ocr_processing_time_ms,
    ADD COLUMN ai_tokens_used           INT NULL AFTER ai_model_used,
    ADD COLUMN ai_processing_time_ms    INT NULL AFTER ai_tokens_used,
    ADD COLUMN ai_prompt_template_id    CHAR(36) NULL AFTER ai_processing_time_ms,
    ADD CONSTRAINT fk_ocr_ai_template
        FOREIGN KEY (ai_prompt_template_id) REFERENCES ai_prompt_templates(id)
        ON DELETE SET NULL;
```

**✅ Checkpoint**: Run SQL thành công.

---

## 🎯 PROMPT 2/10 — Entities + Enums

**Nhiệm vụ**: Tạo `AiPromptTemplate` entity, update `OcrResult` entity (thêm fields), expand `OcrStatus` enum.

**⚠️ CRITICAL**:
- `AiPromptTemplate` extends `BaseEntity`
- `OcrResult` đã tồn tại từ Week 4 — chỉ THÊM properties, KHÔNG viết lại
- `OcrStatus` enum mở rộng thêm states cho multi-phase pipeline

**Code — Updated OcrStatus Enum**:

```csharp
// File: FPT.EXE201.Domain/Enums/OcrStatus.cs
// ⚠️ REPLACE toàn bộ file Week 4
// ⚠️ MIGRATION NOTE: Week 4 dùng "Processing" → Week 5 rename thành "OcrProcessing".
//    Nếu DB đã có rows với status = "Processing", cần chạy SQL update trước:
//    UPDATE ocr_results SET status = 'OcrProcessing' WHERE status = 'Processing';
//    Hoặc truncate bảng nếu đang dev.

namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái pipeline OCR + AI Extraction.
/// Flow: Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded
/// Bất kỳ bước nào fail → Failed
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
    Failed
}
```

**Code — AiPromptTemplate Entity**:

```csharp
// File: FPT.EXE201.Domain/Entities/AiPromptTemplate.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Versioned prompt template cho AI features.
/// Chứa 3 rule layers (System, Domain, Feature) + output schema + model config.
/// Dùng chung cho: Medical Record Extraction, Nutrition Planning, Chat, etc.
/// </summary>
public class AiPromptTemplate : BaseEntity
{
    /// <summary>
    /// Unique key cho template. Ví dụ: "medical_record.extraction", "nutrition.meal_planning".
    /// Kết hợp với Version tạo unique constraint.
    /// </summary>
    public string TemplateKey { get; set; } = null!;

    /// <summary>Version number. Cho phép A/B test hoặc rollback prompt.</summary>
    public int Version { get; set; } = 1;

    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }

    // ═══ Rule Layers (stored as text, assembled by PromptBuilder) ═══

    /// <summary>Layer 1: System rules — language, format, safety constraints.</summary>
    public string SystemRules { get; set; } = null!;

    /// <summary>Layer 2: Domain rules — pregnancy medicine, terminology. Shared across features.</summary>
    public string? DomainRules { get; set; }

    /// <summary>Layer 3: Feature-specific rules — extraction schema, meal planning guidelines.</summary>
    public string FeatureRules { get; set; } = null!;

    /// <summary>JSON schema cho expected AI output. Giúp Gemini conform to structure.</summary>
    public string? OutputSchema { get; set; }

    // ═══ Model Configuration ═══

    /// <summary>Tên model AI. Default: gemini-2.5-flash (balance speed/quality).</summary>
    public string ModelName { get; set; } = "gemini-2.5-flash";

    /// <summary>Temperature: 0.0 = deterministic, 1.0 = creative. Extraction nên dùng 0.1.</summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>Max tokens cho AI response.</summary>
    public int MaxOutputTokens { get; set; } = 8192;

    /// <summary>Template có đang active không. Chỉ active version mới nhất được sử dụng.</summary>
    public bool IsActive { get; set; } = true;
}
```

**Code — Updated OcrResult Entity** (thêm fields):

```csharp
// File: FPT.EXE201.Domain/Entities/OcrResult.cs
// ⚠️ THÊM các properties sau vào entity OcrResult đã có từ Week 4.
// Giữ nguyên tất cả properties cũ, chỉ ADD thêm:

    // ═══ Week 5: AI Processing Fields ═══

    /// <summary>Thời gian OCR (Azure) xử lý, tính bằng ms.</summary>
    public int? OcrProcessingTimeMs { get; set; }

    /// <summary>Tên model AI đã sử dụng (e.g., "gemini-2.5-flash").</summary>
    public string? AiModelUsed { get; set; }

    /// <summary>Số tokens AI đã sử dụng (prompt + completion).</summary>
    public int? AiTokensUsed { get; set; }

    /// <summary>Thời gian AI extraction xử lý, tính bằng ms.</summary>
    public int? AiProcessingTimeMs { get; set; }

    /// <summary>Template đã dùng để tạo prompt.</summary>
    public Guid? AiPromptTemplateId { get; set; }

    // ═══ Navigation ═══
    public AiPromptTemplate? AiPromptTemplate { get; set; }
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 3/10 — EF Configurations + Seed Data

**Nhiệm vụ**: Tạo `AiPromptTemplateConfiguration`, update `OcrResultConfiguration` (thêm columns), seed prompt template cho medical record extraction.

**Code — AiPromptTemplate Configuration**:

```csharp
// File: FPT.EXE201.Infrastructure/Configurations/AiPromptTemplateConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder)
    {
        builder.ToTable("ai_prompt_templates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("CHAR(36)");

        // Properties
        builder.Property(e => e.TemplateKey)
            .HasColumnName("template_key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        // Rule Layers
        builder.Property(e => e.SystemRules)
            .HasColumnName("system_rules")
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.DomainRules)
            .HasColumnName("domain_rules")
            .HasColumnType("TEXT");

        builder.Property(e => e.FeatureRules)
            .HasColumnName("feature_rules")
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.OutputSchema)
            .HasColumnName("output_schema")
            .HasColumnType("TEXT");

        // Model Config
        builder.Property(e => e.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(50)
            .HasDefaultValue("gemini-2.5-flash");

        builder.Property(e => e.Temperature)
            .HasColumnName("temperature")
            .HasColumnType("DECIMAL(3,2)")
            .HasDefaultValue(0.1);

        builder.Property(e => e.MaxOutputTokens)
            .HasColumnName("max_output_tokens")
            .HasDefaultValue(8192);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // BaseEntity
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(e => e.IsDeleted);

        // Unique constraint
        builder.HasIndex(e => new { e.TemplateKey, e.Version })
            .HasDatabaseName("uk_template_key_version")
            .IsUnique();
    }
}
```

**Code — Update OcrResult Configuration** (thêm columns):

```csharp
// ⚠️ THÊM vào OcrResultConfiguration.cs đã có từ Week 4, trong method Configure():

        // Week 5: AI Processing Fields
        builder.Property(e => e.OcrProcessingTimeMs)
            .HasColumnName("ocr_processing_time_ms");

        builder.Property(e => e.AiModelUsed)
            .HasColumnName("ai_model_used")
            .HasMaxLength(50);

        builder.Property(e => e.AiTokensUsed)
            .HasColumnName("ai_tokens_used");

        builder.Property(e => e.AiProcessingTimeMs)
            .HasColumnName("ai_processing_time_ms");

        builder.Property(e => e.AiPromptTemplateId)
            .HasColumnName("ai_prompt_template_id")
            .HasColumnType("CHAR(36)");

        // Relationship: OcrResult → AiPromptTemplate
        builder.HasOne(e => e.AiPromptTemplate)
            .WithMany()
            .HasForeignKey(e => e.AiPromptTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
```

**Code — Seed Data (Medical Record Extraction Template)**:

```csharp
// File: FPT.EXE201.Infrastructure/Configurations/Seeds/AiPromptTemplateSeed.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations.Seeds;

public class AiPromptTemplateSeed : IEntityTypeConfiguration<AiPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder)
    {
        builder.HasData(
            new
            {
                Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"),
                TemplateKey = "medical_record.extraction",
                Version = 1,
                DisplayName = "Medical Record Data Extraction",
                Description = "Extract structured data from prenatal checkup records (Phiếu Khám Thai MS:51/BV2). Output matches VitalsJsonDto schema for direct storage.",

                SystemRules = @"You are a medical data extraction assistant specializing in Vietnamese prenatal care records (Phiếu Khám Thai, mẫu MS: 51/BV2 — Bộ Y tế Việt Nam).

RULES:
1. Always respond with valid JSON matching the provided schema EXACTLY. No extra keys, no missing sections.
2. Extract ONLY information explicitly present in the text. Do NOT infer or assume data.
3. If a field is not found in the text, use null (for scalars) or omit from arrays.
4. Do NOT provide medical advice, diagnosis, or interpretations beyond what is written.
5. Preserve original Vietnamese text for names, facilities, addresses, and notes.
6. Convert dates to ISO 8601 format (yyyy-MM-dd) when possible.
7. Convert numeric values to standard units (kg, cm, mmHg, °C, bpm, g/L, mmol/L).
8. Boolean fields: use true/false/null only. Set true if the document explicitly mentions the condition.
9. Flag lab results as abnormal ONLY if the document explicitly states so or provides reference ranges showing out-of-range values.
10. bloodPressureSystolic and bloodPressureDiastolic MUST be SEPARATE integers (e.g., 120 and 80), NOT a combined string.",

                DomainRules = @"VIETNAMESE PRENATAL CARE DOMAIN KNOWLEDGE (Phiếu Khám Thai MS: 51/BV2):

Document sections (map to vitalsData fields):
A. Thông tin chung → generalInfo (facility, patient demographics, insurance)
B.I. Lần khám trước → previousVisit (diagnosis, treatment)
B.II. Hỏi bệnh → interview (reason, pregnancy number, gestational age, LMP, expected delivery)
B.III. Tiền sử bệnh → medicalHistory (personal, obstetric, gynecology, family)
B.IV. Khám bệnh → examination (vitalSigns, general, obstetric)
B.VI. Chẩn đoán → diagnosis (text + ICD code)
B.VII. Kế hoạch điều trị → treatmentPlan (medication, health education)
B.VIII. Tiên lượng → prognosis
B.IX. Lần khám kế tiếp → nextAppointment

Common metrics and units:
- Mạch (Pulse): lần/phút → pulseBpm (integer)
- Nhiệt độ (Temperature): °C → temperatureCelsius (number)
- Huyết áp / HA: mmHg → bloodPressureSystolic + bloodPressureDiastolic (separate integers)
- Nhịp thở (Respiratory rate): lần/phút → respiratoryRateBpm (integer)
- Cân nặng (Weight): kg → weightKg (number)
- Chiều cao (Height): cm → heightCm (number)
- CCTC / Bề cao tử cung (Fundal height): cm → fundusHeightCm (number)
- Vòng bụng (Abdominal circumference): cm → abdominalCircumferenceCm (number)
- Tim thai / TT / TSM (Fetal heart rate): lần/phút → fetalHeartRateBpm (integer)
- Tuổi thai / Tuần thai: weeks → gestationalWeek (integer, ignore days)
- Protein niệu: g/L or qualitative (+/++/+++) → urineProtein (boolean) + urineProteinValue (number)

Common abbreviations:
- TSM: tim sản mạch (fetal heart)
- TC: tử cung (uterus)
- NK: ngôi kiểu (fetal presentation)
- NT: nước tiểu (urine)
- CTG: cardiotocography
- BCTC: bề cao tử cung (fundal height)
- KCC: kinh cuối cùng (last menstrual period)
- PARA: tiền sử sản khoa (obstetric history)
- CTC: cổ tử cung (cervix)
- ÔVN: ối vỡ non (premature rupture of membranes)",

                FeatureRules = @"EXTRACTION TASK:
Extract structured medical data from the OCR text of a Vietnamese prenatal checkup form.
Fill the 'vitalsData' object following the VitalsJsonDto schema sections:

1. generalInfo: Patient demographics, facility name, insurance info
2. previousVisit: Previous visit date, diagnosis, treatment (if mentioned)
3. interview: Visit reason, pregnancy number, gestational week, LMP date, expected delivery date
4. medicalHistory: Personal diseases, obstetric history (PARA), gynecology, family history
5. examination.vitalSigns: Pulse, temperature, BP systolic/diastolic (SEPARATE integers), respiratory rate, weight, height
6. examination.general: Mental status, edema, urine protein
7. examination.obstetric: Fundal height, abdominal circumference, fetal presentation, fetal heart rate, cervix, amniotic fluid/sac
8. diagnosis: Diagnosis text + ICD code (if present)
9. treatmentPlan: Medications (as single text), next treatment steps, health education
10. prognosis: normal/risky/cesarean_indicated
11. nextAppointment: Date + notes + examiner type

CRITICAL:
- 'bloodPressureSystolic' and 'bloodPressureDiastolic' must be SEPARATE integers. E.g., HA 120/80 → systolic=120, diastolic=80.
- 'gestationalWeek' is an integer (weeks only). E.g., 28T2N → 28.
- Boolean fields: true if mentioned/positive, false if explicitly negative, null if not mentioned.
- Dates: yyyy-MM-dd format.
- If text is partially illegible, extract readable parts and set overallConfidence lower.",

                OutputSchema = @"{
  ""vitalsData"": {
    ""generalInfo"": {
      ""facility"": ""string|null"",
      ""managingAuthority"": ""string|null"",
      ""admissionNumber"": ""string|null"",
      ""patientCode"": ""string|null"",
      ""fullName"": ""string|null"",
      ""dateOfBirth"": ""string|null (yyyy-MM-dd)"",
      ""age"": ""integer|null"",
      ""phone"": ""string|null"",
      ""occupation"": ""string|null"",
      ""ethnicity"": ""string|null"",
      ""nationality"": ""string|null"",
      ""address"": ""string|null"",
      ""ward"": ""string|null"",
      ""district"": ""string|null"",
      ""province"": ""string|null"",
      ""insuranceType"": ""string|null (BHYT|thu_phi|mien|khac)"",
      ""insuranceNumber"": ""string|null"",
      ""insuranceExpiry"": ""string|null (yyyy-MM-dd)"",
      ""idNumber"": ""string|null""
    },
    ""previousVisit"": {
      ""visitDate"": ""string|null (yyyy-MM-dd)"",
      ""diagnosis"": ""string|null"",
      ""treatment"": ""string|null""
    },
    ""interview"": {
      ""reasonForVisit"": ""string|null"",
      ""pregnancyNumber"": ""integer|null"",
      ""totalVisitCount"": ""integer|null"",
      ""lastMenstrualPeriodDate"": ""string|null (yyyy-MM-dd)"",
      ""gestationalWeek"": ""integer|null"",
      ""expectedDeliveryDate"": ""string|null (yyyy-MM-dd)"",
      ""clinicalProgress"": ""string|null"",
      ""generalCondition"": ""string|null (normal|abnormal)"",
      ""generalConditionNote"": ""string|null"",
      ""tetanusVaccineHistory"": ""integer|null""
    },
    ""medicalHistory"": {
      ""personal"": {
        ""allergy"": ""boolean|null"",
        ""allergyNote"": ""string|null"",
        ""medicalHistory"": ""boolean|null"",
        ""medicalHistoryNote"": ""string|null"",
        ""hypertension"": ""boolean|null"",
        ""heartDisease"": ""boolean|null"",
        ""respiratoryDisease"": ""boolean|null"",
        ""thyroidDisease"": ""boolean|null"",
        ""kidneyDisease"": ""boolean|null"",
        ""diabetes"": ""boolean|null"",
        ""otherDiseases"": ""string|null"",
        ""currentMedications"": ""boolean|null"",
        ""medicationNote"": ""string|null"",
        ""surgeryHistory"": ""boolean|null"",
        ""surgeryNote"": ""string|null""
      },
      ""obstetric"": {
        ""para"": ""integer|null"",
        ""previousPregnancies"": [{""endDate"":""string|null"",""gestationalAge"":""string|null"",""complicationsDuringPregnancy"":""string|null"",""deliveryMethod"":""string|null"",""newbornInfo"":""string|null"",""postpartum"":""string|null""}]
      },
      ""gynecology"": {
        ""menstrualCycle"": ""string|null (regular|irregular)"",
        ""menstrualCycleDays"": ""integer|null"",
        ""gynecologySurgery"": ""boolean|null"",
        ""gynecologySurgeryNote"": ""string|null"",
        ""ovarianTumor"": ""boolean|null"",
        ""uterineFibroid"": ""boolean|null"",
        ""genitalMalformation"": ""boolean|null"",
        ""vaginalInfection"": ""boolean|null""
      },
      ""pelvicOrganProlapse"": ""boolean|null"",
      ""gynecologicalDiseaseNote"": ""string|null"",
      ""family"": {
        ""hasHistory"": ""boolean|null"",
        ""familyHistoryNote"": ""string|null"",
        ""twins"": ""boolean|null"",
        ""malformation"": ""boolean|null"",
        ""geneticDisease"": ""boolean|null"",
        ""diabetes"": ""boolean|null"",
        ""hypertension"": ""boolean|null"",
        ""otherNote"": ""string|null""
      }
    },
    ""examination"": {
      ""vitalSigns"": {
        ""pulseBpm"": ""integer|null"",
        ""temperatureCelsius"": ""number|null"",
        ""bloodPressureSystolic"": ""integer|null (mmHg)"",
        ""bloodPressureDiastolic"": ""integer|null (mmHg)"",
        ""respiratoryRateBpm"": ""integer|null"",
        ""weightKg"": ""number|null"",
        ""heightCm"": ""number|null""
      },
      ""general"": {
        ""mentalStatus"": ""string|null (alert|coma|other)"",
        ""mentalStatusNote"": ""string|null"",
        ""edema"": ""boolean|null"",
        ""urineProtein"": ""boolean|null"",
        ""urineProteinValue"": ""number|null (g/L)""
      },
      ""obstetric"": {
        ""oldScar"": ""boolean|null"",
        ""scarPainful"": ""boolean|null"",
        ""pelvis"": ""string|null (normal|abnormal)"",
        ""fundusHeightCm"": ""number|null"",
        ""abdominalCircumferenceCm"": ""number|null"",
        ""fetalPresentation"": ""string|null (normal|abnormal)"",
        ""fetalPresentationNote"": ""string|null"",
        ""uterineContraction"": ""boolean|null"",
        ""uterineContractionFrequency"": ""integer|null (per 10 min)"",
        ""fetalHeartbeat"": ""boolean|null"",
        ""fetalHeartRateBpm"": ""integer|null"",
        ""cervix"": ""string|null (closed|effaced|dilated)"",
        ""cervixDilationCm"": ""number|null"",
        ""amnioticSac"": ""string|null (bulging|flat|pear)"",
        ""membraneStatus"": ""string|null (intact|leaking|ruptured)"",
        ""membraneRuptureTime"": ""string|null (HH:mm)"",
        ""amnioticFluid"": ""string|null (clear|green|bloody)""
      }
    },
    ""diagnosis"": {
      ""text"": ""string|null"",
      ""icdCode"": ""string|null""
    },
    ""treatmentPlan"": {
      ""medication"": ""string|null"",
      ""nextSteps"": ""string|null"",
      ""healthEducation"": ""boolean|null"",
      ""healthEducationNote"": ""string|null""
    },
    ""prognosis"": ""string|null (normal|risky|cesarean_indicated)"",
    ""nextAppointment"": {
      ""date"": ""string|null (yyyy-MM-dd)"",
      ""notes"": ""string|null"",
      ""examinerType"": ""string|null (obstetrician|midwife|pediatric_nurse|other)""
    }
  },
  ""overallConfidence"": ""number (0.0-1.0)""
}",

                ModelName = "gemini-2.5-flash",
                Temperature = 0.1,
                MaxOutputTokens = 8192,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 4/10 — AI Core Abstractions (Interfaces + Models + PromptBuilder)

**Nhiệm vụ**: Tạo AI abstraction layer trong `Application/AI/`. Bao gồm:
- Interfaces: `IAiProvider`, `IOcrProvider`
- Models: `AiPrompt`, `AiResponse`, `AiMessage`, `OcrRequest`, `OcrResponse`
- PromptBuilder: Fluent builder cho layered prompts

**⚠️ TẤT CẢ đặt trong `FPT.EXE201.Application/AI/`**. Tạo folder `AI/` trong Application project.

**Code — AI Models**:

```csharp
// File: FPT.EXE201.Application/AI/Models/AiPrompt.cs
namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Prompt đã lắp ráp hoàn chỉnh, sẵn sàng gửi tới AI provider.
/// SystemMessage = tổng hợp từ các Rule Layers.
/// UserMessage = RAG context + user input.
/// </summary>
public record AiPrompt(
    /// <summary>System message: lắp ráp từ Layer 1 + 2 + 3 + OutputSchema.</summary>
    string SystemMessage,

    /// <summary>User message: RAG context + actual user input (OCR text, question, etc.).</summary>
    string UserMessage,

    /// <summary>Tên model AI (e.g., "gemini-2.5-flash").</summary>
    string ModelName,

    /// <summary>Temperature: 0.0 = deterministic, 1.0 = creative.</summary>
    double Temperature = 0.1,

    /// <summary>Max output tokens.</summary>
    int MaxOutputTokens = 8192,

    /// <summary>Yêu cầu AI trả JSON (responseMimeType = application/json).</summary>
    bool JsonMode = true
);

// File: FPT.EXE201.Application/AI/Models/AiResponse.cs
namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Response từ AI provider.
/// </summary>
public record AiResponse(
    /// <summary>Nội dung text response từ AI.</summary>
    string Content,

    /// <summary>Số tokens input (prompt).</summary>
    int PromptTokens,

    /// <summary>Số tokens output (completion).</summary>
    int CompletionTokens,

    /// <summary>Tổng tokens (prompt + completion).</summary>
    int TotalTokens,

    /// <summary>Model thực tế đã xử lý.</summary>
    string ModelUsed,

    /// <summary>Thời gian xử lý.</summary>
    TimeSpan ProcessingTime
);

// File: FPT.EXE201.Application/AI/Models/AiMessage.cs
namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Message trong multi-turn conversation (dùng cho chat feature — Nutrition Week sau).
/// </summary>
public record AiMessage(
    /// <summary>"user" hoặc "model" (Gemini convention).</summary>
    string Role,

    /// <summary>Nội dung message.</summary>
    string Content
);

// File: FPT.EXE201.Application/AI/Models/OcrRequest.cs
namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Request gửi tới OCR provider.
/// </summary>
public record OcrRequest(
    Stream FileStream,
    string FileName,
    string ContentType,
    string? LanguageHint = "vi"
);

// File: FPT.EXE201.Application/AI/Models/OcrResponse.cs
namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Response từ OCR provider.
/// </summary>
public record OcrResponse(
    /// <summary>Raw text đã trích xuất từ ảnh/PDF.</summary>
    string RawText,

    /// <summary>Confidence score trung bình (0.00 - 100.00). Đồng bộ với OcrResult.ConfidenceScore DECIMAL(5,2).</summary>
    decimal ConfidenceScore,

    /// <summary>Thời gian xử lý.</summary>
    TimeSpan ProcessingTime,

    /// <summary>Engine đã sử dụng (e.g., "azure-document-intelligence-4.0").</summary>
    string EngineUsed
);
```

**Code — Interfaces**:

```csharp
// File: FPT.EXE201.Application/AI/Interfaces/IAiProvider.cs
using FPT.EXE201.Application.AI.Models;

namespace FPT.EXE201.Application.AI.Interfaces;

/// <summary>
/// Abstraction cho AI model provider (Gemini, OpenAI, etc.).
/// Week 5: implement GenerateAsync cho single completion.
/// GenerateAsync dùng cho: extraction, meal planning, summarization.
/// ChatAsync dùng cho: nutrition chat (Week sau).
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Single completion — gửi prompt, nhận 1 response.
    /// Dùng cho extraction, planning, summarization.
    /// </summary>
    Task<AiResponse> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Multi-turn conversation — gửi lịch sử messages, nhận response tiếp theo.
    /// Dùng cho chat feature (Nutrition Week sau).
    /// </summary>
    Task<AiResponse> ChatAsync(
        List<AiMessage> messages,
        string systemMessage,
        string modelName,
        double temperature = 0.7,
        int maxOutputTokens = 2048,
        CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/AI/Interfaces/IOcrProvider.cs
using FPT.EXE201.Application.AI.Models;

namespace FPT.EXE201.Application.AI.Interfaces;

/// <summary>
/// Abstraction cho OCR provider (Azure Document Intelligence, Google Vision, Tesseract, etc.).
/// </summary>
public interface IOcrProvider
{
    /// <summary>Chạy OCR trên file, trả raw text.</summary>
    Task<OcrResponse> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default);

    /// <summary>Danh sách file types mà provider hỗ trợ.</summary>
    IReadOnlyList<string> SupportedContentTypes { get; }
}
```

**Code — PromptBuilder (Pure Logic, Application Layer)**:

```csharp
// File: FPT.EXE201.Application/AI/PromptBuilder.cs
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.AI;

/// <summary>
/// Fluent builder cho AI prompts với Rule Layer system.
/// Lắp ráp: System Rules + Domain Rules + Feature Rules + Output Schema → SystemMessage
///           RAG Context + User Input → UserMessage
///
/// Usage (manual):
///   PromptBuilder.Create()
///     .WithSystemRules("...")
///     .WithDomainRules("...")
///     .WithFeatureRules("...")
///     .WithOutputSchema("...")
///     .WithContext("pregnancy", pregnancyJson)
///     .WithUserMessage(ocrText)
///     .Build();
///
/// Usage (from DB template):
///   PromptBuilder.FromTemplate(template)
///     .WithContext("pregnancy", pregnancyJson)
///     .WithUserMessage(ocrText)
///     .Build();
/// </summary>
public class PromptBuilder
{
    private readonly List<string> _systemParts = new();
    private readonly List<string> _contextParts = new();
    private string _userMessage = "";
    private string? _outputSchema;
    private string _modelName = "gemini-2.5-flash";
    private double _temperature = 0.1;
    private int _maxOutputTokens = 8192;
    private bool _jsonMode = true;

    public static PromptBuilder Create() => new();

    /// <summary>
    /// Khởi tạo PromptBuilder từ AiPromptTemplate (loaded from DB).
    /// Auto-fills System/Domain/Feature rules + model config.
    /// Caller chỉ cần thêm Context + UserMessage.
    /// </summary>
    public static PromptBuilder FromTemplate(AiPromptTemplate template)
    {
        var builder = new PromptBuilder();

        builder.WithSystemRules(template.SystemRules);

        if (!string.IsNullOrWhiteSpace(template.DomainRules))
            builder.WithDomainRules(template.DomainRules);

        builder.WithFeatureRules(template.FeatureRules);

        if (!string.IsNullOrWhiteSpace(template.OutputSchema))
            builder.WithOutputSchema(template.OutputSchema);

        builder._modelName = template.ModelName;
        builder._temperature = template.Temperature;
        builder._maxOutputTokens = template.MaxOutputTokens;

        return builder;
    }

    // ═══ Layer 1: System Rules ═══
    public PromptBuilder WithSystemRules(string rules)
    {
        _systemParts.Insert(0, $"[SYSTEM RULES]\n{rules}");
        return this;
    }

    // ═══ Layer 2: Domain Rules ═══
    public PromptBuilder WithDomainRules(string rules)
    {
        _systemParts.Add($"[DOMAIN KNOWLEDGE]\n{rules}");
        return this;
    }

    // ═══ Layer 3: Feature Rules ═══
    public PromptBuilder WithFeatureRules(string rules)
    {
        _systemParts.Add($"[TASK INSTRUCTIONS]\n{rules}");
        return this;
    }

    // ═══ Output Schema ═══
    public PromptBuilder WithOutputSchema(string schema)
    {
        _outputSchema = schema;
        return this;
    }

    // ═══ Layer 4: RAG Context (injected into user message) ═══
    public PromptBuilder WithContext(string label, string data)
    {
        _contextParts.Add($"[{label.ToUpperInvariant()}]\n{data}");
        return this;
    }

    // ═══ User Input ═══
    public PromptBuilder WithUserMessage(string message)
    {
        _userMessage = message;
        return this;
    }

    // ═══ Model Configuration ═══
    public PromptBuilder WithModel(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    public PromptBuilder WithTemperature(double temperature)
    {
        _temperature = temperature;
        return this;
    }

    public PromptBuilder WithMaxTokens(int maxTokens)
    {
        _maxOutputTokens = maxTokens;
        return this;
    }

    public PromptBuilder WithJsonMode(bool enabled)
    {
        _jsonMode = enabled;
        return this;
    }

    // ═══ Build ═══
    public AiPrompt Build()
    {
        // Assemble system message from rule layers
        var systemParts = new List<string>(_systemParts);

        if (!string.IsNullOrWhiteSpace(_outputSchema))
        {
            systemParts.Add($"[OUTPUT JSON SCHEMA]\nYour response MUST be valid JSON conforming to this schema:\n{_outputSchema}");
        }

        var systemMessage = string.Join("\n\n", systemParts);

        // Assemble user message: RAG context + user input
        var userParts = new List<string>();

        if (_contextParts.Any())
        {
            userParts.Add("CONTEXT (from patient records):");
            userParts.AddRange(_contextParts);
            userParts.Add("---");
        }

        userParts.Add(_userMessage);

        var userMessage = string.Join("\n\n", userParts);

        return new AiPrompt(
            SystemMessage: systemMessage,
            UserMessage: userMessage,
            ModelName: _modelName,
            Temperature: _temperature,
            MaxOutputTokens: _maxOutputTokens,
            JsonMode: _jsonMode
        );
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 5/10 — Extraction Data Models + DTOs

**Nhiệm vụ**: Tạo strongly-typed models cho medical record extraction result + DTOs + updated OcrResultDto.

**Code — Extraction Models** (dùng cho JSON deserialization từ Gemini response):

```csharp
// File: FPT.EXE201.Application/AI/ExtractionModels/MedicalRecordExtractionResult.cs
using System.Text.Json.Serialization;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.AI.ExtractionModels;

/// <summary>
/// Top-level kết quả trích xuất từ medical record.
/// vitalsData maps 1:1 với VitalsJsonDto → serialize trực tiếp thành PrenatalVisit.VitalsJson.
/// </summary>
public class MedicalRecordExtractionResult
{
    /// <summary>
    /// Dữ liệu phiếu khám thai, compatible 1:1 với VitalsJsonDto.
    /// Sẽ được serialize trực tiếp thành PrenatalVisit.VitalsJson trong Week 5.5 confirm flow.
    /// </summary>
    [JsonPropertyName("vitalsData")]
    public VitalsJsonDto? VitalsData { get; set; }

    /// <summary>
    /// AI's overall confidence score for the extraction (0.0 - 1.0).
    /// </summary>
    [JsonPropertyName("overallConfidence")]
    public double OverallConfidence { get; set; }
}
```

**Code — Updated OcrResultDto**:

```csharp
// File: FPT.EXE201.Application/DTOs/MedicalDocuments/OcrResultDto.cs
// ⚠️ REPLACE file Week 4 — thêm AI processing fields
// ⚠️ Dùng class (không dùng positional record) vì AutoMapper cần parameterless constructor + settable properties.

namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

public class OcrResultDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int OcrRunNumber { get; set; }
    public string Status { get; set; } = null!;
    public string? OcrEngine { get; set; }
    public string? LanguageHint { get; set; }
    public string? RawText { get; set; }
    public string? StructuredJson { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? ErrorMessage { get; set; }

    // Week 5: AI Processing Fields
    public int? OcrProcessingTimeMs { get; set; }
    public string? AiModelUsed { get; set; }
    public int? AiTokensUsed { get; set; }
    public int? AiProcessingTimeMs { get; set; }
    public Guid? AiPromptTemplateId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Code — AiPromptTemplateDto**:

```csharp
// File: FPT.EXE201.Application/DTOs/AI/AiPromptTemplateDto.cs
namespace FPT.EXE201.Application.DTOs.AI;

public record AiPromptTemplateDto(
    Guid Id,
    string TemplateKey,
    int Version,
    string DisplayName,
    string? Description,
    string ModelName,
    double Temperature,
    int MaxOutputTokens,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 6/10 — GeminiAiProvider (Infrastructure)

**Nhiệm vụ**: Implement Google Gemini REST API client. Sử dụng `HttpClient` via `IHttpClientFactory`.

**⚠️ CRITICAL**:
- Đặt ở `Infrastructure/AI/GeminiAiProvider.cs`
- Dùng `IHttpClientFactory` (register via `AddHttpClient<IAiProvider, GeminiAiProvider>()` trong DI)
- Gemini API endpoint: `https://generativelanguage.googleapis.com/v1beta`
- API key via `IConfiguration` (chưa dùng `IOptions` — giữ pattern hiện tại)
- Hỗ trợ cả `GenerateAsync` (single) và `ChatAsync` (multi-turn)

**NuGet**: Không cần package — dùng `System.Net.Http` + `System.Text.Json` có sẵn.

**Code — Gemini API Models** (internal, chỉ dùng trong Infrastructure):

```csharp
// File: FPT.EXE201.Infrastructure/AI/GeminiApiModels.cs
using System.Text.Json.Serialization;

namespace FPT.EXE201.Infrastructure.AI;

// ═══ Request Models ═══

internal class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("systemInstruction")]
    public GeminiSystemInstruction? SystemInstruction { get; set; }

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

internal class GeminiContent
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal class GeminiSystemInstruction
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

internal class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("responseMimeType")]
    public string? ResponseMimeType { get; set; }
}

// ═══ Response Models ═══

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }

    [JsonPropertyName("error")]
    public GeminiError? Error { get; set; }
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

internal class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}

internal class GeminiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
```

**Code — GeminiAiProvider**:

```csharp
// File: FPT.EXE201.Infrastructure/AI/GeminiAiProvider.cs
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.Exceptions;

namespace FPT.EXE201.Infrastructure.AI;

/// <summary>
/// Google Gemini REST API client.
/// Implements IAiProvider cho cả single completion và multi-turn chat.
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly ILogger<GeminiAiProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["AI:Gemini:ApiKey"]
            ?? throw new InvalidOperationException("AI:Gemini:ApiKey is not configured.");
        _defaultModel = configuration["AI:Gemini:DefaultModel"] ?? "gemini-2.5-flash";
    }

    public async Task<AiResponse> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var modelName = string.IsNullOrEmpty(prompt.ModelName) ? _defaultModel : prompt.ModelName;

        var request = new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = prompt.UserMessage } }
                }
            },
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = new List<GeminiPart> { new() { Text = prompt.SystemMessage } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = prompt.Temperature,
                MaxOutputTokens = prompt.MaxOutputTokens,
                ResponseMimeType = prompt.JsonMode ? "application/json" : null
            }
        };

        return await SendRequestAsync(modelName, request, cancellationToken);
    }

    public async Task<AiResponse> ChatAsync(
        List<AiMessage> messages,
        string systemMessage,
        string modelName,
        double temperature = 0.7,
        int maxOutputTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrEmpty(modelName) ? _defaultModel : modelName;

        var request = new GeminiRequest
        {
            Contents = messages.Select(m => new GeminiContent
            {
                Role = m.Role, // "user" or "model"
                Parts = new List<GeminiPart> { new() { Text = m.Content } }
            }).ToList(),
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = new List<GeminiPart> { new() { Text = systemMessage } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = temperature,
                MaxOutputTokens = maxOutputTokens
            }
        };

        return await SendRequestAsync(model, request, cancellationToken);
    }

    // ═══ Private ═══

    private async Task<AiResponse> SendRequestAsync(
        string modelName, GeminiRequest request, CancellationToken cancellationToken)
    {
        var url = $"models/{modelName}:generateContent?key={_apiKey}";
        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);

        _logger.LogDebug("Gemini request to {Model}, content length: {Length}", modelName, jsonContent.Length);

        var stopwatch = Stopwatch.StartNew();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        stopwatch.Stop();

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error {StatusCode}: {Body}", httpResponse.StatusCode, responseBody);

            var errorResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);
            var errorMessage = errorResponse?.Error?.Message ?? $"Gemini API returned {httpResponse.StatusCode}";

            throw new BadRequestException($"AI processing failed: {errorMessage}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
        {
            throw new BadRequestException("AI returned no response. The content may have been blocked by safety filters.");
        }

        var candidate = geminiResponse.Candidates[0];
        var content = candidate.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        var usage = geminiResponse.UsageMetadata;

        _logger.LogInformation(
            "Gemini response from {Model}: {Tokens} tokens in {Time}ms",
            modelName,
            usage?.TotalTokenCount ?? 0,
            stopwatch.ElapsedMilliseconds);

        return new AiResponse(
            Content: content,
            PromptTokens: usage?.PromptTokenCount ?? 0,
            CompletionTokens: usage?.CandidatesTokenCount ?? 0,
            TotalTokens: usage?.TotalTokenCount ?? 0,
            ModelUsed: modelName,
            ProcessingTime: stopwatch.Elapsed
        );
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 7/10 — AzureOcrProvider + SupabaseStorageService (Infrastructure)

**Nhiệm vụ**: Implement Azure Document Intelligence REST API client cho OCR + SupabaseStorageService (thay thế StubFileStorageService từ Week 4).

**⚠️ CRITICAL**:
- API: Azure Document Intelligence (formerly Form Recognizer) v4.0, model `prebuilt-read`
- 2-step async pattern: POST analyze → Poll GET until complete
- Đặt ở `Infrastructure/AI/AzureOcrProvider.cs`
- Dùng `IHttpClientFactory` via `AddHttpClient<IInterface, TImpl>()` (xem Prompt 10 DI)

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/AI/AzureOcrProvider.cs
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.Exceptions;

namespace FPT.EXE201.Infrastructure.AI;

/// <summary>
/// Azure Document Intelligence REST API client.
/// Uses prebuilt-read model for general document OCR.
/// Async pattern: POST → poll GET until succeeded.
/// </summary>
public class AzureOcrProvider : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly string _apiVersion;
    private readonly int _pollingIntervalMs;
    private readonly int _timeoutSeconds;
    private readonly ILogger<AzureOcrProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public IReadOnlyList<string> SupportedContentTypes { get; } = new List<string>
    {
        "image/jpeg", "image/png", "image/bmp", "image/tiff", "image/heif",
        "application/pdf"
    };

    public AzureOcrProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureOcrProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["AI:AzureDocumentIntelligence:ApiKey"]
            ?? throw new InvalidOperationException("AI:AzureDocumentIntelligence:ApiKey is not configured.");
        _modelId = configuration["AI:AzureDocumentIntelligence:ModelId"] ?? "prebuilt-read";
        _apiVersion = configuration["AI:AzureDocumentIntelligence:ApiVersion"] ?? "2024-11-30";
        _pollingIntervalMs = int.Parse(configuration["AI:AzureDocumentIntelligence:PollingIntervalMs"] ?? "1000");
        _timeoutSeconds = int.Parse(configuration["AI:AzureDocumentIntelligence:TimeoutSeconds"] ?? "120");
    }

    public async Task<OcrResponse> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        if (!SupportedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            throw new BadRequestException($"Content type '{request.ContentType}' is not supported for OCR. Supported: {string.Join(", ", SupportedContentTypes)}");
        }

        var stopwatch = Stopwatch.StartNew();

        // Step 1: Submit analyze request
        var operationLocation = await SubmitAnalyzeRequestAsync(request, cancellationToken);

        // Step 2: Poll for results
        var result = await PollForResultAsync(operationLocation, cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Azure OCR completed in {Time}ms, extracted {Length} chars, confidence: {Confidence:F2}",
            stopwatch.ElapsedMilliseconds, result.RawText.Length, result.ConfidenceScore);

        return result with
        {
            ProcessingTime = stopwatch.Elapsed,
            EngineUsed = $"azure-document-intelligence-{_apiVersion}"
        };
    }

    // ═══ Private: Submit Analyze ═══

    private async Task<string> SubmitAnalyzeRequestAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        var url = $"documentintelligence/documentModels/{_modelId}:analyze?api-version={_apiVersion}";

        if (!string.IsNullOrEmpty(request.LanguageHint))
        {
            url += $"&locale={request.LanguageHint}";
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

        // Send file as binary
        var streamContent = new StreamContent(request.FileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        httpRequest.Content = streamContent;

        _logger.LogDebug("Submitting Azure OCR for {FileName} ({ContentType})", request.FileName, request.ContentType);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Azure OCR submit failed {StatusCode}: {Body}", httpResponse.StatusCode, errorBody);
            throw new BadRequestException($"Azure OCR failed to start: {httpResponse.StatusCode}");
        }

        // Get operation-location header for polling
        if (!httpResponse.Headers.TryGetValues("Operation-Location", out var values))
        {
            throw new BadRequestException("Azure OCR response missing Operation-Location header.");
        }

        return values.First();
    }

    // ═══ Private: Poll for Results ═══

    private async Task<OcrResponse> PollForResultAsync(string operationLocation, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(_pollingIntervalMs, cancellationToken);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            pollRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            using var pollResponse = await _httpClient.SendAsync(pollRequest, cancellationToken);
            var responseBody = await pollResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!pollResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Azure OCR poll failed {StatusCode}: {Body}", pollResponse.StatusCode, responseBody);
                throw new BadRequestException($"Azure OCR polling failed: {pollResponse.StatusCode}");
            }

            var analyzeResponse = JsonSerializer.Deserialize<AzureAnalyzeResponse>(responseBody, JsonOptions);

            switch (analyzeResponse?.Status?.ToLowerInvariant())
            {
                case "succeeded":
                    return ExtractFromAnalyzeResult(analyzeResponse);

                case "failed":
                    var errorMsg = analyzeResponse.Error?.Message ?? "Unknown OCR error";
                    throw new BadRequestException($"Azure OCR failed: {errorMsg}");

                case "running":
                case "notstarted":
                    _logger.LogDebug("Azure OCR still processing...");
                    continue;

                default:
                    _logger.LogWarning("Unknown Azure OCR status: {Status}", analyzeResponse?.Status);
                    continue;
            }
        }

        throw new BadRequestException($"Azure OCR timed out after {_timeoutSeconds} seconds.");
    }

    // ═══ Private: Extract text from result ═══

    private static OcrResponse ExtractFromAnalyzeResult(AzureAnalyzeResponse response)
    {
        var result = response.AnalyzeResult;
        if (result == null)
        {
            return new OcrResponse("", 0m, TimeSpan.Zero, "");
        }

        // Full extracted text
        var rawText = result.Content ?? "";

        // Average confidence across all pages/lines
        // Azure returns word.Confidence in range 0.0-1.0 → convert to 0.00-100.00
        double totalConfidence = 0;
        int wordCount = 0;

        if (result.Pages != null)
        {
            foreach (var page in result.Pages)
            {
                if (page.Words != null)
                {
                    foreach (var word in page.Words)
                    {
                        totalConfidence += word.Confidence;
                        wordCount++;
                    }
                }
            }
        }

        // Convert from 0.0-1.0 ratio → 0.00-100.00 percentage (matches DECIMAL(5,2) column)
        var avgConfidencePercent = wordCount > 0
            ? (decimal)(totalConfidence / wordCount * 100)
            : 0m;

        return new OcrResponse(
            RawText: rawText,
            ConfidenceScore: Math.Round(avgConfidencePercent, 2),
            ProcessingTime: TimeSpan.Zero, // Set by caller
            EngineUsed: "" // Set by caller
        );
    }
}

// ═══ Azure API Response Models (internal) ═══

internal class AzureAnalyzeResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("analyzeResult")]
    public AzureAnalyzeResult? AnalyzeResult { get; set; }

    [JsonPropertyName("error")]
    public AzureErrorInfo? Error { get; set; }
}

internal class AzureAnalyzeResult
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("pages")]
    public List<AzureOcrPage>? Pages { get; set; }
}

internal class AzureOcrPage
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("lines")]
    public List<AzureOcrLine>? Lines { get; set; }

    [JsonPropertyName("words")]
    public List<AzureOcrWord>? Words { get; set; }
}

internal class AzureOcrLine
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

internal class AzureOcrWord
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal class AzureErrorInfo
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
```

**✅ Checkpoint**: Build thành công (AzureOcrProvider).

---

### SupabaseStorageService — Thay thế StubFileStorageService (Week 4)

> **Tại sao Supabase?**: Supabase Storage cung cấp S3-compatible object storage với public URL, miễn phí cho tier nhỏ, dễ setup. File upload thật sẽ được gửi lên Supabase, trả về public URL cho client hiển thị ảnh.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Services/SupabaseStorageService.cs
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
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
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Supabase upload failed ({response.StatusCode}): {error}");
        }

        // 4. Return result with public URL
        return new StorageFileResult(
            ObjectKey: objectKey,
            PublicUrl: GetPublicUrl(objectKey),
            OriginalFileName: fileName,
            MimeType: contentType,
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

    public string GetPublicUrl(string objectKey)
    {
        return $"{_publicBaseUrl.TrimEnd('/')}/{_bucketName}/{objectKey}";
    }
}
```

> **⚠️ Migration Note**: Sau khi deploy SupabaseStorageService, các StorageFile records cũ (từ Week 4 stub) vẫn có `StorageProvider = "stub"` và placeholder URL. Có thể chạy migration script để re-upload nếu cần, hoặc chấp nhận records cũ có URL không hợp lệ.

**✅ Checkpoint**: Build thành công (SupabaseStorageService).

---

## 🎯 PROMPT 8/10 — MedicalRecordAiService (Full Pipeline)

**Nhiệm vụ**: Implement full pipeline: OCR → RAG Context Retrieval → Prompt Building → Gemini Extraction → Save Results.

**⚠️ Đặt ở `Application/Services/`** — đây là business logic layer.

**Code — Service Interface**:

```csharp
// File: FPT.EXE201.Application/IServices/IMedicalRecordAiService.cs
using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý full pipeline: OCR → AI Extraction cho medical records.
/// ⚠️ CHỈ áp dụng cho PRENATAL_CHECKUP documents. Các loại khác sẽ bị reject.
/// Dùng RAG pattern để inject pregnancy context vào AI prompt.
/// </summary>
public interface IMedicalRecordAiService
{
    /// <summary>
    /// Chạy full pipeline cho 1 PRENATAL_CHECKUP document:
    /// 1. Validate DocumentType == PRENATAL_CHECKUP
    /// 2. Download file từ storage
    /// 3. Azure OCR → raw text
    /// 4. Retrieve pregnancy context (RAG)
    /// 5. Build prompt (Rule Layers + Context)
    /// 6. Gemini extraction → structured JSON
    /// 7. Save kết quả vào OcrResult
    /// ⚠️ Throws BadRequestException nếu DocumentType != PRENATAL_CHECKUP
    /// </summary>
    Task<OcrResultDto> ProcessDocumentAsync(
        Guid documentId, Guid currentUserId,
        string? languageHint = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chạy lại extraction (chỉ Gemini, dùng raw text đã có) với template mới hoặc context mới.
    /// </summary>
    Task<OcrResultDto> ReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
```

**Code — RAG Context Model**:

```csharp
// File: FPT.EXE201.Application/AI/ExtractionModels/PregnancyContext.cs
namespace FPT.EXE201.Application.AI.ExtractionModels;

/// <summary>
/// RAG context retrieved from database.
/// Injected vào AI prompt để cung cấp context cho extraction chính xác hơn.
/// ⚡ Reusable cho Nutrition Planning AI (Week sau).
/// </summary>
public class PregnancyContext
{
    public Guid PregnancyId { get; set; }
    public int? CurrentGestationalWeek { get; set; }
    public string? PregnancyStatus { get; set; }

    /// <summary>Danh sách bệnh lý đã biết (từ PregnancyConditions).</summary>
    public List<string> KnownConditions { get; set; } = new();

    /// <summary>Tóm tắt medical record gần nhất (cho consistency check).</summary>
    public string? PreviousRecordSummary { get; set; }
}
```

**Code — MedicalRecordAiService Implementation**:

```csharp
// File: FPT.EXE201.Application/Services/MedicalRecordAiService.cs
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI;
using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MedicalRecordAiService : IMedicalRecordAiService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiProvider _aiProvider;
    private readonly IOcrProvider _ocrProvider;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalRecordAiService> _logger;

    private const string TemplateKey = "medical_record.extraction";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MedicalRecordAiService(
        IUnitOfWork unitOfWork,
        IAiProvider aiProvider,
        IOcrProvider ocrProvider,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<MedicalRecordAiService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _ocrProvider = ocrProvider;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OcrResultDto> ProcessDocumentAsync(
        Guid documentId, Guid currentUserId,
        string? languageHint = "vi",
        CancellationToken cancellationToken = default)
    {
        // 1. Verify document exists + ownership
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        // 1.5. ⚠️ Validate DocumentType = PRENATAL_CHECKUP (4-Phase Flow Rule)
        var docTypeCode = document.DocumentType?.Code;
        if (docTypeCode != "PRENATAL_CHECKUP")
            throw new BadRequestException(
                "OCR + AI extraction is only supported for PRENATAL_CHECKUP documents. " +
                $"This document is '{docTypeCode ?? "unset"}'.");

        // 2. Get next run number
        var latestOcr = await _unitOfWork.OcrResults.GetLatestByDocumentIdAsync(documentId, cancellationToken);
        var nextRunNo = (latestOcr?.OcrRunNumber ?? 0) + 1;

        // 3. Create OcrResult with Pending status
        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = nextRunNo,
            Status = OcrStatus.Pending,
            LanguageHint = languageHint ?? "vi"
        };
        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // 4. Phase 1: OCR — Azure Document Intelligence
            ocrResult.Status = OcrStatus.OcrProcessing;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ocrResponse = await RunOcrAsync(document, languageHint, cancellationToken);

            ocrResult.RawText = ocrResponse.RawText;
            ocrResult.ConfidenceScore = ocrResponse.ConfidenceScore;
            ocrResult.OcrEngine = ocrResponse.EngineUsed;
            ocrResult.OcrProcessingTimeMs = (int)ocrResponse.ProcessingTime.TotalMilliseconds;
            ocrResult.Status = OcrStatus.OcrCompleted;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("OCR completed for document {DocId}, run {Run}. Text length: {Len}",
                documentId, nextRunNo, ocrResponse.RawText.Length);

            // 5. Validate OCR output
            if (string.IsNullOrWhiteSpace(ocrResponse.RawText))
            {
                ocrResult.Status = OcrStatus.Failed;
                ocrResult.ErrorMessage = "OCR returned empty text. The image may be blank or unreadable.";
                _unitOfWork.OcrResults.Update(ocrResult);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return _mapper.Map<OcrResultDto>(ocrResult);
            }

            // 6. Phase 2: AI Extraction — Gemini
            ocrResult.Status = OcrStatus.AiExtracting;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var aiResult = await RunAiExtractionAsync(
                ocrResponse.RawText, document.PregnancyId, cancellationToken);

            ocrResult.StructuredJson = aiResult.StructuredJson;
            ocrResult.AiModelUsed = aiResult.ModelUsed;
            ocrResult.AiTokensUsed = aiResult.TotalTokens;
            ocrResult.AiProcessingTimeMs = (int)aiResult.ProcessingTime.TotalMilliseconds;
            ocrResult.AiPromptTemplateId = aiResult.TemplateId;
            ocrResult.Status = OcrStatus.Succeeded;

            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AI extraction completed for document {DocId}, run {Run}. Tokens: {Tokens}",
                documentId, nextRunNo, aiResult.TotalTokens);

            return _mapper.Map<OcrResultDto>(ocrResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed for document {DocId}, run {Run} at status {Status}",
                documentId, nextRunNo, ocrResult.Status);

            ocrResult.Status = OcrStatus.Failed;
            ocrResult.ErrorMessage = $"Pipeline failed at {ocrResult.Status}: {ex.Message}";
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    public async Task<OcrResultDto> ReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get existing OCR result
        var existingOcr = await _unitOfWork.OcrResults.GetByIdAsync(ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        if (string.IsNullOrWhiteSpace(existingOcr.RawText))
            throw new BadRequestException("No raw text available for re-extraction. Please run the full pipeline again.");

        // 2. Verify ownership through document → pregnancy chain
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(existingOcr.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        // 3. Create new OcrResult (new run number, copy raw text)
        var nextRunNo = existingOcr.OcrRunNumber + 1;
        var ocrResult = new OcrResult
        {
            DocumentId = existingOcr.DocumentId,
            OcrRunNumber = nextRunNo,
            Status = OcrStatus.AiExtracting,
            LanguageHint = existingOcr.LanguageHint,
            RawText = existingOcr.RawText,
            ConfidenceScore = existingOcr.ConfidenceScore,
            OcrEngine = existingOcr.OcrEngine,
            OcrProcessingTimeMs = existingOcr.OcrProcessingTimeMs
        };
        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // 4. Run AI extraction only (skip OCR)
            var aiResult = await RunAiExtractionAsync(
                existingOcr.RawText, document.PregnancyId, cancellationToken);

            ocrResult.StructuredJson = aiResult.StructuredJson;
            ocrResult.AiModelUsed = aiResult.ModelUsed;
            ocrResult.AiTokensUsed = aiResult.TotalTokens;
            ocrResult.AiProcessingTimeMs = (int)aiResult.ProcessingTime.TotalMilliseconds;
            ocrResult.AiPromptTemplateId = aiResult.TemplateId;
            ocrResult.Status = OcrStatus.Succeeded;

            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OcrResultDto>(ocrResult);
        }
        catch (Exception ex)
        {
            ocrResult.Status = OcrStatus.Failed;
            ocrResult.ErrorMessage = $"Re-extraction failed: {ex.Message}";
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // ═══════════════════════════════════════════════
    // Private: OCR Phase
    // ═══════════════════════════════════════════════

    private async Task<OcrResponse> RunOcrAsync(
        MedicalDocument document, string? languageHint, CancellationToken cancellationToken)
    {
        var files = document.Files.OrderBy(f => f.SortOrder).ToList();
        if (files.Count == 0)
            throw new InvalidOperationException("Document has no files attached.");

        // Single file — original behaviour
        if (files.Count == 1)
        {
            var sf = files[0].StorageFile;
            var stream = await _fileStorageService.DownloadAsync(sf.ObjectKey, cancellationToken);
            return await _ocrProvider.ExtractTextAsync(
                new OcrRequest(stream, sf.OriginalFileName, sf.MimeType, languageHint), cancellationToken);
        }

        // Multi-file — OCR each page, concatenate with separators
        var allTexts = new List<string>();
        double totalConf = 0; int confCount = 0; long totalMs = 0;

        for (int i = 0; i < files.Count; i++)
        {
            var sf = files[i].StorageFile;
            var stream = await _fileStorageService.DownloadAsync(sf.ObjectKey, cancellationToken);
            var page = await _ocrProvider.ExtractTextAsync(
                new OcrRequest(stream, sf.OriginalFileName, sf.MimeType, languageHint), cancellationToken);

            allTexts.Add($"--- Page {i + 1} ---");
            allTexts.Add(page.ExtractedText);
            totalConf += page.Confidence; confCount++;
            totalMs += page.ProcessingTimeMs;
        }

        return new OcrResponse(
            ExtractedText: string.Join("\n", allTexts),
            Confidence: confCount > 0 ? totalConf / confCount : 0,
            ProcessingTimeMs: totalMs,
            PageCount: files.Count);
    }

    // ═══════════════════════════════════════════════
    // Private: AI Extraction Phase (RAG + Prompt + Gemini)
    // ═══════════════════════════════════════════════

    private record AiExtractionPipelineResult(
        string StructuredJson, string ModelUsed, int TotalTokens,
        TimeSpan ProcessingTime, Guid? TemplateId);

    private async Task<AiExtractionPipelineResult> RunAiExtractionAsync(
        string rawText, Guid pregnancyId, CancellationToken cancellationToken)
    {
        // Step 1: Load prompt template from DB
        var template = await _unitOfWork.AiPromptTemplates
            .GetActiveByKeyAsync(TemplateKey, cancellationToken)
            ?? throw new NotFoundException($"AI prompt template '{TemplateKey}' not found. Please seed the database.");

        // Step 2: Retrieve RAG context
        var context = await RetrievePregnancyContextAsync(pregnancyId, cancellationToken);

        // Step 3: Build prompt using Rule Layers + RAG Context
        var prompt = PromptBuilder.FromTemplate(template)
            .WithContext("PATIENT CONTEXT", FormatPregnancyContext(context))
            .WithUserMessage($"Extract structured data from this OCR text:\n\n---\n{rawText}\n---")
            .Build();

        // Step 4: Call Gemini
        var aiResponse = await _aiProvider.GenerateAsync(prompt, cancellationToken);

        // Step 5: Validate JSON response
        var structuredJson = ValidateAndFormatJson(aiResponse.Content);

        return new AiExtractionPipelineResult(
            StructuredJson: structuredJson,
            ModelUsed: aiResponse.ModelUsed,
            TotalTokens: aiResponse.TotalTokens,
            ProcessingTime: aiResponse.ProcessingTime,
            TemplateId: template.Id
        );
    }

    // ═══════════════════════════════════════════════
    // Private: RAG Context Retrieval
    // ═══════════════════════════════════════════════

    private async Task<PregnancyContext> RetrievePregnancyContextAsync(
        Guid pregnancyId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null) return new PregnancyContext { PregnancyId = pregnancyId };

        // Calculate current gestational week from LMP
        // ⚠️ LastMenstrualPeriodDate is DateOnly (not DateTime) — use DayNumber arithmetic
        int? gestWeek = null;
        if (pregnancy.LastMenstrualPeriodDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalDays = today.DayNumber - pregnancy.LastMenstrualPeriodDate.Value.DayNumber;
            gestWeek = totalDays >= 0 && totalDays <= 315 ? totalDays / 7 : null;
        }

        // Get known conditions
        var conditions = await _unitOfWork.PregnancyConditions
            .GetByPregnancyIdAsync(pregnancyId, "vi", cancellationToken);
        var conditionNames = conditions
            .Select(c => c.Condition?.Translations?.FirstOrDefault()?.DisplayName ?? c.Condition?.Code ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        // Get most recent OCR result for consistency context
        string? previousSummary = null;
        var recentDocs = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, cancellationToken: cancellationToken);
        var recentOcr = recentDocs
            .SelectMany(d => d.OcrResults ?? Enumerable.Empty<OcrResult>())
            .Where(o => o.Status == OcrStatus.Succeeded && !string.IsNullOrEmpty(o.StructuredJson))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (recentOcr != null)
        {
            // Extract summary from previous extraction for consistency
            try
            {
                var prevResult = JsonSerializer.Deserialize<MedicalRecordExtractionResult>(
                    recentOcr.StructuredJson!, JsonOptions);
                if (prevResult?.VitalsData?.Interview != null)
                {
                    previousSummary = $"Previous record ({recentOcr.CreatedAt:yyyy-MM-dd}): " +
                        $"Week {prevResult.VitalsData.Interview.GestationalWeek}, " +
                        $"Weight {prevResult.VitalsData?.Examination?.VitalSigns?.WeightKg}kg, " +
                        $"BP {prevResult.VitalsData?.Examination?.VitalSigns?.BloodPressureSystolic}/{prevResult.VitalsData?.Examination?.VitalSigns?.BloodPressureDiastolic}";
                }
            }
            catch { /* Ignore deserialization errors in context */ }
        }

        return new PregnancyContext
        {
            PregnancyId = pregnancyId,
            CurrentGestationalWeek = gestWeek,
            PregnancyStatus = pregnancy.Status.ToString(),
            KnownConditions = conditionNames,
            PreviousRecordSummary = previousSummary
        };
    }

    private static string FormatPregnancyContext(PregnancyContext context)
    {
        var parts = new List<string>();

        if (context.CurrentGestationalWeek.HasValue)
            parts.Add($"Current gestational week: {context.CurrentGestationalWeek}");

        if (!string.IsNullOrEmpty(context.PregnancyStatus))
            parts.Add($"Pregnancy status: {context.PregnancyStatus}");

        if (context.KnownConditions.Any())
            parts.Add($"Known conditions: {string.Join(", ", context.KnownConditions)}");

        if (!string.IsNullOrEmpty(context.PreviousRecordSummary))
            parts.Add(context.PreviousRecordSummary);

        return parts.Any()
            ? string.Join("\n", parts)
            : "No prior pregnancy data available.";
    }

    // ═══════════════════════════════════════════════
    // Private: JSON Validation
    // ═══════════════════════════════════════════════

    private string ValidateAndFormatJson(string content)
    {
        try
        {
            // Parse to validate + re-format
            var parsed = JsonSerializer.Deserialize<MedicalRecordExtractionResult>(content, JsonOptions);
            if (parsed == null)
                throw new BadRequestException("AI returned null JSON.");

            // Re-serialize with formatting
            return JsonSerializer.Serialize(parsed, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI response is not valid JSON for expected schema. Storing raw.");
            // Store raw AI response even if it doesn't match our schema exactly
            return content;
        }
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 9/10 — Repository Interface + UnitOfWork Update + Enhanced OcrService + Background Processing

**Nhiệm vụ**: Thêm `IAiPromptTemplateRepository`, update `IUnitOfWork` + `UnitOfWork`, enhance `OcrService` với background job queue, tạo OcrBackgroundService + OcrJobQueue, update `StorageProvider` trong MedicalDocumentService.

> **⚠️ Nhớ cập nhật**: Trong `MedicalDocumentService.CreateWithFilesAsync()` (Week 4), đổi `StorageProvider = "stub"` → `StorageProvider = "supabase"` vì giờ đã dùng SupabaseStorageService thật.

**Code — Update StorageProvider trong MedicalDocumentService** (1 dòng):

```csharp
// File: FPT.EXE201.Application/Services/MedicalDocumentService.cs
// Trong method CreateWithFilesAsync(), tìm dòng:
//   StorageProvider = "stub",
// Đổi thành:
            StorageProvider = "supabase",
```

**Code — AiPromptTemplate Repository Interface**:

```csharp
// File: FPT.EXE201.Application/IRepositories/IAiPromptTemplateRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IAiPromptTemplateRepository : IGenericRepository<AiPromptTemplate>
{
    /// <summary>
    /// Lấy active prompt template theo key (latest version).
    /// Ví dụ: GetActiveByKeyAsync("medical_record.extraction")
    /// </summary>
    Task<AiPromptTemplate?> GetActiveByKeyAsync(string templateKey, CancellationToken cancellationToken = default);
}
```

**Code — AiPromptTemplate Repository Implementation**:

```csharp
// File: FPT.EXE201.Infrastructure/Repositories/AiPromptTemplateRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class AiPromptTemplateRepository : GenericRepository<AiPromptTemplate>, IAiPromptTemplateRepository
{
    public AiPromptTemplateRepository(AppDbContext context) : base(context) { }

    public async Task<AiPromptTemplate?> GetActiveByKeyAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TemplateKey == templateKey && t.IsActive && t.DeletedAt == null)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

**Update `IUnitOfWork`** — thêm Week 5 repository:

```csharp
// Add to IUnitOfWork.cs (after existing Week 4 properties)

// Week 5 — AI Infrastructure
IAiPromptTemplateRepository AiPromptTemplates { get; }
```

**Update `UnitOfWork`** — lazy init:

```csharp
// Add to UnitOfWork.cs

// Week 5
private IAiPromptTemplateRepository? _aiPromptTemplates;

public IAiPromptTemplateRepository AiPromptTemplates
    => _aiPromptTemplates ??= new AiPromptTemplateRepository(_context);
```

**Code — IOcrJobQueue Interface** (Application layer):

```csharp
// File: FPT.EXE201.Application/IServices/IOcrJobQueue.cs

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// In-process job queue for OCR processing.
/// Uses System.Threading.Channels internally — no external dependencies.
/// </summary>
public interface IOcrJobQueue
{
    /// <summary>Enqueue a document for OCR + AI processing. Returns immediately.</summary>
    ValueTask EnqueueAsync(OcrJobItem job, CancellationToken cancellationToken = default);

    /// <summary>Dequeue next job (blocks until available). Used by BackgroundService.</summary>
    ValueTask<OcrJobItem> DequeueAsync(CancellationToken cancellationToken);
}

/// <summary>Job payload for background OCR processing.</summary>
public record OcrJobItem(
    Guid OcrResultId,
    Guid DocumentId,
    Guid UserId,
    string LanguageHint = "vi"
);
```

**Code — OcrJobQueue Implementation** (Infrastructure layer):

```csharp
// File: FPT.EXE201.Infrastructure/Services/OcrJobQueue.cs

using System.Threading.Channels;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Channel-based in-process job queue.
/// Capacity: 100 pending jobs. If full, waits (backpressure).
/// ⚠️ Jobs are lost on app restart — acceptable for this app scale.
/// For durability, upgrade to Redis Queue or database-backed queue.
/// </summary>
public class OcrJobQueue : IOcrJobQueue
{
    private readonly Channel<OcrJobItem> _channel;

    public OcrJobQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<OcrJobItem>(options);
    }

    public async ValueTask EnqueueAsync(OcrJobItem job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<OcrJobItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
```

**Code — OcrBackgroundService** (Infrastructure layer):

```csharp
// File: FPT.EXE201.Infrastructure/Services/OcrBackgroundService.cs

using FPT.EXE201.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Background worker that continuously dequeues OCR jobs and processes them.
/// Runs as a hosted service — starts with the app, stops on shutdown.
/// Uses IServiceScopeFactory to create scoped services (UnitOfWork, etc.) per job.
/// </summary>
public class OcrBackgroundService : BackgroundService
{
    private readonly IOcrJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OcrBackgroundService> _logger;

    public OcrBackgroundService(
        IOcrJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<OcrBackgroundService> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Background Service started. Waiting for jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            OcrJobItem? job = null;
            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Processing OCR job: DocumentId={DocumentId}, OcrResultId={OcrResultId}",
                    job.DocumentId, job.OcrResultId);

                // ⚠️ MUST create a new scope per job.
                // BackgroundService is Singleton, but MedicalRecordAiService/UnitOfWork are Scoped.
                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IMedicalRecordAiService>();

                await aiService.ProcessDocumentAsync(
                    job.DocumentId, job.UserId, job.LanguageHint, stoppingToken);

                _logger.LogInformation(
                    "OCR job completed: DocumentId={DocumentId}", job.DocumentId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App shutting down — normal exit
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OCR job failed: DocumentId={DocumentId}, OcrResultId={OcrResultId}. Error: {Message}",
                    job?.DocumentId, job?.OcrResultId, ex.Message);

                // ⚠️ ProcessDocumentAsync already sets OcrResult.Status = Failed internally.
                // No need to update here — just log and continue to next job.
            }
        }

        _logger.LogInformation("OCR Background Service stopped.");
    }
}
```

**Code — Enhanced OcrService** (replace Week 4 stub):

```csharp
// File: FPT.EXE201.Infrastructure/Services/OcrService.cs
// ⚠️ REPLACE toàn bộ OcrService Week 4 stub

using AutoMapper;
using FPT.EXE201.Application;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Enhanced OcrService — Replaces Week 4 stub.
/// ✅ PRODUCTION-READY: QueueOcrAsync creates a Pending OcrResult + enqueues
/// a background job. Upload endpoint returns immediately (~100ms).
/// OcrBackgroundService processes the job asynchronously (10-30s).
/// Flutter polls GET /api/ocr/{id}/status until Succeeded/Failed.
/// </summary>
public class OcrService : IOcrService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOcrJobQueue _jobQueue;
    private readonly IMapper _mapper;

    public OcrService(IUnitOfWork unitOfWork, IOcrJobQueue jobQueue, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _jobQueue = jobQueue;
        _mapper = mapper;
    }

    public async Task<Guid> QueueOcrAsync(
        Guid documentId, string? languageHint = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Create OcrResult with Pending status (saved to DB immediately)
        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = 1,
            Status = OcrStatus.Pending,
            LanguageHint = languageHint ?? "vi"
        };

        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Get document owner for background job
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        // 3. Enqueue background job — returns immediately, does NOT block
        await _jobQueue.EnqueueAsync(new OcrJobItem(
            OcrResultId: ocrResult.Id,
            DocumentId: documentId,
            UserId: document.Pregnancy.UserId,
            LanguageHint: languageHint ?? "vi"
        ), cancellationToken);

        return ocrResult.Id;
    }

    public async Task<OcrResultDto> GetResultAsync(
        Guid ocrResultId, CancellationToken cancellationToken = default)
    {
        var ocr = await _unitOfWork.OcrResults.GetByIdAsync(ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        return _mapper.Map<OcrResultDto>(ocr);
    }

    public async Task<List<OcrResultDto>> GetByDocumentIdAsync(
        Guid documentId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Keep backward compatibility with Week 4 endpoint
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        var ocrResults = await _unitOfWork.OcrResults.GetByDocumentIdAsync(documentId, cancellationToken);
        return _mapper.Map<List<OcrResultDto>>(ocrResults);
    }
}
```

**⚠️ CRITICAL: Update MedicalDocumentService.CreateWithFilesAsync** — thêm PRENATAL_CHECKUP guard trước khi gọi QueueOcrAsync:

```csharp
// File: FPT.EXE201.Application/Services/MedicalDocumentService.cs
// ⚠️ THAY THẾ đoạn "7. Queue OCR for images/PDFs" hiện tại:

        // 7. Queue OCR for PRENATAL_CHECKUP images/PDFs ONLY (non-blocking)
        // ⚠️ Per 4-Phase flow: OCR chỉ chạy cho PRENATAL_CHECKUP.
        // Test types (BLOOD_TEST, ULTRASOUND...) và Others (PRESCRIPTION...) KHÔNG chạy OCR.
        // ✅ QueueOcrAsync enqueues a background job and returns immediately.
        // Flutter sẽ poll GET /api/ocr/{id}/status để check khi nào xong.
        if (dto.DocumentTypeId.HasValue && (contentType.StartsWith("image/") || contentType == "application/pdf"))
        {
            var uploadedDocType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
                dto.DocumentTypeId.Value, cancellationToken: cancellationToken);
            if (uploadedDocType?.Code == "PRENATAL_CHECKUP")
            {
                await _ocrService.QueueOcrAsync(document.Id, "vi", cancellationToken);
            }
        }
```

**⚠️ QUAN TRỌNG**: Nếu KHÔNG thêm guard này, upload BLOOD_TEST/PRESCRIPTION sẽ crash với 400 BadRequest vì `ProcessDocumentAsync` validate `DocumentType == PRENATAL_CHECKUP`.

**⚠️ AppDbContext**: Thêm `DbSet<AiPromptTemplate>` vào `AppDbContext.cs`:

```csharp
// Thêm sau dòng "public DbSet<OcrResult> OcrResults { get; set; }"

// Week 5 — AI Prompt Templates
public DbSet<AiPromptTemplate> AiPromptTemplates { get; set; }
```

**Update AutoMapper Profile** — thêm mapping cho AI fields:

```csharp
// ⚠️ THÊM vào MedicalDocumentProfile.cs đã có từ Week 4:

        // Week 5: Updated OcrResult → OcrResultDto mapping (thêm AI fields)
        // AutoMapper tự map all matching properties by name.
        // .ToString() cho Status enum → string đã được handle tự động vì OcrResultDto.Status là string.
        CreateMap<OcrResult, OcrResultDto>();
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 10/10 — Controllers + DI Registration + appsettings

**Nhiệm vụ**: Update Controllers, DI registration cho AI services, cấu hình API keys, permissions.

**Code — Updated OcrController** (replace Week 4):

```csharp
// File: FPT.EXE201.Api/Controllers/OcrController.cs
// ⚠️ REPLACE toàn bộ controller Week 4

using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
public class OcrController : BaseApiController
{
    private readonly IMedicalRecordAiService _aiService;
    private readonly IOcrService _ocrService;

    public OcrController(IMedicalRecordAiService aiService, IOcrService ocrService)
    {
        _aiService = aiService;
        _ocrService = ocrService;
    }

    /// <summary>
    /// Chạy full pipeline OCR + AI Extraction cho PRENATAL_CHECKUP document.
    /// Flow: Validate type → Azure OCR → RAG Context → Gemini Extraction → Structured JSON.
    /// ⚠️ CHỈ áp dụng cho PRENATAL_CHECKUP. Gọi cho loại khác → 400 BadRequest.
    /// </summary>
    [HttpPost("documents/{documentId}/ocr/process")]
    [RequirePermission("ocr.trigger")]
    public async Task<IActionResult> ProcessDocument(
        Guid documentId,
        [FromQuery] string lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.ProcessDocumentAsync(documentId, userId, lang, cancellationToken);
        return Created(result, "OCR + AI extraction completed successfully.");
    }

    /// <summary>
    /// Chạy lại AI extraction (chỉ Gemini, dùng raw text đã có).
    /// Hữu ích khi update prompt template hoặc context đã thay đổi.
    /// </summary>
    [HttpPost("ocr/{ocrResultId}/re-extract")]
    [RequirePermission("ocr.trigger")]
    public async Task<IActionResult> ReExtract(
        Guid ocrResultId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.ReExtractAsync(ocrResultId, userId, cancellationToken);
        return Created(result, "AI re-extraction completed successfully.");
    }

    /// <summary>Kiểm tra trạng thái + kết quả OCR.</summary>
    [HttpGet("ocr/{id}/status")]
    [RequirePermission("ocr.view")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ocrService.GetResultAsync(id, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// Get all OCR results for a document (ordered by run number desc).
    /// Backward-compatible endpoint from Week 4.
    /// </summary>
    [HttpGet("documents/{documentId}/ocr")]
    [RequirePermission("ocr.view")]
    public async Task<IActionResult> GetByDocumentId(
        Guid documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var results = await _ocrService.GetByDocumentIdAsync(documentId, userId, cancellationToken);
        return Success(results);
    }
}
```

**Code — DI Registration**:

```csharp
// ════════════════════════════════════════════════
// Add to FPT.EXE201.Infrastructure/DependencyInjection.cs
// (trong method AddInfrastructure)
// ════════════════════════════════════════════════

using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Infrastructure.AI;

// Week 5 — AI Provider HTTP Clients
// ⚠️ Dùng AddHttpClient<IInterface, TImpl>() để register cả HttpClient + DI mapping cùng lúc.
// KHÔNG dùng AddHttpClient<T>() + AddScoped<I,T>() riêng biệt (sẽ bypass HttpClientFactory).
services.AddHttpClient<IAiProvider, GeminiAiProvider>(client =>
{
    var baseUrl = configuration["AI:Gemini:BaseUrl"]
        ?? "https://generativelanguage.googleapis.com/v1beta/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(
        int.Parse(configuration["AI:Gemini:TimeoutSeconds"] ?? "60"));
});

services.AddHttpClient<IOcrProvider, AzureOcrProvider>(client =>
{
    var endpoint = configuration["AI:AzureDocumentIntelligence:Endpoint"]
        ?? throw new InvalidOperationException("AI:AzureDocumentIntelligence:Endpoint is required.");
    client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(
        int.Parse(configuration["AI:AzureDocumentIntelligence:TimeoutSeconds"] ?? "120"));
});

// Week 5 — Supabase Storage (replaces Week 4 StubFileStorageService)
// ⚠️ REPLACE dòng services.AddScoped<IFileStorageService, StubFileStorageService>() cũ của Week 4
services.AddHttpClient<IFileStorageService, SupabaseStorageService>(client =>
{
    var supabaseUrl = configuration["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url is required.");
    var serviceKey = configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is required.");

    client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/storage/v1/");
    client.DefaultRequestHeaders.Add("apikey", serviceKey);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);
});

// Week 5 — Infrastructure services
// ⚠️ IOcrService đã có từ Week 4. REPLACE dòng cũ, KHÔNG thêm duplicate.
services.AddScoped<IOcrService, OcrService>();

// Week 5 — Background OCR Processing (Channel + BackgroundService)
// ⚠️ OcrJobQueue PHẢI là Singleton (shared giữa OcrService và BackgroundService)
services.AddSingleton<IOcrJobQueue, OcrJobQueue>();
services.AddHostedService<OcrBackgroundService>();

// ════════════════════════════════════════════════
// Add to FPT.EXE201.Application/DependencyInjection.cs
// (trong method AddApplication)
// ════════════════════════════════════════════════

// Week 5 — AI Application Services
services.AddScoped<IMedicalRecordAiService, MedicalRecordAiService>();
```

**Code — appsettings.json update**:

```json
{
  "Supabase": {
    "Url": "https://YOUR_PROJECT.supabase.co",
    "ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY_HERE",
    "Storage": {
      "BucketName": "medical-documents",
      "PublicBaseUrl": "https://YOUR_PROJECT.supabase.co/storage/v1/object/public"
    }
  },
  "AI": {
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
      "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/",
      "DefaultModel": "gemini-2.5-flash",
      "TimeoutSeconds": "60"
    },
    "AzureDocumentIntelligence": {
      "Endpoint": "https://YOUR_RESOURCE.cognitiveservices.azure.com",
      "ApiKey": "YOUR_AZURE_KEY_HERE",
      "ModelId": "prebuilt-read",
      "ApiVersion": "2024-11-30",
      "PollingIntervalMs": "1000",
      "TimeoutSeconds": "120"
    }
  }
}
```

**⚠️ SECURITY**: Trong production, đặt API keys + Supabase keys vào `appsettings.Development.json` hoặc User Secrets hoặc Azure Key Vault. KHÔNG commit keys vào git.

**Permissions to seed** (thêm vào PermissionSeeder):

```
ocr.trigger      — Chạy OCR + AI extraction
ocr.view         — Xem kết quả OCR
ai.admin         — Quản lý prompt templates (future)
```

**✅ Final Checkpoint**:
1. Build project thành công
2. Run migration (ALTER + CREATE TABLE)
3. Seed prompt template
4. Verify Supabase Storage bucket `medical-documents` đã tạo (public read)
5. Test file upload qua `POST /api/pregnancies/{id}/documents` → file được lưu trên Supabase, public URL hoạt động
6. Test API endpoints:
   - `POST /api/documents/{id}/ocr/process?lang=vi` → Full pipeline
   - `POST /api/ocr/{id}/re-extract` → Re-run AI extraction only
   - `GET /api/ocr/{id}/status` → Check OCR result
7. Verify: Upload ảnh phiếu khám (PRENATAL_CHECKUP) → Response ngay (<1s) → Background processing → Poll status → Succeeded + StructuredJson → meaningful data
8. Verify: StructuredJson contains meaningful medical data
9. Verify: StorageFile records có `StorageProvider = "supabase"` và real public URL
10. ⚠️ Negative test: Upload BLOOD_TEST/PRESCRIPTION document → NO OCR queued (upload thành công, không có OcrResult)
11. ⚠️ Console test: verify "OCR Background Service started" log on app startup
12. ⚠️ Console test: verify "Processing OCR job" + "OCR job completed" logs after PRENATAL_CHECKUP upload

---

## ✅ WEEK 5 COMPLETE — FINAL CHECKLIST

### 1. Database Layer
- [ ] `ai_prompt_templates` table created
- [ ] `ocr_results` table altered (5 new columns: ocr_processing_time_ms, ai_model_used, ai_tokens_used, ai_processing_time_ms, ai_prompt_template_id)
- [ ] Seed: medical_record.extraction template v1

### 2. Domain Layer
- [ ] `AiPromptTemplate` entity extends `BaseEntity`
- [ ] `OcrResult` entity updated (5 new properties + navigation)
- [ ] `OcrStatus` enum expanded: Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded / Failed

### 3. Application Layer — AI Core (`Application/AI/`)
- [ ] `IAiProvider` interface (GenerateAsync + ChatAsync)
- [ ] `IOcrProvider` interface (ExtractTextAsync + SupportedContentTypes)
- [ ] `AiPrompt`, `AiResponse`, `AiMessage` records
- [ ] `OcrRequest`, `OcrResponse` records
- [ ] `PromptBuilder` — fluent builder with Rule Layers + FromTemplate
- [ ] `MedicalRecordExtractionResult` + sub-models (strongly-typed)
- [ ] `PregnancyContext` — RAG context model

### 4. Application Layer — Services
- [ ] `IMedicalRecordAiService` interface
- [ ] `MedicalRecordAiService` implementation:
  - [ ] `ProcessDocumentAsync` validates `DocumentType == PRENATAL_CHECKUP` before OCR
  - [ ] Full pipeline: OCR → RAG → Prompt → Gemini → Save
  - [ ] ReExtract: skip OCR, re-run AI with updated context/template
  - [ ] Context retrieval: pregnancy week, conditions, previous records
  - [ ] JSON validation + formatting
  - [ ] Error handling at each pipeline phase
  - [ ] Negative test: non-PRENATAL_CHECKUP → 400 BadRequest

### 5. Infrastructure Layer — Third-party Providers
- [ ] `GeminiAiProvider`: REST API client with typed request/response models
- [ ] `AzureOcrProvider`: Document Intelligence with async polling
- [ ] `SupabaseStorageService`: Supabase Storage upload/download/delete (replaces Week 4 StubFileStorageService)
- [ ] All three use `AddHttpClient<IInterface, TImpl>()` for proper HttpClient + DI mapping

### 6. Infrastructure Layer — Repository + UnitOfWork + Background Processing
- [ ] `IAiPromptTemplateRepository` + `AiPromptTemplateRepository`
- [ ] `IUnitOfWork.AiPromptTemplates` property
- [ ] `UnitOfWork` lazy init
- [ ] Enhanced `OcrService` (replaces Week 4 stub, non-blocking queue pattern)
- [ ] `IOcrJobQueue` interface (Application layer)
- [ ] `OcrJobQueue` implementation (Channel-based, Infrastructure layer)
- [ ] `OcrBackgroundService` (BackgroundService, Infrastructure layer)

### 7. API Layer
- [ ] Updated `OcrController` with 3 endpoints + backward-compat `GetByDocumentId`
- [ ] DI registration for all AI services + SupabaseStorageService
- [ ] DI: `AddSingleton<IOcrJobQueue, OcrJobQueue>()`
- [ ] DI: `AddHostedService<OcrBackgroundService>()`
- [ ] `appsettings.json` — Supabase config + AI configuration sections

### 8. Architecture Reusability (ready for Nutrition Week)
- [ ] `IAiProvider.ChatAsync()` — ready for nutrition chat
- [ ] `PromptBuilder.FromTemplate()` — load template by key
- [ ] `PregnancyContext` model — reusable for nutrition context
- [ ] Rule Layer System — add new templates for nutrition without code changes
- [ ] Seed new template `nutrition.meal_planning` when needed

---

### 🧪 TESTING WORKFLOW

```bash
# 1. Run API
dotnet run --project src/FPT.EXE201.Api

# 2. Test Full Pipeline
# Prerequisites:
#   - Có pregnancy + document đã upload (from Week 3+4)
#   - Supabase project đã setup, bucket `medical-documents` đã tạo
#   - Gemini API key configured
#   - Azure Doc Intelligence endpoint + key configured

# Step 1: Upload ảnh phiếu khám (Week 4 endpoint — now uploads to Supabase)
# ⚠️ Document MUST have DocumentType = PRENATAL_CHECKUP for OCR to auto-queue
# ✅ Upload trả về ngay (<1s). OCR chạy background.
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
file: [ảnh phiếu khám.jpg]
documentTypeId: {PRENATAL_CHECKUP guid}
→ Response: MedicalDocumentDto (ngay lập tức)
→ Verify: StorageFile đã được tạo với real Supabase URL
→ Console log: "Processing OCR job: DocumentId=..." (background)

# Step 2: Poll OCR status (Flutter sẽ poll mỗi 3-5s)
GET /api/ocr/{ocrResultId}/status
→ Lần 1: Status = "Pending" hoặc "OcrProcessing" hoặc "AiExtracting"
→ ...đợi 10-30s...
→ Lần cuối: Status = "Succeeded", structuredJson có data

# Step 3: Check result
GET /api/ocr/{ocrResultId}/status
→ Response: Full OcrResultDto with all fields

# Step 4: Re-extract (sau khi update template)
POST /api/ocr/{ocrResultId}/re-extract
→ Response: New OcrResultDto with updated extraction

# 3. Verify structured JSON contains (VitalsJsonDto schema):
{
  "vitalsData": {
    "generalInfo": { "facility": "Bệnh viện Phụ sản...", ... },
    "interview": { "gestationalWeek": 28, ... },
    "examination": {
      "vitalSigns": { "bloodPressureSystolic": 120, "bloodPressureDiastolic": 80, ... },
      "obstetric": { "fetalHeartRateBpm": 140, ... }
    },
    "diagnosis": { "text": "Thai phát triển bình thường", ... }
  },
  "overallConfidence": 0.85
}
```

---

### 📊 ARCHITECTURE — REUSABILITY MAP

```
╔════════════════════════════════════════════════════════════════════════╗
║  Component            │ Week 5 (Medical Record) │ Future (Nutrition)  ║
╠═══════════════════════╪═════════════════════════╪═════════════════════╣
║  IAiProvider           │ GenerateAsync           │ ChatAsync           ║
║  IOcrProvider          │ ExtractTextAsync         │ (not needed)       ║
║  PromptBuilder         │ FromTemplate()          │ FromTemplate()      ║
║  Template Key          │ medical_record.extract  │ nutrition.planning  ║
║  Rule Layer 1 (System) │ JSON output, no advice  │ JSON output, safe   ║
║  Rule Layer 2 (Domain) │ Pregnancy medicine      │ Pregnancy nutrition ║
║  Rule Layer 3 (Feature)│ Extraction schema       │ Meal plan schema    ║
║  Rule Layer 4 (Context)│ PregnancyContext        │ NutritionContext    ║
║  Service               │ MedicalRecordAiService  │ NutritionAiService  ║
╚════════════════════════════════════════════════════════════════════════╝
```

---

### 🔜 NEXT STEPS — Nutrition Planning AI (Week sau)

Để implement Nutrition Planning AI, chỉ cần:

1. **Seed thêm `ai_prompt_templates`** cho `nutrition.meal_planning` + `nutrition.chat`
2. **Tạo `NutritionContext`** extends pattern từ `PregnancyContext` (thêm weight, allergies, preferences)
3. **Tạo `NutritionAiService`** dùng `IAiProvider.ChatAsync()` + `PromptBuilder.FromTemplate()`
4. **Tạo chat entities** (conversations, messages) nếu cần persistent chat
5. **KHÔNG cần sửa** GeminiAiProvider, PromptBuilder, or any AI infrastructure code

```csharp
// Example: How Nutrition AI will reuse the infrastructure
var template = await _unitOfWork.AiPromptTemplates
    .GetActiveByKeyAsync("nutrition.meal_planning", ct);

var context = await RetrieveNutritionContextAsync(pregnancyId, ct);

var prompt = PromptBuilder.FromTemplate(template)
    .WithContext("PREGNANCY", FormatPregnancyContext(context))
    .WithContext("NUTRITION PROFILE", FormatNutritionProfile(context))
    .WithUserMessage("Lên thực đơn cho tuần 28, bà bầu bị tiểu đường thai kỳ")
    .Build();

var response = await _aiProvider.GenerateAsync(prompt, ct);
// → Structured meal plan JSON
```

---

**🎉 Week 5 hoàn thành! Supabase Storage + AI Infrastructure sẵn sàng cho Medical Record Extraction + Nutrition Planning AI.**
