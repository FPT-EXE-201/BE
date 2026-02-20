# Medical Record Workflow — 4-Phase Document Flow

> **Mục đích**: Tài liệu workflow đầy đủ cho chức năng Medical Record — giúp developer mới hiểu flow trước khi implement hoặc maintain code.  
> **Cập nhật**: 2026-02-15  
> **Trạng thái**: Week 4 (CRUD + Stub) ✅ — Week 5 (OCR + AI real) chưa implement — Week 5.5 (Auto-Fill) chưa implement  
> **Xem thêm**: `WEEK_4_PROMPTS_GUIDE.md` (chi tiết code), `WEEK_5_PROMPTS_GUIDE.md` (AI pipeline), `WEEK_5.5_PROMPTS_GUIDE.md` (Auto-Fill), `FEATURES_WORKFLOW_GUIDE.md` (tổng quan tất cả features)

---

## 1. TỔNG QUAN

Chức năng Medical Record cho phép mẹ bầu:
1. **Chụp ảnh** tài liệu y tế → upload lên hệ thống
2. **PRENATAL_CHECKUP**: OCR (Azure) + AI (Gemini) tự động trích xuất → user review → confirm → tạo PrenatalVisit
3. **Test types** (BLOOD_TEST, ULTRASOUND...): **Không có OCR** — hiện ảnh, user tự nhập metadata → confirm → tạo PrenatalTest
4. **Others** (PRESCRIPTION, VACCINATION...): Chỉ lưu trữ archive, **không cần confirm**
5. User có thể xem, sửa, xóa, đánh dấu yêu thích tài liệu

> **⚠️ KEY PRINCIPLE**: OCR + AI extraction chỉ áp dụng cho `PRENATAL_CHECKUP`. Tất cả document types khác KHÔNG chạy OCR.

---

## 2. ENTITY RELATIONSHIP DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ENTITY RELATIONSHIPS                             │
│                                                                         │
│  User                                                                   │
│    │                                                                    │
│    │ 1:N (owns)                                                         │
│    ▼                                                                    │
│  Pregnancy ──────────────────────────────────────────────────────┐      │
│    │                                                              │      │
│    │ 1:N                                                          │ 1:N  │
│    ▼                                                              ▼      │
│  MedicalDocument ◄──────── N:1 ──────► PrenatalVisit             │      │
│    │         │                           │                              │
│    │         │                           │ 1:N                          │
│    │ 1:1     │ 1:N                       ▼                              │
│    ▼         ▼                        PrenatalTest                      │
│  DocumentFile  OcrResult                  │                              │
│    │ N:1        │                       │ N:1                          │
│    ▼             │                       ▼                              │
│  StorageFile    │                    RefTestType                       │
│    │             │                                                      │
│    │             └── StructuredJson (PrenatalExaminationData)           │
│    │                  = SINGLE SOURCE OF TRUTH cho AI output           │
│    │                                                                    │
│    └── File vật lý (ảnh/PDF trên storage)                              │
│                                                                         │
│  PrenatalTest ─── N:1 ───► MedicalDocument (via DocumentId?)           │
│    │  DocumentId FK, ON DELETE SET NULL                                 │
│    │  → Chỉ dùng cho test types (BLOOD_TEST, ULTRASOUND...)            │
│                                                                         │
│  RefDocumentType ──── 1:N ──── RefDocumentTypeTranslation (i18n)       │
│    │                                                                    │
│    └── N:1 ──── MedicalDocument.DocumentTypeId                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Entity Summary

| Entity | Table | Vai trò | BaseEntity |
|--------|-------|---------|:----------:|
| `StorageFile` | `storage_files` | File vật lý (objectKey, publicUrl, checksum) | ✅ |
| `DocumentFile` | `document_files` | Junction: MedicalDocument ↔ StorageFile (multi-file, sortOrder) | ✅ |
| `MedicalDocument` | `medical_documents` | Metadata tài liệu + FK → Pregnancy | ✅ |
| `OcrResult` | `ocr_results` | Kết quả OCR + AI per-run (multi-run history) | ✅ |
| `RefDocumentType` | `ref_document_types` | Master data loại tài liệu (14 types seeded) | ✅ |
| `RefDocumentTypeTranslation` | `ref_document_type_translations` | i18n tên loại tài liệu | ❌ composite PK |

### 2.2 Key Relationships

