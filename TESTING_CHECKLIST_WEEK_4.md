# 📋 TESTING CHECKLIST — WEEK 4: Medical Documents (Hồ sơ y tế)

> **Prerequisite**: Đã đăng ký tài khoản, đăng nhập, có JWT Bearer Token. Đã có ít nhất 1 pregnancy (Active).
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## 0️⃣ PRE-TEST: Lấy Reference Data + Chuẩn bị

### 0.1 Lấy danh mục loại tài liệu (Public — Không cần Auth)
```
GET /api/ref/document-types?lang=vi
```
**Expected**: 200 OK — Danh sách 8 loại tài liệu y tế với tên tiếng Việt.
```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    { "id": "b0000001-0000-0000-0000-000000000001", "code": "PRENATAL_CHECKUP", "displayName": "Khám thai", "description": "Phiếu khám thai định kỳ" },
    { "id": "b0000001-0000-0000-0000-000000000002", "code": "ULTRASOUND", "displayName": "Siêu âm", "description": "Kết quả siêu âm thai" },
    ...
  ]
}
```
**Lưu lại**: Ghi nhớ các `id` trả về để dùng cho test tạo document.

### 0.2 Lấy danh mục loại tài liệu bằng tiếng Anh
```
GET /api/ref/document-types?lang=en
```
**Expected**: 200 OK — Tên tiếng Anh (Prenatal Checkup, Ultrasound, Blood Test, ...).

### 0.3 Lấy enum DocumentSource
```
GET /api/ref/enums/documentSource
```
**Expected**: 200 OK — Array: Upload(0), Share(1), Import(2).

### 0.4 Lấy enum OcrStatus
```
GET /api/ref/enums/ocrStatus
```
**Expected**: 200 OK — Array: Pending(0), Processing(1), Succeeded(2), Failed(3).

### 0.5 Chuẩn bị: Đảm bảo đã có 1 pregnancy Active
```
GET /api/pregnancies/active
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có pregnancy đang Active.
**Nếu chưa có**: Tạo mới bằng `POST /api/pregnancies` (xem TESTING_CHECKLIST_WEEK_3).
**Lưu lại**: `{pregnancyId}` để dùng cho test.

### 0.6 Chuẩn bị: Đảm bảo đã có ít nhất 1 visit
```
GET /api/pregnancies/{pregnancyId}/visits
Authorization: Bearer {token}
```
**Nếu chưa có**: Tạo 1 visit (xem TESTING_CHECKLIST_WEEK_3).
**Lưu lại**: `{visitId}` để dùng cho test link document vào visit.

---

## Seed IDs (Reference)
| Resource | Code | Seed ID |
|----------|------|---------|
| Document Type | PRENATAL_CHECKUP | `b0000001-0000-0000-0000-000000000001` |
| Document Type | ULTRASOUND | `b0000001-0000-0000-0000-000000000002` |
| Document Type | BLOOD_TEST | `b0000001-0000-0000-0000-000000000003` |
| Document Type | URINE_TEST | `b0000001-0000-0000-0000-000000000004` |
| Document Type | PRESCRIPTION | `b0000001-0000-0000-0000-000000000005` |
| Document Type | VACCINATION_RECORD | `b0000001-0000-0000-0000-000000000006` |
| Document Type | MEDICAL_REPORT | `b0000001-0000-0000-0000-000000000007` |
| Document Type | OTHER | `b0000001-0000-0000-0000-000000000008` |

---

## 1️⃣ MEDICAL DOCUMENTS — Upload + CRUD

> **⚠️ LƯU Ý QUAN TRỌNG**:
> - Upload dùng `multipart/form-data`, KHÔNG phải JSON.
> - File gửi qua field `files` (type: File[], có thể chọn nhiều file), metadata qua form fields.
> - File upload hiện dùng `StubFileStorageService` → file KHÔNG được upload thật, chỉ tạo placeholder URL.
> - Khi upload ảnh/PDF, hệ thống tự động tạo OCR result (status = Pending).
> - Trong Postman: Body → form-data → thêm field `files` (type: File, có thể chọn nhiều).

---

### ✅ TC-D01: Upload tài liệu — Happy Path (chỉ file, không metadata)
```
POST /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều file ảnh (`.jpg`, `.png`) từ máy tính |

