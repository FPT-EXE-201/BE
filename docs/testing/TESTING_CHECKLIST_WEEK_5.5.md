# 📋 TESTING CHECKLIST — WEEK 5.5: Auto-Fill (Review & Confirm AI Extraction)

> **Prerequisite**: Đã hoàn thành TESTING_CHECKLIST_WEEK_5. Có JWT Bearer Token, có pregnancy Active, có OcrResult với Status = `Succeeded`.
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## ⚠️ QUAN TRỌNG — Trước khi test

### Checklist chuẩn bị

| # | Chuẩn bị | Cách lấy |
|---|----------|----------|
| 1 | JWT Token | Login → lưu `{token}` |
| 2 | Pregnancy Active | `GET /api/pregnancies/active` → lưu `{pregnancyId}` |
| 3 | Document đã upload (PRENATAL_CHECKUP) | Week 4/5 upload → lưu `{documentId_prenatal}` |
| 4 | OcrResult Succeeded | Week 5 OCR+AI pipeline → lưu `{ocrResultId_succeeded}` |
| 5 | (Optional) Document ULTRASOUND | Upload → lưu `{documentId_ultrasound}` |
| 6 | (Optional) Document BLOOD_TEST | Upload → lưu `{documentId_blood}` |

### Database Seeds đã có (run migration + seeder)

| Permission | Assigned to |
|------------|-------------|
| `ocr.review` | USER, DOCTOR, ADMIN |
| `ocr.confirm` | USER, DOCTOR, ADMIN |

### OcrStatus Enum (Week 5.5 update)

| Value | Code |
|-------|------|
| 0 | Pending |
| 1 | OcrProcessing |
| 2 | OcrCompleted |
| 3 | AiExtracting |
| 4 | Succeeded |
| 5 | Failed |
| 6 | **Confirmed** ← NEW |

### RefDocumentType Codes (seed sẵn)

| Code | Guid |
|------|------|
| `PRENATAL_CHECKUP` | `b0000001-0000-0000-0000-000000000001` |
| `ULTRASOUND` | `b0000001-0000-0000-0000-000000000002` |
| `BLOOD_TEST` | `b0000001-0000-0000-0000-000000000003` |
| `URINE_TEST` | `b0000001-0000-0000-0000-000000000004` |
| `PRESCRIPTION` | `b0000001-0000-0000-0000-000000000005` |
| `VACCINATION_RECORD` | `b0000001-0000-0000-0000-000000000006` |
| `MEDICAL_REPORT` | `b0000001-0000-0000-0000-000000000007` |
| `OTHER` | `b0000001-0000-0000-0000-000000000008` |
| `HIV_TEST` | `b0000001-0000-0000-0000-000000000009` |
| `HEPATITIS_B_TEST` | `b0000001-0000-0000-0000-00000000000a` |
| `THYROID_TEST` | `b0000001-0000-0000-0000-00000000000b` |
| `GLUCOSE_TEST` | `b0000001-0000-0000-0000-00000000000c` |
| `CBC_TEST` | `b0000001-0000-0000-0000-00000000000d` |
| `NT_SCAN` | `b0000001-0000-0000-0000-00000000000e` |

---

## 0️⃣ PRE-TEST: Xác nhận hệ thống hoạt động

### ✅ TC-AF00: Application startup — Verify build
```
dotnet run --project src/FPT.EXE201.Api
```
**Kiểm tra**:
- [ ] Không có exception khi khởi động
- [ ] Build succeeded (0 warnings, 0 errors)

---

### ✅ TC-AF01: Enum OcrStatus đã có Confirmed
```
GET /api/ref/enums/ocrStatus
```
**Expected**: 200 OK — 7 giá trị (thêm Confirmed):
```json
{
  "success": true,
  "data": [
    { "name": "Pending", "value": 0 },
    { "name": "OcrProcessing", "value": 1 },
    { "name": "OcrCompleted", "value": 2 },
    { "name": "AiExtracting", "value": 3 },
    { "name": "Succeeded", "value": 4 },
    { "name": "Failed", "value": 5 },
    { "name": "Confirmed", "value": 6 }
  ]
}
```
**Verify**:
- [ ] `Confirmed` xuất hiện ở value = 6

---

