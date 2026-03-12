# 📋 TESTING CHECKLIST — WEEK 5: Supabase Storage + OCR + AI Extraction

> **Prerequisite**: Đã hoàn thành TESTING_CHECKLIST_WEEK_3 + WEEK_4. Có JWT Bearer Token, có pregnancy Active, có documents đã upload.
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## ⚠️ QUAN TRỌNG — Cấu hình API Keys trước khi test

Week 5 tích hợp 3 dịch vụ bên ngoài. **PHẢI cấu hình đầy đủ trước khi test**:

### Checklist cấu hình (`appsettings.json` hoặc `appsettings.Development.json`)

| Service | Config Key | Cách lấy |
|---------|-----------|----------|
| Supabase Storage | `Supabase:Url` | Supabase Dashboard → Settings → API → Project URL |
| Supabase Storage | `Supabase:ServiceRoleKey` | Supabase Dashboard → Settings → API → service_role key |
| Supabase Storage | `Supabase:Storage:BucketName` | Tạo bucket `medical-documents` trong Supabase Storage |
| Supabase Storage | `Supabase:Storage:PublicBaseUrl` | `{Supabase:Url}/storage/v1/object/public` |
| Azure Doc Intelligence | `AI:AzureDocumentIntelligence:Endpoint` | Azure Portal → Document Intelligence resource → Endpoint |
| Azure Doc Intelligence | `AI:AzureDocumentIntelligence:ApiKey` | Azure Portal → Document Intelligence resource → Keys |
| Google Gemini | `AI:Gemini:ApiKey` | Google AI Studio → API Keys |

### Chuẩn bị Supabase Storage
1. Truy cập Supabase Dashboard → Storage
2. Tạo bucket `medical-documents` (Public: Enabled cho read)
3. Verify bucket URL: `{Supabase:Url}/storage/v1/object/public/medical-documents/`

---

## 0️⃣ PRE-TEST: Xác nhận hệ thống khởi động đúng

### ✅ TC-S00: Application startup — Background Service
```
dotnet run --project src/FPT.EXE201.Api
```
**Kiểm tra Console Output**:
- [ ] Không có exception khi khởi động
- [ ] Log: `"OCR Background Service started"` (hoặc tương tự) xuất hiện trong console

**Expected**: App khởi động thành công, không crash, background service registered.

---

### ✅ TC-S01: Enum OcrStatus đã cập nhật
```
GET /api/ref/enums/ocrStatus
```
**Expected**: 200 OK — 6 giá trị mới:
```json
{
  "success": true,
  "data": [
    { "name": "Pending", "value": 0 },
    { "name": "OcrProcessing", "value": 1 },
    { "name": "OcrCompleted", "value": 2 },
    { "name": "AiExtracting", "value": 3 },
    { "name": "Succeeded", "value": 4 },
    { "name": "Failed", "value": 5 }
  ]
}
```
> **⚠️ So với Week 4**: Trước đây chỉ có 4 giá trị (Pending, Processing, Succeeded, Failed). Nay mở rộng thành 6.

---

### ✅ TC-S02: Chuẩn bị — Đảm bảo có pregnancy Active
```
GET /api/pregnancies/active
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có pregnancy đang Active.
**Lưu lại**: `{pregnancyId}`.

---

## 1️⃣ SUPABASE STORAGE — Upload thực (thay thế Stub)

> **⚠️ THAY ĐỔI QUAN TRỌNG so với Week 4**:
> - Week 4: `StubFileStorageService` → `files[].fileUrl` là placeholder URL, không mở được.
> - Week 5: `SupabaseStorageService` → `files[].fileUrl` là URL thực trên Supabase, có thể mở trên trình duyệt.

---

### ✅ TC-U01: Upload file ảnh — Verify Supabase URL
```
POST /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều ảnh `.jpg` hoặc `.png` (ảnh phiếu khám thai nếu có) |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000001` (PRENATAL_CHECKUP) |
| `title` | Text | `Phiếu khám thai tuần 20 - Test Week 5` |
| `documentDate` | Text | `2026-02-10` |

**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Document created successfully.",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "documentTypeId": "b0000001-0000-0000-0000-000000000001",
    "documentTypeDisplayName": "Khám thai",
    "files": [
      {
        "id": "<guid>",
        "storageFileId": "<guid>",
        "originalFileName": "phieu-kham.jpg",
        "mimeType": "image/jpeg",
        "fileSizeBytes": 123456,
        "fileUrl": "https://YOUR_PROJECT.supabase.co/storage/v1/object/public/medical-documents/...",
        "sortOrder": 1,
        "pageLabel": null
      }
    ],
    "totalFileSizeBytes": 123456,
    "title": "Phiếu khám thai tuần 20 - Test Week 5",
    "source": "Upload",
    ...
  }
}
```
**Verify**:
- [ ] `files[0].fileUrl` bắt đầu bằng `https://...supabase.co/storage/...` (KHÔNG phải placeholder)
- [ ] Mở `files[0].fileUrl` trên trình duyệt → hiển thị ảnh đúng
- [ ] Console log: `"Processing OCR job: DocumentId=..."` (background auto-queue cho PRENATAL_CHECKUP)

