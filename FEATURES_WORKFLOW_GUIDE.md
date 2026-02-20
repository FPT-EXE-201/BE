# Features Workflow Guide — Medical Records, OCR, AI, Nutrition

> **Mục đích**: Tổng hợp workflow, architecture, data flow cho các tính năng AI-powered — để AI đọc hiểu và implement code đúng.  
> **Cập nhật**: 2026-02-15  
> **Xem thêm**: `DEVELOPMENT_WORKFLOW_GUIDE.md` (conventions, patterns), `DATABASE_SCHEMA.sql` (DDL 59 tables), `WEEK_*_PROMPTS_GUIDE.md` (chi tiết code)

---

## 1. TỔNG QUAN KIẾN TRÚC AI

### 1.1 Architecture Diagram

```
┌─── API Layer ─────────────────────────────────────────────────┐
│  MedicalDocumentsController    NutritionController             │
│  OcrController                 MealPlansController             │
│  WeightLogsController          AiAdminController               │
├─── Application Layer ─────────────────────────────────────────┤
│                                                                │
│  AI/ (shared abstractions)                                     │
│    ├── Interfaces/  → IAiProvider, IOcrProvider                │
│    ├── Models/      → AiPrompt, AiResponse, OcrRequest/Resp   │
│    ├── ExtractionModels/ → MedicalRecordExtractionResult       │
│    └── PromptBuilder     → Fluent builder, Rule Layer system   │
│                                                                │
│  Services/                                                     │
│    ├── MedicalRecordAiService  → Full pipeline: OCR → AI      │
│    ├── NutritionAiService      → Meal plan generation          │
│    ├── MedicalDocumentService  → Upload + CRUD documents       │
│    └── WeightLogService        → Weight tracking + alerts      │
│                                                                │
├─── Infrastructure Layer ──────────────────────────────────────┤
│                                                                │
│  AI/                                                           │
│    ├── GeminiAiProvider         → Google Gemini REST client    │
│    └── AzureOcrProvider         → Azure Document Intelligence │
│                                                                │
│  Services/                                                     │
│    ├── SupabaseStorageService   → File upload (replaces stub) │
│    └── OcrService (enhanced)    → Orchestrate OCR pipeline    │
│                                                                │
├─── Domain Layer ──────────────────────────────────────────────┤
│  Entities: StorageFile, DocumentFile, MedicalDocument, OcrResult,            │
│    AiPromptTemplate, AiRequestLog, WeightLog,                  │
│    RefFoodItem, MealPlan, MealPlanDay, MealItem, etc.          │
│  Enums: OcrStatus, DocumentSource, MealType, WeightSource...  │
└────────────────────────────────────────────────────────────────┘
```

### 1.2 Shared AI Infrastructure

Tất cả AI features (Medical Record, Nutrition, Chat) dùng chung:

| Component | Location | Vai trò |
|-----------|----------|---------|
| `IAiProvider` | Application/AI/Interfaces | Abstraction cho AI model (Gemini, future OpenAI) |
| `IOcrProvider` | Application/AI/Interfaces | Abstraction cho OCR engine (Azure, future Google Vision) |
| `PromptBuilder` | Application/AI | Fluent builder — lắp ráp Rule Layers + RAG context |
| `AiPrompt` | Application/AI/Models | Record: SystemMessage + UserMessage + model config |
| `AiResponse` | Application/AI/Models | Record: Content + tokens + processing time |
| `GeminiAiProvider` | Infrastructure/AI | Google Gemini 2.0 Flash REST client via `IHttpClientFactory` |
| `AzureOcrProvider` | Infrastructure/AI | Azure Document Intelligence v4.0 (prebuilt-read) |
| `ai_prompt_templates` | Database | Versioned prompt templates với Rule Layers |
| `ai_request_logs` | Database | Centralized tracking cho ALL AI features |

---

## 2. FILE STORAGE PIPELINE

### 2.1 Storage Strategy

```
Week 4: StubFileStorageService   → chỉ lưu metadata + placeholder URL
Week 5: SupabaseStorageService   → upload file thật lên Supabase Storage (thay stub)
```

### 2.2 Upload Flow