### ✅ TC-AF02: Permissions đã seed — ocr.review + ocr.confirm
```
GET /api/roles
Authorization: Bearer {admin_token}
```
**Expected**: 200 OK
**Verify**:
- [ ] Role USER có permissions: `ocr.review`, `ocr.confirm`
- [ ] Role DOCTOR có permissions: `ocr.review`, `ocr.confirm`

---

### ✅ TC-AF03: Chuẩn bị — Có OcrResult status Succeeded
```
GET /api/ocr/{ocrResultId_succeeded}/status
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "id": "{ocrResultId_succeeded}",
    "status": "Succeeded",
    "structuredJson": "{ ... }",
    "confidenceScore": 85.00,
    ...
  }
}
```
**Verify**:
- [ ] `status` = `"Succeeded"`
- [ ] `structuredJson` không null
- [ ] `confirmedAt` = null (chưa confirm)

**Lưu lại**: `{ocrResultId_succeeded}` để dùng cho các test tiếp theo.

---

## 1️⃣ REVIEW — GET /api/ocr/{ocrResultId}/review

### ✅ TC-AF04: Review extraction — PRENATAL_CHECKUP (Happy path)
```
GET /api/ocr/{ocrResultId_succeeded}/review?lang=vi
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "ocrResultId": "{ocrResultId_succeeded}",
    "documentId": "{documentId_prenatal}",
    "pregnancyId": "{pregnancyId}",
    "documentTypeCode": "PRENATAL_CHECKUP",
    "documentTypeDisplayName": "Khám thai",
    "status": "Succeeded",
    "confidenceScore": 85.00,
    "vitals": {
      "generalInfo": { ... },
      "interview": { ... },
      "examination": {
        "vitalSigns": {
          "bloodPressureSystolic": 120,
          "bloodPressureDiastolic": 80,
          "weightKg": 65.5,
          ...
        },
        ...
      },
      ...
    },
      "overallConfidence": 0.7,
    "rawStructuredJson": "{ ... }",
    "canAutoFill": true,
    "cannotAutoFillReason": null
  }
}
```
**Verify**:
- [ ] `documentTypeCode` = `"PRENATAL_CHECKUP"`
- [ ] `vitals` object không null, chứa dữ liệu VitalsJsonDto
- [ ] `vitals.examination.vitalSigns` có dữ liệu (blood pressure, weight, etc.)
- [ ] `canAutoFill` = `true`
- [ ] `cannotAutoFillReason` = `null`
- [ ] `overallConfidence` là number 0.0-1.0
- [ ] `rawStructuredJson` không null

---