| From | To | Type | FK | Khi nào populate |
|------|-----|------|-----|-----------------|
| MedicalDocument | Pregnancy | N:1 | `pregnancy_id` NOT NULL | Upload time |
| MedicalDocument | DocumentFile | 1:N | `document_id` on DocumentFile | Upload time |
| DocumentFile | StorageFile | N:1 | `storage_file_id` NOT NULL | Upload time |
| MedicalDocument | PrenatalVisit | N:1 | `visit_id` NULL | Sau khi AI extraction xong |
| MedicalDocument | RefDocumentType | N:1 | `document_type_id` NULL | Upload time (user chọn) |
| OcrResult | MedicalDocument | N:1 | `document_id` NOT NULL | Mỗi lần chạy OCR (PRENATAL_CHECKUP only) |
| PrenatalTest | MedicalDocument | N:1 | `document_id` NULL | Confirm time (test types only), ON DELETE SET NULL |
| StorageFile | User | N:1 | `owner_user_id` NULL | Upload time |

---

## 3. FULL PIPELINE FLOW — 4 PHASES

### 3.1 Phase 1: UPLOAD + LƯU TRỮ

```
┌─────────────────────────────────────────────────────────────────────┐
│                     PHASE 1: UPLOAD + LƯU TRỮ                      │
│                                                                     │
│  User chụp/chọn ảnh → POST /api/pregnancies/{id}/documents         │
│       │  body: { documentTypeId?, title?, documentDate?, source,    │
│       │         notes? } + files[] (List<IFormFile>)                 │
│       ▼                                                             │
│  ┌─ MedicalDocumentService.CreateWithFilesAsync ───────────────┐  │
│  │                                                                │  │
│  │  Step 1: Verify pregnancy ownership                            │  │
│  │       │  pregnancy.UserId == currentUserId?                    │  │
│  │       │  → NotFoundException / ForbiddenException               │  │
│  │       ▼                                                        │  │
│  │  Step 2: IFileStorageService.UploadAsync(stream, fileName...)  │  │
│  │       │  Week 4: StubFileStorageService (placeholder URLs)     │  │
│  │       │  Week 5: SupabaseStorageService (upload thật)          │  │
│  │       ▼                                                        │  │
│  │  Step 3: For each file: Tạo StorageFile + DocumentFile         │  │
│  │       ▼                                                        │  │
│  │  Step 4: Tạo MedicalDocument record trong DB                   │  │
│  │       │  PregnancyId, DocumentTypeId...                          │  │
│  │       │  VisitId = NULL (chưa biết buổi khám nào)              │  │
│  │       ▼                                                        │  │
│  │  Step 5: SaveChanges (commit cả 2 records)                     │  │
│  │       ▼                                                        │  │
│  │  Step 6: DocumentType = PRENATAL_CHECKUP?                      │  │
│  │       │                                                        │  │
│  │       ├─ YES → OcrService.QueueOcrAsync(documentId, "vi")      │  │
│  │       │        → Tạo OcrResult { Status = Pending, RunNo = 1 } │  │
│  │       │        → Enqueue to Channel (non-blocking)              │  │
│  │       │        → OcrBackgroundService xử lý ngầm (10-30s)      │  │
│  │       │                                                        │  │
│  │       ├─ Test types → KHÔNG queue OCR                          │  │
│  │       │                                                        │  │
│  │       └─ Others (PRESCRIPTION, VACCINATION...) →               │  │
│  │            KHÔNG queue OCR, chỉ lưu trữ                        │  │
│  │            ✅ Document DONE — không cần confirm gì thêm        │  │
│  │       ▼                                                        │  │
│  │  Step 7: Reload document with details → Return DTO              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  Response: 201 Created → MedicalDocumentDto (trả về ngay <1s)       │
│    { id, pregnancyId, title, documentDate, source, notes,            │
│      isFavorite, files: [{ storageFileId, fileUrl, mimeType }] }     │
│                                                                     │
│  ⚠️ PRENATAL_CHECKUP: OCR chạy ngầm. Flutter polls:                │
│     GET /api/ocr/{id}/status mỗi 3-5s                              │
│     → Pending → OcrProcessing → AiExtracting → Succeeded ✅        │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Phase 2: REVIEW (chỉ PRENATAL_CHECKUP + Test types)

```
┌─────────────────────────────────────────────────────────────────────┐
│         PHASE 2.A: PRENATAL_CHECKUP — OCR + AI Review               │
│                                                                     │
│  Background Job picks up OcrResult (Status = Pending)               │
│       │                                                             │
│       ▼                                                             │
│  ┌─ Azure OCR ──────────────────────────────────────────────────┐   │
│  │  OcrResult.Status: Pending → OcrProcessing                    │   │
│  │                                                                │   │
│  │  1. Download ảnh từ Storage (qua DocumentFile → StorageFile)     │   │
│  │  2. POST ảnh → Azure Document Intelligence (prebuilt-read)    │   │
│  │  3. Poll GET until status = "succeeded"                        │   │
│  │  4. Lưu OcrResult.RawText, OcrEngine, ConfidenceScore         │   │
│  │                                                                │   │
│  │  OcrResult.Status: OcrProcessing → OcrCompleted               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│       │                                                             │
│       ▼                                                             │
│  ┌─ Gemini AI Extraction ───────────────────────────────────────┐   │
│  │  OcrResult.Status: OcrCompleted → AiExtracting                │   │
│  │                                                                │   │
│  │  1. RAG Context: Pregnancy, Conditions, Recent OcrResults     │   │
│  │  2. Load AiPromptTemplate (key: "medical_record.extraction")  │   │
│  │  3. PromptBuilder → 4-layer prompt (System + Domain +         │   │
│  │     Feature + RAG Context) + RAW TEXT = UserMessage            │   │
│  │  4. Gemini API → PrenatalExaminationData JSON                 │   │
│  │  5. Lưu OcrResult.StructuredJson                              │   │
│  │                                                                │   │
│  │  OcrResult.Status: AiExtracting → Succeeded                   │   │
│  └───────────────────────────────────────────────────────────────┘   │
│       │                                                             │
│       ▼                                                             │
│  GET /api/ocr/{id}/review                                           │
│  → Trả về extracted data: vitals, diagnoses, medications            │
│  → User xem, chỉnh sửa nếu AI extract sai                         │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│         PHASE 2.B: TEST TYPES — Manual Metadata Entry               │
│                                                                     │
│  ⚠️ KHÔNG CÓ OCR — hiện ảnh cho user xem                          │
│                                                                     │
│  Áp dụng cho: BLOOD_TEST, ULTRASOUND, URINE_TEST, HIV_TEST,        │
│  HEPATITIS_B_TEST, THYROID_TEST, GLUCOSE_TEST, CBC_TEST, NT_SCAN    │
│                                                                     │
│  User tự chọn/nhập:                                                 │
│    - TestType (loại xét nghiệm — chọn từ RefTestType)               │
│    - TestDate (ngày xét nghiệm)                                     │
│    - Notes (ghi chú kết quả)                                        │
│    - IsAbnormal (kết quả bất thường?)                                │
│                                                                     │
│  → Flutter app render form nhập liệu bên cạnh ảnh upload            │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.3 Phase 3: CONFIRM hoặc SKIP