**Lưu lại**: `{documentId_prenatal}`, `{ocrResultId_auto}` (nếu response trả về).

---

### ✅ TC-U02: Upload file PDF — Verify Supabase URL
```
POST /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều file PDF |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000001` (PRENATAL_CHECKUP) |
| `title` | Text | `PDF khám thai - Test Week 5` |

**Expected**: 201 Created — `files[0].mimeType: "application/pdf"`, `files[0].fileUrl` là URL thực trên Supabase.
**Verify**: Mở `files[0].fileUrl` → tải/hiển thị PDF đúng.

**Lưu lại**: `{documentId_pdf}`.

---

### ✅ TC-U03: Upload loại ULTRASOUND — KHÔNG auto-queue OCR
```
POST /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều ảnh siêu âm |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000002` (ULTRASOUND) |
| `title` | Text | `Siêu âm tuần 20 - Test no OCR` |

**Expected**: 201 Created — Upload thành công, file trên Supabase.
**Verify**:
- [ ] `files[0].fileUrl` là URL Supabase thực
- [ ] Console **KHÔNG** có log `"Processing OCR job"` cho document này
- [ ] Không có OcrResult tự động tạo (chỉ PRENATAL_CHECKUP mới auto-queue)

**Lưu lại**: `{documentId_ultrasound}`.

---