```
Client (multipart/form-data: files[] + metadata)
    ↓
MedicalDocumentsController.Upload()
    ↓
MedicalDocumentService.CreateWithFilesAsync()
    1. Validate: each file size ≤ 10MB, MIME type ∈ {jpeg, png, pdf}
    2. For each file:
       IFileStorageService.UploadAsync(stream, fileName, contentType)
       → Supabase: POST /object/{bucket}/{objectKey}
       → Returns: objectKey, publicUrl, checksum
       Create StorageFile entity
       Create DocumentFile entity (sortOrder, pageLabel)
    3. Create MedicalDocument entity (pregnancyId, title, documentDate, source)
    4. SaveChanges
    5. If DocumentType == PRENATAL_CHECKUP:
       OcrService.QueueOcrAsync(documentId) → non-blocking
       → Tạo OcrResult(Pending) + enqueue to Channel
       → OcrBackgroundService picks up job và chạy OCR+AI background
       If test types or others: SKIP OCR (không queue)
    ↓
Response: MedicalDocumentDto { id, files[] } → trả về ngay (<1s)

Flutter polls GET /api/ocr/{ocrResultId}/status mỗi 3-5s
→ Pending → OcrProcessing → AiExtracting → Succeeded ✅
```

### 2.3 Supabase Storage Configuration

```json
// appsettings.json
{
  "Supabase": {
    "Storage": {
      "ProjectUrl": "https://xxxxx.supabase.co",
      "ServiceRoleKey": "eyJ...",
      "BucketName": "medical-documents",
      "PublicBaseUrl": "https://xxxxx.supabase.co/storage/v1/object/public"
    }
  }
}
```

```csharp
// Infrastructure/DependencyInjection.cs
services.AddHttpClient<IFileStorageService, SupabaseStorageService>(client =>
{
    client.BaseAddress = new Uri($"{projectUrl}/storage/v1/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");
});
```

### 2.4 IFileStorageService Interface

```csharp
public interface IFileStorageService
{
    Task<StorageFileResult> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        Guid? ownerUserId = null, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
    string GetPublicUrl(string objectKey);
}

public record StorageFileResult(
    string ObjectKey, string PublicUrl, string OriginalFileName,
    string MimeType, long FileSizeBytes, byte[] ChecksumSha256);
```

---

## 3. OCR + AI EXTRACTION PIPELINE (PRENATAL_CHECKUP only)

> **⚠️ IMPORTANT**: OCR + AI extraction chỉ áp dụng cho `PRENATAL_CHECKUP` documents.  
> Test types (BLOOD_TEST, ULTRASOUND...) chỉ lưu ảnh, **không chạy OCR**.  
> Others (PRESCRIPTION, VACCINATION...) chỉ archive.

### 3.1 Full Pipeline Flow

```
┌─────────────── MedicalRecordAiService.ProcessDocumentAsync() ───────────────┐
│                                                                              │
│  ⚠️ Chỉ chạy cho DocumentType = PRENATAL_CHECKUP                           │
│                                                                              │
│  Phase 1: OCR (Azure Document Intelligence)                                 │
│  ─────────────────────────────────────────                                   │
│  1. Download file từ Supabase → Stream                                       │
│  2. POST stream → Azure Document Intelligence (prebuilt-read)               │
│  3. Poll GET until status = "succeeded"                                      │
│  4. Extract raw text + confidence score                                      │
│  OcrResult.Status: Pending → OcrProcessing → OcrCompleted                   │
│                                                                              │
│  Phase 2: AI Extraction (Gemini)                                            │
│  ────────────────────────────                                                │
│  5. RAG Context Retrieval:                                                   │
│     - Pregnancy (gestational week, status)                                   │
│     - PregnancyConditions (danh sách bệnh lý)                               │
│     - Recent OcrResults (previous extraction for consistency)                │
│  6. Load AiPromptTemplate from DB (key: "medical_record.extraction")        │
│  7. PromptBuilder.FromTemplate(template)                                    │
│       .WithContext("PATIENT CONTEXT", pregnancyContext)                      │
│       .WithUserMessage(ocrRawText)                                          │
│       .Build() → AiPrompt (SystemMessage + UserMessage)                     │
│  8. GeminiAiProvider.GenerateAsync(prompt) → AiResponse                     │
│  9. Validate JSON → MedicalRecordExtractionResult                           │
│  10. Save to OcrResult.StructuredJson + metrics                             │
│  OcrResult.Status: AiExtracting → Succeeded / Failed                        │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 OcrStatus Enum (Multi-phase)

```csharp
// HIỆN TẠI (Week 4)
public enum OcrStatus
{
    Pending,      // Đang chờ
    Processing,   // Đang xử lý
    Succeeded,    // Thành công
    Failed        // Thất bại
}
// Week 5 sẽ expand thành multi-phase:
//   Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded / Failed
```

### 3.3 Rule Layer System (Prompt Construction)

```
┌──────────────────────────────────────────────────┐
│ Layer 1: SYSTEM RULES                             │
│   Ngôn ngữ, format JSON, safety constraints      │
│   "Always respond with valid JSON..."             │
├──────────────────────────────────────────────────┤
│ Layer 2: DOMAIN RULES (shared — tái sử dụng)     │
│   Vietnamese medical terminology                  │
│   HA = Huyết áp, CCTC = Chiều cao tử cung        │
│   Standard ranges for pregnancy metrics           │
├──────────────────────────────────────────────────┤
│ Layer 3: FEATURE RULES (per feature)              │
│   Medical Record: extraction schema, field mapping│
│   Nutrition: dietary guidelines per trimester      │
│   Chat: conversation style, empathy               │
├──────────────────────────────────────────────────┤
│ Layer 4: USER CONTEXT (RAG — from Database)       │
│   Gestational week, known conditions,             │
│   previous records, food preferences              │
└──────────────────────────────────────────────────┘