### ✅ TC-AF05: Review — Language parameter (EN)
```
GET /api/ocr/{ocrResultId_succeeded}/review?lang=en
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] `documentTypeDisplayName` hiển thị tiếng Anh (nếu có translation)
- [ ] Các trường khác giống TC-AF04

---

### ✅ TC-AF06: Review — OcrResult not found (404)
```
GET /api/ocr/00000000-0000-0000-0000-000000000000/review
Authorization: Bearer {token}
```
**Expected**: 404 Not Found
```json
{
  "success": false,
  "statusCode": 404,
  "message": "OCR result not found."
}
```
- [ ] Response 404

---

### ✅ TC-AF07: Review — Status chưa Succeeded (400)

> Cần 1 OcrResult có status `Pending` hoặc `Failed`. Nếu không có, tạo mới bằng cách upload document mới và KHÔNG chờ OCR hoàn tất.

```
GET /api/ocr/{ocrResultId_pending}/review
Authorization: Bearer {token}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Cannot review extraction when status is 'Pending'. Must be 'Succeeded'."
}
```
- [ ] Response 400
- [ ] Message chứa status hiện tại

---

### ✅ TC-AF08: Review — Document thuộc user khác (403)

> Login bằng tài khoản user khác (user B), thử review OcrResult thuộc user A.

```
GET /api/ocr/{ocrResultId_of_userA}/review
Authorization: Bearer {token_userB}
```
**Expected**: 403 Forbidden
```json
{
  "success": false,
  "statusCode": 403,
  "message": "You do not have access to this document."
}
```
- [ ] Response 403

---

### ✅ TC-AF09: Review — Không có permission (401/403)

> Login bằng user không có permission `ocr.review`.

```
GET /api/ocr/{ocrResultId}/review
Authorization: Bearer {token_no_permission}
```
**Expected**: 403 Forbidden
- [ ] Response 403

---

## 2️⃣ CONFIRM — POST /api/ocr/{ocrResultId}/confirm

### ✅ TC-AF10: Confirm PRENATAL_CHECKUP — Tạo Visit mới (Happy path)
```
POST /api/ocr/{ocrResultId_succeeded}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
**Body** (copy `vitals` từ TC-AF04 review response, có thể chỉnh sửa):
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10",
  "existingVisitId": null,
  "vitals": {
    "generalInfo": {
      "facility": "Bệnh viện Từ Dũ",
      "fullName": "Nguyễn Thị Test"
    },
    "examination": {
      "vitalSigns": {
        "bloodPressureSystolic": 120,
        "bloodPressureDiastolic": 80,
        "weightKg": 65.5,
        "heightCm": 160,
        "pulseBpm": 80,
        "temperatureCelsius": 36.5
      },
      "obstetric": {
        "fetalHeartRateBpm": 140,
        "fundusHeightCm": 28
      }
    },
    "diagnosis": {
      "text": "Thai phát triển bình thường"
    }
  },
  "location": "Bệnh viện Từ Dũ",
  "notes": "Khám thai tuần 28 - kết quả bình thường"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Created prenatal visit for 10/02/2026.",
  "data": {
    "ocrResultId": "{ocrResultId_succeeded}",
    "documentTypeCode": "PRENATAL_CHECKUP",
    "createdVisitId": "<new-guid>",
    "createdTestIds": [],
    "documentLinkedToVisit": true,
    "summary": "Created prenatal visit for 10/02/2026."
  }
}
```
**Verify**:
- [ ] Response 201
- [ ] `createdVisitId` không null → Visit mới đã tạo
- [ ] `documentLinkedToVisit` = `true`
- [ ] `createdTestIds` = `[]` (PRENATAL_CHECKUP không tạo test)
- [ ] `summary` chứa ngày

**Lưu lại**: `{createdVisitId}`.

---

### ✅ TC-AF11: Verify Visit đã tạo — VitalsJson populated
```
GET /api/pregnancies/{pregnancyId}/visits
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] Visit mới xuất hiện với `visitDate` = `"2026-02-10"`
- [ ] `visitType` = `"Routine"`
- [ ] `location` = `"Bệnh viện Từ Dũ"`
- [ ] `vitalsJson` chứa dữ liệu VitalsJsonDto (không null)
- [ ] `notes` chứa text đã gửi

---

### ✅ TC-AF12: Verify Document đã link Visit
```
GET /api/documents/{documentId_prenatal}
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] `visitId` = `{createdVisitId}` (đã link)

---

### ✅ TC-AF13: Verify OcrResult status = Confirmed
```
GET /api/ocr/{ocrResultId_succeeded}/status
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] `status` = `"Confirmed"`
- [ ] `confirmedAt` không null
- [ ] `confirmedBy` = user ID hiện tại
- [ ] `confirmedJson` không null (chứa dữ liệu user đã gửi)
- [ ] `autoFillResultJson` không null (chứa kết quả auto-fill)

---

### ✅ TC-AF14: Confirm lần 2 — Already confirmed (400)
```
POST /api/ocr/{ocrResultId_succeeded}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
**Body**: (giống TC-AF10)
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "This extraction has already been confirmed."
}
```
- [ ] Response 400
- [ ] Message: "This extraction has already been confirmed."

---

### ✅ TC-AF15: Review sau khi Confirm — Already confirmed (400)
```
GET /api/ocr/{ocrResultId_succeeded}/review
Authorization: Bearer {token}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "This extraction has already been confirmed."
}
```
- [ ] Response 400

---

### ✅ TC-AF16: Confirm với ExistingVisitId — Link vào Visit có sẵn

> **Chuẩn bị**: Cần 1 OcrResult `Succeeded` mới (upload + OCR lại document PRENATAL_CHECKUP khác).
> Cần 1 PrenatalVisit có sẵn (tạo thủ công hoặc từ TC-AF10).

