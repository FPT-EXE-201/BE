# 📋 TESTING CHECKLIST — WEEK 3: Pregnancy Core

> **Prerequisite**: Đã đăng ký tài khoản, đăng nhập, có JWT Bearer Token.
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## 0️⃣ PRE-TEST: Lấy Reference Data (Public — Không cần Auth)

### 0.1 Lấy danh sách enum values
```
GET /api/ref/enums
```
**Expected**: 200 OK — Object chứa tất cả enum values (babyGender, pregnancyStatus, pregnancyType, dueDateSource, deliveryMethod, conditionSeverity, visitType).

### 0.2 Lấy danh mục bệnh lý thai kỳ
```
GET /api/ref/pregnancy-conditions?lang=vi
```
**Expected**: 200 OK — Danh sách 10 bệnh lý (Tiểu đường thai kỳ, Tiền sản giật, ...).
**Lưu lại**: Ghi nhớ các `id` trả về để dùng cho test Pregnancy Conditions.

### 0.3 Lấy danh mục loại xét nghiệm
```
GET /api/ref/test-types?lang=vi
```
**Expected**: 200 OK — Danh sách 10 loại xét nghiệm.
**Lưu lại**: Ghi nhớ các `id` trả về để dùng cho test Prenatal Tests.

### 0.4 Lấy enum theo tên
```
GET /api/ref/enums/visitType
```
**Expected**: 200 OK — Array: Routine(0), Emergency(1), FollowUp(2), LabOnly(3), Other(4).

### 0.5 Lấy test types theo category
```
GET /api/ref/test-types?lang=vi&category=LAB
```
**Expected**: 200 OK — Chỉ trả về test types thuộc category LAB.

---

## 1️⃣ PREGNANCY — CRUD + Status

### Seed IDs (Reference)
| Resource | Code | Seed ID |
|----------|------|---------|
| Condition | GESTATIONAL_DIABETES | `a0000001-0000-0000-0000-000000000001` |
| Condition | PREECLAMPSIA | `a0000001-0000-0000-0000-000000000002` |
| Condition | ANEMIA | `a0000001-0000-0000-0000-000000000003` |
| Test Type | BIOCHEMISTRY | `b0000001-0000-0000-0000-000000000001` |
| Test Type | ULTRASOUND | `b0000001-0000-0000-0000-000000000002` |
| Test Type | CBC | `b0000001-0000-0000-0000-000000000004` |
| Test Type | OGTT | `b0000001-0000-0000-0000-00000000000a` |

---

### ✅ TC-P01: Tạo thai kỳ — Happy Path (tối thiểu)
```
POST /api/pregnancies
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "lastMenstrualPeriodDate": "2025-11-01",
  "notes": "Thai kỳ đầu tiên"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Pregnancy created successfully",
  "data": {
    "id": "<guid>",
    "userId": "<your-user-id>",
    "pregnancyNumber": 1,
    "status": "Active",
    "lastMenstrualPeriodDate": "2025-11-01",
    "expectedDeliveryDate": "2026-08-08",
    "currentGestationalWeek": 15,
    "gestationalAgeDisplay": "15w0d",
    "notes": "Thai kỳ đầu tiên",
    "babyGender": "Unknown",
    "pregnancyType": "Singleton",
    "dueDateSource": "LMP",
    "prePregnancyBmi": null,
    "obstetricFormula": null,
    "createdAt": "..."
  }
}
```
**Lưu lại**: Copy `id` (pregnancyId) để dùng cho các test tiếp theo. Gọi là `{pregnancyId}`.

---

### ✅ TC-P02: Tạo thai kỳ — Full Fields
```
POST /api/pregnancies
Content-Type: application/json
```
```json
{
  "lastMenstrualPeriodDate": "2025-10-15",
  "estimatedConceptionDate": "2025-10-29",
  "notes": "Thai kỳ lần 2, có tiền sử tiểu đường",
  "babyNickname": "Bé Bông",
  "babyGender": 1,
  "pregnancyType": 0,
  "motherBloodType": "O+",
  "prePregnancyWeightKg": 55.5,
  "heightCm": 162,
  "dueDateSource": 0,
  "gravida": 2,
  "para": 1,
  "coverImageUrl": "https://example.com/cover.jpg"
}
```
**Expected**: 201 Created — Response có `babyNickname: "Bé Bông"`, `babyGender: "Male"`, BMI auto-computed (`prePregnancyBmi ≈ 21.15`), `obstetricFormula: "G2P1"`.