→ Layer 1+2+3+OutputSchema = SystemMessage
→ Layer 4 + user input       = UserMessage
→ Assembled by PromptBuilder → AiPrompt
```

### 3.4 PromptBuilder Usage

```csharp
// Medical Record Extraction
var prompt = PromptBuilder.FromTemplate(template)   // Loads L1+L2+L3 from DB
    .WithContext("PATIENT CONTEXT", pregnancyContext) // L4: RAG
    .WithUserMessage($"Extract from OCR text:\n{rawText}")
    .Build();
var response = await _aiProvider.GenerateAsync(prompt, ct);

// Nutrition Meal Planning (same infrastructure, different template)
var prompt = PromptBuilder.FromTemplate(nutritionTemplate)
    .WithContext("PREGNANCY", pregnancyContext)
    .WithContext("NUTRITION_PROFILE", foodPreferences)
    .WithUserMessage("Lên thực đơn 7 ngày cho tuần thai 28")
    .Build();
var response = await _aiProvider.GenerateAsync(prompt, ct);
```

### 3.5 Extraction Output Schema

```json
{
  "documentInfo": {
    "documentDate": "2026-02-10",
    "facilityName": "Bệnh viện Từ Dũ",
    "doctorName": "BS. Nguyễn Văn A",
    "documentType": "prenatal_checkup"
  },
  "maternalHealth": {
    "gestationalWeek": 28,
    "bloodPressure": "120/80",
    "weightKg": 65.5,
    "heartRate": 80,
    "fundalHeightCm": 28.0,
    "edema": "none"
  },
  "fetalHealth": {
    "fetalHeartRate": 140,
    "fetalPosition": "Ngôi đầu",
    "fetalMovement": "Bình thường",
    "estimatedWeightGrams": 1200
  },
  "diagnoses": ["Thai phát triển bình thường"],
  "medications": [
    { "name": "Acid folic", "dosage": "400mg", "frequency": "1 lần/ngày", "duration": "Đến khi sinh" }
  ],
  "recommendations": ["Tái khám sau 2 tuần"],
  "nextAppointmentDate": "2026-02-24",
  "notes": null,
  "overallConfidence": 0.85
}
```

---

## 4. AZURE DOCUMENT INTELLIGENCE (OCR)

### 4.1 API Pattern (Async 2-step)

```
Step 1: POST /documentModels/prebuilt-read:analyze?api-version=2024-11-30
        Headers: Ocp-Apim-Subscription-Key, Content-Type: image/jpeg
        Body: binary file stream
        → 202 Accepted + Header: Operation-Location: {url}

Step 2: Poll GET {Operation-Location}
        → status: "running" → wait 1s → retry
        → status: "succeeded" → analyzeResult.content = raw text
        → status: "failed" → error.message