```
POST /api/ocr/{ocrResultId_new}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-15",
  "existingVisitId": "{existing_visitId}",
  "vitals": {
    "examination": {
      "vitalSigns": {
        "bloodPressureSystolic": 118,
        "bloodPressureDiastolic": 76,
        "weightKg": 66.0
      }
    }
  },
  "location": "Phòng khám Dr. Nguyễn",
  "notes": "Cập nhật chỉ số mới"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `createdVisitId` = `{existing_visitId}` (link vào visit cũ, KHÔNG tạo mới)
- [ ] `documentLinkedToVisit` = `true`
- [ ] `summary` chứa "Updated prenatal visit"

**Verify Visit đã update**:
```
GET /api/pregnancies/{pregnancyId}/visits/{existing_visitId}
```
- [ ] `vitalsJson` đã được update với chỉ số mới
- [ ] `location` đã update

---

### ✅ TC-AF17: Confirm ExistingVisitId không thuộc pregnancy (400)
```
POST /api/ocr/{ocrResultId}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-15",
  "existingVisitId": "{visit_of_different_pregnancy}",
  "vitals": null,
  "location": null,
  "notes": null
}
```
**Expected**: 400 Bad Request — "Visit does not belong to this pregnancy."
- [ ] Response 400

---

## 3️⃣ TEST TYPE — Auto-create PrenatalTest

### ✅ TC-AF18: Confirm ULTRASOUND — Direct match → 1 Test

> **Chuẩn bị**: Upload document type ULTRASOUND, trigger OCR manually (`POST /api/documents/{id}/ocr/process`), chờ status = Succeeded.

```
POST /api/ocr/{ocrResultId_ultrasound}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000002",
  "eventDate": "2026-02-12",
  "existingVisitId": null,
  "vitals": null,
  "location": null,
  "notes": "Siêu âm tuần 28 - thai phát triển tốt"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "documentTypeCode": "ULTRASOUND",
    "createdVisitId": null,
    "createdTestIds": ["<new-guid>"],
    "documentLinkedToVisit": false,
    "summary": "Created ULTRASOUND test result for 12/02/2026."
  }
}
```
**Verify**:
- [ ] `createdTestIds` có 1 phần tử
- [ ] `createdVisitId` = null (ULTRASOUND không tạo visit)

**Verify Test đã tạo**:
```
GET /api/pregnancies/{pregnancyId}/tests
```
- [ ] Test mới xuất hiện với `testType.code` = `"ULTRASOUND"`
- [ ] `testDate` = `"2026-02-12"`
- [ ] `documentId` = `{documentId_ultrasound}` (linked)
- [ ] `notes` chứa text đã gửi
- [ ] `imageUrlsJson` chứa URLs từ document files (nếu có)

---

### ✅ TC-AF19: Confirm BLOOD_TEST — Direct Match

> **Chuẩn bị**: Upload ảnh xét nghiệm máu.

**Step 1: Confirm**
```
POST /api/ocr/{ocrResultId_blood}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000003",
  "eventDate": "2026-02-14",
  "existingVisitId": null,
  "vitals": null,
  "location": null,
  "notes": "Xét nghiệm máu tổng quát"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "documentTypeCode": "BLOOD_TEST",
    "createdVisitId": "<auto-created-visit-guid>",
    "createdTestIds": ["<guid>"],
    "documentLinkedToVisit": true,
    "summary": "Created BLOOD_TEST test result for 14/02/2026."
  }
}
```
**Verify**:
- [ ] `createdTestIds` có 1 phần tử (direct mapping → 1 test)
- [ ] `createdVisitId` không null (auto-created Routine visit)
- [ ] Document linked to visit

**Verify Tests**:
```
GET /api/pregnancies/{pregnancyId}/tests
```
- [ ] 1 test mới xuất hiện với `testType.code` = `"BLOOD_TEST"`
- [ ] Test có `documentId` = document gốc

---

### ✅ TC-AF20: Confirm URINE_TEST — Direct match → 1 Test
```
POST /api/ocr/{ocrResultId_urine}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000004",
  "eventDate": "2026-02-13",
  "existingVisitId": null,
  "vitals": null,
  "location": null,
  "notes": "Xét nghiệm nước tiểu - bình thường"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `createdTestIds` có 1 phần tử
- [ ] Test type code = `"URINE_TEST"` (direct mapping)

---

### ✅ TC-AF21: Confirm HIV_TEST — Direct match → HIV_SCREEN Test
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000009",
  "eventDate": "2026-02-13",
  "existingVisitId": null,
  "vitals": null,
  "notes": "HIV Âm tính"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] Test type code = `"HIV_SCREEN"` (not `"HIV_TEST"` — mapping khác)
- [ ] `documentId` linked

---