**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Document created successfully.",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "visitId": null,
    "documentTypeId": null,
    "documentTypeDisplayName": null,
    "files": [
      {
        "id": "<guid>",
        "storageFileId": "<guid>",
        "originalFileName": "photo.jpg",
        "mimeType": "image/jpeg",
        "fileSizeBytes": 123456,
        "fileUrl": "https://placeholder.storage/uploads/2026/02/15/<guid>.jpg",
        "sortOrder": 1,
        "pageLabel": null
      }
    ],
    "totalFileSizeBytes": 123456,
    "title": null,
    "documentDate": null,
    "capturedAt": "2026-02-15T...",
    "source": "Upload",
    "notes": null,
    "isFavorite": false,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Lưu lại**: Copy `id` → gọi là `{documentId}`.

> **⚠️ Stub**: `files[].fileUrl` là placeholder URL, không mở được. Đúng behavior cho giai đoạn chưa có Supabase.

---

### ✅ TC-D02: Upload tài liệu — Full metadata
```
POST /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều file ảnh siêu âm |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000002` (ULTRASOUND) |
| `title` | Text | `Siêu âm tuần 20` |
| `documentDate` | Text | `2026-02-10` |
| `notes` | Text | `Siêu âm 4D, bé phát triển tốt` |

**Expected**: 201 Created
```json
{
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "documentTypeId": "b0000001-0000-0000-0000-000000000002",
    "documentTypeDisplayName": "Siêu âm",
    "title": "Siêu âm tuần 20",
    "documentDate": "2026-02-10",
    "source": "Upload",
    "notes": "Siêu âm 4D, bé phát triển tốt",
    "isFavorite": false,
    "files": [
      {
        "originalFileName": "...",
        "mimeType": "image/...",
        "fileUrl": "https://placeholder.storage/..."
      }
    ],
    "totalFileSizeBytes": 123456
  }
}
```
**Lưu lại**: `{documentId2}` — document thứ 2.

---

### ✅ TC-D03: Upload tài liệu — PDF (đơn thuốc)
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều file PDF bất kỳ |
| `documentTypeId` | Text | `b0000001-0000-0000-0000-000000000005` (PRESCRIPTION) |
| `title` | Text | `Đơn thuốc tháng 2` |
| `documentDate` | Text | `2026-02-01` |
| `notes` | Text | `Bổ sung sắt + acid folic` |

**Expected**: 201 Created — `mimeType: "application/pdf"`, `documentTypeDisplayName: "Đơn thuốc"`.
**Lưu lại**: `{documentId3}` — document thứ 3.

---

### ✅ TC-D04: Upload tài liệu — Không chọn documentTypeId
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | Chọn 1 hoặc nhiều file ảnh |
| `title` | Text | `Tài liệu chưa phân loại` |

**Expected**: 201 Created — `documentTypeId: null`, `documentTypeDisplayName: null`.

---

### ❌ TC-D05: Upload — Không gửi file
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
(Không thêm field `files`, chỉ gửi metadata)

**Expected**: 400 Bad Request — File is required.

---

### ❌ TC-D06: Upload — pregnancyId không tồn tại
```
POST /api/pregnancies/00000000-0000-0000-0000-000000000000/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file ảnh |

**Expected**: 404 Not Found — Pregnancy not found.

---

### ❌ TC-D07: Upload — Pregnancy thuộc user khác
> Đăng nhập tài khoản khác, dùng `{pregnancyId}` của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

### ❌ TC-D08: Upload — documentTypeId không tồn tại
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file ảnh |
| `documentTypeId` | Text | `00000000-0000-0000-0000-000000000000` |

**Expected**: 404 Not Found — Document type not found (hoặc bỏ qua nếu service không validate).

---

### ❌ TC-D09: Upload — Title quá dài (>200 ký tự)
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file ảnh |
| `title` | Text | (chuỗi 201+ ký tự) |

**Expected**: 400 Bad Request — Title must not exceed 200 characters.

---

### ❌ TC-D10: Upload — documentDate trong tương lai
```
POST /api/pregnancies/{pregnancyId}/documents
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `files` | File[] | 1 file ảnh |
| `documentDate` | Text | `2027-12-31` |

**Expected**: 400 Bad Request — Document date must not be in the future.

---

## 2️⃣ MEDICAL DOCUMENTS — Read (List + Detail)