```

### 4.2 Configuration

```json
{
  "AI": {
    "AzureDocumentIntelligence": {
      "Endpoint": "https://xxx.cognitiveservices.azure.com/",
      "ApiKey": "your-key",
      "ModelId": "prebuilt-read",
      "ApiVersion": "2024-11-30",
      "PollingIntervalMs": 1000,
      "TimeoutSeconds": 120
    }
  }
}
```

```csharp
// DI Registration
services.AddHttpClient<IOcrProvider, AzureOcrProvider>(client =>
{
    client.BaseAddress = new Uri(endpoint);
});
```

### 4.3 Supported File Types

`image/jpeg`, `image/png`, `image/bmp`, `image/tiff`, `image/heif`, `application/pdf`

---

## 5. GOOGLE GEMINI AI

### 5.1 API Pattern (Single Request)

```
POST /v1beta/models/{model}:generateContent?key={apiKey}
{
  "contents": [{ "role": "user", "parts": [{ "text": "..." }] }],
  "systemInstruction": { "parts": [{ "text": "system prompt" }] },
  "generationConfig": {
    "temperature": 0.1,
    "maxOutputTokens": 4096,
    "responseMimeType": "application/json"  // JSON mode
  }
}
→ Response: candidates[0].content.parts[0].text = JSON string
→ usageMetadata: { promptTokenCount, candidatesTokenCount, totalTokenCount }
```

### 5.2 Multi-turn Chat (cho Nutrition Chat)

```
POST /v1beta/models/{model}:generateContent?key={apiKey}
{
  "contents": [
    { "role": "user", "parts": [{ "text": "Tôi dị ứng đậu phộng, nên ăn gì?" }] },
    { "role": "model", "parts": [{ "text": "Bạn có thể thay..." }] },
    { "role": "user", "parts": [{ "text": "Còn canxi thì sao?" }] }
  ],
  "systemInstruction": ...,
  "generationConfig": { "temperature": 0.7 }
}
```

### 5.3 Configuration

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "your-gemini-api-key",
      "DefaultModel": "gemini-2.0-flash",
      "BaseUrl": "https://generativelanguage.googleapis.com/v1beta"
    }
  }
}
```

```csharp
// DI Registration
services.AddHttpClient<IAiProvider, GeminiAiProvider>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});
```

### 5.4 Rate Limits (Free Tier)

| Model | RPM | RPD | TPM |
|-------|-----|-----|-----|
| gemini-2.0-flash | 15 | 1500 | 1M |

**Recommendation**: Max 15 AI requests/pregnancy/day để tránh exhaust quota.

---

## 6. MEDICAL DOCUMENTS MODULE (Week 4)

### 6.1 Tables

| Table | Vai trò | Extends BaseEntity |
|-------|---------|-------------------|
| `storage_files` | File vật lý (objectKey, publicUrl, checksum) | ✅ |
| `document_files` | Junction table: MedicalDocument ↔ StorageFile (multi-file) | ✅ |
| `ref_document_types` | Master data loại tài liệu (14 types seeded) | ✅ |
| `ref_document_type_translations` | i18n tên loại tài liệu | ❌ composite PK |
| `medical_documents` | Metadata document + FK → pregnancies | ✅ |
| `ocr_results` | OCR + AI extraction results (multi-run) | ✅ |

### 6.2 Seeded Document Types (14 types)

```
# Original 8
PRENATAL_CHECKUP, ULTRASOUND, BLOOD_TEST, URINE_TEST,
PRESCRIPTION, VACCINATION_RECORD, MEDICAL_REPORT, OTHER

# Week 5.5: Specific test types for direct matching
HIV_TEST, HEPATITIS_B_TEST, THYROID_TEST, GLUCOSE_TEST, CBC_TEST, NT_SCAN
```

### 6.3 API Endpoints

```
POST   /api/pregnancies/{id}/documents              → Upload (multipart/form-data)
GET    /api/pregnancies/{id}/documents?isFavorite=   → List by pregnancy (filter by favorite)
GET    /api/documents/{id}                           → Detail + OCR results
PUT    /api/documents/{id}                           → Update metadata (title, notes, date, documentTypeId)
DELETE /api/documents/{id}                           → Soft delete

POST   /api/documents/{id}/ocr/rerun                → Re-run OCR + AI (PRENATAL_CHECKUP only)
GET    /api/ocr/{id}/status                          → Check pipeline status
GET    /api/documents/{documentId}/ocr               → List OCR results by document

PATCH  /api/documents/{id}/favorite                  → Toggle favorite

GET    /api/ref/document-types?lang=vi               → Reference data (public)
GET    /api/pregnancies/{id}/timeline                → Documents + visits timeline
```

### 6.4 Business Rules

- File size ≤ 10MB, MIME ∈ {image/jpeg, image/png, application/pdf}
- OCR run number auto-increment per document: `uk_ocr_results_doc_run (document_id, ocr_run_no)`
- **OCR chỉ chạy cho PRENATAL_CHECKUP** — test types và others **không có OCR**
- `medical_documents.visit_id` ban đầu NULL → populated sau khi Confirm (Week 5.5)
- Ownership: user chỉ access documents của own pregnancies
- **File replacement**: blocked nếu document đã có OCR Confirmed. Allowed cho test types và others.
- **Skip**: soft delete document (IsDeleted=true). Admin vẫn thấy, user không thấy.
- **PrenatalTest.DocumentId?** FK → `medical_documents.id`, ON DELETE SET NULL

---

## 6.5 AUTO-FILL MODULE (Week 5.5)