> **Enum int values**: BabyGender: Unknown=0, Male=1, Female=2. PregnancyType: Singleton=0, Twins=1, Triplets=2, Other=3. DueDateSource: LMP=0, Ultrasound=1, IVF=2, Manual=3.

---

### ❌ TC-P03: Tạo thai kỳ — Validation: Thiếu cả LMP và ConceptionDate
```json
{
  "notes": "Thiếu ngày"
}
```
**Expected**: 400 Bad Request — `"Either Last Menstrual Period date or conception date must be provided"`

---

### ❌ TC-P04: Tạo thai kỳ — Validation: LMP trong tương lai
```json
{
  "lastMenstrualPeriodDate": "2027-01-01"
}
```
**Expected**: 400 Bad Request — `"Last Menstrual Period date cannot be in the future"`

---

### ❌ TC-P05: Tạo thai kỳ — Validation: LMP quá cũ (>45 tuần)
```json
{
  "lastMenstrualPeriodDate": "2024-01-01"
}
```
**Expected**: 400 Bad Request — `"Last Menstrual Period date cannot be more than 45 weeks ago"`

---

### ❌ TC-P06: Tạo thai kỳ — Validation: Weight/Height ngoài range
```json
{
  "lastMenstrualPeriodDate": "2025-11-01",
  "prePregnancyWeightKg": 500,
  "heightCm": 50
}
```
**Expected**: 400 Bad Request — Lỗi weight (>300) và height (<100).

---

### ✅ TC-P07: Lấy thai kỳ đang Active
```
GET /api/pregnancies/active
Authorization: Bearer {token}
```
**Expected**: 200 OK — Thai kỳ có status = Active.

---

### ✅ TC-P08: Lấy tất cả thai kỳ
```
GET /api/pregnancies
Authorization: Bearer {token}
```
**Expected**: 200 OK — Danh sách tất cả thai kỳ của user.

---

### ✅ TC-P09: Lấy thai kỳ theo ID
```
GET /api/pregnancies/{pregnancyId}
Authorization: Bearer {token}
```
**Expected**: 200 OK — Chi tiết thai kỳ đã tạo.

---

### ❌ TC-P10: Lấy thai kỳ — ID không tồn tại
```
GET /api/pregnancies/00000000-0000-0000-0000-000000000000
```
**Expected**: 404 Not Found.

---

### ✅ TC-P11: Cập nhật thai kỳ
```
PUT /api/pregnancies/{pregnancyId}
Content-Type: application/json
```
```json
{
  "lastMenstrualPeriodDate": "2025-11-01",
  "babyNickname": "Bé Đậu (updated)",
  "babyGender": 2,
  "motherBloodType": "A+",
  "prePregnancyWeightKg": 58,
  "heightCm": 160,
  "gravida": 2,
  "para": 1,
  "notes": "Cập nhật thông tin bé"
}
```
**Expected**: 200 OK — `babyNickname: "Bé Đậu (updated)"`, `babyGender: "Female"`, BMI re-computed.

---

### ✅ TC-P12: Thay đổi trạng thái → Delivered
```
PATCH /api/pregnancies/{pregnancyId}/status
Content-Type: application/json
```
```json
{
  "status": 3,
  "actualDeliveryDate": "2026-02-10",
  "deliveryMethod": 0
}
```
**Expected**: 200 OK — `status: "Delivered"`, `actualDeliveryDate: "2026-02-10"`.

> **Enum**: PregnancyStatus: Active=0, Ended=1, Miscarriage=2, Delivered=3. DeliveryMethod: NaturalBirth=0, Cesarean=1, VacuumAssisted=2, ForcepsAssisted=3, WaterBirth=4, Other=5 (kiểm tra enum thực tế qua GET /api/ref/enums/deliveryMethod).

---

### ❌ TC-P13: Thay đổi trạng thái Delivered — Thiếu actualDeliveryDate
```json
{
  "status": 3
}
```
**Expected**: 400 Bad Request — `"Actual delivery date is required when status is Delivered"`

---