### ✅ TC-D11: Danh sách tài liệu của thai kỳ
```
GET /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
```
**Expected**: 200 OK — Array chứa tất cả documents đã upload (TC-D01 → D04), mỗi item có đầy đủ fields.
```json
{
  "data": [
    {
      "id": "<guid>",
      "pregnancyId": "{pregnancyId}",
      "documentTypeDisplayName": "Siêu âm",
      "files": [
        { "originalFileName": "...", "mimeType": "image/jpeg", "fileUrl": "..." }
      ],
      "title": "Siêu âm tuần 20",
      "source": "Upload",
      "isFavorite": false,
      ...
    },
    ...
  ]
}
```

---

### ✅ TC-D12: Chi tiết 1 tài liệu
```
GET /api/documents/{documentId}
Authorization: Bearer {token}
```
**Expected**: 200 OK — Full detail của document, bao gồm files[] với fileUrl, timestamps.
```json
{
  "data": {
    "id": "{documentId}",
    "pregnancyId": "{pregnancyId}",
    "visitId": null,
    "documentTypeId": "b0000001-0000-0000-0000-000000000002",
    "documentTypeDisplayName": "Siêu âm",
    "files": [
      {
        "id": "<guid>",
        "storageFileId": "<guid>",
        "originalFileName": "ultrasound.jpg",
        "mimeType": "image/jpeg",
        "fileSizeBytes": 234567,
        "fileUrl": "https://placeholder.storage/...",
        "sortOrder": 1,
        "pageLabel": null
      }
    ],
    "totalFileSizeBytes": 234567,
    "title": "Siêu âm tuần 20",
    "documentDate": "2026-02-10",
    "capturedAt": "...",
    "source": "Upload",
    "notes": "Siêu âm 4D, bé phát triển tốt",
    "isFavorite": false,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```

---

### ❌ TC-D13: Chi tiết — Document ID không tồn tại
```
GET /api/documents/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-D14: Chi tiết — Document thuộc user khác
> Đăng nhập tài khoản khác, dùng `{documentId}` của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

## 3️⃣ MEDICAL DOCUMENTS — Update

### ✅ TC-D15: Cập nhật metadata — Title + Notes + DocumentDate
```
PUT /api/documents/{documentId}
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "title": "Siêu âm tuần 20 (cập nhật)",
  "documentDate": "2026-02-11",
  "notes": "Cập nhật: siêu âm 4D, bé nặng 350g"
}
```
**Expected**: 200 OK — Title, documentDate, notes đã thay đổi.
```json
{
  "message": "Document updated successfully.",
  "data": {
    "title": "Siêu âm tuần 20 (cập nhật)",
    "documentDate": "2026-02-11",
    "notes": "Cập nhật: siêu âm 4D, bé nặng 350g"
  }
}
```

---

### ✅ TC-D16: Cập nhật — Đổi documentTypeId
```
PUT /api/documents/{documentId}
Content-Type: application/json
```
```json
{
  "documentTypeId": "b0000001-0000-0000-0000-000000000007",
  "title": "Báo cáo siêu âm tổng hợp"
}
```
**Expected**: 200 OK — `documentTypeId` đổi sang MEDICAL_REPORT, `documentTypeDisplayName: "Báo cáo y tế"`.

---

### ✅ TC-D17: Cập nhật — Link document vào visit
```
PUT /api/documents/{documentId}
Content-Type: application/json
```
```json
{
  "visitId": "{visitId}"
}
```
**Expected**: 200 OK — `visitId` có giá trị, document được liên kết với buổi khám.

---

### ❌ TC-D18: Cập nhật — VisitId thuộc pregnancy khác
> Tạo 1 pregnancy khác, tạo visit trong đó, rồi dùng visitId đó để link vào document hiện tại.

**Expected**: 400 Bad Request — `"Visit does not belong to this pregnancy"` hoặc tương tự.

---

### ❌ TC-D19: Cập nhật — Title quá dài (>200 ký tự)
```json
{
  "title": "aaaaa... (201 ký tự)"
}
```
**Expected**: 400 Bad Request — Title must not exceed 200 characters.

---

### ❌ TC-D20: Cập nhật — documentDate trong tương lai
```json
{
  "documentDate": "2027-12-31"
}
```
**Expected**: 400 Bad Request — Document date must not be in the future.

---

## 4️⃣ TOGGLE FAVORITE

### ✅ TC-D21: Toggle Favorite — Lần 1 (false → true)
```
PATCH /api/documents/{documentId}/favorite
Authorization: Bearer {token}
```
**Expected**: 200 OK — `isFavorite: true`.
```json
{
  "message": "Favorite status updated.",
  "data": {
    "id": "{documentId}",
    "isFavorite": true,
    ...
  }
}
```

---

### ✅ TC-D22: Toggle Favorite — Lần 2 (true → false)
```
PATCH /api/documents/{documentId}/favorite
Authorization: Bearer {token}
```
**Expected**: 200 OK — `isFavorite: false` (toggle ngược lại).

---

### ✅ TC-D23: Toggle Favorite — Verify bằng GET
```
GET /api/documents/{documentId}
Authorization: Bearer {token}
```
**Expected**: 200 OK — `isFavorite` đúng với trạng thái cuối cùng đã toggle.

---

### ❌ TC-D24: Toggle Favorite — Document không tồn tại
```
PATCH /api/documents/00000000-0000-0000-0000-000000000000/favorite
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