### ✅ TC-U04: Upload loại PRESCRIPTION — KHÔNG auto-queue OCR
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 hoặc nhiều file ảnh đơn thuốc |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000005` (PRESCRIPTION) |
| `title` | Text | `Đơn thuốc tháng 2 - Test no OCR` |

**Expected**: 201 Created — Upload thành công. **KHÔNG** có OCR job queued trong console.

---

### ✅ TC-U05: Upload không có documentTypeId — KHÔNG auto-queue OCR
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 hoặc nhiều file ảnh |
| `title` | Text | `Tài liệu chưa phân loại` |

**Expected**: 201 Created — Upload thành công, `documentTypeId: null`. **KHÔNG** có OCR job queued.

---

## 2️⃣ OCR + AI FULL PIPELINE — Process Document

> **⚠️ FLOW MỚI của Week 5**:
> ```
> Upload (auto-queue nếu PRENATAL_CHECKUP)
>   → Background: Azure OCR (extract text) → Gemini AI (structured JSON)
>   → OcrResult: Status = Succeeded, StructuredJson có data
> ```
> Hoặc trigger thủ công qua endpoint `/ocr/process`.

---

### ✅ TC-P01: Full Pipeline — Manual trigger cho PRENATAL_CHECKUP
```
POST /api/documents/{documentId_prenatal}/ocr/process?lang=vi
Authorization: Bearer {token}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "OCR + AI extraction completed successfully.",
  "data": {
    "id": "<ocrResultId>",
    "documentId": "{documentId_prenatal}",
    "ocrRunNumber": 1,
    "status": "Succeeded",
    "ocrEngine": "AzureDocumentIntelligence",
    "rawText": "Bệnh viện Phụ sản Trung ương...\nHọ tên: Nguyễn Thị A...",
    "structuredJson": "{ \"vitalsData\": { \"generalInfo\": {...}, \"interview\": {...}, \"examination\": {...}, ... }, \"overallConfidence\": 0.85 }",
    "confidenceScore": 0.85,
    "languageHint": "vi",
    "ocrProcessingTimeMs": 5000,
    "aiModelUsed": "gemini-2.5-flash",
    "aiTokensUsed": 1500,
    "aiProcessingTimeMs": 3000,
    "errorMessage": null,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `status` = `"Succeeded"`
- [ ] `rawText` chứa text đọc được từ ảnh (tiếng Việt nếu ảnh phiếu khám VN)
- [ ] `structuredJson` là JSON hợp lệ, chứa `vitalsData` (generalInfo, interview, examination, diagnosis, ...), `overallConfidence`
- [ ] `ocrEngine` = `"AzureDocumentIntelligence"`
- [ ] `aiModelUsed` = `"gemini-2.5-flash"`
- [ ] `ocrProcessingTimeMs` > 0
- [ ] `aiProcessingTimeMs` > 0
- [ ] `aiTokensUsed` > 0

**Lưu lại**: `{ocrResultId}`.

---

### ✅ TC-P02: Verify StructuredJson — Parse JSON
> Lấy `structuredJson` từ TC-P01, parse bằng JSON formatter.

**Expected JSON structure** (matches VitalsJsonDto schema):
```json
{
  "vitalsData": {
    "generalInfo": {
      "facility": "Bệnh viện Phụ sản Trung ương",
      "fullName": "Nguyễn Thị A",
      "dateOfBirth": "1996-05-15",
      "age": 30,
      "address": "Hà Nội"
    },
    "interview": {
      "reasonForVisit": "Khám thai định kỳ",
      "pregnancyNumber": 1,
      "gestationalWeek": 20,
      "lastMenstrualPeriodDate": "2025-09-20",
      "expectedDeliveryDate": "2026-06-27"
    },
    "examination": {
      "vitalSigns": {
        "pulseBpm": 80,
        "bloodPressureSystolic": 120,
        "bloodPressureDiastolic": 80,
        "weightKg": 55.5,
        "heightCm": 160
      },
      "obstetric": {
        "fundusHeightCm": 18,
        "fetalHeartRateBpm": 140,
        "fetalPresentation": "normal"
      }
    },
    "diagnosis": {
      "text": "Thai phát triển bình thường",
      "icdCode": "Z34.0"
    },
    "treatmentPlan": {
      "medication": "Sắt 1 viên/ngày, Canxi 1 viên/ngày",
      "healthEducation": true
    },
    "nextAppointment": {
      "date": "2026-03-10",
      "examinerType": "obstetrician"
    }
  },
  "overallConfidence": 0.85
}
```
**Verify**:
- [ ] JSON parse không lỗi
- [ ] Có ít nhất 1 field chứa data thực (không phải null hết)
- [ ] `overallConfidence` nằm trong khoảng 0.0 → 1.0

---

### ✅ TC-P03: Full Pipeline — With lang=en
```
POST /api/documents/{documentId_pdf}/ocr/process?lang=en
Authorization: Bearer {token}
```
**Expected**: 201 Created — Pipeline chạy thành công với `languageHint: "en"`.
**Verify**: `rawText` vẫn chứa text, `structuredJson` vẫn có data.

---

### ✅ TC-P04: Full Pipeline — Default lang (không truyền query param)
```
POST /api/documents/{documentId_prenatal}/ocr/process
Authorization: Bearer {token}
```
**Expected**: 201 Created — Default `lang=vi`, `languageHint: "vi"`.

---

### ❌ TC-P05: Full Pipeline — Document type không phải PRENATAL_CHECKUP
```
POST /api/documents/{documentId_ultrasound}/ocr/process?lang=vi
Authorization: Bearer {token}
```
**Expected**: 400 Bad Request — `"Only PRENATAL_CHECKUP documents can be processed"` (hoặc message tương tự).

> **⚠️ Lý do**: `MedicalRecordAiService.ProcessDocumentAsync` validate `DocumentType == PRENATAL_CHECKUP`.

---

### ❌ TC-P06: Full Pipeline — Document không tồn tại
```
POST /api/documents/00000000-0000-0000-0000-000000000000/ocr/process?lang=vi
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-P07: Full Pipeline — Document thuộc user khác
> Đăng nhập tài khoản khác, dùng `{documentId_prenatal}` của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

### ❌ TC-P08: Full Pipeline — Không có quyền ocr.trigger
> Dùng account không có permission `ocr.trigger`.

```
POST /api/documents/{documentId_prenatal}/ocr/process?lang=vi
Authorization: Bearer {token_no_permission}
```
**Expected**: 403 Forbidden.

---

## 3️⃣ RE-EXTRACT — Chạy lại AI (skip OCR)

> **Use case**: Sau khi update prompt template hoặc pregnancy context thay đổi, chạy lại chỉ phần Gemini AI extraction mà không cần OCR lại.

### ✅ TC-E01: Re-extract AI — Happy Path
```
POST /api/ocr/{ocrResultId}/re-extract
Authorization: Bearer {token}
```
**Expected**: 201 Created
```json
{
  "message": "AI re-extraction completed successfully.",
  "data": {
    "id": "<new-or-same-ocrResultId>",
    "status": "Succeeded",
    "rawText": "... (giữ nguyên text cũ)",
    "structuredJson": "{ ... (có thể khác nếu template thay đổi) }",
    "aiModelUsed": "gemini-2.5-flash",
    "aiTokensUsed": 1200,
    "aiProcessingTimeMs": 2500,
    ...
  }
}
```
**Verify**:
- [ ] `rawText` giữ nguyên (không gọi OCR lại)
- [ ] `structuredJson` có data (AI extract lại)
- [ ] `aiProcessingTimeMs` > 0
- [ ] `ocrProcessingTimeMs` giữ nguyên giá trị cũ (không OCR lại)

---

### ❌ TC-E02: Re-extract — OcrResult không tồn tại
```
POST /api/ocr/00000000-0000-0000-0000-000000000000/re-extract
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-E03: Re-extract — OcrResult chưa có rawText (Pending)
> Nếu có OcrResult ở trạng thái Pending (chưa có rawText), thử re-extract.

**Expected**: 400 Bad Request — `"No raw text available for re-extraction"` (hoặc tương tự).

---

### ❌ TC-E04: Re-extract — Không có quyền ocr.trigger
```
POST /api/ocr/{ocrResultId}/re-extract
Authorization: Bearer {token_no_permission}
```
**Expected**: 403 Forbidden.

---

## 4️⃣ GET OCR STATUS — Kiểm tra trạng thái + kết quả

### ✅ TC-G01: Get OCR Status — Succeeded
```
GET /api/ocr/{ocrResultId}/status
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "data": {
    "id": "{ocrResultId}",
    "documentId": "{documentId_prenatal}",
    "ocrRunNumber": 1,
    "status": "Succeeded",
    "ocrEngine": "AzureDocumentIntelligence",
    "rawText": "Bệnh viện...",
    "structuredJson": "{ ... }",
    "confidenceScore": 0.85,
    "languageHint": "vi",
    "ocrProcessingTimeMs": 5000,
    "aiModelUsed": "gemini-2.5-flash",
    "aiTokensUsed": 1500,
    "aiProcessingTimeMs": 3000,
    "errorMessage": null,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] Tất cả AI fields có giá trị (không null)
- [ ] `status` = `"Succeeded"`

---

### ✅ TC-G02: Get OCR Results by Document
```
GET /api/documents/{documentId_prenatal}/ocr
Authorization: Bearer {token}
```
**Expected**: 200 OK — Array chứa tất cả OcrResult của document, ordered by latest first.
```json
{
  "data": [
    {
      "id": "<ocrResultId-latest>",
      "ocrRunNumber": 3,
      "status": "Succeeded",
      ...
    },
    {
      "id": "<ocrResultId-older>",
      "ocrRunNumber": 2,
      "status": "Succeeded",
      ...
    },
    ...
  ]
}
```
**Verify**: `ocrRunNumber` giảm dần (latest first).

---

### ❌ TC-G03: Get OCR Status — ID không tồn tại
```
GET /api/ocr/00000000-0000-0000-0000-000000000000/status
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-G04: Get OCR Results — Document không tồn tại
```
GET /api/documents/00000000-0000-0000-0000-000000000000/ocr
Authorization: Bearer {token}
```
**Expected**: 404 Not Found (hoặc 200 OK với array rỗng, tùy service implementation).

---

### ❌ TC-G05: Get OCR Results — Document thuộc user khác
> Đăng nhập tài khoản khác, dùng `{documentId_prenatal}` của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

## 5️⃣ BACKGROUND PROCESSING — Auto-queue khi upload

> **Flow**: Upload PRENATAL_CHECKUP → auto-queue OCR job → Background Service chạy full pipeline → OcrResult updated.

### ✅ TC-B01: Upload PRENATAL_CHECKUP → Background OCR auto-triggered
1. Upload ảnh phiếu khám:
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Ảnh phiếu khám thai thật |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000001` (PRENATAL_CHECKUP) |
| `title` | Text | `Phiếu khám - Test background` |

**Verify Step 1** — Response trả về ngay (<1s):
- [ ] 201 Created — Upload xong
- [ ] `files[0].fileUrl` là URL Supabase thực

2. Kiểm tra console log:
- [ ] Log: `"Processing OCR job: DocumentId=..."` xuất hiện
- [ ] Log: `"OCR job completed..."` sau 10-30 giây

3. Kiểm tra OcrResult sau khi background xong (đợi 15-30s):
```
GET /api/documents/{newDocumentId}/ocr
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có OcrResult với `status: "Succeeded"`, `structuredJson` có data.

---

### ✅ TC-B02: Upload BLOOD_TEST → KHÔNG auto-triggered
1. Upload file:
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 hoặc nhiều file ảnh |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000003` (BLOOD_TEST) |
| `title` | Text | `Xét nghiệm máu - Test no background` |

**Verify**:
- [ ] 201 Created — Upload thành công
- [ ] Console **KHÔNG** có `"Processing OCR job"` cho document này
- [ ] `GET /api/documents/{id}/ocr` → Array rỗng (không có OcrResult tự động)

---

### ✅ TC-B03: Upload nhiều PRENATAL_CHECKUP liên tiếp → Background xử lý tuần tự
1. Upload 2-3 ảnh PRENATAL_CHECKUP liên tiếp (nhanh, không đợi)
2. Kiểm tra console: các job được xử lý tuần tự (Channel queue)

**Verify**:
- [ ] Tất cả uploads trả về 201 ngay lập tức
- [ ] Console logs hiển thị processing lần lượt
- [ ] Sau khi tất cả xong, mỗi document đều có OcrResult Succeeded

---

## 6️⃣ EDGE CASES + ERROR HANDLING

### ❌ TC-ERR01: Upload file không hỗ trợ (ví dụ: .exe, .zip)
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file `.exe` hoặc `.zip` |

**Expected**: 400 Bad Request — Unsupported file type (hoặc upload thành công nhưng OCR fail).

---

### ❌ TC-ERR02: Upload file quá lớn (>10MB nếu có limit)
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file >10MB |

**Expected**: 400 Bad Request hoặc 413 Payload Too Large (tùy config).

---

### ✅ TC-ERR03: Verify AI extraction với ảnh chất lượng thấp
> Upload ảnh mờ, chụp nghiêng, chất lượng kém.

```
POST /api/documents/{blurryDocId}/ocr/process?lang=vi
Authorization: Bearer {token}
```
**Expected**: 201 Created — Pipeline vẫn chạy, nhưng:
- `confidenceScore` thấp hơn (< 0.5)
- `structuredJson` có thể thiếu nhiều fields
- `rawText` có thể không đầy đủ

---

### ✅ TC-ERR04: Verify OCR with non-medical text image
> Upload ảnh có text nhưng không phải phiếu khám (ví dụ: screenshot app, menu nhà hàng).

```
POST /api/documents/{nonMedicalDocId}/ocr/process?lang=vi
Authorization: Bearer {token}
```
**Expected**: 201 Created — AI extract sẽ có `overallConfidence` rất thấp, nhiều fields null.

---

## 7️⃣ CROSS-FEATURE TESTS

### ✅ TC-X01: Supabase URL persistence — GET document sau upload
```
GET /api/documents/{documentId_prenatal}
Authorization: Bearer {token}
```
**Verify**:
- [ ] `files[0].fileUrl` vẫn là URL Supabase thực (không phải placeholder)
- [ ] URL mở được trên trình duyệt

---

### ✅ TC-X02: Timeline hiển thị document với Supabase URL
```
GET /api/pregnancies/{pregnancyId}/timeline
Authorization: Bearer {token}
```
**Expected**: 200 OK — Documents mới upload xuất hiện trong timeline.

---

### ✅ TC-X03: Permission isolation — ocr.trigger required cho process
> User không có permission `ocr.trigger` → không thể `POST /ocr/process`, `POST /re-extract`.

**Expected**: 403 Forbidden cho cả 2 endpoints.

---

### ✅ TC-X04: Permission isolation — ocr.view required cho get status
> User không có permission `ocr.view` → không thể `GET /ocr/status`, `GET /documents/{id}/ocr`.

**Expected**: 403 Forbidden cho cả 2 endpoints.

---

### ✅ TC-X05: Multiple OCR runs — ocrRunNumber tăng
1. `POST /documents/{id}/ocr/process` → ocrRunNumber = n
2. `POST /documents/{id}/ocr/process` → ocrRunNumber = n+1
3. `GET /documents/{id}/ocr` → List ordered by runNumber desc

**Verify**: Mỗi lần process tạo OcrResult mới với `ocrRunNumber` tăng dần.

---

### ✅ TC-X06: Re-extract giữ nguyên rawText, chỉ update AI fields
1. `POST /documents/{id}/ocr/process` → Lưu `rawText_original`
2. `POST /ocr/{ocrResultId}/re-extract` → So sánh `rawText`

**Verify**: `rawText` giống nhau, `structuredJson` có thể khác, `aiProcessingTimeMs` mới.

---

### ✅ TC-X07: Soft-delete document → OCR results vẫn accessible?
1. `DELETE /api/documents/{documentId}` → 200 OK
2. `GET /api/ocr/{ocrResultId}/status`

**Expected**: Kiểm tra behavior — OCR result có bị ẩn theo document không? (Phụ thuộc soft-delete filter).

---

## 📊 CHECKLIST SUMMARY

| # | Test Case | Type | Result |
|---|-----------|------|--------|
| **Startup & Chuẩn bị** | | | |
| S00 | App startup — Background Service | ✅ | ☐ |
| S01 | Enum OcrStatus đã cập nhật (6 values) | ✅ | ☐ |
| S02 | Chuẩn bị — Có pregnancy Active | ✅ | ☐ |
| **Supabase Storage** | | | |
| U01 | Upload ảnh PRENATAL_CHECKUP → Supabase URL | ✅ | ☐ |
| U02 | Upload PDF PRENATAL_CHECKUP → Supabase URL | ✅ | ☐ |
| U03 | Upload ULTRASOUND → No auto-queue OCR | ✅ | ☐ |
| U04 | Upload PRESCRIPTION → No auto-queue OCR | ✅ | ☐ |
| U05 | Upload không có type → No auto-queue OCR | ✅ | ☐ |
| **OCR + AI Full Pipeline** | | | |
| P01 | Full Pipeline — Manual trigger (PRENATAL_CHECKUP) | ✅ | ☐ |
| P02 | Verify StructuredJson — Parse JSON | ✅ | ☐ |
| P03 | Full Pipeline — lang=en | ✅ | ☐ |
| P04 | Full Pipeline — Default lang | ✅ | ☐ |
| P05 | Full Pipeline — Non-PRENATAL_CHECKUP → 400 | ❌ 400 | ☐ |
| P06 | Full Pipeline — Document không tồn tại | ❌ 404 | ☐ |
| P07 | Full Pipeline — Document user khác | ❌ 403/404 | ☐ |
| P08 | Full Pipeline — Không có quyền | ❌ 403 | ☐ |
| **Re-extract AI** | | | |
| E01 | Re-extract — Happy path | ✅ | ☐ |
| E02 | Re-extract — OcrResult không tồn tại | ❌ 404 | ☐ |
| E03 | Re-extract — Chưa có rawText | ❌ 400 | ☐ |
| E04 | Re-extract — Không có quyền | ❌ 403 | ☐ |
| **Get OCR Status** | | | |
| G01 | Get Status — Succeeded + AI fields | ✅ | ☐ |
| G02 | Get Results by Document | ✅ | ☐ |
| G03 | Get Status — ID không tồn tại | ❌ 404 | ☐ |
| G04 | Get Results — Document không tồn tại | ❌ 404 | ☐ |
| G05 | Get Results — Document user khác | ❌ 403/404 | ☐ |
| **Background Processing** | | | |
| B01 | Upload PRENATAL_CHECKUP → Auto background OCR | ✅ | ☐ |
| B02 | Upload BLOOD_TEST → No auto-trigger | ✅ | ☐ |
| B03 | Upload nhiều PRENATAL → Tuần tự processing | ✅ | ☐ |
| **Edge Cases** | | | |
| ERR01 | Upload file không hỗ trợ | ❌ 400 | ☐ |
| ERR02 | Upload file quá lớn | ❌ 400/413 | ☐ |
| ERR03 | Ảnh chất lượng thấp → Low confidence | ✅ | ☐ |
| ERR04 | Ảnh non-medical → Low confidence | ✅ | ☐ |
| **Cross-Feature** | | | |
| X01 | Supabase URL persistence | ✅ | ☐ |
| X02 | Timeline hiển thị document mới | ✅ | ☐ |
| X03 | Permission — ocr.trigger | ❌ 403 | ☐ |
| X04 | Permission — ocr.view | ❌ 403 | ☐ |
| X05 | Multiple runs — ocrRunNumber tăng | ✅ | ☐ |
| X06 | Re-extract giữ rawText | ✅ | ☐ |
| X07 | Soft-delete → OCR behavior | ✅ | ☐ |

**Tổng: 38 test cases** (23 happy path ✅ + 15 error cases ❌)

---

## ⚙️ RECOMMENDED TEST ORDER

1. **Startup** (S00 → S02) — Verify app chạy, enum đúng, có pregnancy
2. **Supabase Upload** (U01 → U05) — Verify file lên Supabase thật, auto-queue logic
3. **Full Pipeline** (P01 → P08) — Test manual OCR + AI endpoint
4. **Re-extract** (E01 → E04) — Test AI-only extraction
5. **Get Status** (G01 → G05) — Verify status + results endpoints
6. **Background** (B01 → B03) — Test auto-processing
7. **Edge Cases** (ERR01 → ERR04) — Error handling
8. **Cross-Feature** (X01 → X07) — Integration tests

> **⚡ Tip**: Chạy U01 trước để có `documentId_prenatal` + Supabase URL verify. Sau đó P01 để có `ocrResultId` cho các test sau.

---

## 📝 ENUM REFERENCE (Week 5 Updated)

| Enum | Values |
|------|--------|
| DocumentSource | Upload(0), Share(1), Import(2) |
| OcrStatus | Pending(0), OcrProcessing(1), OcrCompleted(2), AiExtracting(3), Succeeded(4), Failed(5) |

---

## 🔑 PERMISSIONS REFERENCE (Week 5)

| Permission | Used by | Roles |
|------------|---------|-------|
| `document.create` | POST upload | USER, DOCTOR, ADMIN |
| `document.view` | GET list/detail | USER, DOCTOR, ADMIN |
| `ocr.trigger` | POST process, re-extract | USER, DOCTOR, ADMIN |
| `ocr.view` | GET status, GET by document | USER, DOCTOR, ADMIN |
| `ai.admin` | Manage AI prompt templates (future) | ADMIN |

---

## 🔧 TROUBLESHOOTING

### App crash on startup
- **Nguyên nhân**: `Supabase:Url` hoặc `AI:AzureDocumentIntelligence:Endpoint` chưa set → `InvalidOperationException`
- **Fix**: Set đầy đủ config trong `appsettings.Development.json`

### OCR returns 500 Internal Server Error
- **Nguyên nhân**: Azure Doc Intelligence API key sai hoặc hết quota
- **Fix**: Kiểm tra key/endpoint trong Azure Portal, verify quota

### AI extraction trả về null StructuredJson
- **Nguyên nhân**: Gemini API key sai hoặc rate limited
- **Fix**: Kiểm tra key tại Google AI Studio, check response logs

### Upload trả về 500
- **Nguyên nhân**: Supabase bucket chưa tạo hoặc ServiceRoleKey sai
- **Fix**: Tạo bucket `medical-documents` (public read), verify key

### Background job không chạy
- **Nguyên nhân**: `OcrBackgroundService` không log gì
- **Fix**: Kiểm tra `AddHostedService<OcrBackgroundService>()` trong DI, kiểm tra console log level ≥ Information

### OcrResult status stuck ở "OcrProcessing" / "AiExtracting"
- **Nguyên nhân**: Background job crash giữa chừng
- **Fix**: Check console for exceptions, re-trigger bằng `POST /ocr/process`