### ✅ TC-P14: Soft Delete thai kỳ
```
DELETE /api/pregnancies/{pregnancyId}
Authorization: Bearer {token}
```
**Expected**: 200 OK — `"Pregnancy deleted successfully"`. Thai kỳ vẫn còn trong DB nhưng có DeletedAt, không trả về trong GET nữa.

---

### ✅ TC-P15: Tạo thai kỳ mới cho các test tiếp theo
> Tạo lại 1 pregnancy để dùng cho Condition, Visit, Test tests.
```json
{
  "lastMenstrualPeriodDate": "2025-10-01",
  "babyNickname": "Bé Test",
  "prePregnancyWeightKg": 52,
  "heightCm": 158,
  "gravida": 1,
  "para": 0
}
```
**Lưu lại**: `{pregnancyId}` mới.

---

## 2️⃣ PREGNANCY CONDITIONS — Gán/Gỡ bệnh lý

### ✅ TC-C01: Gán bệnh lý — Tiểu đường thai kỳ
```
POST /api/pregnancies/{pregnancyId}/conditions?lang=vi
Content-Type: application/json
```
```json
{
  "conditionId": "a0000001-0000-0000-0000-000000000001",
  "diagnosedDate": "2025-12-15",
  "severity": 1,
  "notes": "Phát hiện qua OGTT lúc 24 tuần"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "conditionId": "a0000001-0000-0000-0000-000000000001",
    "conditionCode": "GESTATIONAL_DIABETES",
    "conditionDisplayName": "Tiểu đường thai kỳ",
    "conditionDescription": "...",
    "diagnosedDate": "2025-12-15",
    "severity": "Moderate",
    "notes": "Phát hiện qua OGTT lúc 24 tuần"
  }
}
```
**Lưu lại**: `{conditionId}` (id của record pregnancy_condition vừa tạo).

> **Enum ConditionSeverity**: Mild=0, Moderate=1, Severe=2.

---

### ✅ TC-C02: Gán thêm bệnh lý thứ 2 — Thiếu máu
```json
{
  "conditionId": "a0000001-0000-0000-0000-000000000003",
  "severity": 0,
  "notes": "Thiếu sắt nhẹ"
}
```
**Expected**: 201 Created — `conditionDisplayName: "Thiếu máu"`, `severity: "Mild"`.

---

### ❌ TC-C03: Gán bệnh lý trùng (cùng conditionId)
```json
{
  "conditionId": "a0000001-0000-0000-0000-000000000001"
}
```
**Expected**: 409 Conflict — Bệnh lý đã được gán cho thai kỳ này.

---

### ❌ TC-C04: Gán bệnh lý — conditionId không tồn tại
```json
{
  "conditionId": "  "
}
```
**Expected**: 404 Not Found.

---

### ✅ TC-C05: Danh sách bệnh lý của thai kỳ
```
GET /api/pregnancies/{pregnancyId}/conditions?lang=vi
```
**Expected**: 200 OK — Array 2 items (Tiểu đường + Thiếu máu), có tên tiếng Việt.

### Thử với lang=en
```
GET /api/pregnancies/{pregnancyId}/conditions?lang=en
```
**Expected**: 200 OK — Tên tiếng Anh (Gestational Diabetes, Anemia).

---

### ✅ TC-C06: Cập nhật bệnh lý
```
PUT /api/pregnancies/{pregnancyId}/conditions/{conditionId}?lang=vi
Content-Type: application/json
```
```json
{
  "diagnosedDate": "2025-12-20",
  "severity": 2,
  "notes": "Mức độ nặng hơn, cần theo dõi chặt"
}
```
**Expected**: 200 OK — `severity: "Severe"`, `diagnosedDate: "2025-12-20"`.

---

### ✅ TC-C07: Xóa bệnh lý
```
DELETE /api/pregnancies/{pregnancyId}/conditions/{conditionId}
```
**Expected**: 200 OK — `"Condition removed successfully"`. GET lại danh sách: chỉ còn 1 item.

---

## 3️⃣ PRENATAL VISITS — Buổi khám thai