> **Prerequisite**: Week 5 (OCR + AI Extraction) phải hoàn thành trước.  
> **Chi tiết**: `WEEK_5.5_PROMPTS_GUIDE.md`

### 6.5.1 Mục tiêu

Week 5 chỉ extract dữ liệu → lưu JSON thô. Week 5.5 thêm "Review & Confirm" flow — user xem AI extracted data, chỉnh sửa nếu cần, confirm → hệ thống tự động tạo PrenatalVisit / PrenatalTest.

> **⚠️ LƯU Ý**: Auto-Fill flow chỉ áp dụng cho **PRENATAL_CHECKUP** (có OCR data).  
> Test types: user tự nhập metadata (TestType, Date, Notes, IsAbnormal) → confirm → tạo PrenatalTest.  
> Others (PRESCRIPTION, VACCINATION...): chỉ lưu trữ archive, **không cần confirm**.

### 6.5.2 Flow

```
PRENATAL_CHECKUP:
  Upload → Response ngay (<1s)
  Background: OCR → AI → StructuredJson (10-30s)
  Flutter polls GET /ocr/{id}/status → Succeeded
                            ↓
  GET /ocr/{id}/review          → User xem + chỉnh sửa extracted data
  POST /ocr/{id}/confirm        → Auto-create entities
                            ↓
  PrenatalVisit (+ VitalsJson)
  MedicalDocument.VisitId ← created visit
  OcrResult.Status = Confirmed

TEST TYPES (BLOOD_TEST, ULTRASOUND...):
  Upload → Hiện ảnh → User nhập metadata
                            ↓
  POST /documents/{id}/confirm  → Tạo PrenatalTest
                            ↓
  PrenatalTest.DocumentId ← document này

OTHERS (PRESCRIPTION, VACCINATION...):
  Upload → ✅ DONE (chỉ archive, không có review/confirm)
```

### 6.5.3 Document Type → Action Strategy

| DocumentType Code | Action | Output |
|-------------------|--------|--------|
| `PRENATAL_CHECKUP` | Create PrenatalVisit + VitalsJson + link document | Visit entity |
| `BLOOD_TEST` | Direct match → BLOOD_TEST RefTestType | Test entity |
| `URINE_TEST` | Direct match → URINE_TEST RefTestType | Test entity |
| `ULTRASOUND` | Direct match → ULTRASOUND RefTestType | Test entity |
| `HIV_TEST` | Direct match → HIV_SCREEN RefTestType | Test entity |
| `HEPATITIS_B_TEST` | Direct match → HEPATITIS_B RefTestType | Test entity |
| `THYROID_TEST` | Direct match → TSH RefTestType | Test entity |
| `GLUCOSE_TEST` | Direct match → OGTT RefTestType | Test entity |
| `CBC_TEST` | Direct match → CBC_TEST RefTestType | Test entity |
| `NT_SCAN` | Direct match → NT_SCAN RefTestType | Test entity |
| `PRESCRIPTION` | Save to notes only | Notes |
| `VACCINATION_RECORD` | Save to notes only | Notes |
| `MEDICAL_REPORT` | Save to notes only | Notes |
| `OTHER` | Save to notes only | Notes |

### 6.5.4 Auto-Create Visit for Test Types

Khi confirm test type mà không có `existingVisitId`, BE tự động tạo một Routine visit để test không bị orphaned.
DocumentType Code → RefTestType Code qua `DocTypeToTestTypeCode` dictionary (tất cả đều là direct mapping).

### 6.5.5 Database Changes

- `OcrStatus` enum: thêm `Confirmed`
- `OcrResult` entity: 4 fields mới (`ConfirmedAt`, `ConfirmedBy`, `ConfirmedJson`, `AutoFillResultJson`)
- Permissions: `ocr.review`, `ocr.confirm`

### 6.5.6 API Endpoints

```
GET  /api/documents/{id}/extraction/review   → ExtractionReviewDto (dữ liệu AI + suggestions)
POST /api/documents/{id}/extraction/confirm  → AutoFillResultDto (entities đã tạo)
```

---

## 7. WEIGHT TRACKING MODULE (Week 6)

### 7.1 Tables

| Table | Vai trò |
|-------|---------|
| `weight_logs` | Daily weight entries per pregnancy |
| `weight_goal_ranges` | Height, pre-pregnancy weight, BMI, recommended gain (1 per pregnancy) |
| `weight_alerts` | Auto-generated alerts khi gain quá nhanh/chậm |

### 7.2 Business Rules