## 5️⃣ OCR — Rerun & Status

> **⚠️ LƯU Ý**: OCR hiện là **stub** — chỉ tạo record OcrResult với trạng thái Pending.
> Không có OCR engine thật chạy. Đúng behavior cho giai đoạn chưa integrate Azure Document Intelligence.

### ✅ TC-O01: Rerun OCR
```
POST /api/documents/{documentId}/ocr/rerun
Authorization: Bearer {token}
```
**Expected**: 201 Created
```json
{
  "message": "OCR has been queued for processing.",
  "data": {
    "ocrResultId": "<guid>"
  }
}
```
**Lưu lại**: `{ocrResultId}`.

---

### ✅ TC-O02: Kiểm tra trạng thái OCR
```
GET /api/ocr/{ocrResultId}/status
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "data": {
    "id": "{ocrResultId}",
    "documentId": "{documentId}",
    "ocrRunNumber": 2,
    "status": "Pending",
    "ocrEngine": null,
    "rawText": null,
    "structuredJson": null,
    "confidenceScore": null,
    "errorMessage": null,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```

> **Giải thích `ocrRunNumber`**: Lần rerun thứ 2 (lần 1 được tạo tự động khi upload document).

---

### ✅ TC-O03: Rerun OCR lần nữa → ocrRunNumber tăng
```
POST /api/documents/{documentId}/ocr/rerun
Authorization: Bearer {token}
```
**Expected**: 201 Created — `ocrRunNumber: 3`.

---

### ❌ TC-O04: Rerun OCR — Document không tồn tại
```
POST /api/documents/00000000-0000-0000-0000-000000000000/ocr/rerun
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-O05: Get OCR status — OcrResult ID không tồn tại
```
GET /api/ocr/00000000-0000-0000-0000-000000000000/status
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

## 6️⃣ TIMELINE

### ✅ TC-TL01: Lấy timeline của thai kỳ
```
GET /api/pregnancies/{pregnancyId}/timeline
Authorization: Bearer {token}
```
**Expected**: 200 OK — Array các events, sắp xếp theo ngày giảm dần. Bao gồm cả Documents lẫn Visits.
```json
{
  "data": [
    {
      "eventType": "Document",
      "eventId": "<document-guid>",
      "eventDate": "2026-02-15T...",
      "title": "Siêu âm tuần 20 (cập nhật)",
      "description": "Siêu âm"
    },
    {
      "eventType": "Visit",
      "eventId": "<visit-guid>",
      "eventDate": "2026-01-10T09:00:00",
      "title": "Routine",
      "description": "Bệnh viện Từ Dũ"
    },
    ...
  ]
}
```

---

### ✅ TC-TL02: Timeline — Pregnancy không có documents + visits
> Tạo 1 pregnancy mới, không tạo gì thêm.
```
GET /api/pregnancies/{newPregnancyId}/timeline
Authorization: Bearer {token}
```
**Expected**: 200 OK — Array rỗng `[]`.

---

### ❌ TC-TL03: Timeline — Pregnancy không tồn tại
```
GET /api/pregnancies/00000000-0000-0000-0000-000000000000/timeline
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-TL04: Timeline — Pregnancy thuộc user khác
> Đăng nhập tài khoản khác, dùng `{pregnancyId}` của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

## 7️⃣ DELETE DOCUMENT

### ✅ TC-D25: Soft delete document
```
DELETE /api/documents/{documentId3}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "message": "Document deleted successfully.",
  "data": null
}
```

---

### ✅ TC-D26: Verify sau delete — GET by ID
```
GET /api/documents/{documentId3}
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — Document đã bị soft delete, không trả về nữa.

---