### ✅ TC-V01: Tạo buổi khám — Tối thiểu
```
POST /api/pregnancies/{pregnancyId}/visits
Content-Type: application/json
```
```json
{
  "visitDateTime": "2026-01-10T09:00:00",
  "visitType": 0,
  "location": "Bệnh viện Từ Dũ",
  "notes": "Khám thai định kỳ lần 1"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "visitDateTime": "2026-01-10T09:00:00",
    "visitType": "Routine",
    "location": "Bệnh viện Từ Dũ",
    "notes": "Khám thai định kỳ lần 1",
    "vitals": null,
    "testCount": 0
  }
}
```
**Lưu lại**: `{visitId}` để dùng cho gắn test.

> **Enum VisitType**: Routine=0, Emergency=1, FollowUp=2, LabOnly=3, Other=4.

---

### ✅ TC-V02: Tạo buổi khám — Full Vitals (Phiếu khám thai MS: 51/BV2)
```
POST /api/pregnancies/{pregnancyId}/visits
Content-Type: application/json
```
```json
{
  "visitDateTime": "2026-02-01T10:30:00",
  "visitType": 0,
  "location": "Bệnh viện Phụ sản Trung ương",
  "notes": "Khám thai tuần 17, tình trạng ổn định",
  "vitals": {
    "generalInfo": {
      "facility": "Bệnh viện Phụ sản Trung ương",
      "fullName": "Nguyễn Thị Test",
      "dateOfBirth": "1995-05-15",
      "age": 30,
      "phone": "0912345678",
      "address": "123 Nguyễn Trãi, Q.1, TP.HCM",
      "province": "Hồ Chí Minh"
    },
    "interview": {
      "reasonForVisit": "Khám thai định kỳ",
      "pregnancyNumber": 1,
      "totalVisitCount": 2,
      "gestationalWeek": 17,
      "clinicalProgress": "Bình thường",
      "generalCondition": "Tốt"
    },
    "examination": {
      "vitalSigns": {
        "pulseBpm": 78,
        "temperatureCelsius": 36.5,
        "bloodPressureSystolic": 120,
        "bloodPressureDiastolic": 75,
        "respiratoryRateBpm": 18,
        "weightKg": 56.5,
        "heightCm": 158
      },
      "general": {
        "mentalStatus": "Tỉnh táo",
        "edema": false,
        "urineProtein": false
      },
      "obstetric": {
        "fundusHeightCm": 16,
        "fetalPresentation": "Ngôi đầu",
        "fetalHeartbeat": true,
        "fetalHeartRateBpm": 142,
        "uterineContraction": false,
        "cervix": "Đóng kín"
      }
    },
    "diagnosis": {
      "text": "Thai 17 tuần, phát triển bình thường",
      "icdCode": "Z34.0"
    },
    "treatmentPlan": {
      "medication": "Sắt + acid folic",
      "nextSteps": "Tiếp tục theo dõi",
      "healthEducation": true,
      "healthEducationNote": "Hướng dẫn dinh dưỡng thai kỳ"
    },
    "prognosis": "normal",
    "nextAppointment": {
      "date": "2026-03-01",
      "notes": "Khám thai định kỳ tháng tiếp",
      "examinerType": "Bác sĩ"
    }
  }
}
```
**Expected**: 201 Created — Response trả `vitals` là object (không phải JSON string), `testCount: 0`.

---

### ✅ TC-V03: Tạo buổi khám — Partial Vitals (chỉ vitalSigns)
```json
{
  "visitDateTime": "2026-02-05T14:00:00",
  "visitType": 2,
  "notes": "Tái khám, chỉ đo chỉ số cơ bản",
  "vitals": {
    "examination": {
      "vitalSigns": {
        "bloodPressureSystolic": 115,
        "bloodPressureDiastolic": 72,
        "weightKg": 57,
        "fetalHeartRateBpm": null
      }
    }
  }
}
```
**Expected**: 201 Created — Vitals chỉ chứa examination.vitalSigns, các phần khác null.

---

### ✅ TC-V04: Tạo buổi khám — Không có Vitals
```json
{
  "visitDateTime": "2026-01-20T08:00:00",
  "visitType": 3,
  "notes": "Chỉ làm xét nghiệm, không khám"
}
```
**Expected**: 201 Created — `vitals: null`, `visitType: "LabOnly"`.

---

### ❌ TC-V05: Tạo buổi khám — Validation: Thiếu visitDateTime
```json
{
  "visitType": 0
}
```
**Expected**: 400 Bad Request — `"visitDateTime" is required`.

---