### ✅ TC-AF22: Confirm test type + ExistingVisitId — Test linked to Visit
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000002",
  "eventDate": "2026-02-15",
  "existingVisitId": "{existing_visitId}",
  "vitals": null,
  "notes": "Siêu âm trong buổi khám"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `documentLinkedToVisit` = `true`
- [ ] Test mới có `visitId` = `{existing_visitId}`
- [ ] Document `visitId` = `{existing_visitId}`

---

## 4️⃣ NOTES-ONLY — Defensive Fallback

### ✅ TC-AF23: Confirm PRESCRIPTION — Notes only (No entities created)

> **Chuẩn bị**: Upload document PRESCRIPTION, trigger OCR, chờ Succeeded.

```
POST /api/ocr/{ocrResultId_prescription}/confirm
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000005",
  "eventDate": "2026-02-10",
  "existingVisitId": null,
  "vitals": null,
  "location": null,
  "notes": "Thuốc bổ sung sắt + canxi"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "documentTypeCode": "PRESCRIPTION",
    "createdVisitId": null,
    "createdTestIds": [],
    "documentLinkedToVisit": false,
    "summary": "Document notes updated."
  }
}
```
**Verify**:
- [ ] `createdVisitId` = null (PRESCRIPTION không tạo visit)
- [ ] `createdTestIds` = `[]` (PRESCRIPTION không tạo test)
- [ ] `documentLinkedToVisit` = `false`

**Verify Document**:
```
GET /api/documents/{documentId_prescription}
```
- [ ] `notes` chứa "Thuốc bổ sung sắt + canxi"
- [ ] `documentDate` đã set (nếu trước đó chưa có)

---

### ✅ TC-AF24: Confirm VACCINATION_RECORD — Notes only
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000006",
  "eventDate": "2026-02-11",
  "notes": "Tiêm uốn ván lần 1"
}
```
**Expected**: 201 Created — `summary` = "Document notes updated."
- [ ] Không tạo Visit/Test

---

## 5️⃣ VALIDATION — FluentValidation

### ✅ TC-AF25: Validation — DocumentTypeId rỗng (400)
```json
{
  "documentTypeId": "00000000-0000-0000-0000-000000000000",
  "eventDate": "2026-02-10"
}
```
**Expected**: 400 Bad Request
- [ ] Validation error: "Document type is required."

---

### ✅ TC-AF26: Validation — EventDate trong tương lai (400)
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2099-12-31"
}
```
**Expected**: 400 Bad Request
- [ ] Validation error: "Event date cannot be in the future."

---

### ✅ TC-AF27: Validation — Location quá dài (400)
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10",
  "location": "<string 256 ký tự>"
}
```
**Expected**: 400 Bad Request
- [ ] Validation error: "Location must not exceed 255 characters."

---

### ✅ TC-AF28: Validation — Notes quá dài (400)
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10",
  "notes": "<string 2001 ký tự>"
}
```
**Expected**: 400 Bad Request
- [ ] Validation error: "Notes must not exceed 2000 characters."

---

---

## 6️⃣ ERROR HANDLING — Business Logic

### ✅ TC-AF30: Confirm — OcrResult not found (404)
```
POST /api/ocr/00000000-0000-0000-0000-000000000000/confirm
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10"
}
```
**Expected**: 404 Not Found — "OCR result not found."
- [ ] Response 404

---

### ✅ TC-AF31: Confirm — Status chưa Succeeded (400)
```
POST /api/ocr/{ocrResultId_pending}/confirm
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10"
}
```
**Expected**: 400 Bad Request — "Can only confirm when status is 'Succeeded'. Current: 'Pending'."
- [ ] Response 400

---

### ✅ TC-AF32: Confirm — Document type not found (404)
```json
{
  "documentTypeId": "ffffffff-ffff-ffff-ffff-ffffffffffff",
  "eventDate": "2026-02-10"
}
```
**Expected**: 404 Not Found — "Document type not found."
- [ ] Response 404

---

### ✅ TC-AF33: Confirm — Document thuộc user khác (403)
```
POST /api/ocr/{ocrResultId_of_userA}/confirm
Authorization: Bearer {token_userB}
```
**Expected**: 403 Forbidden — "You do not have access to this document."
- [ ] Response 403

---