```
┌─────────────────────────────────────────────────────────────────────┐
│            PHASE 3.A: USER CHỌN CONFIRM                             │
│                                                                     │
│  POST /api/ocr/{id}/confirm (hoặc /documents/{id}/confirm)          │
│       │                                                             │
│       ├─ PRENATAL_CHECKUP:                                          │
│       │   → Tạo PrenatalVisit + VitalsJson                          │
│       │   → Set MedicalDocument.VisitId ← visit mới                 │
│       │   → OcrResult.Status = Confirmed                            │
│       │   → OcrResult.ConfirmedAt, ConfirmedBy, ConfirmedJson       │
│       │                                                             │
│       ├─ Test types:                                                 │
│       │   → Tạo PrenatalTest (imageUrlsJson, notes, abnormal)       │
│       │   → Set PrenatalTest.DocumentId ← document này              │
│       │   → TestTypeId ← user chọn hoặc auto-match từ DocType       │
│       │                                                             │
│       └─ ✅ Document + entities đều ACTIVE, user thấy bình thường   │
│                                                                     │
│  Response: AutoFillResultDto (entities đã tạo summary)              │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│            PHASE 3.B: USER CHỌN SKIP                                │
│                                                                     │
│  POST /api/documents/{id}/skip (hoặc DELETE)                        │
│       │                                                             │
│       ├─ Soft delete MedicalDocument (set DeletedAt)                │
│       ├─ User KHÔNG thấy document này nữa                          │
│       ├─ Admin vẫn thấy (query include deleted)                     │
│       ├─ StorageFile(s) VẪN GIỮ (không xóa file vật lý)              │
│       └─ OcrResult giữ nguyên (audit trail)                         │
│                                                                     │
│  ⚠️ User đổi ý? Admin restore hoặc user upload lại                │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.4 Phase 4: SAU KHI CONFIRM — Update Rules

```
┌─────────────────────────────────────────────────────────────────────┐
│  Update metadata (TẤT CẢ document types)                            │
│  ─────────────────────────────────────────                           │
│  PUT /api/documents/{id}                                            │
│  ✅ title, notes, documentDate, documentTypeId, visitId             │
│  ✅ Luôn cho phép                                                    │
│                                                                     │
│  ═══════════════════════════════════════════════════                  │
│                                                                     │
│  Đổi file ảnh                                                       │
│  ────────────                                                        │
│  PUT /api/documents/{id}/file                                       │
│                                                                     │
│  PRENATAL_CHECKUP (đã confirm OCR):                                 │
│    ❌ Block — "Cannot replace file after OCR confirmed.             │
│              Upload a new document instead."                         │
│    Lý do: VitalsJson extracted từ file cũ                           │
│                                                                     │
│  Test types (không có OCR):                                          │
│    ✅ Cho phép — chỉ update ảnh reference                           │
│    → Tạo StorageFile(s) + DocumentFile(s) mới, cập nhật files     │
│    → PrenatalTest.ImageUrlsJson cập nhật theo                       │
│                                                                     │
│  Others (PRESCRIPTION, VACCINATION...):                              │
│    ✅ Cho phép — chỉ archive                                        │
│                                                                     │
│  ═══════════════════════════════════════════════════                  │
│                                                                     │
│  Xóa document đã confirm                                            │
│  ────────────────────────                                            │
│  DELETE /api/documents/{id}                                         │
│                                                                     │
│  → Soft delete MedicalDocument                                      │
│  → PrenatalVisit / PrenatalTest VẪN GIỮ                             │
│    (data y tế không mất, chỉ ẩn document gốc)                      │
│  → PrenatalTest.DocumentId = null (ON DELETE SET NULL)              │
│  → MedicalDocument.VisitId giữ nguyên (soft delete)                │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.5 Rerun OCR — PRENATAL_CHECKUP only

