# Medical Record & OCR — Hướng dẫn API cho Frontend

> **Mục đích**: Hướng dẫn Flutter/FE team nối API cho tính năng **Medical Record** (upload tài liệu y tế, OCR + AI extraction, review & confirm, timeline).  
> **Base URL**: `https://{domain}/api`  
> **Auth**: Tất cả API (trừ ref data) yêu cầu header `Authorization: Bearer {accessToken}`  
> **Cập nhật**: 2026-03-10

---

## MỤC LỤC

1. [Tổng quan luồng](#1-tổng-quan-luồng)
2. [Enums & Reference Data](#2-enums--reference-data)
3. [API: Upload Document](#3-api-upload-document)
4. [API: Document CRUD](#4-api-document-crud)
5. [API: OCR Pipeline](#5-api-ocr-pipeline)
6. [API: Review & Confirm (AutoFill)](#6-api-review--confirm-autofill)
7. [API: Timeline](#7-api-timeline)
8. [VitalsJson — Chi tiết tất cả fields](#8-vitalsjson--chi-tiết-tất-cả-fields)
9. [Luồng tích hợp từng bước](#9-luồng-tích-hợp-từng-bước)
10. [Response Format chuẩn](#10-response-format-chuẩn)
11. [Error Handling](#11-error-handling)

---

## 1. TỔNG QUAN LUỒNG

Tính năng Medical Record có **3 luồng** khác nhau tùy theo loại tài liệu:

```
                              User upload ảnh tài liệu y tế
                                         │
                        ┌────────────────┼────────────────┐
                        │                │                │
                   PRENATAL_CHECKUP    TEST TYPES      OTHERS
                   (Phiếu khám thai)   (Xét nghiệm)   (Đơn thuốc, tiêm chủng...)
                        │                │                │
                   ✅ Có OCR + AI      ❌ Không OCR     ❌ Không OCR
                        │                │                │
                   Background:           │                ✅ DONE
                   Azure OCR →           │                (chỉ lưu trữ archive)
                   Gemini AI →           │
                   StructuredJson        │
                        │                │
                   FE polls status       │
                   3-5 giây/lần          │
                        │                │
                   GET review            │
                   (AI pre-fill form)    │
                        │                │
                   User review +      User tự nhập
                   chỉnh sửa         metadata
                        │                │
                   POST confirm       POST confirm
                        │                │
                   → Tạo             → Tạo
                     PrenatalVisit     PrenatalTest
                     + VitalsJson      + link Document
```

### Phân loại Document Type

| Nhóm | Document Type Codes | Có OCR? | Confirm tạo gì? |
|------|---------------------|:-------:|-----------------|
| **Phiếu khám** | `PRENATAL_CHECKUP` | ✅ | PrenatalVisit (chứa VitalsJson) |
| **Xét nghiệm** | `BLOOD_TEST`, `URINE_TEST`, `ULTRASOUND`, `HIV_TEST`, `HEPATITIS_B_TEST`, `THYROID_TEST`, `GLUCOSE_TEST`, `CBC_TEST`, `NT_SCAN` | ❌ | PrenatalTest |
| **Lưu trữ** | `PRESCRIPTION`, `VACCINATION_RECORD`, `MEDICAL_REPORT`, `OTHER` | ❌ | Không cần confirm |

---

## 2. ENUMS & REFERENCE DATA

### 2.1 Enums quan trọng

#### OcrStatus — Trạng thái pipeline OCR + AI

```
Pending        → Đang chờ xử lý (vừa queue)
OcrProcessing  → Azure OCR đang chạy
OcrCompleted   → OCR xong, chờ AI extraction
AiExtracting   → Gemini AI đang trích xuất dữ liệu
Succeeded      → Pipeline hoàn tất ✅ (FE có thể gọi review)
Failed         → Pipeline thất bại ❌
Confirmed      → User đã review + confirm xong ✅
```

**FE cần xử lý:**
- `Pending`, `OcrProcessing`, `OcrCompleted`, `AiExtracting` → Hiện loading spinner, tiếp tục polling
- `Succeeded` → Dừng polling, hiện nút "Xem kết quả" / chuyển sang màn review
- `Failed` → Hiện lỗi + nút "Thử lại"
- `Confirmed` → Đã hoàn tất, hiện trạng thái xong

#### DocumentSource — Nguồn upload

```
Upload     → User tự chụp/chọn ảnh upload
Share      → Được chia sẻ từ bác sĩ/người thân
Import     → Import từ hệ thống khác
```

#### VisitType — Loại buổi khám

```
Routine    → Khám định kỳ
Emergency  → Cấp cứu
FollowUp   → Tái khám
LabOnly    → Chỉ xét nghiệm
Other      → Khác
```

### 2.2 API Lấy danh sách Document Types (PUBLIC — không cần auth)

```
GET /api/ref/document-types?lang=vi
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `lang` | string | `"vi"` | Mã ngôn ngữ (`vi`, `en`) |

**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    { "id": "b0000001-...-000000000001", "code": "PRENATAL_CHECKUP", "displayName": "Khám thai", "description": "..." },
    { "id": "b0000001-...-000000000002", "code": "ULTRASOUND", "displayName": "Siêu âm", "description": null },
    { "id": "b0000001-...-000000000003", "code": "BLOOD_TEST", "displayName": "Xét nghiệm máu", "description": null }
  ]
}
```

**FE sử dụng:** Cache danh sách này. Dùng `id` khi upload document. Dùng `code` để phân biệt loại xử lý (OCR hay không).

### 2.3 API Lấy danh sách Test Types (PUBLIC)

```
GET /api/ref/test-types?lang=vi
```

**Response:**
```json
{
  "success": true,
  "data": [
    { "id": "c0000001-...", "code": "CBC_TEST", "category": "LAB", "displayName": "Xét nghiệm công thức máu", "description": null },
    { "id": "c0000001-...", "code": "ULTRASOUND", "category": "IMAGING", "displayName": "Siêu âm", "description": null }
  ]
}
```

### 2.4 API Lấy tất cả Enum values

```
GET /api/ref/enums
GET /api/ref/enums/OcrStatus        ← lấy 1 enum cụ thể
GET /api/ref/enums/DocumentSource
GET /api/ref/enums/VisitType
```

---

## 3. API: UPLOAD DOCUMENT

### `POST /api/pregnancies/{pregnancyId}/documents`

Upload 1-N ảnh/PDF + tạo Medical Document. Dùng **multipart/form-data**.

**Headers:**
```
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**Request (form-data):**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `files` | File[] | ✅ | 1-N file ảnh/PDF. Max 10MB/file. MIME: `image/jpeg`, `image/png`, `application/pdf` |
| `documentTypeId` | Guid | ❌ | ID loại tài liệu (lấy từ `/api/ref/document-types`). Nếu null → xử lý như `OTHER` |
| `title` | string | ❌ | Tiêu đề tài liệu |
| `documentDate` | DateOnly | ❌ | Ngày ghi trên tài liệu (format: `yyyy-MM-dd`) |
| `notes` | string | ❌ | Ghi chú |

**Flutter example (Dio):**
```dart
final formData = FormData.fromMap({
  'files': [
    await MultipartFile.fromFile(imagePath, filename: 'phieu-kham.jpg'),
  ],
  'documentTypeId': 'b0000001-0000-0000-0000-000000000001', // PRENATAL_CHECKUP
  'title': 'Khám thai tuần 28',
  'documentDate': '2026-03-10',
  'notes': 'Bệnh viện Từ Dũ',
});

final response = await dio.post(
  '/api/pregnancies/$pregnancyId/documents',
  data: formData,
);
```

**Response (201 Created):**
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Document created successfully.",
  "data": {
    "id": "d0000001-...",
    "pregnancyId": "a0000001-...",
    "visitId": null,
    "documentTypeId": "b0000001-...-000000000001",
    "documentTypeDisplayName": "Khám thai",
    "files": [
      {
        "id": "f0000001-...",
        "storageFileId": "s0000001-...",
        "originalFileName": "phieu-kham.jpg",
        "mimeType": "image/jpeg",
        "fileSizeBytes": 2048576,
        "fileUrl": "https://xxx.supabase.co/storage/v1/object/public/medical-documents/2026/03/10/abc123.jpg",
        "sortOrder": 0,
        "pageLabel": null
      }
    ],
    "totalFileSizeBytes": 2048576,
    "title": "Khám thai tuần 28",
    "documentDate": "2026-03-10",
    "capturedAt": "2026-03-10T10:30:00Z",
    "source": "Upload",
    "notes": "Bệnh viện Từ Dũ",
    "isFavorite": false,
    "createdAt": "2026-03-10T10:30:00Z",
    "updatedAt": "2026-03-10T10:30:00Z"
  }
}
```

**⚠️ Quan trọng:**
- Response trả về **ngay** (<1 giây), KHÔNG đợi OCR.
- Nếu `documentTypeId` = PRENATAL_CHECKUP → BE tự động queue OCR ở background.
- FE cần **lưu `document.id`** rồi bắt đầu trigger OCR pipeline (bước tiếp).

---

## 4. API: DOCUMENT CRUD

### 4.1 List documents theo thai kỳ

```
GET /api/pregnancies/{pregnancyId}/documents
GET /api/pregnancies/{pregnancyId}/documents?isFavorite=true
```

| Query Param | Type | Mô tả |
|-------------|------|-------|
| `isFavorite` | bool? | `true` = chỉ yêu thích, `false` = chỉ không yêu thích, null = tất cả |

**Response:** `data` = `MedicalDocumentDto[]` (cùng format như [response upload ở trên](#3-api-upload-document))

---

### 4.2 Chi tiết 1 document

```
GET /api/documents/{id}
```

**Response:** `data` = `MedicalDocumentDto` (bao gồm `files[]`, `documentTypeDisplayName`)

---

### 4.3 Update metadata

```
PUT /api/documents/{id}
```

**Body (JSON):**
```json
{
  "visitId": null,
  "documentTypeId": "b0000001-...-000000000001",
  "title": "Khám thai tuần 28 - updated",
  "documentDate": "2026-03-10",
  "notes": "Cập nhật ghi chú"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `visitId` | Guid? | Link document → PrenatalVisit (thường do AutoFill tự set) |
| `documentTypeId` | Guid? | Đổi loại tài liệu |
| `title` | string? | Tiêu đề |
| `documentDate` | DateOnly? | Ngày tài liệu |
| `notes` | string? | Ghi chú |

---

### 4.4 Toggle yêu thích

```
PATCH /api/documents/{id}/favorite
```

Không cần body. Server tự toggle `isFavorite` (true ↔ false).

**Response:** `data` = `MedicalDocumentDto` (với `isFavorite` mới)

---

### 4.5 Xóa document (soft delete)

```
DELETE /api/documents/{id}
```

**Response:** `data` = null, `message` = "Document deleted successfully."

**⚠️ Lưu ý:**
- Soft delete: document bị ẩn khỏi danh sách user, nhưng data y tế (PrenatalVisit/PrenatalTest) **vẫn giữ nguyên**.
- Nếu document đã được confirm → Visit/Test không bị ảnh hưởng.

---

## 5. API: OCR PIPELINE

### 5.1 Trigger OCR + AI Extraction

> **Khi nào gọi?** Sau khi upload document PRENATAL_CHECKUP thành công.

```
POST /api/documents/{documentId}/ocr/process?lang=vi
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `documentId` | Guid | (path) | ID document vừa upload |
| `lang` | string | `"vi"` | Ngôn ngữ OCR hint (`vi` = Tiếng Việt, `en` = English) |

**Response (202 Accepted):**
```json
{
  "success": true,
  "statusCode": 202,
  "message": "OCR + AI extraction queued. Poll GET /api/ocr/{id}/status to check progress.",
  "data": {
    "id": "ocr-result-id-...",
    "documentId": "d0000001-...",
    "ocrRunNumber": 1,
    "status": "Pending",
    "ocrEngine": null,
    "languageHint": "vi",
    "rawText": null,
    "structuredJson": null,
    "confidenceScore": null,
    "errorMessage": null,
    "createdAt": "2026-03-10T10:30:05Z",
    "updatedAt": "2026-03-10T10:30:05Z"
  }
}
```

**⚠️ FE lưu `data.id`** (ocrResultId) để polling status ở bước tiếp.

---

### 5.2 Polling trạng thái OCR

> **Khi nào gọi?** Sau khi trigger OCR, poll mỗi **3-5 giây** cho đến khi `status` = `Succeeded` hoặc `Failed`.

```
GET /api/ocr/{ocrResultId}/status
```

**Response khi đang xử lý:**
```json
{
  "success": true,
  "data": {
    "id": "ocr-result-id-...",
    "documentId": "d0000001-...",
    "ocrRunNumber": 1,
    "status": "OcrProcessing",
    "rawText": null,
    "structuredJson": null,
    "confidenceScore": null,
    "errorMessage": null,
    "ocrProcessingTimeMs": null,
    "aiModelUsed": null,
    "aiTokensUsed": null,
    "aiProcessingTimeMs": null,
    "confirmedAt": null,
    "confirmedBy": null,
    "createdAt": "2026-03-10T10:30:05Z",
    "updatedAt": "2026-03-10T10:30:08Z"
  }
}
```

**Response khi thành công (status = "Succeeded"):**
```json
{
  "success": true,
  "data": {
    "id": "ocr-result-id-...",
    "status": "Succeeded",
    "confidenceScore": 87.50,
    "ocrProcessingTimeMs": 3200,
    "aiModelUsed": "gemini-2.5-flash",
    "aiTokensUsed": 2100,
    "aiProcessingTimeMs": 4500,
    "structuredJson": "{ ... JSON string ... }",
    "confirmedAt": null,
    "confirmedBy": null,
    "createdAt": "2026-03-10T10:30:05Z",
    "updatedAt": "2026-03-10T10:30:15Z"
  }
}
```

**Polling logic (Flutter pseudo-code):**
```dart
Timer.periodic(Duration(seconds: 4), (timer) async {
  final response = await dio.get('/api/ocr/$ocrResultId/status');
  final status = response.data['data']['status'];

  switch (status) {
    case 'Pending':
    case 'OcrProcessing':
    case 'OcrCompleted':
    case 'AiExtracting':
      // Hiện loading, tiếp tục poll
      updateLoadingMessage(status);
      break;
    case 'Succeeded':
      timer.cancel();
      navigateToReviewScreen(ocrResultId); // Chuyển sang review
      break;
    case 'Failed':
      timer.cancel();
      showError(response.data['data']['errorMessage']);
      break;
    case 'Confirmed':
      timer.cancel();
      showAlreadyConfirmed();
      break;
  }
});
```

**Loading message gợi ý theo status:**

| Status | Message gợi ý cho user |
|--------|------------------------|
| `Pending` | "Đang chuẩn bị xử lý..." |
| `OcrProcessing` | "Đang đọc ảnh phiếu khám..." |
| `OcrCompleted` | "Đã đọc xong, đang phân tích..." |
| `AiExtracting` | "AI đang trích xuất dữ liệu..." |
| `Succeeded` | "Hoàn tất! Xem kết quả..." |
| `Failed` | "Xử lý thất bại. Vui lòng thử lại." |

---

### 5.3 Re-extract AI (không cần OCR lại)

> **Khi nào dùng?** Khi AI extract sai nhưng ảnh đọc (OCR) OK → chỉ chạy lại Gemini.

```
POST /api/ocr/{ocrResultId}/re-extract
```

**Response:** 202 Accepted, tương tự trigger OCR. FE poll lại status.

---

### 5.4 Lấy tất cả OCR results của 1 document

```
GET /api/documents/{documentId}/ocr
```

**Response:** `data` = `OcrResultDto[]` (sắp xếp theo `ocrRunNumber` giảm dần — mới nhất trước)  

**Dùng khi:** Hiện lịch sử các lần chạy OCR (PRENATAL_CHECKUP có thể rerun nhiều lần).

---

## 6. API: REVIEW & CONFIRM (AutoFill)

### 6.1 Lấy dữ liệu AI đã extract để review

> **Khi nào gọi?** Sau khi OCR `status` = `Succeeded`, FE chuyển sang màn review.  
> **Chỉ áp dụng cho:** PRENATAL_CHECKUP

```
GET /api/ocr/{ocrResultId}/review?lang=vi
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `ocrResultId` | Guid | (path) | ID OcrResult có status = Succeeded |
| `lang` | string | `"vi"` | Ngôn ngữ cho tên DocumentType |

**Response:**
```json
{
  "success": true,
  "data": {
    "ocrResultId": "ocr-result-id-...",
    "documentId": "d0000001-...",
    "pregnancyId": "a0000001-...",
    "documentTypeId": "b0000001-...-000000000001",
    "documentTypeCode": "PRENATAL_CHECKUP",
    "documentTypeDisplayName": "Khám thai",
    "status": "Succeeded",
    "confidenceScore": 87.50,
    "fileUrls": [
      "https://xxx.supabase.co/storage/v1/object/public/.../abc123.jpg"
    ],
    "vitals": {
      "generalInfo": {
        "facility": "Bệnh viện Từ Dũ",
        "fullName": "Nguyễn Thị A",
        "dateOfBirth": "1995-06-15",
        "age": 30,
        "phone": "0901234567",
        "address": "123 Nguyễn Huệ, Q.1, TP.HCM"
      },
      "interview": {
        "reasonForVisit": "Khám thai định kỳ",
        "pregnancyNumber": 1,
        "gestationalWeek": 28,
        "lastMenstrualPeriodDate": "2025-09-01",
        "expectedDeliveryDate": "2026-06-08"
      },
      "examination": {
        "vitalSigns": {
          "pulseBpm": 80,
          "temperatureCelsius": 36.5,
          "bloodPressureSystolic": 120,
          "bloodPressureDiastolic": 80,
          "respiratoryRateBpm": 18,
          "weightKg": 65.5,
          "heightCm": 160.0
        },
        "general": {
          "mentalStatus": "alert",
          "edema": false,
          "urineProtein": false
        },
        "obstetric": {
          "fundusHeightCm": 28.0,
          "abdominalCircumferenceCm": 92.0,
          "fetalPresentation": "normal",
          "fetalHeartbeat": true,
          "fetalHeartRateBpm": 145,
          "cervix": "closed"
        }
      },
      "diagnosis": {
        "text": "Thai 28 tuần, phát triển bình thường",
        "icdCode": "Z34.0"
      },
      "treatmentPlan": {
        "medication": "Acid folic 400mg x 1 lần/ngày, Sắt 60mg x 1 lần/ngày",
        "nextSteps": "Tái khám sau 2 tuần",
        "healthEducation": true,
        "healthEducationNote": "Tư vấn dinh dưỡng tam cá nguyệt 3"
      },
      "prognosis": "normal",
      "nextAppointment": {
        "date": "2026-03-24",
        "notes": "Mang theo kết quả xét nghiệm máu",
        "examinerType": "obstetrician"
      }
    },
    "overallConfidence": 0.85,
    "rawStructuredJson": "{ ... original AI output string ... }",
    "canAutoFill": true,
    "cannotAutoFillReason": null
  }
}
```

**FE hiển thị:**
- Hiện ảnh gốc (từ `fileUrls[]`) bên trái
- Hiện form pre-filled từ `vitals` bên phải — cho user review + chỉnh sửa
- Nếu `canAutoFill` = false → hiện `cannotAutoFillReason`, disable nút Confirm

---

### 6.2 Confirm — Auto-tạo PrenatalVisit / PrenatalTest

> **Khi nào gọi?** Sau khi user review xong, bấm nút "Xác nhận".

```
POST /api/ocr/{ocrResultId}/confirm
```

**Body (JSON):**
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-03-10",
  "existingVisitId": null,
  "vitals": {
    "generalInfo": { ... },
    "interview": { ... },
    "examination": {
      "vitalSigns": {
        "pulseBpm": 80,
        "temperatureCelsius": 36.5,
        "bloodPressureSystolic": 120,
        "bloodPressureDiastolic": 80,
        "respiratoryRateBpm": 18,
        "weightKg": 65.5,
        "heightCm": 160.0
      },
      "general": { ... },
      "obstetric": { ... }
    },
    "diagnosis": { ... },
    "treatmentPlan": { ... },
    "prognosis": "normal",
    "nextAppointment": { ... }
  },
  "location": "Bệnh viện Từ Dũ",
  "notes": "Khám bình thường, thai phát triển tốt"
}
```

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `documentTypeId` | Guid | ✅ | ID loại tài liệu (xác định strategy: tạo Visit hay Test) |
| `eventDate` | DateOnly | ✅ | Ngày khám / ngày xét nghiệm (format: `yyyy-MM-dd`) |
| `existingVisitId` | Guid? | ❌ | Nếu muốn gắn vào buổi khám đã tồn tại, truyền visitId. Null = tạo visit mới |
| `vitals` | VitalsJsonDto? | ❌ | Toàn bộ dữ liệu phiếu khám (chỉ dùng cho PRENATAL_CHECKUP). **Xem [Section 8](#8-vitalsjson--chi-tiết-tất-cả-fields)** |
| `location` | string? | ❌ | Địa điểm khám |
| `notes` | string? | ❌ | Ghi chú thêm |

**Response (201 Created):**
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Đã tạo 1 buổi khám thai từ phiếu khám",
  "data": {
    "ocrResultId": "ocr-result-id-...",
    "documentId": "d0000001-...",
    "documentTypeCode": "PRENATAL_CHECKUP",
    "createdVisitId": "v0000001-...",
    "createdTestIds": [],
    "documentLinkedToVisit": true,
    "summary": "Đã tạo 1 buổi khám thai từ phiếu khám"
  }
}
```

**⚠️ Sau khi confirm thành công:**
- OcrResult.Status chuyển thành `Confirmed`
- MedicalDocument.VisitId được populate = visit mới tạo
- FE có thể navigate đến chi tiết PrenatalVisit (`createdVisitId`)

---

### 6.3 Confirm cho TEST TYPES (BLOOD_TEST, ULTRASOUND...)

Cùng API `POST /api/ocr/{ocrResultId}/confirm` nhưng body khác:

```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000003",
  "eventDate": "2026-03-10",
  "existingVisitId": null,
  "vitals": null,
  "location": null,
  "notes": "Kết quả bình thường"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "documentTypeCode": "BLOOD_TEST",
    "createdVisitId": "v0000002-...",
    "createdTestIds": ["t0000001-..."],
    "documentLinkedToVisit": true,
    "summary": "Đã tạo 1 xét nghiệm máu"
  }
}
```

> Lưu ý: Nếu không truyền `existingVisitId`, BE tự tạo 1 PrenatalVisit loại `Routine` để test không bị orphan.

---

## 7. API: TIMELINE

```
GET /api/pregnancies/{pregnancyId}/timeline
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "eventType": "Document",
      "eventId": "d0000001-...",
      "eventDate": "2026-03-10T00:00:00Z",
      "title": "Khám thai tuần 28",
      "description": "PRENATAL_CHECKUP"
    },
    {
      "eventType": "Visit",
      "eventId": "v0000001-...",
      "eventDate": "2026-03-10T00:00:00Z",
      "title": "Khám thai định kỳ",
      "description": "Routine"
    }
  ]
}
```

| Field | Mô tả |
|-------|-------|
| `eventType` | `"Document"` hoặc `"Visit"` |
| `eventId` | ID để navigate đến chi tiết |
| `eventDate` | Ngày sự kiện |
| `title` | Tiêu đề hiển thị |
| `description` | Thông tin phụ |

---

## 8. VITALSJSON — CHI TIẾT TẤT CẢ FIELDS

`VitalsJson` là object chứa **toàn bộ dữ liệu phiếu khám thai** (theo mẫu phiếu Bộ Y tế MS:51/BV2). Đây là dữ liệu AI extract từ ảnh, user review, rồi confirm. Sau khi confirm, `VitalsJson` được lưu vào `PrenatalVisit.VitalsJson`.

### Cấu trúc tổng thể

```
VitalsJsonDto (root)
├── generalInfo          → A. Thông tin chung (bệnh nhân, cơ sở y tế)
├── previousVisit        → B.I. Lần khám trước
├── interview            → B.II. Hỏi bệnh (lý do khám, tuổi thai)
├── medicalHistory       → Tiền sử bệnh (cá nhân, sản khoa, phụ khoa, gia đình)
├── examination          → Khám bệnh
│   ├── vitalSigns       → Sinh hiệu (mạch, HA, cân nặng...)
│   ├── general          → Khám tổng quát (phù, protein niệu)
│   └── obstetric        → Khám sản khoa (CCTC, tim thai, ngôi thai)
├── diagnosis            → Chẩn đoán + ICD
├── treatmentPlan        → Kế hoạch điều trị (thuốc, giáo dục sức khỏe)
├── prognosis            → Tiên lượng
└── nextAppointment      → Lần khám kế tiếp
```

---

### 8.1 `generalInfo` — Thông tin chung

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `facility` | string? | Tên cơ sở y tế | `"Bệnh viện Từ Dũ"` |
| `managingAuthority` | string? | Cơ quan chủ quản | `"Sở Y tế TP.HCM"` |
| `admissionNumber` | string? | Số vào viện | `"12345"` |
| `patientCode` | string? | Mã bệnh nhân | `"BN-2026-001"` |
| `fullName` | string? | Họ tên | `"Nguyễn Thị A"` |
| `dateOfBirth` | string? | Ngày sinh (`yyyy-MM-dd`) | `"1995-06-15"` |
| `age` | int? | Tuổi | `30` |
| `phone` | string? | Số điện thoại | `"0901234567"` |
| `occupation` | string? | Nghề nghiệp | `"Nhân viên văn phòng"` |
| `ethnicity` | string? | Dân tộc | `"Kinh"` |
| `nationality` | string? | Quốc tịch | `"Việt Nam"` |
| `address` | string? | Địa chỉ | `"123 Nguyễn Huệ"` |
| `ward` | string? | Phường/xã | `"Bến Nghé"` |
| `district` | string? | Quận/huyện | `"Quận 1"` |
| `province` | string? | Tỉnh/thành | `"TP. Hồ Chí Minh"` |
| `insuranceType` | string? | Loại bảo hiểm | `"BHYT"` / `"thu_phi"` / `"mien"` / `"khac"` |
| `insuranceNumber` | string? | Số thẻ BHYT | `"HS4010012345678"` |
| `insuranceExpiry` | string? | Ngày hết hạn BHYT (`yyyy-MM-dd`) | `"2026-12-31"` |
| `idNumber` | string? | Số CMND/CCCD | `"079095001234"` |

---

### 8.2 `previousVisit` — Lần khám trước

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `visitDate` | string? | Ngày khám lần trước (`yyyy-MM-dd`) | `"2026-02-24"` |
| `diagnosis` | string? | Chẩn đoán lần trước | `"Thai 26 tuần, bình thường"` |
| `treatment` | string? | Điều trị lần trước | `"Bổ sung sắt + acid folic"` |

---

### 8.3 `interview` — Hỏi bệnh

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `reasonForVisit` | string? | Lý do khám | `"Khám thai định kỳ"` |
| `pregnancyNumber` | int? | Lần mang thai thứ mấy | `1` |
| `totalVisitCount` | int? | Tổng số lần khám | `6` |
| `lastMenstrualPeriodDate` | string? | Ngày kinh cuối cùng (`yyyy-MM-dd`) | `"2025-09-01"` |
| `gestationalWeek` | int? | Tuổi thai (tuần) | `28` |
| `expectedDeliveryDate` | string? | Ngày dự sinh (`yyyy-MM-dd`) | `"2026-06-08"` |
| `clinicalProgress` | string? | Diễn biến lâm sàng | `"Ổn định"` |
| `generalCondition` | string? | Tình trạng toàn thân | `"normal"` / `"abnormal"` |
| `generalConditionNote` | string? | Ghi chú tình trạng | `null` |
| `tetanusVaccineHistory` | int? | Số mũi tiêm phòng uốn ván | `2` |

---

### 8.4 `medicalHistory` — Tiền sử bệnh

#### `medicalHistory.personal` — Tiền sử cá nhân

| JSON key | Type | Mô tả |
|----------|------|-------|
| `allergy` | bool? | Có dị ứng? |
| `allergyNote` | string? | Chi tiết dị ứng |
| `medicalHistory` | bool? | Có tiền sử bệnh? |
| `medicalHistoryNote` | string? | Chi tiết tiền sử |
| `hypertension` | bool? | Tăng huyết áp? |
| `heartDisease` | bool? | Bệnh tim? |
| `respiratoryDisease` | bool? | Bệnh hô hấp? |
| `thyroidDisease` | bool? | Bệnh tuyến giáp? |
| `kidneyDisease` | bool? | Bệnh thận? |
| `diabetes` | bool? | Tiểu đường? |
| `otherDiseases` | string? | Bệnh khác |
| `currentMedications` | bool? | Đang dùng thuốc? |
| `medicationNote` | string? | Thuốc đang dùng |
| `surgeryHistory` | bool? | Tiền sử phẫu thuật? |
| `surgeryNote` | string? | Chi tiết phẫu thuật |

#### `medicalHistory.obstetric` — Tiền sử sản khoa

| JSON key | Type | Mô tả |
|----------|------|-------|
| `para` | int? | Số lần sinh (PARA) |
| `previousPregnancies` | array? | Mảng các lần mang thai trước |

Mỗi phần tử `previousPregnancies[]`:

| JSON key | Type | Mô tả |
|----------|------|-------|
| `endDate` | string? | Ngày kết thúc |
| `gestationalAge` | string? | Tuổi thai khi kết thúc |
| `complicationsDuringPregnancy` | string? | Biến chứng thai kỳ |
| `deliveryMethod` | string? | Phương pháp sinh |
| `newbornInfo` | string? | Thông tin trẻ sơ sinh |
| `postpartum` | string? | Tình trạng hậu sản |

#### `medicalHistory.gynecology` — Tiền sử phụ khoa

| JSON key | Type | Mô tả |
|----------|------|-------|
| `menstrualCycle` | string? | `"regular"` / `"irregular"` |
| `menstrualCycleDays` | int? | Chu kỳ kinh (ngày) |
| `gynecologySurgery` | bool? | Phẫu thuật phụ khoa? |
| `gynecologySurgeryNote` | string? | Chi tiết |
| `ovarianTumor` | bool? | U nang buồng trứng? |
| `uterineFibroid` | bool? | U xơ tử cung? |
| `genitalMalformation` | bool? | Dị tật sinh dục? |
| `vaginalInfection` | bool? | Viêm nhiễm âm đạo? |

#### `medicalHistory` (top-level)

| JSON key | Type | Mô tả |
|----------|------|-------|
| `pelvicOrganProlapse` | bool? | Sa tạng vùng chậu? |
| `gynecologicalDiseaseNote` | string? | Ghi chú bệnh phụ khoa |

#### `medicalHistory.family` — Tiền sử gia đình

| JSON key | Type | Mô tả |
|----------|------|-------|
| `hasHistory` | bool? | Có tiền sử gia đình? |
| `familyHistoryNote` | string? | Chi tiết |
| `twins` | bool? | Sinh đôi? |
| `malformation` | bool? | Dị tật bẩm sinh? |
| `geneticDisease` | bool? | Bệnh di truyền? |
| `diabetes` | bool? | Tiểu đường? |
| `hypertension` | bool? | Tăng huyết áp? |
| `otherNote` | string? | Ghi chú khác |

---

### 8.5 `examination` — Khám bệnh

#### ⭐ `examination.vitalSigns` — Sinh hiệu *(QUAN TRỌNG NHẤT)*

> Đây là phần dữ liệu FE **cần hiển thị nổi bật nhất** và cho user dễ chỉnh sửa.

| JSON key | Type | Đơn vị | Mô tả | Ví dụ |
|----------|------|--------|-------|-------|
| `pulseBpm` | int? | lần/phút | Mạch | `80` |
| `temperatureCelsius` | decimal? | °C | Nhiệt độ | `36.5` |
| `bloodPressureSystolic` | int? | mmHg | Huyết áp tâm thu (số trên) | `120` |
| `bloodPressureDiastolic` | int? | mmHg | Huyết áp tâm trương (số dưới) | `80` |
| `respiratoryRateBpm` | int? | lần/phút | Nhịp thở | `18` |
| `weightKg` | decimal? | kg | Cân nặng | `65.5` |
| `heightCm` | decimal? | cm | Chiều cao | `160.0` |

**⚠️ Lưu ý cho FE:**
- `bloodPressureSystolic` và `bloodPressureDiastolic` là **2 trường riêng biệt** (int). FE hiển thị dạng `120/80` nhưng cần gửi **2 field riêng lẻ** khi confirm.
- Tất cả nullable — nếu AI không extract được sẽ trả `null`. FE nên render ô trống cho user tự nhập.

#### `examination.general` — Khám tổng quát

| JSON key | Type | Mô tả | Giá trị |
|----------|------|-------|---------|
| `mentalStatus` | string? | Tri giác | `"alert"` / `"coma"` / `"other"` |
| `mentalStatusNote` | string? | Ghi chú tri giác | |
| `edema` | bool? | Phù? | `true` / `false` / `null` |
| `urineProtein` | bool? | Protein niệu? | `true` / `false` / `null` |
| `urineProteinValue` | decimal? | Giá trị protein niệu (g/L) | `0.3` |

#### `examination.obstetric` — Khám sản khoa

| JSON key | Type | Đơn vị | Mô tả |
|----------|------|--------|-------|
| `oldScar` | bool? | — | Vết mổ cũ? |
| `scarPainful` | bool? | — | Vết mổ có đau? |
| `pelvis` | string? | — | Khung chậu (`"normal"` / `"abnormal"`) |
| `fundusHeightCm` | decimal? | cm | Chiều cao tử cung (CCTC/BCTC) |
| `abdominalCircumferenceCm` | decimal? | cm | Vòng bụng |
| `fetalPresentation` | string? | — | Ngôi thai (`"normal"` / `"abnormal"`) |
| `fetalPresentationNote` | string? | — | Ghi chú ngôi thai |
| `uterineContraction` | bool? | — | Có cơn co tử cung? |
| `uterineContractionFrequency` | int? | /10 phút | Tần số cơn co |
| `fetalHeartbeat` | bool? | — | Có tim thai? |
| `fetalHeartRateBpm` | int? | lần/phút | Nhịp tim thai |
| `cervix` | string? | — | Cổ tử cung (`"closed"` / `"effaced"` / `"dilated"`) |
| `cervixDilationCm` | decimal? | cm | Độ mở CTC |
| `amnioticSac` | string? | — | Đầu ối (`"bulging"` / `"flat"` / `"pear"`) |
| `membraneStatus` | string? | — | Màng ối (`"intact"` / `"leaking"` / `"ruptured"`) |
| `membraneRuptureTime` | string? | HH:mm | Giờ vỡ ối |
| `amnioticFluid` | string? | — | Nước ối (`"clear"` / `"green"` / `"bloody"`) |

---

### 8.6 `diagnosis` — Chẩn đoán

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `text` | string? | Nội dung chẩn đoán | `"Thai 28 tuần, ngôi đầu, phát triển bình thường"` |
| `icdCode` | string? | Mã ICD-10 | `"Z34.0"` |

---

### 8.7 `treatmentPlan` — Kế hoạch điều trị

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `medication` | string? | Đơn thuốc (text gộp) | `"Acid folic 400mg x 1 lần/ngày, Sắt 60mg"` |
| `nextSteps` | string? | Bước tiếp theo | `"Tái khám sau 2 tuần"` |
| `healthEducation` | bool? | Có tư vấn GDSK? | `true` |
| `healthEducationNote` | string? | Nội dung tư vấn | `"Tư vấn dinh dưỡng"` |

---

### 8.8 `prognosis` — Tiên lượng

| Type | Giá trị | Mô tả |
|------|---------|-------|
| string? | `"normal"` | Thai bình thường |
| | `"risky"` | Thai có nguy cơ |
| | `"cesarean_indicated"` | Chỉ định mổ |
| | `null` | Không ghi |

---

### 8.9 `nextAppointment` — Lần khám kế tiếp

| JSON key | Type | Mô tả | Ví dụ |
|----------|------|-------|-------|
| `date` | string? | Ngày hẹn (`yyyy-MM-dd`) | `"2026-03-24"` |
| `notes` | string? | Dặn dò | `"Mang theo kết quả XN máu"` |
| `examinerType` | string? | Người khám | `"obstetrician"` / `"midwife"` / `"pediatric_nurse"` / `"other"` |

---

### 8.10 VitalsJson — Tóm tắt cách FE đọc/gửi

**Khi NHẬN (GET review):**
```
response.data.vitals → VitalsJsonDto (full object)
```
FE đọc trực tiếp các field, render vào form để user xem/chỉnh sửa.

**Khi GỬI (POST confirm):**
```
{
  "vitals": { ... toàn bộ VitalsJsonDto đã chỉnh sửa ... }
}
```
FE gửi **toàn bộ object VitalsJsonDto** (kể cả fields user không sửa). BE lưu nguyên cả object vào `PrenatalVisit.VitalsJson`.

**⚠️ KHÔNG cần tách riêng vitalSigns khi gửi.** Gửi nguyên cả root `VitalsJsonDto`. BE xử lý toàn bộ.

---

## 9. LUỒNG TÍCH HỢP TỪNG BƯỚC

### Luồng A: Upload PRENATAL_CHECKUP (Phiếu khám thai — có OCR)

```
Bước 1  GET /api/ref/document-types?lang=vi
        → Cache danh sách, tìm ID cho "PRENATAL_CHECKUP"

Bước 2  POST /api/pregnancies/{pregnancyId}/documents
        → form-data: files + documentTypeId (PRENATAL_CHECKUP)
        → Lưu response.data.id (documentId)

Bước 3  POST /api/documents/{documentId}/ocr/process?lang=vi
        → Lưu response.data.id (ocrResultId)

Bước 4  POLL: GET /api/ocr/{ocrResultId}/status
        → Mỗi 3-5 giây
        → Dừng khi status = "Succeeded" hoặc "Failed"

Bước 5  GET /api/ocr/{ocrResultId}/review?lang=vi
        → Nhận VitalsJsonDto pre-filled
        → Render form cho user review

Bước 6  POST /api/ocr/{ocrResultId}/confirm
        → Gửi VitalsJsonDto (đã chỉnh sửa) + eventDate + location
        → Nhận AutoFillResultDto (createdVisitId)

Bước 7  (Optional) Navigate tới Visit detail:
        GET /api/prenatal-visits/{createdVisitId}
```

### Luồng B: Upload TEST TYPE (Xét nghiệm — không có OCR)

```
Bước 1  GET /api/ref/document-types?lang=vi
        → Tìm ID cho "BLOOD_TEST" / "ULTRASOUND" / etc.

Bước 2  POST /api/pregnancies/{pregnancyId}/documents
        → form-data: files + documentTypeId (BLOOD_TEST)
        → Lưu response.data.id (documentId)

Bước 3  ❌ KHÔNG gọi OCR (không trigger process)

Bước 4  Hiện ảnh + form thủ công cho user nhập:
        - Ngày xét nghiệm (eventDate)
        - Ghi chú (notes)
        - Kết quả bất thường? (boolean)

Bước 5  POST /api/ocr/{ocrResultId}/confirm
        → body: { documentTypeId, eventDate, vitals: null, notes }
        → Nhận AutoFillResultDto (createdTestIds)

        ⚠️ Lưu ý: Test types KHÔNG có ocrResultId từ OCR.
        FE cần gọi GET /api/documents/{documentId}/ocr
        để lấy ocrResultId (nếu BE tạo sẵn stub),
        hoặc dùng endpoint POST /api/documents/{documentId}/ocr/process
        để BE tạo OcrResult placeholder rồi confirm.
```

### Luồng C: Upload OTHERS (Đơn thuốc, tiêm chủng — chỉ archive)

```
Bước 1  POST /api/pregnancies/{pregnancyId}/documents
        → form-data: files + documentTypeId (PRESCRIPTION)

Bước 2  ✅ DONE — Không cần bước nào thêm.
        Document đã lưu, user xem trong danh sách.
```

### Tóm tắt nhanh

| Luồng | Upload | OCR | Poll | Review | Confirm | Kết quả |
|-------|:------:|:---:|:----:|:------:|:-------:|---------|
| A (Khám thai) | ✅ | ✅ | ✅ | ✅ | ✅ | PrenatalVisit + VitalsJson |
| B (Xét nghiệm) | ✅ | ❌ | ❌ | ❌ | ✅ | PrenatalTest |
| C (Lưu trữ) | ✅ | ❌ | ❌ | ❌ | ❌ | Chỉ archive |

---

## 10. RESPONSE FORMAT CHUẨN

Tất cả API đều trả về format thống nhất:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "data": { ... },
  "errors": null,
  "timestamp": "2026-03-10T10:30:00Z"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `success` | bool | `true` = thành công, `false` = lỗi |
| `message` | string | Thông báo |
| `statusCode` | int | HTTP status code |
| `data` | object? | Dữ liệu response (null khi lỗi hoặc delete) |
| `errors` | string[]? | Danh sách lỗi (khi validation fail) |
| `timestamp` | string | ISO 8601 timestamp |

---

## 11. ERROR HANDLING

### HTTP Status Codes

| Code | Ý nghĩa | Khi nào xảy ra |
|------|---------|----------------|
| 200 | OK | GET, PUT, PATCH, DELETE thành công |
| 201 | Created | POST tạo mới thành công |
| 202 | Accepted | OCR job đã được queue (non-blocking) |
| 400 | Bad Request | Validation fail (thiếu field, sai format) |
| 401 | Unauthorized | Token hết hạn hoặc không hợp lệ |
| 403 | Forbidden | Không có quyền (permission hoặc không phải owner) |
| 404 | Not Found | Document/OCR result không tồn tại |
| 409 | Conflict | Trùng lặp (VD: confirm lần 2) |

### Validation Error Response

```json
{
  "success": false,
  "message": "Validation failed",
  "statusCode": 400,
  "data": null,
  "errors": [
    "At least one file is required.",
    "File size exceeds 10MB limit.",
    "Unsupported file type. Allowed: image/jpeg, image/png, application/pdf"
  ]
}
```

### Ownership Error

```json
{
  "success": false,
  "message": "Access denied",
  "statusCode": 403,
  "data": null,
  "errors": null
}
```

---

## PHỤ LỤC: Document Type Code → Test Type Code Mapping

Khi confirm test types, BE tự động map:

| DocumentType Code | → RefTestType Code | Loại |
|-------------------|--------------------|------|
| `BLOOD_TEST` | `BLOOD_TEST` | LAB |
| `URINE_TEST` | `URINE_TEST` | LAB |
| `ULTRASOUND` | `ULTRASOUND` | IMAGING |
| `HIV_TEST` | `HIV_SCREEN` | LAB |
| `HEPATITIS_B_TEST` | `HEPATITIS_B` | LAB |
| `THYROID_TEST` | `TSH` | LAB |
| `GLUCOSE_TEST` | `OGTT` | LAB |
| `CBC_TEST` | `CBC_TEST` | LAB |
| `NT_SCAN` | `NT_SCAN` | IMAGING |

> FE không cần gửi `testTypeId` khi confirm — BE tự map từ `documentTypeId`.