### ❌ TC-V06: Tạo buổi khám — Validation: VisitType không hợp lệ
```json
{
  "visitDateTime": "2026-01-10T09:00:00",
  "visitType": 99
}
```
**Expected**: 400 Bad Request — Invalid enum.

---

### ✅ TC-V07: Danh sách buổi khám
```
GET /api/pregnancies/{pregnancyId}/visits
```
**Expected**: 200 OK — Array các buổi khám đã tạo, có `testCount` cho mỗi visit.

---

### ✅ TC-V08: Chi tiết buổi khám (kèm danh sách test)
```
GET /api/visits/{visitId}?lang=vi
```
**Expected**: 200 OK — Trả `PrenatalVisitDetailDto` gồm `vitals` object + `tests: []` (chưa có test nào gắn vào visit này).

---

### ✅ TC-V09: Cập nhật buổi khám
```
PUT /api/visits/{visitId}
Content-Type: application/json
```
```json
{
  "visitDateTime": "2026-01-10T09:30:00",
  "visitType": 0,
  "location": "Bệnh viện Từ Dũ (cập nhật)",
  "notes": "Đã cập nhật giờ khám",
  "vitals": {
    "examination": {
      "vitalSigns": {
        "pulseBpm": 80,
        "bloodPressureSystolic": 118,
        "bloodPressureDiastolic": 73,
        "weightKg": 57.2
      },
      "obstetric": {
        "fetalHeartbeat": true,
        "fetalHeartRateBpm": 145,
        "fundusHeightCm": 17
      }
    },
    "diagnosis": {
      "text": "Thai phát triển bình thường",
      "icdCode": "Z34.0"
    }
  }
}
```
**Expected**: 200 OK — Vitals được cập nhật.

---

### ✅ TC-V10: Xóa buổi khám
```
DELETE /api/visits/{visitId}
```
**Expected**: 200 OK — `"Visit deleted successfully"`.

> **⚠️ LƯU Ý**: Tạo lại visit sau khi test delete, vì Prenatal Test cần `visitId`.

---

## 4️⃣ PRENATAL TESTS — Xét nghiệm thai (File Upload via multipart/form-data)

> **⚠️ LƯU Ý QUAN TRỌNG**:
> - File upload hiện dùng `StubFileStorageService` → file KHÔNG được upload thật, chỉ tạo placeholder URL.
> - Gửi `multipart/form-data`, KHÔNG phải JSON.
> - Ảnh gửi qua field `images` (type: File), metadata qua form fields.
> - Trong Postman: Body → form-data → thêm field `images` (type: File).

### ✅ TC-T01: Tạo test — Không có ảnh
```
POST /api/pregnancies/{pregnancyId}/tests?lang=vi
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| `TestTypeId` | `b0000001-0000-0000-0000-000000000004` (CBC) |
| `TestDate` | `2026-01-15` |
| `Notes` | `Xét nghiệm công thức máu, chỉ số bình thường` |
| `IsAbnormalResult` | `false` |

**Expected**: 201 Created
```json
{
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "visitId": null,
    "testTypeId": "b0000001-0000-0000-0000-000000000004",
    "testTypeCode": "CBC",
    "testTypeDisplayName": "Công thức máu toàn phần",
    "testDate": "2026-01-15",
    "imageUrls": null,
    "notes": "Xét nghiệm công thức máu, chỉ số bình thường",
    "isAbnormalResult": false
  }
}
```
**Lưu lại**: `{testId}`.

---

### ✅ TC-T02: Tạo test — Có ảnh (Stub upload)
```
POST /api/pregnancies/{pregnancyId}/tests?lang=vi
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| `TestTypeId` | `b0000001-0000-0000-0000-000000000002` (Ultrasound) |
| `TestDate` | `2026-02-01` |
| `Notes` | `Siêu âm tuần 17, thai phát triển tốt` |
| `IsAbnormalResult` | `false` |
| `images` | (chọn 1-2 file ảnh bất kỳ từ máy tính, ví dụ `.jpg`, `.png`) |

**Expected**: 201 Created — `imageUrls` chứa 1-2 placeholder URLs dạng `https://placeholder.storage/uploads/2026/02/13/<guid>.jpg`.

> **⚠️ Stub**: URL là placeholder, không mở được. Đúng behavior cho giai đoạn chưa có Supabase.

---