```
POST /api/documents/{documentId}/ocr/rerun
     │
     ▼
OcrService.RerunOcrAsync(documentId, currentUserId)
     │
     ├── Verify ownership (document.Pregnancy.UserId == currentUserId)
     ├── Verify DocumentType = PRENATAL_CHECKUP
     ├── Get latest OcrResult → nextRunNo = latestRunNo + 1
     ├── Create new OcrResult { Status = Pending, RunNumber = nextRunNo }
     ├── SaveChanges
     └── (Week 5: queue background job)
     
Kết quả: OcrResult mới KHÔNG overwrite cũ → giữ nguyên lịch sử
     
     OcrResult #1 (RunNumber=1, Succeeded, StructuredJson=...)  ← giữ
     OcrResult #2 (RunNumber=2, Succeeded, StructuredJson=...)  ← giữ
     OcrResult #3 (RunNumber=3, Pending)                         ← mới
```

---

## 4. DATA FLOW — StructuredJson là SINGLE SOURCE OF TRUTH

### 4.1 Tại sao StructuredJson ở OcrResult (không phải MedicalDocument)?

```
TRƯỚC (bị trùng lặp):
  OcrResult.StructuredJson    = AI output per-run
  MedicalDocument.MetadataJson = copy/merge từ StructuredJson  ← TRÙNG!

SAU (đã sửa — HIỆN TẠI):
  OcrResult.StructuredJson    = AI output per-run (SINGLE SOURCE OF TRUTH)
  MedicalDocument.MetadataJson = ĐÃ XÓA

Khi cần structured data → query:
  OcrResult mới nhất = MAX(OcrRunNumber) WHERE DocumentId = x AND Status = Succeeded
```

### 4.2 StructuredJson Schema = PrenatalExaminationData

`StructuredJson` tuân theo schema `PrenatalExaminationData` (~290 dòng), đầy đủ mẫu phiếu khám thai Việt Nam:

```
PrenatalExaminationData
├── GeneralInfo                    ← A. THÔNG TIN CHUNG
│   ├── PatientName, DateOfBirth, Gender, Age
│   ├── PhoneNumber, Address, Occupation
│   ├── BloodType, RhFactor
│   ├── WeightKg, HeightCm
│   ├── InsuranceType, InsuranceNumber
│   └── ExaminationDate
│
├── PreviousVisit                  ← B. LẦN KHÁM TRƯỚC
│   ├── LastVisitDate, Location
│   ├── ChiefComplaint, ClinicalDiagnosis
│   └── LastMenstrualPeriod, ExpectedDeliveryDate
│
├── MedicalHistory                 ← II. HỘI BỆNH
│   ├── ReasonForVisit
│   ├── NumberOfPreviousPregnancies, NumberOfLiveBirths
│   └── PreviousDeliveries[]
│
├── DiseaseHistory                 ← III. TIỀN SỬ BỆNH
│   ├── Diabetes, Hypertension, HeartDisease
│   └── KidneyDisease, LiverDisease, ThyroidDisease...
│
├── GynecologicalHistory           ← TIỀN SỬ PHỤ KHOA
│   ├── MenstrualCycleRegular, MenstrualCycleDays
│   └── GynecologicalSurgery, CongenitalAnomalies
│
├── PersonalHistory                ← IV. HỘI SƠ BẢN THÂN
│   ├── AllergiesPresent, Allergies[]
│   └── CurrentMedications, Medications[]
│
├── FamilyHistory                  ← TIỀN SỬ GIA ĐÌNH
│   ├── TwinHistory, DiabetesHistory
│   └── HypertensionHistory, BirthDefectsHistory
│
├── PhysicalExamination            ← IV. KHÁM BỆNH
│   ├── VitalSigns                 ← extract ra PrenatalVisit.VitalsJson
│   │   ├── PulseRate, TemperatureCelsius
│   │   ├── BloodPressure, RespiratoryRate
│   │   └── WeightKg, HeightCm
│   ├── ConsciousnessState, Edema
│   ├── FundalHeightCm, AbdominalCircumferenceCm
│   ├── FetalPresentation, FetalHeartRate
│   └── CervicalStatus, CervicalDilationCm
│
├── LabTests                       ← V. XÉT NGHIỆM
│   ├── BloodTests (Hemoglobin, WBC, Platelets, HIV, HBsAg, Syphilis)
│   ├── UrineTests (Protein, Glucose)
│   └── Ultrasound (GestationalAge, FetalWeight, BPD, HC, AC, FL)
│
├── Diagnosis                      ← VI. CHẨN ĐOÁN
├── TreatmentPlan                  ← VII. KẾ HOẠCH ĐIỀU TRỊ
│   ├── Recommendations[]
│   └── Prescriptions[] (MedicationName, Dosage, Frequency)
│
├── Prognosis                      ← VIII. TIÊN LƯỢNG
│   └── NormalPregnancy, HighRiskPregnancy, RiskFactors
│
├── NextVisit                      ← IX. LẦN KHÁM KẾ TIẾP
│   └── NextVisitDate, VisitPurpose, DoctorName
│
├── OcrConfidence (decimal?)
└── ExtractedAt (DateTime)
```

### 4.3 StructuredJson → PrenatalVisit.VitalsJson

`PrenatalVisit.VitalsJson` là **subset nhỏ** được extract từ `StructuredJson`:

```
OcrResult.StructuredJson.PhysicalExamination.VitalSigns
    ├── PulseRate: 80
    ├── BloodPressure: "120/80"
    ├── WeightKg: 65.5
    └── TemperatureCelsius: 36.5
            ↓ AI Service extract ↓
PrenatalVisit.VitalsJson = {"bloodPressure":"120/80","weightKg":65.5,"pulseRate":80,"temperature":36.5}
```

---

## 5. API ENDPOINTS

### 5.1 Documents CRUD

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| POST | `/api/pregnancies/{id}/documents` | `document.create` | Upload ảnh + tạo document (multipart/form-data) |
| GET | `/api/pregnancies/{id}/documents?isFavorite=` | `document.view` | List documents của thai kỳ (filter by favorite) |
| GET | `/api/documents/{id}` | `document.view` | Chi tiết 1 document (include Files, DocumentType) |
| PUT | `/api/documents/{id}` | `document.update` | Update metadata (title, notes, visitId, documentTypeId, documentDate) |
| DELETE | `/api/documents/{id}` | `document.delete` | Soft delete document (entities VẪN GIỮ) |
| PATCH | `/api/documents/{id}/favorite` | `document.favorite` | Toggle IsFavorite (true ↔ false) |

### 5.2 OCR (PRENATAL_CHECKUP only)

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| POST | `/api/documents/{documentId}/ocr/rerun` | `ocr.trigger` | Re-run OCR + AI (PRENATAL_CHECKUP only) |
| GET | `/api/ocr/{id}/status` | `ocr.view` | Check OCR pipeline status |
| GET | `/api/documents/{documentId}/ocr` | `ocr.view` | List OCR results by document |

### 5.3 Reference Data & Timeline

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| GET | `/api/ref/document-types?lang=vi` | Public | Danh sách loại tài liệu (14 types) |
| GET | `/api/pregnancies/{pregnancyId}/timeline` | `document.view` | Timeline documents + visits |