### ✅ TC-AF34: Confirm — Visit not found (404)
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10",
  "existingVisitId": "ffffffff-ffff-ffff-ffff-ffffffffffff"
}
```
**Expected**: 404 Not Found — "Visit not found."
- [ ] Response 404

---

---

## 7️⃣ DOCUMENT TYPE MAPPING — Direct Match Tests

### ✅ TC-AF37: Mapping table verification

| Document Type Code | Expected Test Type Code | Test |
|-------------------|------------------------|------|
| BLOOD_TEST | BLOOD_TEST | TC-AF19 |
| ULTRASOUND | ULTRASOUND | TC-AF18 |
| URINE_TEST | URINE_TEST | TC-AF20 |
| HIV_TEST | HIV_SCREEN | TC-AF21 |
| HEPATITIS_B_TEST | HEPATITIS_B | Confirm + verify |
| THYROID_TEST | TSH | Confirm + verify |
| GLUCOSE_TEST | OGTT | Confirm + verify |
| CBC_TEST | CBC_TEST | Confirm + verify |
| NT_SCAN | NT_SCAN | Confirm + verify |
| PRENATAL_CHECKUP | (tạo Visit) | TC-AF10 |
| PRESCRIPTION | (notes only) | TC-AF23 |
| VACCINATION_RECORD | (notes only) | TC-AF24 |
| MEDICAL_REPORT | (notes only) | Confirm + verify |
| OTHER | (notes only) | Confirm + verify |

**Verify mỗi dòng**: Confirm với documentTypeId tương ứng → test type code đúng.

---

## 9️⃣ EDGE CASES

### ✅ TC-AF38: Confirm PRENATAL_CHECKUP không có Vitals — Visit VitalsJson = null
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000001",
  "eventDate": "2026-02-10",
  "vitals": null,
  "notes": "Khám thai không có chỉ số"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] Visit được tạo với `vitalsJson` = null
- [ ] `notes` chứa text

---

### ✅ TC-AF39: Confirm BLOOD_TEST — Direct mapping creates 1 test + auto-create visit
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000003",
  "eventDate": "2026-02-10",
  "notes": "Xét nghiệm máu tổng quát"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `createdTestIds` có 1 phần tử
- [ ] `createdVisitId` không null (auto-created Routine visit vì không có `existingVisitId`)
- [ ] Test có `testType.code` = `"BLOOD_TEST"` (direct mapping)
- [ ] `summary` chứa "Created BLOOD_TEST test result"

---

### ✅ TC-AF40: Confirm — DocumentTypeId khác document ban đầu → Update document's DocumentTypeId
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000003",
  "eventDate": "2026-02-10"
}
```
> Ví dụ: Document ban đầu upload là PRENATAL_CHECKUP, nhưng user confirm lại là BLOOD_TEST.

**Expected**: 201 Created
**Verify**:
```
GET /api/documents/{documentId}
```
- [ ] `documentTypeId` đã thay đổi thành `b0000001-0000-0000-0000-000000000003`

---

### ✅ TC-AF41: OcrResult ConfirmedJson chứa data user đã gửi
```
GET /api/ocr/{confirmedOcrResultId}/status
```
**Parse `confirmedJson`** (JSON string):
- [ ] Chứa `documentTypeId` user đã chọn
- [ ] Chứa `eventDate` user đã chọn
- [ ] Chứa `vitals` (nếu gửi)

---

### ✅ TC-AF42: OcrResult AutoFillResultJson chứa kết quả tạo entities
```
GET /api/ocr/{confirmedOcrResultId}/status
```
**Parse `autoFillResultJson`** (JSON string):
- [ ] Chứa `createdVisitId` (nếu tạo visit)
- [ ] Chứa `createdTestIds` (nếu tạo tests)
- [ ] Chứa `summary`

---

## 🔟 FULL END-TO-END FLOW

### ✅ TC-AF43: E2E — Upload → OCR → Review → Confirm → Verify