### ✅ TC-D27: Verify sau delete — List documents
```
GET /api/pregnancies/{pregnancyId}/documents
Authorization: Bearer {token}
```
**Expected**: 200 OK — Document đã xóa không xuất hiện trong danh sách.

---

### ❌ TC-D28: Delete — Document không tồn tại
```
DELETE /api/documents/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found.

---

### ❌ TC-D29: Delete — Document thuộc user khác
> Đăng nhập tài khoản khác, thử xóa document của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found.

---

## 8️⃣ CROSS-FEATURE TESTS

### ✅ TC-X01: Ownership Isolation — User khác không xem được
> Đăng nhập bằng tài khoản khác, thử:
> - `GET /api/pregnancies/{pregnancyId}/documents`
> - `GET /api/documents/{documentId}`
> - `PATCH /api/documents/{documentId}/favorite`

**Expected**: 403 Forbidden hoặc 404 Not Found cho tất cả.

---

### ✅ TC-X02: Permission — Không có quyền document.create
> Nếu user không có role với permission `document.create`, thử `POST` upload document.

**Expected**: 403 Forbidden.

---

### ✅ TC-X03: Permission — Không có quyền ocr.trigger
> Nếu user không có permission `ocr.trigger`, thử `POST /api/documents/{documentId}/ocr/rerun`.

**Expected**: 403 Forbidden.

---

### ✅ TC-X04: Document liên kết Visit — Verify từ Visit Detail
> Sau khi link document vào visit (TC-D17), kiểm tra:
```
GET /api/visits/{visitId}?lang=vi
Authorization: Bearer {token}
```
**Expected**: 200 OK — Visit detail hiển thị thông tin liên kết (nếu service trả về documents trong visit detail).

---

### ✅ TC-X05: Timeline phản ánh đúng sau delete
> Sau khi delete 1 document (TC-D25), kiểm tra timeline:
```
GET /api/pregnancies/{pregnancyId}/timeline
Authorization: Bearer {token}
```
**Expected**: 200 OK — Document đã xóa KHÔNG xuất hiện trong timeline.

---

### ✅ TC-X06: Upload nhiều documents cho cùng 1 pregnancy
> Upload 3-5 documents liên tiếp, kiểm tra list:
```
GET /api/pregnancies/{pregnancyId}/documents
```
**Expected**: 200 OK — Tất cả documents đều xuất hiện, mỗi document có `files[]` riêng biệt.

---

### ✅ TC-X07: Không cần Auth cho Ref Document Types
> Gọi KHÔNG kèm Authorization header:
```
GET /api/ref/document-types?lang=vi
```
**Expected**: 200 OK — Public endpoint, trả về danh sách bình thường.

---

## 📊 CHECKLIST SUMMARY

| # | Test Case | Type | Result |
|---|-----------|------|--------|
| **Ref Data & Chuẩn bị** | | | |
| 0.1 | GET /ref/document-types?lang=vi | ✅ | ☐ |
| 0.2 | GET /ref/document-types?lang=en | ✅ | ☐ |
| 0.3 | GET /ref/enums/documentSource | ✅ | ☐ |
| 0.4 | GET /ref/enums/ocrStatus | ✅ | ☐ |
| 0.5 | Đảm bảo có pregnancy Active | ✅ | ☐ |
| 0.6 | Đảm bảo có visit | ✅ | ☐ |
| **Upload Document** | | | |
| D01 | Upload — Chỉ file, không metadata | ✅ | ☐ |
| D02 | Upload — Full metadata (ULTRASOUND) | ✅ | ☐ |
| D03 | Upload — PDF (PRESCRIPTION) | ✅ | ☐ |
| D04 | Upload — Không chọn documentTypeId | ✅ | ☐ |
| D05 | Upload — Không gửi file | ❌ 400 | ☐ |
| D06 | Upload — Pregnancy không tồn tại | ❌ 404 | ☐ |
| D07 | Upload — Pregnancy user khác | ❌ 403/404 | ☐ |
| D08 | Upload — documentTypeId không tồn tại | ❌ 404 | ☐ |
| D09 | Upload — Title >200 ký tự | ❌ 400 | ☐ |
| D10 | Upload — documentDate tương lai | ❌ 400 | ☐ |
| **Read Document** | | | |
| D11 | List documents của pregnancy | ✅ | ☐ |
| D12 | Chi tiết 1 document | ✅ | ☐ |
| D13 | Chi tiết — ID không tồn tại | ❌ 404 | ☐ |
| D14 | Chi tiết — Document user khác | ❌ 403/404 | ☐ |
| **Update Document** | | | |
| D15 | Update — Title + Notes + Date | ✅ | ☐ |
| D16 | Update — Đổi documentTypeId | ✅ | ☐ |
| D17 | Update — Link vào visit | ✅ | ☐ |
| D18 | Update — VisitId pregnancy khác | ❌ 400 | ☐ |
| D19 | Update — Title >200 ký tự | ❌ 400 | ☐ |
| D20 | Update — documentDate tương lai | ❌ 400 | ☐ |
| **Toggle Favorite** | | | |
| D21 | Toggle — false → true | ✅ | ☐ |
| D22 | Toggle — true → false | ✅ | ☐ |
| D23 | Toggle — Verify bằng GET | ✅ | ☐ |
| D24 | Toggle — Document không tồn tại | ❌ 404 | ☐ |
| **OCR** | | | |
| O01 | Rerun OCR | ✅ | ☐ |
| O02 | Get OCR status | ✅ | ☐ |
| O03 | Rerun OCR nữa — runNumber tăng | ✅ | ☐ |
| O04 | Rerun — Document không tồn tại | ❌ 404 | ☐ |
| O05 | Get OCR — ID không tồn tại | ❌ 404 | ☐ |
| **Timeline** | | | |
| TL01 | Timeline đầy đủ | ✅ | ☐ |
| TL02 | Timeline rỗng | ✅ | ☐ |
| TL03 | Timeline — Pregnancy không tồn tại | ❌ 404 | ☐ |
| TL04 | Timeline — Pregnancy user khác | ❌ 403/404 | ☐ |
| **Delete Document** | | | |
| D25 | Soft delete | ✅ | ☐ |
| D26 | Verify sau delete — GET by ID | ✅ | ☐ |
| D27 | Verify sau delete — List | ✅ | ☐ |
| D28 | Delete — ID không tồn tại | ❌ 404 | ☐ |
| D29 | Delete — Document user khác | ❌ 403/404 | ☐ |
| **Cross-Feature** | | | |
| X01 | Ownership isolation | ✅ | ☐ |
| X02 | Permission — document.create | ❌ 403 | ☐ |
| X03 | Permission — ocr.trigger | ❌ 403 | ☐ |
| X04 | Document liên kết Visit | ✅ | ☐ |
| X05 | Timeline sau delete | ✅ | ☐ |
| X06 | Upload nhiều documents | ✅ | ☐ |
| X07 | Ref Document Types — No Auth | ✅ | ☐ |

**Tổng: 47 test cases** (28 happy path ✅ + 19 error cases ❌)

---

## ⚙️ RECOMMENDED TEST ORDER

1. **Ref Data + Chuẩn bị** (0.1 → 0.6) — lấy document type IDs + đảm bảo có pregnancy & visit
2. **Upload Document** (D01 → D10) — upload các loại file với metadata khác nhau
3. **Read Document** (D11 → D14) — list + detail + error cases
4. **Update Document** (D15 → D20) — cập nhật metadata + link visit
5. **Toggle Favorite** (D21 → D24) — toggle on/off + verify
6. **OCR** (O01 → O05) — rerun + check status (stub)
7. **Timeline** (TL01 → TL04) — verify timeline tổng hợp
8. **Delete Document** (D25 → D29) — soft delete + verify
9. **Cross-Feature** (X01 → X07) — ownership, permissions, integrations

> **⚡ Tip**: Chạy theo thứ tự trên vì các test sau phụ thuộc ID tạo ở test trước (pregnancyId → documentId → ocrResultId).

---

## 📝 ENUM REFERENCE

| Enum | Values |
|------|--------|
| DocumentSource | Upload(0), Share(1), Import(2) |
| OcrStatus | Pending(0), Processing(1), Succeeded(2), Failed(3) |

---

## 🔑 PERMISSIONS REFERENCE

| Permission | Used by | Roles |
|------------|---------|-------|
| `document.create` | POST upload | USER, DOCTOR, ADMIN |
| `document.view` | GET list/detail/timeline | USER, DOCTOR, ADMIN |
| `document.update` | PUT update | USER, DOCTOR, ADMIN |
| `document.delete` | DELETE | USER, DOCTOR, ADMIN |
| `document.favorite` | PATCH toggle | USER, DOCTOR, ADMIN |
| `ocr.trigger` | POST rerun OCR | USER, DOCTOR, ADMIN |
| `ocr.view` | GET OCR status | USER, DOCTOR, ADMIN |