### ✅ TC-T03: Tạo test — Gắn vào Visit
```
POST /api/pregnancies/{pregnancyId}/tests?lang=vi
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| `TestTypeId` | `b0000001-0000-0000-0000-00000000000a` (OGTT) |
| `VisitId` | `{visitId}` (ID của visit đã tạo ở TC-V01) |
| `TestDate` | `2026-01-10` |
| `Notes` | `Nghiệm pháp dung nạp glucose` |
| `IsAbnormalResult` | `true` |

**Expected**: 201 Created — `visitId` có giá trị, `isAbnormalResult: true`.

---

### ❌ TC-T04: Tạo test — VisitId thuộc pregnancy khác
> Tạo 1 pregnancy khác (hoặc dùng pregnancy đã xóa), lấy visitId của nó, rồi gửi vào pregnancy hiện tại.

**Expected**: 400 Bad Request — `"The specified visit does not belong to this pregnancy"`.

---

### ❌ TC-T05: Tạo test — TestTypeId không tồn tại
| Field | Value |
|-------|-------|
| `TestTypeId` | `00000000-0000-0000-0000-000000000000` |
| `TestDate` | `2026-01-15` |

**Expected**: 404 Not Found — `"Test type ... not found"`.

---

### ✅ TC-T06: Danh sách test của thai kỳ
```
GET /api/pregnancies/{pregnancyId}/tests?lang=vi
```
**Expected**: 200 OK — Array 3 tests (CBC, Ultrasound, OGTT), tên xét nghiệm tiếng Việt.

### Thử với lang=en
```
GET /api/pregnancies/{pregnancyId}/tests?lang=en
```
**Expected**: 200 OK — Tên xét nghiệm tiếng Anh (Complete Blood Count, Ultrasound, ...).

---

### ✅ TC-T07: Chi tiết test
```
GET /api/tests/{testId}?lang=vi
```
**Expected**: 200 OK — Full test detail.

---

### ✅ TC-T08: Cập nhật test — Thêm ảnh mới + giữ ảnh cũ
```
PUT /api/tests/{testId}?lang=vi
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| `ExistingImageUrls[0]` | `https://placeholder.storage/uploads/...` (URL ảnh cũ muốn giữ) |
| `Notes` | `Cập nhật: thêm ảnh mới` |
| `IsAbnormalResult` | `false` |
| `newImages` | (chọn 1 file ảnh mới) |

**Expected**: 200 OK — `imageUrls` chứa cả URL cũ + URL mới.

> **Postman Tips**: Trong form-data, thêm field `ExistingImageUrls[0]` (text) = URL cũ.

---