**Step 1: Upload document**
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
files: [ảnh phiếu khám thai]
documentTypeId: b0000001-0000-0000-0000-000000000001
title: E2E Test Week 5.5
```
- [ ] 201 Created → lưu `{documentId}`, `{ocrResultId}` (auto-queued)

**Step 2: Poll OCR status → chờ Succeeded**
```
GET /api/ocr/{ocrResultId}/status
```
- [ ] Poll mỗi 3-5 giây
- [ ] Status chuyển: Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded
- [ ] `structuredJson` không null khi Succeeded

**Step 3: Review extracted data**
```
GET /api/ocr/{ocrResultId}/review?lang=vi
```
- [ ] 200 OK
- [ ] `vitals` chứa dữ liệu VitalsJsonDto
- [ ] `canAutoFill` = `true`

**Step 4: Confirm (edit nếu cần)**
```
POST /api/ocr/{ocrResultId}/confirm
Body: { ... vitals từ step 3, chỉnh sửa nếu cần ... }
```
- [ ] 201 Created
- [ ] `createdVisitId` có giá trị

**Step 5: Verify entities**
```
GET /api/pregnancies/{pregnancyId}/visits
```
- [ ] Visit mới xuất hiện, `vitalsJson` populated

```
GET /api/documents/{documentId}
```
- [ ] `visitId` linked

```
GET /api/ocr/{ocrResultId}/status
```
- [ ] `status` = `"Confirmed"`
- [ ] `confirmedAt` not null

---

## 📊 SUMMARY CHECKLIST

| # | Test Case | Status |
|---|-----------|--------|
| TC-AF00 | App startup | ⬜ |
| TC-AF01 | Enum OcrStatus + Confirmed | ⬜ |
| TC-AF02 | Permissions seeded | ⬜ |
| TC-AF03 | OcrResult Succeeded ready | ⬜ |
| TC-AF04 | Review PRENATAL_CHECKUP (happy) | ⬜ |
| TC-AF05 | Review lang=en | ⬜ |
| TC-AF06 | Review not found (404) | ⬜ |
| TC-AF07 | Review status not Succeeded (400) | ⬜ |
| TC-AF08 | Review wrong user (403) | ⬜ |
| TC-AF09 | Review no permission (403) | ⬜ |
| TC-AF10 | Confirm PRENATAL_CHECKUP → Visit (happy) | ⬜ |
| TC-AF11 | Verify Visit VitalsJson | ⬜ |
| TC-AF12 | Verify Document linked | ⬜ |
| TC-AF13 | Verify OcrResult Confirmed | ⬜ |
| TC-AF14 | Confirm lần 2 (400) | ⬜ |
| TC-AF15 | Review after confirm (400) | ⬜ |
| TC-AF16 | Confirm ExistingVisitId | ⬜ |
| TC-AF17 | ExistingVisitId wrong pregnancy (400) | ⬜ |
| TC-AF18 | Confirm ULTRASOUND → Test | ⬜ |
| TC-AF19 | Confirm BLOOD_TEST → Multiple Tests | ⬜ |
| TC-AF20 | Confirm URINE_TEST → Test | ⬜ |
| TC-AF21 | Confirm HIV_TEST → HIV_SCREEN | ⬜ |
| TC-AF22 | Test + ExistingVisitId | ⬜ |
| TC-AF23 | Confirm PRESCRIPTION → Notes | ⬜ |
| TC-AF24 | Confirm VACCINATION → Notes | ⬜ |
| TC-AF25 | Validation: empty DocumentTypeId | ⬜ |
| TC-AF26 | Validation: future EventDate | ⬜ |
| TC-AF27 | Validation: Location too long | ⬜ |
| TC-AF28 | Validation: Notes too long | ⬜ |
| TC-AF29 | *(Removed — labResults no longer used)* | ✅ |
| TC-AF30 | Error: OcrResult not found | ⬜ |
| TC-AF31 | Error: Status not Succeeded | ⬜ |
| TC-AF32 | Error: DocumentType not found | ⬜ |
| TC-AF33 | Error: Wrong user | ⬜ |
| TC-AF34 | Error: Visit not found | ⬜ |
| TC-AF35 | *(Removed — labResults no longer used)* | ✅ |
| TC-AF36 | *(Removed — fuzzy match no longer used)* | ✅ |
| TC-AF37 | Document type mapping table | ⬜ |
| TC-AF38 | Edge: Confirm no vitals | ⬜ |
| TC-AF39 | Edge: BLOOD_TEST direct mapping | ⬜ |
| TC-AF40 | Edge: Change DocumentTypeId | ⬜ |
| TC-AF41 | Edge: ConfirmedJson content | ⬜ |
| TC-AF42 | Edge: AutoFillResultJson content | ⬜ |
| TC-AF43 | E2E full flow | ⬜ |

**Total**: 44 test cases