---

## 6. SEEDED DOCUMENT TYPES

14 loại tài liệu (GUIDs: `b0000001-0000-0000-0000-00000000000X`):

| Code | Vietnamese | English | OCR? | Confirm → |
|------|------------|---------|:----:|----------|
| `PRENATAL_CHECKUP` | Khám thai | Prenatal Checkup | ✅ | PrenatalVisit |
| `ULTRASOUND` | Siêu âm | Ultrasound | ❌ | PrenatalTest |
| `BLOOD_TEST` | Xét nghiệm máu | Blood Test | ❌ | PrenatalTest |
| `URINE_TEST` | Xét nghiệm nước tiểu | Urine Test | ❌ | PrenatalTest |
| `HIV_TEST` | Xét nghiệm HIV | HIV Test | ❌ | PrenatalTest |
| `HEPATITIS_B_TEST` | Xét nghiệm viêm gan B | Hepatitis B Test | ❌ | PrenatalTest |
| `THYROID_TEST` | Xét nghiệm tuyến giáp | Thyroid Test | ❌ | PrenatalTest |
| `GLUCOSE_TEST` | Xét nghiệm đường huyết | Glucose Test | ❌ | PrenatalTest |
| `CBC_TEST` | Xét nghiệm công thức máu | CBC Test | ❌ | PrenatalTest |
| `NT_SCAN` | Đo độ mờ da gáy | NT Scan | ❌ | PrenatalTest |
| `PRESCRIPTION` | Đơn thuốc | Prescription | ❌ | — (archive) |
| `VACCINATION_RECORD` | Sổ tiêm chủng | Vaccination Record | ❌ | — (archive) |
| `MEDICAL_REPORT` | Báo cáo y tế | Medical Report | ❌ | — (archive) |
| `OTHER` | Khác | Other | ❌ | — (archive) |

---

## 7. BUSINESS RULES

| Rule | Chi tiết |
|------|----------|
| File size | ≤ 10MB |
| MIME types | `image/jpeg`, `image/png`, `application/pdf` |
| **OCR scope** | **Chỉ trigger cho PRENATAL_CHECKUP** — test types và others KHÔNG có OCR |
| OCR unique | `uk_ocr_results_doc_run (document_id, ocr_run_no)` — 1 run number per document |
| Ownership | User chỉ access documents của own pregnancies |
| VisitId lifecycle | NULL khi upload → populated sau khi Phase 3 Confirm (PRENATAL_CHECKUP) |
| IsFavorite | Toggle qua PATCH endpoint, filter qua `?isFavorite=true` query param |
| Soft delete | `deleted_at` column, global query filter exclude deleted records |
| **File replacement** | ❌ Block nếu document đã có OCR Confirmed. ✅ Allowed cho test types + others |
| **Skip** | Soft delete document. User không thấy, admin vẫn thấy. StorageFile(s) + OcrResult giữ nguyên |
| **PrenatalTest.DocumentId** | FK nullable → `medical_documents.id`, ON DELETE SET NULL |
| **Delete after confirm** | Soft delete document. PrenatalVisit/PrenatalTest VẪN GIỮ (data y tế không mất) |

---

## 8. ENUMS

```csharp
public enum DocumentSource
{
    Upload,     // User tự chụp/upload
    Share,      // Được chia sẻ từ bác sĩ/người thân
    Import      // Import từ hệ thống khác
}

// HIỆN TẠI (Week 4)
public enum OcrStatus
{
    Pending,      // Đang chờ (Week 4: dừng ở đây)
    Processing,   // Đang xử lý OCR
    Succeeded,    // Thành công
    Failed        // Thất bại
}
// Week 5 sẽ expand: thêm OcrProcessing, OcrCompleted, AiExtracting
// Week 5.5 sẽ thêm: Confirmed (user đã review + confirm extracted data)
```

---

## 9. FILE STRUCTURE — Code liên quan