- 1 weight log per pregnancy per day: `uk_weight_logs_pregnancy_date (pregnancy_id, logged_on)`
- Weight: `DECIMAL(5,2)`, CHECK > 0 AND < 500
- 1 goal range per pregnancy: `uk_weight_goals_pregnancy (pregnancy_id)`
- BMI auto-calculate: `weight_kg / (height_cm/100)²`
- Alert types: `GAIN_TOO_FAST`, `GAIN_TOO_SLOW`, `TARGET_EXCEEDED`

### 7.3 API Endpoints

```
POST   /api/pregnancies/{id}/weight-logs        → Record weight
GET    /api/pregnancies/{id}/weight-logs        → List + chart data
PUT    /api/weight-logs/{id}                    → Update entry
DELETE /api/weight-logs/{id}                    → Soft delete

POST   /api/pregnancies/{id}/weight-goals       → Set goal range
GET    /api/pregnancies/{id}/weight-goals       → Get current goals
PUT    /api/weight-goals/{id}                   → Update goals

GET    /api/pregnancies/{id}/weight-alerts      → List alerts
PUT    /api/weight-alerts/{id}/resolve          → Resolve alert
```

---

## 8. NUTRITION + AI MEAL PLANNING MODULE (Week 7)

### 8.1 Tables

| Table | Vai trò |
|-------|---------|
| `ref_food_items` + translations | Master data thực phẩm (seeded) |
| `ref_nutrients` + translations | Master data chất dinh dưỡng (seeded) |
| `pregnancy_food_preferences` | Allergy/dislike per pregnancy + severity |
| `recipes` | Recipe definitions (from AI or manual) |
| `meal_plans` | Meal plan metadata + FK → ai_request_logs |
| `meal_plan_days` | Daily breakdown |
| `meal_items` | Individual meals (breakfast/lunch/dinner/snack) |
| `meal_item_nutrients` | Nutrient amounts per meal item |
| `meal_plan_feedback` | User feedback per plan (rating 1-5) |
| `meal_item_feedback` | User feedback per item (liked/disliked) |

### 8.2 AI Meal Planning Flow

```
POST /api/pregnancies/{id}/nutrition/ai-generate-meal-plan
{ "days": 7, "preferences": "Ăn chay, ít muối" }
    ↓
NutritionAiService.GenerateMealPlanAsync()
    1. Validate: max 15 AI requests/pregnancy/day
    2. Create AiRequestLog (status: Pending, feature: "nutrition")
    3. Retrieve RAG context:
       - Pregnancy (week, trimester, status)
       - PregnancyConditions (gestational diabetes → restrict sugar)
       - FoodPreferences (allergies, dislikes + severity)
       - WeightLog (latest weight, BMI)
       - Previous MealPlan feedback (learn from user preferences)
    4. Load AiPromptTemplate (key: "nutrition.meal_planning")
    5. PromptBuilder:
       .FromTemplate(nutritionTemplate)
       .WithContext("PREGNANCY", pregnancyContext)
       .WithContext("ALLERGIES", allergyList)
       .WithContext("WEIGHT", weightContext)
       .WithUserMessage("Lên thực đơn {days} ngày tuần thai {week}")
       .Build()
    6. GeminiAiProvider.GenerateAsync(prompt)
    7. Parse JSON → Create entities:
       MealPlan → MealPlanDays → MealItems → MealItemNutrients
    8. Update AiRequestLog (status: Succeeded, tokens, latency)
    ↓
Response: MealPlanDto { id, days: [...], totalCalories, nutrients }
```

### 8.3 Nutrition RAG Context

```csharp
var context = new NutritionContext
{
    GestationalWeek = 28,
    Trimester = 3,
    Allergies = ["Đậu phộng (SEVERE)", "Sữa bò (MEDIUM)"],
    Dislikes = ["Cá trích", "Mướp đắng"],
    NutritionNotes = ["Ăn chay ngày thứ 2"],
    CurrentWeightKg = 65.5,
    Bmi = 24.2,
    KnownConditions = ["Tiểu đường thai kỳ"],
    PreviousFeedback = "Rating 4/5 — user thích thực đơn nhiều rau"
};
```

### 8.4 API Endpoints

```
# Food Preferences
POST   /api/pregnancies/{id}/food-preferences   → Add allergy/dislike
GET    /api/pregnancies/{id}/food-preferences   → List preferences
DELETE /api/pregnancies/{id}/food-preferences/{prefId}

# AI Meal Planning
POST   /api/pregnancies/{id}/nutrition/ai-generate → Generate via Gemini
GET    /api/pregnancies/{id}/nutrition/ai-requests  → List AI requests
POST   /api/nutrition/ai-requests/{id}/retry       → Retry failed request

# Meal Plans
GET    /api/pregnancies/{id}/meal-plans            → List plans
GET    /api/meal-plans/{id}                        → Detail + days + items
PUT    /api/meal-plans/{id}                        → Update notes
DELETE /api/meal-plans/{id}                        → Soft delete

# Feedback
POST   /api/meal-plans/{id}/feedback               → Rate plan (1-5)
GET    /api/meal-plans/{id}/feedback               → Get feedback
POST   /api/meal-items/{id}/feedback               → Like/dislike item
```