### ✅ TC-T09: Cập nhật test — Xóa hết ảnh (không gửi ExistingImageUrls, không gửi newImages)
```
PUT /api/tests/{testId}?lang=vi
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| `Notes` | `Đã xóa ảnh` |
| `IsAbnormalResult` | `false` |

**Expected**: 200 OK — `imageUrls: null`.

---

### ✅ TC-T10: Xóa test
```
DELETE /api/tests/{testId}
```
**Expected**: 200 OK — `"Test deleted successfully"`.

---

### ✅ TC-T11: Verify Visit detail kèm Tests
```
GET /api/visits/{visitId}?lang=vi
```
**Expected**: 200 OK — `tests` array chứa test OGTT (TC-T03) đã gắn vào visit này.

---

## 5️⃣ CROSS-FEATURE TESTS

### ✅ TC-X01: Ownership Isolation — User khác không xem được
> Đăng nhập bằng tài khoản khác, thử GET pregnancy/visit/test/condition của user ban đầu.

**Expected**: 403 Forbidden hoặc 404 Not Found (tùy implementation).

---

### ✅ TC-X02: Xóa pregnancy → Test và Visit vẫn trong DB (soft delete)
```
DELETE /api/pregnancies/{pregnancyId}
```
Sau đó `GET /api/pregnancies/{pregnancyId}` → Expected: 404 Not Found.
Nhưng GET visits/tests/conditions cũng → 404 (vì pregnancy không còn active).

---

### ✅ TC-X03: Permission — Không có quyền
> Nếu user không có role với permission `pregnancy.write`, thử POST → Expected: 403 Forbidden.

---

## 📊 CHECKLIST SUMMARY

| # | Test Case | Type | Result |
|---|-----------|------|--------|
| **Ref Data (Public)** | | | |
| 0.1 | GET /ref/enums | ✅ | ☐ |
| 0.2 | GET /ref/pregnancy-conditions | ✅ | ☐ |
| 0.3 | GET /ref/test-types | ✅ | ☐ |
| 0.4 | GET /ref/enums/{name} | ✅ | ☐ |
| 0.5 | GET /ref/test-types?category=LAB | ✅ | ☐ |
| **Pregnancy** | | | |
| P01 | Create — Happy Path (min) | ✅ | ☐ |
| P02 | Create — Full Fields | ✅ | ☐ |
| P03 | Create — Missing dates | ❌ 400 | ☐ |
| P04 | Create — Future LMP | ❌ 400 | ☐ |
| P05 | Create — LMP too old | ❌ 400 | ☐ |
| P06 | Create — Invalid ranges | ❌ 400 | ☐ |
| P07 | Get Active | ✅ | ☐ |
| P08 | Get All | ✅ | ☐ |
| P09 | Get by ID | ✅ | ☐ |
| P10 | Get — Not Found | ❌ 404 | ☐ |
| P11 | Update | ✅ | ☐ |
| P12 | Change Status → Delivered | ✅ | ☐ |
| P13 | Delivered — Missing date | ❌ 400 | ☐ |
| P14 | Soft Delete | ✅ | ☐ |
| P15 | Re-create for next tests | ✅ | ☐ |
| **Pregnancy Conditions** | | | |
| C01 | Add condition | ✅ | ☐ |
| C02 | Add 2nd condition | ✅ | ☐ |
| C03 | Add duplicate | ❌ 409 | ☐ |
| C04 | Add — Invalid conditionId | ❌ 404 | ☐ |
| C05 | List (vi & en) | ✅ | ☐ |
| C06 | Update severity | ✅ | ☐ |
| C07 | Remove condition | ✅ | ☐ |
| **Prenatal Visits** | | | |
| V01 | Create — Minimal | ✅ | ☐ |
| V02 | Create — Full Vitals | ✅ | ☐ |
| V03 | Create — Partial Vitals | ✅ | ☐ |
| V04 | Create — No Vitals | ✅ | ☐ |
| V05 | Create — Missing dateTime | ❌ 400 | ☐ |
| V06 | Create — Invalid visitType | ❌ 400 | ☐ |
| V07 | List visits | ✅ | ☐ |
| V08 | Detail (with tests) | ✅ | ☐ |
| V09 | Update | ✅ | ☐ |
| V10 | Delete | ✅ | ☐ |
| **Prenatal Tests** | | | |
| T01 | Create — No images | ✅ | ☐ |
| T02 | Create — With images (stub) | ✅ | ☐ |
| T03 | Create — Linked to visit | ✅ | ☐ |
| T04 | Create — Wrong visit | ❌ 400 | ☐ |
| T05 | Create — Invalid testTypeId | ❌ 404 | ☐ |
| T06 | List (vi & en) | ✅ | ☐ |
| T07 | Detail | ✅ | ☐ |
| T08 | Update — Add/keep images | ✅ | ☐ |
| T09 | Update — Remove all images | ✅ | ☐ |
| T10 | Delete | ✅ | ☐ |
| T11 | Visit detail with tests | ✅ | ☐ |
| **Cross-Feature** | | | |
| X01 | Ownership isolation | ✅ | ☐ |
| X02 | Cascade soft delete | ✅ | ☐ |
| X03 | Missing permission | ❌ 403 | ☐ |

**Tổng: 40 test cases** (27 happy path ✅ + 13 error cases ❌)

---

## ⚙️ RECOMMENDED TEST ORDER

1. **Ref Data** (0.1 → 0.5) — lấy enum values + seed IDs
2. **Pregnancy** (P01 → P15) — CRUD + status lifecycle
3. **Pregnancy Conditions** (C01 → C07) — gán/gỡ bệnh lý
4. **Prenatal Visits** (V01 → V10) — khám thai + VitalsJson
5. **Prenatal Tests** (T01 → T11) — xét nghiệm + file upload (stub)
6. **Cross-Feature** (X01 → X03) — ownership + permission

> **⚡ Tip**: Chạy theo thứ tự trên vì các test sau phụ thuộc ID tạo ở test trước (pregnancyId → conditionId → visitId → testId).