```
src/
├── FPT.EXE201.Domain/
│   ├── Entities/
│   │   ├── StorageFile.cs              ← File vật lý
│   │   ├── DocumentFile.cs             ← Junction: Document ↔ StorageFile
│   │   ├── MedicalDocument.cs          ← Tài liệu y tế (NO MetadataJson)
│   │   ├── OcrResult.cs                ← OCR + AI result (HAS StructuredJson)
│   │   ├── RefDocumentType.cs          ← Master data loại tài liệu
│   │   └── RefDocumentTypeTranslation.cs ← i18n
│   └── Enums/
│       ├── DocumentSource.cs
│       └── OcrStatus.cs
│
├── FPT.EXE201.Application/
│   ├── DTOs/MedicalDocuments/
│   │   ├── CreateMedicalDocumentDto.cs
│   │   ├── UpdateMedicalDocumentDto.cs
│   │   ├── MedicalDocumentDto.cs
│   │   └── OcrResultDto.cs
│   ├── DTOs/Timeline/
│   │   └── TimelineEventDto.cs
│   ├── IRepositories/
│   │   ├── IStorageFileRepository.cs
│   │   ├── IDocumentFileRepository.cs
│   │   ├── IMedicalDocumentRepository.cs  ← GetByPregnancyIdWithDetailsAsync(isFavorite?)
│   │   ├── IOcrResultRepository.cs         ← GetLatestByDocumentIdAsync, GetByDocumentIdAsync
│   │   └── IRefDocumentTypeRepository.cs   ← GetActiveWithTranslationsAsync
│   ├── IServices/
│   │   ├── IFileStorageService.cs          ← UploadAsync, DownloadAsync, DeleteAsync, GetPublicUrl
│   │   ├── IOcrService.cs                  ← QueueOcrAsync, RerunOcrAsync, GetResultAsync
│   │   └── IMedicalDocumentService.cs      ← CreateWithFilesAsync, CRUD, ToggleFavorite, Timeline
│   ├── Services/
│   │   ├── MedicalDocumentService.cs       ← Full business logic
│   │   └── RefDataService.cs              ← GetActiveDocumentTypesAsync
│   ├── MapperProfiles/
│   │   └── MedicalDocumentProfile.cs       ← MedicalDocument→DTO, OcrResult→DTO
│   └── Validations/
│       ├── CreateMedicalDocumentDtoValidator.cs
│       └── UpdateMedicalDocumentDtoValidator.cs
│
├── FPT.EXE201.Infrastructure/
│   ├── Configurations/
│   │   ├── StorageFileConfiguration.cs
│   │   ├── DocumentFileConfiguration.cs
│   │   ├── MedicalDocumentConfiguration.cs
│   │   ├── OcrResultConfiguration.cs
│   │   ├── RefDocumentTypeConfiguration.cs
│   │   └── RefDocumentTypeTranslationConfiguration.cs
│   ├── Repositories/
│   │   ├── StorageFileRepository.cs
│   │   ├── DocumentFileRepository.cs
│   │   ├── MedicalDocumentRepository.cs
│   │   ├── OcrResultRepository.cs
│   │   └── RefDocumentTypeRepository.cs
│   ├── Services/
│   │   ├── StubFileStorageService.cs     ← Week 4 stub (placeholder URLs)
│   │   └── OcrService.cs                ← Week 4 stub (creates Pending record only)
│   └── Persistence/Seeders/
│       └── DocumentTypeSeeder.cs        ← 14 types × 2 languages = 28 translations
│
└── FPT.EXE201.Api/Controllers/
    ├── MedicalDocumentsController.cs     ← CRUD + favorite toggle + isFavorite filter
    ├── TimelineController.cs             ← Timeline endpoint
    ├── OcrController.cs                  ← Rerun + status + list by document
    └── RefDataController.cs             ← Document types + enums
```

---

## 10. DI REGISTRATION

```csharp
// Application/DependencyInjection.cs
services.AddScoped<IMedicalDocumentService, MedicalDocumentService>();
// + IRefDataService (document types part already registered)

// Infrastructure/DependencyInjection.cs
services.AddScoped<IFileStorageService, StubFileStorageService>();
// Week 5: sẽ đổi thành → SupabaseStorageService
services.AddScoped<IOcrService, OcrService>();
```

---

## 11. WEEK 5 TRANSITION CHECKLIST

Khi implement Week 5 (OCR + AI real), cần:

- [ ] Thay `StubFileStorageService` → `SupabaseStorageService` (upload thật lên Supabase Storage)
- [ ] Tạo `PrenatalExaminationData` model trong Domain/Models/ (schema cho StructuredJson)
- [ ] Expand `OcrStatus` enum: thêm `OcrProcessing`, `OcrCompleted`, `AiExtracting`
- [ ] Implement `AzureOcrProvider` (Azure Document Intelligence v4.0, async 2-step polling)
- [ ] Implement `GeminiAiProvider` (Google Gemini REST client, JSON mode)
- [ ] Implement `PromptBuilder` (Rule Layer system: L1 system + L2 domain + L3 feature + L4 RAG)
- [ ] Tạo `AiPromptTemplate` entity + seed data (key: "medical_record.extraction")
- [ ] Implement `MedicalRecordAiService` (full pipeline: OCR → AI → auto-create Visit/Test)
- [ ] Thay OcrService stub → real implementation (non-blocking queue via Channel + BackgroundService)
- [ ] Tạo `IOcrJobQueue` interface + `OcrJobQueue` implementation (Channel-based)
- [ ] Tạo `OcrBackgroundService` (BackgroundService — xử lý OCR+AI ngầm)
- [ ] DI: `AddSingleton<IOcrJobQueue, OcrJobQueue>()` + `AddHostedService<OcrBackgroundService>()`
- [ ] Log mọi AI call vào `ai_request_logs` table
- [ ] Configuration: `AI:Gemini:ApiKey`, `AI:AzureDocumentIntelligence:Endpoint`, `Supabase:Storage:*`

---

## 11.5. WEEK 5.5 TRANSITION CHECKLIST

Khi implement Week 5.5 (Auto-Fill: Review & Confirm), cần:

- [ ] Expand `OcrStatus` enum: thêm `Confirmed`
- [ ] `OcrResult` entity: thêm 4 fields (`ConfirmedAt`, `ConfirmedBy`, `ConfirmedJson`, `AutoFillResultJson`)
- [ ] EF Configuration + Migration cho OcrResult mới
- [ ] Seed permissions: `ocr.review`, `ocr.confirm`
- [ ] DTOs: `ExtractionReviewDto`, `ConfirmExtractionDto`, `AutoFillResultDto`
- [ ] FluentValidation: `ConfirmExtractionDtoValidator`
- [ ] `IAutoFillService` interface (ReviewAsync, ConfirmAsync)
- [ ] `AutoFillService` implementation (strategy by DocumentType.Code)
- [ ] Fuzzy Vietnamese keyword matching → `RefTestType.Code` (English UPPER_SNAKE_CASE)
- [ ] `AutoFillController` (GET review, POST confirm)
- [ ] DI registration: `IAutoFillService → AutoFillService`
- [ ] Update `OcrResultDto` (4 confirm fields)
- [ ] Chi tiết: `WEEK_5.5_PROMPTS_GUIDE.md`

---

## 12. FAQ

**Q: Tại sao MedicalDocument không có MetadataJson?**
A: Để tránh trùng lặp. `OcrResult.StructuredJson` đã chứa toàn bộ dữ liệu trích xuất theo mẫu `PrenatalExaminationData`. Giữ StructuredJson ở OcrResult vì:
- Mỗi lần rerun OCR → có snapshot riêng (audit trail)
- Single source of truth — không cần merge/sync
- Khi cần data → query OcrResult mới nhất (max RunNumber, Status = Succeeded)

**Q: OcrResult.StructuredJson vs PrenatalVisit.VitalsJson khác gì?**
A: `StructuredJson` = **full phiếu khám thai** (~30 sections). `VitalsJson` = **chỉ số sinh tồn** (~5 fields: mạch, HA, cân nặng, nhiệt độ). AI service extract VitalsJson từ StructuredJson khi auto-create PrenatalVisit.

**Q: User rerun OCR thì chuyện gì xảy ra?**
A: Tạo OcrResult mới với RunNumber++ → pipeline chạy lại → StructuredJson mới. Record cũ KHÔNG bị xóa → giữ lịch sử.

**Q: VisitId populate khi nào?**
A: Ban đầu NULL. Sau khi Phase 3 Confirm (PRENATAL_CHECKUP) → tạo PrenatalVisit → update `MedicalDocument.VisitId = newVisit.Id`.

**Q: PrenatalTest.DocumentId populate khi nào?**
A: Khi Phase 3 Confirm (test types) → tạo PrenatalTest → set `PrenatalTest.DocumentId = document.Id`. Nếu user xóa document sau đó → `DocumentId = NULL` (ON DELETE SET NULL). PrenatalTest vẫn giữ.

**Q: Test types có chạy OCR không?**
A: KHÔNG. Chỉ PRENATAL_CHECKUP chạy OCR + AI. Test types chỉ lưu ảnh, user tự nhập metadata (TestType, Date, Notes, IsAbnormal).

**Q: Document type Others (PRESCRIPTION, VACCINATION...) cần confirm không?**
A: KHÔNG. Others chỉ archive — upload xong là DONE.

**Q: Có thể thay file ảnh sau khi confirm không?**
A: PRENATAL_CHECKUP đã confirm → ❌ Block (vì VitalsJson đã extract từ file cũ). Test types và Others → ✅ Cho phép.