### 8.5 Business Rules

- Unique preference: `(pregnancy_id, food_item_id, preference_type)`
- Max 15 AI requests/pregnancy/day
- Meal plan: `end_date >= start_date`, max 14 days
- Each MealItem must have `recipe_id` OR `item_name` (CHECK constraint)
- 1 feedback per user per meal_plan: `uk_meal_plan_feedback (meal_plan_id, user_id)`
- Rating 1-5: `CHECK (rating BETWEEN 1 AND 5)`

---

## 9. AI REQUEST LOGGING (Centralized)

### 9.1 `ai_request_logs` Table

Dùng chung cho TẤT CẢ AI features — KHÔNG tạo table riêng per feature.

```
feature           → "medical_record" / "nutrition" / "chat" / "weight_advice"
pregnancy_id      → FK optional (null cho admin requests)
user_id           → FK optional
template_id       → FK → ai_prompt_templates
status            → Pending / Processing / Succeeded / Failed
model             → "gemini-2.0-flash"
request_payload   → JSON (prompt gửi đi)
response_payload  → JSON (AI response)
tokens_input      → prompt tokens
tokens_output     → completion tokens
processing_time_ms → latency
error_message     → error nếu failed
```

### 9.2 Flow

```csharp
// Trong NutritionAiService (hoặc bất kỳ AI service nào):
var log = new AiRequestLog
{
    Feature = "nutrition",
    PregnancyId = pregnancyId,
    UserId = currentUserId,
    TemplateId = template.Id,
    Status = AiRequestStatus.Pending,
    RequestPayload = JsonSerializer.Serialize(promptPayload)
};
await _unitOfWork.AiRequestLogs.AddAsync(log, ct);
await _unitOfWork.SaveChangesAsync(ct);

try {
    var response = await _aiProvider.GenerateAsync(prompt, ct);
    log.Status = AiRequestStatus.Succeeded;
    log.ResponsePayload = response.Content;
    log.Model = response.ModelUsed;
    log.TokensInput = response.PromptTokens;
    log.TokensOutput = response.CompletionTokens;
    log.ProcessingTimeMs = (int)response.ProcessingTime.TotalMilliseconds;
} catch (Exception ex) {
    log.Status = AiRequestStatus.Failed;
    log.ErrorMessage = ex.Message;
}
_unitOfWork.AiRequestLogs.Update(log);
await _unitOfWork.SaveChangesAsync(ct);
```

---

## 10. CONFIGURATION SUMMARY

### 10.1 appsettings.json Keys

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "...",
      "DefaultModel": "gemini-2.0-flash",
      "BaseUrl": "https://generativelanguage.googleapis.com/v1beta"
    },
    "AzureDocumentIntelligence": {
      "Endpoint": "https://xxx.cognitiveservices.azure.com/",
      "ApiKey": "...",
      "ModelId": "prebuilt-read",
      "ApiVersion": "2024-11-30",
      "PollingIntervalMs": 1000,
      "TimeoutSeconds": 120
    }
  },
  "Supabase": {
    "Storage": {
      "ProjectUrl": "https://xxxxx.supabase.co",
      "ServiceRoleKey": "eyJ...",
      "BucketName": "medical-documents",
      "PublicBaseUrl": "https://xxxxx.supabase.co/storage/v1/object/public"
    }
  }
}
```

### 10.2 DI Registration

```csharp
// Infrastructure/DependencyInjection.cs

// AI Providers
services.AddHttpClient<IAiProvider, GeminiAiProvider>(client =>
{
    client.BaseAddress = new Uri(configuration["AI:Gemini:BaseUrl"]!);
});

services.AddHttpClient<IOcrProvider, AzureOcrProvider>(client =>
{
    client.BaseAddress = new Uri(configuration["AI:AzureDocumentIntelligence:Endpoint"]!);
});

// Storage
services.AddHttpClient<IFileStorageService, SupabaseStorageService>(client =>
{
    var projectUrl = configuration["Supabase:Storage:ProjectUrl"]!;
    var serviceKey = configuration["Supabase:Storage:ServiceRoleKey"]!;
    client.BaseAddress = new Uri($"{projectUrl}/storage/v1/");
    client.DefaultRequestHeaders.Add("apikey", serviceKey);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceKey}");
});

// Services
services.AddScoped<IMedicalRecordAiService, MedicalRecordAiService>();
services.AddScoped<INutritionAiService, NutritionAiService>();
services.AddScoped<IOcrService, OcrService>();
```

---

## 11. DATA FLOW DIAGRAM PER FEATURE

### 11.1 Medical Record Upload → OCR → AI

```
[User uploads image(s)]
       ↓
[Supabase Storage] ← file binaries
       ↓ objectKey + publicUrl (per file)
[StorageFile] ← metadata (per file)
       ↓
[DocumentFile] ← sortOrder, pageLabel (junction)
       ↓ documentId
[MedicalDocument] ← pregnancyId, title, documentDate
       ↓ documentId
[OcrResult] status: Pending
       ↓
[Azure OCR] ← file stream from Supabase
       ↓ rawText + confidence
[OcrResult] status: OcrCompleted
       ↓
[RAG Context] ← Pregnancy + Conditions + Previous OCR
       ↓
[PromptBuilder] ← Template + Context + rawText
       ↓
[Gemini AI] → structured JSON
       ↓
[OcrResult] status: Succeeded, structuredJson saved
       ↓ (future: auto-create PrenatalVisit from extracted data)
[MedicalDocument] visit_id updated
```

### 11.2 Nutrition AI Meal Plan

```
[User requests meal plan]
       ↓
[AiRequestLog] status: Pending, feature: "nutrition"
       ↓
[RAG Context] ← Pregnancy + Allergies + Weight + Previous Feedback
       ↓
[PromptBuilder] ← nutrition.meal_planning template + context
       ↓
[Gemini AI] → meal plan JSON
       ↓ parse
[MealPlan] → [MealPlanDays] → [MealItems] → [MealItemNutrients]
       ↓
[AiRequestLog] status: Succeeded, tokens + latency saved
       ↓
[User can provide feedback] → [MealPlanFeedback] + [MealItemFeedback]
       ↓ (feedback injected into context for next generation)
```

---

## 12. PERMISSIONS (RBAC)

| Module | Permissions |
|--------|------------|
| Documents | `document.create`, `document.view`, `document.update`, `document.delete`, `document.favorite`, `ocr.trigger`, `ocr.view` |
| Weight | `weight_log.read`, `weight_log.write`, `weight_log.delete` |
| Nutrition | `nutrition.read`, `nutrition.write`, `ai_features.access` (premium) |
| AI Admin | `ai_templates.manage` (admin only) |

Xem chi tiết RBAC: `RBAC_IMPLEMENTATION_GUIDE.md`

---

## 13. ENUMS REFERENCE

```csharp
// Week 4 (hiện tại)
public enum DocumentSource { Upload, Share, Import }
public enum OcrStatus { Pending, Processing, Succeeded, Failed }
// Week 5 sẽ expand OcrStatus: thêm OcrProcessing, OcrCompleted, AiExtracting
// Week 5.5 sẽ thêm OcrStatus.Confirmed (user đã review + confirm extracted data)

// Week 6
public enum WeightSource { Manual, Device, Import }
public enum WeightAlertType { GainTooFast, GainTooSlow, TargetExceeded }

// Week 7
public enum MealType { Breakfast, Lunch, Dinner, Snack }
public enum MealPlanSource { AI, Manual }
public enum PreferenceType { Allergy, Dislike }
public enum Severity { Low, Medium, High }

// Shared AI
public enum AiRequestStatus { Pending, Processing, Succeeded, Failed }
```

> **Convention nhắc lại**: Tất cả enum lưu DB dạng `VARCHAR + HasConversion<string>()`, KHÔNG dùng int.

---

## 14. CHECKLIST — Trước khi implement AI Feature mới

- [ ] Tạo AiPromptTemplate seed data (template_key, 3 rule layers, output_schema)
- [ ] Service log mọi AI call vào `ai_request_logs`
- [ ] Implement rate limiting (configurable per feature)
- [ ] Handle Gemini safety filters (candidates empty → clear error message)
- [ ] Validate JSON response (graceful fallback nếu không match schema)
- [ ] RAG context chỉ lấy data user sở hữu (ownership check)
- [ ] Error trong pipeline → cập nhật status = Failed + error_message
- [ ] DI register: `AddHttpClient<T>` cho HTTP-based providers
- [ ] Configuration keys documented trong appsettings.json
- [ ] Permissions seed + assign cho roles phù hợp
