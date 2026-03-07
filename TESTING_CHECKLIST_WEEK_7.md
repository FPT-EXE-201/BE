# 📋 TESTING CHECKLIST — WEEK 7: Nutrition & Meal Planning (AI)

> **Prerequisite**: Đã hoàn thành TESTING_CHECKLIST_WEEK_3 + WEEK_4 + WEEK_5 + WEEK_6. Có JWT Bearer Token, có pregnancy Active, có `heightCm` + `prePregnancyWeightKg` trong Weight Goal (hoặc trong Pregnancy).
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## ⚠️ QUAN TRỌNG — Cấu hình trước khi test

Week 7 sử dụng **Google Gemini 2.5 Flash** (AI) cho tính năng Generate Meal Plan.
**PHẢI đảm bảo cấu hình đúng** trước khi test:

| Service | Config Key | Ghi chú |
|---------|-----------|---------|
| Google Gemini | `AI:Gemini:ApiKey` | API key từ Google Cloud |
| Google Gemini | `AI:Gemini:ModelId` | `gemini-2.5-flash` (đã seed trong DB) |

> **Lưu ý**: Nếu **KHÔNG** test phần AI Generate Meal Plan (Section 5), có thể bỏ qua cấu hình Gemini. Các tính năng Food Preferences, Nutrition Notes, Reference Data, Feedback hoạt động **độc lập** không cần AI.

---

## 0️⃣ PRE-TEST: Xác nhận hệ thống khởi động đúng

### ✅ TC-N00: Application startup
```
dotnet run --project src/FPT.EXE201.Api
```
**Kiểm tra Console Output**:
- [ ] Không có exception khi khởi động
- [ ] EF Migration / Database update không lỗi
- [ ] 45 food items + translations seeded (vi + en)
- [ ] 15 nutrients + translations seeded (vi + en)
- [ ] AI prompt template `nutrition.meal_plan` seeded
- [ ] 12 nutrition permissions seeded (USER: 12, DOCTOR: 11)

**Expected**: App khởi động thành công, không crash.

---

### ✅ TC-N01: Chuẩn bị — Đảm bảo có pregnancy Active
```
GET /api/pregnancies/active
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có pregnancy đang Active.
**Lưu lại**: `{pregnancyId}`.

> **⚠️ Yêu cầu**: Pregnancy phải có `heightCm` và `prePregnancyWeightKg` (đã set trong Weight Goal ở Week 6) để AI tính BMI + calories mục tiêu.

---

### ✅ TC-N02: Kiểm tra Enum values mới (Week 7)

```
GET /api/ref/enums
```
**Expected**: 200 OK — Response chứa 7 enums mới:
```json
{
  "data": {
    "foodPreferenceType": { "Allergy": 0, "Dislike": 1 },
    "allergySeverity": { "Low": 0, "Medium": 1, "High": 2 },
    "mealType": { "Breakfast": 0, "Lunch": 1, "Dinner": 2, "Snack": 3 },
    "mealPlanSource": { "AI": 0, "Manual": 1 },
    "nutritionNoteType": { "Diet": 0, "Note": 1, "Other": 2 },
    "aiFeature": { "MedicalRecord": 0, "NutritionMealPlan": 1 },
    "aiRequestStatus": { "Processing": 0, "Succeeded": 1, "Failed": 2 }
  }
}
```
**Verify**:
- [ ] `foodPreferenceType` có `Allergy`, `Dislike`
- [ ] `allergySeverity` có `Low`, `Medium`, `High`
- [ ] `mealType` có `Breakfast`, `Lunch`, `Dinner`, `Snack`
- [ ] `nutritionNoteType` có `Diet`, `Note`, `Other`
- [ ] `aiFeature` có `NutritionMealPlan`

---

### ✅ TC-N03: Kiểm tra Enum by name

```
GET /api/ref/enums/foodPreferenceType
```
**Expected**: 200 OK
```json
{
  "data": { "Allergy": 0, "Dislike": 1 }
}
```

```
GET /api/ref/enums/allergySeverity
```
**Expected**: 200 OK
```json
{
  "data": { "Low": 0, "Medium": 1, "High": 2 }
}
```

---

## 1️⃣ REFERENCE DATA — Danh mục thực phẩm & dưỡng chất

### ✅ TC-R01: Lấy danh sách food items (Vietnamese)
```
GET /api/ref/food-items?lang=vi
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": [
    {
      "id": "c7010001-0000-0000-0000-000000000001",
      "code": "CHICKEN",
      "displayName": "Thịt gà"
    },
    {
      "id": "c7010002-0000-0000-0000-000000000002",
      "code": "PEANUT",
      "displayName": "Đậu phộng"
    }
  ]
}
```
**Verify**:
- [ ] Trả về 45 food items
- [ ] `displayName` tiếng Việt (lang=vi)
- [ ] Mỗi item có `id`, `code`, `displayName`

**Lưu lại**: 2-3 `{foodItemId}` để dùng cho Food Preference tests:
- `{foodItemId_peanut}` = `c7010002-0000-0000-0000-000000000002` (PEANUT / Đậu phộng)
- `{foodItemId_shrimp}` = `c7010001-0000-0000-0000-000000000007` (SHRIMP / Tôm)
- `{foodItemId_cilantro}` = `c7010003-0000-0000-0000-000000000001` (CILANTRO / Rau mùi)
- `{foodItemId_durian}` = `c7010004-0000-0000-0000-000000000001` (DURIAN / Sầu riêng)

---

### ✅ TC-R02: Lấy danh sách food items (English)
```
GET /api/ref/food-items?lang=en
```
**Expected**: 200 OK
**Verify**:
- [ ] `displayName` tiếng Anh (ví dụ: "Chicken", "Peanut", "Cilantro (coriander)")
- [ ] Số lượng = 45

---

### ✅ TC-R03: Lấy danh sách nutrients
```
GET /api/ref/nutrients?lang=vi
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": [
    {
      "id": "<guid>",
      "code": "PROTEIN",
      "displayName": "Chất đạm",
      "unit": "g"
    },
    {
      "id": "<guid>",
      "code": "IRON",
      "displayName": "Sắt",
      "unit": "mg"
    }
  ]
}
```
**Verify**:
- [ ] Trả về 15 nutrients
- [ ] Mỗi item có `id`, `code`, `displayName`, `unit`
- [ ] `displayName` tiếng Việt

---

### ✅ TC-R04: Lấy nutrients (English)
```
GET /api/ref/nutrients?lang=en
```
**Expected**: 200 OK
**Verify**:
- [ ] `displayName` tiếng Anh ("Protein", "Iron", "Folic Acid")

---

## 2️⃣ FOOD PREFERENCES — Quản lý sở thích / dị ứng thực phẩm

### ✅ TC-FP01: Tạo food preference — Allergy (dị ứng)
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010002-0000-0000-0000-000000000002",
  "preferenceType": "Allergy",
  "severity": "High",
  "notes": "Dị ứng đậu phộng nặng, có triệu chứng sốc phản vệ"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Food preference created successfully",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "foodItemId": "c7010002-0000-0000-0000-000000000002",
    "foodItemCode": "PEANUT",
    "foodItemDisplayName": "Đậu phộng",
    "preferenceType": "Allergy",
    "severity": "High",
    "notes": "Dị ứng đậu phộng nặng, có triệu chứng sốc phản vệ",
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `preferenceType` = `"Allergy"`
- [ ] `severity` = `"High"`
- [ ] `foodItemDisplayName` = `"Đậu phộng"` (tiếng Việt vì lang=vi)
- [ ] `foodItemCode` = `"PEANUT"`
- [ ] `id`, `createdAt`, `updatedAt` có giá trị

**Lưu lại**: `{prefId_allergy}`

---

### ✅ TC-FP02: Tạo food preference — Dislike (không thích)
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010003-0000-0000-0000-000000000001",
  "preferenceType": "Dislike",
  "notes": "Không thích mùi rau mùi"
}
```
**Expected**: 201 Created
```json
{
  "data": {
    "foodItemCode": "CILANTRO",
    "foodItemDisplayName": "Rau mùi (ngò)",
    "preferenceType": "Dislike",
    "severity": null,
    "notes": "Không thích mùi rau mùi"
  }
}
```
**Verify**:
- [ ] `preferenceType` = `"Dislike"`
- [ ] `severity` = `null` (Dislike không cần severity)
- [ ] `foodItemDisplayName` = `"Rau mùi (ngò)"`

**Lưu lại**: `{prefId_dislike}`

---

### ✅ TC-FP03: Tạo food preference — Allergy severity Low
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010001-0000-0000-0000-000000000007",
  "preferenceType": "Allergy",
  "severity": "Low",
  "notes": "Ngứa nhẹ khi ăn tôm"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `foodItemCode` = `"SHRIMP"`
- [ ] `severity` = `"Low"`

**Lưu lại**: `{prefId_shrimp}`

---

### ❌ TC-FP04: Tạo food preference — Duplicate (cùng pregnancy + food + type) → 409
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010002-0000-0000-0000-000000000002",
  "preferenceType": "Allergy",
  "severity": "Medium"
}
```
**Expected**: 409 Conflict
```json
{
  "success": false,
  "statusCode": 409,
  "message": "A Allergy preference for this food item already exists."
}
```

---

### ✅ TC-FP05: Tạo food preference — Cùng food khác type → OK
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010002-0000-0000-0000-000000000002",
  "preferenceType": "Dislike",
  "notes": "Cũng không thích vị đậu phộng"
}
```
**Expected**: 201 Created — Cùng food item `PEANUT` nhưng khác type (`Dislike` vs `Allergy`) → cho phép.
**Verify**:
- [ ] `preferenceType` = `"Dislike"` (khác với TC-FP01 là `Allergy`)
- [ ] `foodItemCode` = `"PEANUT"`

---

### ❌ TC-FP06: Tạo food preference — Food item không tồn tại → 404
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "00000000-0000-0000-0000-000000000000",
  "preferenceType": "Allergy"
}
```
**Expected**: 404 Not Found — `"Food item not found."`

---

### ❌ TC-FP07: Validation — Missing foodItemId
```
POST /api/pregnancies/{pregnancyId}/food-preferences
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "preferenceType": "Allergy"
}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": ["Food item ID is required."]
}
```

---

### ❌ TC-FP08: Validation — Invalid PreferenceType
```
POST /api/pregnancies/{pregnancyId}/food-preferences
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010001-0000-0000-0000-000000000001",
  "preferenceType": "InvalidType"
}
```
**Expected**: 400 Bad Request — `"Preference type must be Allergy or Dislike."`

---

### ❌ TC-FP09: Validation — Notes quá dài (> 255 ký tự)
```
POST /api/pregnancies/{pregnancyId}/food-preferences
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010001-0000-0000-0000-000000000001",
  "preferenceType": "Dislike",
  "notes": "AAAAAAAAAA... (> 255 characters)"
}
```
**Expected**: 400 Bad Request — `"Notes cannot exceed 255 characters."`

---

### ✅ TC-FP10: Đọc danh sách food preferences
```
GET /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
```
**Expected**: 200 OK — Trả về danh sách tất cả preferences đã tạo.
```json
{
  "success": true,
  "data": [
    {
      "id": "{prefId_allergy}",
      "foodItemCode": "PEANUT",
      "foodItemDisplayName": "Đậu phộng",
      "preferenceType": "Allergy",
      "severity": "High"
    },
    {
      "id": "{prefId_dislike}",
      "foodItemCode": "CILANTRO",
      "foodItemDisplayName": "Rau mùi (ngò)",
      "preferenceType": "Dislike"
    }
  ]
}
```
**Verify**:
- [ ] Danh sách sort theo `preferenceType` rồi `createdAt`
- [ ] `foodItemDisplayName` theo ngôn ngữ `lang=vi`
- [ ] Tất cả preferences đã tạo đều xuất hiện

---

### ✅ TC-FP11: Đọc food preferences (English)
```
GET /api/pregnancies/{pregnancyId}/food-preferences?lang=en
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] `foodItemDisplayName` = `"Peanut"`, `"Cilantro (coriander)"`, `"Shrimp"` (tiếng Anh)

---

### ✅ TC-FP12: Cập nhật food preference
```
PUT /api/pregnancies/{pregnancyId}/food-preferences/{prefId_allergy}?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "severity": "Medium",
  "notes": "Đã giảm mức độ sau điều trị"
}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Food preference updated successfully",
  "data": {
    "id": "{prefId_allergy}",
    "severity": "Medium",
    "notes": "Đã giảm mức độ sau điều trị",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `severity` thay đổi từ `"High"` → `"Medium"`
- [ ] `notes` đã cập nhật
- [ ] `updatedAt` đã thay đổi
- [ ] Các field không gửi (`foodItemId`, `preferenceType`) vẫn giữ nguyên

---

### ✅ TC-FP13: Cập nhật food preference — Chỉ severity
```
PUT /api/pregnancies/{pregnancyId}/food-preferences/{prefId_shrimp}?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "severity": "High"
}
```
**Expected**: 200 OK — Chỉ `severity` thay đổi, `notes` giữ nguyên.

---

### ❌ TC-FP14: Cập nhật food preference — Không tồn tại → 404
```
PUT /api/pregnancies/{pregnancyId}/food-preferences/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "severity": "Low"
}
```
**Expected**: 404 Not Found — `"Food preference not found."`

---

### ✅ TC-FP15: Xóa food preference
```
DELETE /api/pregnancies/{pregnancyId}/food-preferences/{prefId_dislike}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Food preference deleted successfully",
  "data": null
}
```
**Verify**:
- [ ] GET preferences → không còn item đã xóa
- [ ] Soft delete (deleted_at được set, record vẫn tồn tại trong DB)

---

### ✅ TC-FP16: Xóa rồi tạo lại cùng combo (pregnancy + food + type) → Restore
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010003-0000-0000-0000-000000000001",
  "preferenceType": "Dislike",
  "notes": "Tạo lại sau khi xóa — restored from soft-deleted"
}
```
**Expected**: 201 Created — Soft-deleted record được **khôi phục** (restore) thay vì tạo mới → không vi phạm unique constraint.
**Verify**:
- [ ] `id` có thể **giống hoặc khác** `{prefId_dislike}` ban đầu (cùng record được restore)
- [ ] `notes` = `"Tạo lại sau khi xóa — restored from soft-deleted"`
- [ ] `deletedAt` = null trong DB (đã restore)

---

### ❌ TC-FP17: Pregnancy không phải của user → 403
```
GET /api/pregnancies/{otherPregnancyId}/food-preferences
Authorization: Bearer {token}
```
**Expected**: 403 Forbidden — `"Access denied."`

---

### ❌ TC-FP18: Pregnancy không tồn tại → 404
```
GET /api/pregnancies/00000000-0000-0000-0000-000000000099/food-preferences
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Pregnancy not found."`

---

### ❌ TC-FP19: Không có token → 401
```
GET /api/pregnancies/{pregnancyId}/food-preferences
(No Authorization header)
```
**Expected**: 401 Unauthorized

---

## 3️⃣ NUTRITION NOTES — Ghi chú dinh dưỡng

### ✅ TC-NN01: Tạo nutrition note — Diet
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Diet",
  "valueText": "Ăn chay trường, không ăn thịt"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Nutrition note created successfully",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "noteType": "Diet",
    "valueText": "Ăn chay trường, không ăn thịt",
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `noteType` = `"Diet"`
- [ ] `valueText` unicode tiếng Việt đúng
- [ ] `id`, `createdAt`, `updatedAt` có giá trị

**Lưu lại**: `{noteId_diet}`

---

### ✅ TC-NN02: Tạo nutrition note — Note
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Note",
  "valueText": "Bác sĩ khuyên bổ sung thêm sắt và acid folic"
}
```
**Expected**: 201 Created
**Lưu lại**: `{noteId_note}`

---

### ✅ TC-NN03: Tạo nutrition note — Other
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Other",
  "valueText": "Thích ăn chua, nghén nặng 3 tháng đầu"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `noteType` = `"Other"`

**Lưu lại**: `{noteId_other}`

---

### ❌ TC-NN04: Validation — Missing valueText → 400
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Diet"
}
```
**Expected**: 400 Bad Request — `"Value text is required."`

---

### ❌ TC-NN05: Validation — Empty valueText → 400
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Diet",
  "valueText": ""
}
```
**Expected**: 400 Bad Request — `"Value text is required."`

---

### ❌ TC-NN06: Validation — valueText > 200 ký tự → 400
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Note",
  "valueText": "AAAA... (> 200 characters)"
}
```
**Expected**: 400 Bad Request — `"Value text cannot exceed 200 characters."`

---

### ❌ TC-NN07: Validation — Invalid NoteType → 400
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "InvalidType",
  "valueText": "Test"
}
```
**Expected**: 400 Bad Request — `"Note type must be Diet, Note, or Other."`

---

### ✅ TC-NN08: Đọc danh sách nutrition notes
```
GET /api/pregnancies/{pregnancyId}/nutrition-notes
Authorization: Bearer {token}
```
**Expected**: 200 OK — Trả về danh sách notes đã tạo.
**Verify**:
- [ ] Tất cả notes đã tạo đều xuất hiện (3 notes)
- [ ] Mỗi note có `id`, `pregnancyId`, `noteType`, `valueText`, `createdAt`, `updatedAt`

---

### ✅ TC-NN09: Cập nhật nutrition note
```
PUT /api/pregnancies/{pregnancyId}/nutrition-notes/{noteId_diet}
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "noteType": "Other",
  "valueText": "Đã đổi sang ăn chay linh hoạt (flexitarian)"
}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Nutrition note updated successfully",
  "data": {
    "id": "{noteId_diet}",
    "noteType": "Other",
    "valueText": "Đã đổi sang ăn chay linh hoạt (flexitarian)",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `noteType` thay đổi `"Diet"` → `"Other"`
- [ ] `valueText` đã cập nhật
- [ ] `updatedAt` thay đổi

---

### ✅ TC-NN10: Cập nhật nutrition note — Partial (chỉ valueText)
```
PUT /api/pregnancies/{pregnancyId}/nutrition-notes/{noteId_note}
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "valueText": "Thêm vitamin D vào buổi sáng"
}
```
**Expected**: 200 OK — Chỉ `valueText` thay đổi, `noteType` giữ nguyên `"Note"`.

---

### ✅ TC-NN11: Xóa nutrition note
```
DELETE /api/pregnancies/{pregnancyId}/nutrition-notes/{noteId_other}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Nutrition note deleted successfully",
  "data": null
}
```
**Verify**:
- [ ] GET notes → không còn note đã xóa
- [ ] Soft delete (deleted_at set trong DB)

---

### ❌ TC-NN12: Xóa nutrition note — Không tồn tại → 404
```
DELETE /api/pregnancies/{pregnancyId}/nutrition-notes/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Nutrition note not found."`

---

## 4️⃣ QUERY SPECS — Kiểm tra search/sort/paging

### ✅ TC-QS01: Query specs cho meal plans
```
GET /api/ref/query-specs/mealPlans
```
**Expected**: 200 OK
```json
{
  "data": {
    "entity": "mealPlans",
    "searchKeys": ["title", "notes"],
    "sortableFields": ["startdate", "enddate", "createdat"],
    "defaultSort": "createdat_desc",
    "example": "?search=Tuần 20&sort=startdate_asc&page=1&pageSize=10"
  }
}
```
**Verify**:
- [ ] `searchKeys` = `["title", "notes"]`
- [ ] `sortableFields` chứa `startdate`, `enddate`, `createdat`
- [ ] `defaultSort` = `"createdat_desc"`

---

## 5️⃣ MEAL PLAN — Tạo thực đơn bằng AI

> **⚠️ Section này yêu cầu Google Gemini API key đã cấu hình đúng.**
> **⚠️ Pregnancy phải có `heightCm` + `prePregnancyWeightKg` (hoặc có weight log gần nhất).**
> **⚠️ Rate limit: 15 AI calls/ngày/user. Mỗi lần generate tốn `durationWeeks` calls.**

### ✅ TC-MP01: Generate meal plan — 1 tuần
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 1,
  "additionalNotes": "Ưu tiên món Việt Nam, dễ nấu cho người bận rộn"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Meal plan generated successfully",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "startDate": "2026-03-09",
    "endDate": "2026-03-15",
    "source": "AI",
    "title": "<AI generated title>",
    "notes": "Ưu tiên món Việt Nam, dễ nấu cho người bận rộn",
    "days": [
      {
        "id": "<guid>",
        "planDate": "2026-03-09",
        "totalCalories": 2200,
        "mealCount": 4
      },
      {
        "id": "<guid>",
        "planDate": "2026-03-10",
        "totalCalories": 2150,
        "mealCount": 4
      }
    ],
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `source` = `"AI"`
- [ ] `startDate` = `"2026-03-09"`, `endDate` = `"2026-03-15"` (7 ngày)
- [ ] `days` có đúng 7 items (7 ngày)
- [ ] Mỗi day có `totalCalories` > 0 và `mealCount` = 4 (BREAKFAST, LUNCH, DINNER, SNACK)
- [ ] `title` do AI generate (không null)
- [ ] `notes` = input `additionalNotes`
- [ ] Không chứa food items mà user đã đánh dấu Allergy (PEANUT, SHRIMP)

**Lưu lại**: `{mealPlanId}`, `{planDate}` (ngày đầu tiên, ví dụ `2026-03-09`)

> **⏱️ Lưu ý**: Endpoint này có thể mất 15-60 giây để AI generate. Timeout Postman nên set ≥ 120s.

---

### ✅ TC-MP02: Generate meal plan — 2 tuần (multi-week)
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-16",
  "durationWeeks": 2
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `endDate` = `startDate + 14 ngày - 1` = `"2026-03-29"`
- [ ] `days` có 14 items
- [ ] Tuần 2 đa dạng món ăn, không lặp lại tuần 1 (AI được instruct "đảm bảo đa dạng")
- [ ] Tốn 2 AI calls (kiểm tra rate limit remaining)

**Lưu lại**: `{mealPlanId_2weeks}`

---

### ✅ TC-MP03: Generate meal plan — Overlap auto-delete
> Tạo meal plan mới có date range trùng với `{mealPlanId}` (TC-MP01).
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 1,
  "additionalNotes": "Thay thế plan cũ, muốn ăn đa dạng hơn"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] Plan cũ `{mealPlanId}` bị **soft-deleted** tự động (auto-delete overlapping)
- [ ] GET plan cũ → 404 Not Found
- [ ] Plan mới được tạo thành công
- [ ] Console log: `"Auto-deleted overlapping meal plan {PlanId}"`

**Lưu lại**: `{mealPlanId_new}` (plan mới thay thế)

---

### ❌ TC-MP04: Validation — DurationWeeks = 0 → 400
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 0
}
```
**Expected**: 400 Bad Request — `"Duration must be between 1 and 4 weeks."`

---

### ❌ TC-MP05: Validation — DurationWeeks = 5 → 400
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 5
}
```
**Expected**: 400 Bad Request — `"Duration must be between 1 and 4 weeks."`

---

### ❌ TC-MP06: Validation — StartDate trong quá khứ → 400
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2025-01-01",
  "durationWeeks": 1
}
```
**Expected**: 400 Bad Request — `"Start date must be today or in the future."`

---

### ❌ TC-MP07: Validation — AdditionalNotes > 500 ký tự → 400
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 1,
  "additionalNotes": "AAAA... (> 500 characters)"
}
```
**Expected**: 400 Bad Request — `"Additional notes cannot exceed 500 characters."`

---

### ❌ TC-MP08: Generate — Missing height/weight → 400
> Dùng pregnancy **chưa** có `heightCm` / `prePregnancyWeightKg` và chưa có weight log.
```
POST /api/pregnancies/{pregnancyId_no_weight}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 1
}
```
**Expected**: 400 Bad Request — `"Pre-pregnancy weight (or current weight) and height are required for calorie calculation."`

---

### ❌ TC-MP09: Rate limit exceeded → 400
> Gọi generate liên tục cho đến khi vượt 15 calls/ngày.
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-04-01",
  "durationWeeks": 4
}
```
**Expected**: 400 Bad Request (khi hết quota)
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Daily AI limit: need 4 calls, remaining 2/15. Try again tomorrow."
}
```
**Verify**:
- [ ] Message chứa số calls cần thiết (`need X calls`)
- [ ] Message chứa số remaining (`remaining Y/15`)

---

## 6️⃣ MEAL PLAN — Đọc / Danh sách / Xóa

### ✅ TC-ML01: Danh sách meal plans (paging)
```
GET /api/pregnancies/{pregnancyId}/meal-plans?page=1&pageSize=10
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "<guid>",
        "pregnancyId": "{pregnancyId}",
        "startDate": "2026-03-16",
        "endDate": "2026-03-29",
        "source": "AI",
        "title": "...",
        "totalDays": 14,
        "createdAt": "..."
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalItems": 2
  }
}
```
**Verify**:
- [ ] Paging hoạt động đúng
- [ ] `totalDays` = số ngày trong plan
- [ ] Default sort `createdat_desc` (mới nhất trước)
- [ ] Plan đã bị soft-delete KHÔNG xuất hiện

---

### ✅ TC-ML02: Sort meal plans theo startDate
```
GET /api/pregnancies/{pregnancyId}/meal-plans?sort=startdate_asc
Authorization: Bearer {token}
```
**Expected**: 200 OK — Sort theo `startDate` ascending.
**Verify**:
- [ ] Item đầu tiên có `startDate` nhỏ nhất

---

### ✅ TC-ML03: Search meal plans theo title
```
GET /api/pregnancies/{pregnancyId}/meal-plans?search={keyword_from_title}&searchBy=title
Authorization: Bearer {token}
```
**Expected**: 200 OK — Chỉ trả về plans có title chứa keyword.

---

### ✅ TC-ML04: Meal plan detail
```
GET /api/pregnancies/{pregnancyId}/meal-plans/{mealPlanId_new}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "id": "{mealPlanId_new}",
    "pregnancyId": "{pregnancyId}",
    "startDate": "2026-03-09",
    "endDate": "2026-03-15",
    "source": "AI",
    "title": "...",
    "notes": "...",
    "days": [
      {
        "id": "<guid>",
        "planDate": "2026-03-09",
        "totalCalories": 2200,
        "mealCount": 4
      }
    ],
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `days` sorted by `planDate` ascending
- [ ] Mỗi day có `totalCalories` (sum of meals) và `mealCount`
- [ ] 7 days cho 1-week plan

---

### ❌ TC-ML05: Meal plan detail — Không tồn tại → 404
```
GET /api/pregnancies/{pregnancyId}/meal-plans/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Meal plan not found."`

---

### ❌ TC-ML06: Meal plan detail — Thuộc user khác → 403
```
GET /api/pregnancies/{otherPregnancyId}/meal-plans/{planId}
Authorization: Bearer {token}
```
**Expected**: 403 Forbidden — `"Access denied."`

---

### ✅ TC-ML07: Xóa meal plan
```
DELETE /api/pregnancies/{pregnancyId}/meal-plans/{mealPlanId_2weeks}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Meal plan deleted successfully",
  "data": null
}
```
**Verify**:
- [ ] GET plan → 404 (đã soft-delete)
- [ ] GET list → plan không còn xuất hiện

---

### ❌ TC-ML08: Xóa meal plan — Không tồn tại → 404
```
DELETE /api/pregnancies/{pregnancyId}/meal-plans/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Meal plan not found."`

---

## 7️⃣ MEAL DAY DETAIL — Chi tiết bữa ăn theo ngày

### ✅ TC-DD01: Lấy chi tiết ngày (Vietnamese)
```
GET /api/meal-plans/{mealPlanId_new}/days/2026-03-09?lang=vi
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "id": "<guid>",
    "mealPlanId": "{mealPlanId_new}",
    "planDate": "2026-03-09",
    "totalCalories": 2200,
    "meals": [
      {
        "id": "<guid>",
        "mealType": "Breakfast",
        "recipeId": "<guid>",
        "itemName": "Cháo yến mạch với trứng",
        "portionText": "1 bát lớn",
        "caloriesKcal": 450,
        "notes": null,
        "nutrients": [
          {
            "nutrientCode": "PROTEIN",
            "nutrientName": "Chất đạm",
            "unit": "g",
            "amount": 15.5
          },
          {
            "nutrientCode": "IRON",
            "nutrientName": "Sắt",
            "unit": "mg",
            "amount": 3.2
          }
        ]
      },
      {
        "mealType": "Lunch",
        "itemName": "Canh chua cá lóc",
        "caloriesKcal": 550,
        "nutrients": [...]
      },
      {
        "mealType": "Dinner",
        "itemName": "...",
        "caloriesKcal": 600
      },
      {
        "mealType": "Snack",
        "itemName": "...",
        "caloriesKcal": 300
      }
    ]
  }
}
```
**Verify**:
- [ ] 4 meals: Breakfast, Lunch, Dinner, Snack
- [ ] Meals sorted by `mealType`
- [ ] Mỗi meal có `recipeId` (not null) — AI tạo recipe cho mỗi món
- [ ] `nutrients` có `nutrientCode`, `nutrientName`, `unit`, `amount`
- [ ] `nutrientName` tiếng Việt (lang=vi)
- [ ] `totalCalories` = sum of all meals' `caloriesKcal`
- [ ] Không có món chứa PEANUT hoặc SHRIMP (đã đánh dấu allergy)

---

### ✅ TC-DD02: Lấy chi tiết ngày (English)
```
GET /api/meal-plans/{mealPlanId_new}/days/2026-03-09?lang=en
Authorization: Bearer {token}
```
**Expected**: 200 OK
**Verify**:
- [ ] `nutrientName` tiếng Anh ("Protein", "Iron", "Folic Acid")

---

### ❌ TC-DD03: Day detail — Ngày không tồn tại trong plan → 404
```
GET /api/meal-plans/{mealPlanId_new}/days/2026-12-25?lang=vi
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"No meal plan data for date 2026-12-25."`

---

### ❌ TC-DD04: Day detail — Plan không tồn tại → 404
```
GET /api/meal-plans/00000000-0000-0000-0000-000000000000/days/2026-03-09
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Meal plan not found."`

---

## 8️⃣ RECIPE — Xem công thức nấu ăn

### ✅ TC-RC01: Lấy recipe detail
> Sử dụng `{recipeId}` lấy từ TC-DD01 response.
```
GET /api/recipes/{recipeId}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "id": "{recipeId}",
    "pregnancyId": "{pregnancyId}",
    "title": "Cháo yến mạch với trứng",
    "instructions": "1. Nấu yến mạch với nước...\n2. Đánh trứng...",
    "servings": 1,
    "prepMinutes": 5,
    "cookMinutes": 15,
    "createdAt": "..."
  }
}
```
**Verify**:
- [ ] `title` có giá trị
- [ ] `instructions` chứa hướng dẫn nấu
- [ ] `servings`, `prepMinutes`, `cookMinutes` là số > 0
- [ ] `pregnancyId` đúng

---

### ❌ TC-RC02: Recipe không tồn tại → 404
```
GET /api/recipes/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Recipe not found."`

---

### ❌ TC-RC03: Recipe thuộc pregnancy khác → 403
> Dùng `recipeId` thuộc pregnancy của user khác.
```
GET /api/recipes/{otherUserRecipeId}
Authorization: Bearer {token}
```
**Expected**: 403 Forbidden — `"Access denied."`

---

## 9️⃣ FEEDBACK — Đánh giá meal plan & meal item

### ✅ TC-FB01: Tạo meal plan feedback (5 sao)
```
POST /api/meal-plans/{mealPlanId_new}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 5,
  "comment": "Thực đơn rất phù hợp, đa dạng và ngon miệng!"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Feedback submitted successfully",
  "data": {
    "id": "<guid>",
    "mealPlanId": "{mealPlanId_new}",
    "userId": "<current_user_id>",
    "rating": 5,
    "comment": "Thực đơn rất phù hợp, đa dạng và ngon miệng!",
    "createdAt": "..."
  }
}
```
**Verify**:
- [ ] `rating` = 5
- [ ] `comment` đúng
- [ ] `userId` = current user

**Lưu lại**: `{feedbackId_plan}`

---

### ❌ TC-FB02: Duplicate meal plan feedback → 409
```
POST /api/meal-plans/{mealPlanId_new}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 3,
  "comment": "Muốn đánh giá lại"
}
```
**Expected**: 409 Conflict — `"You have already rated this meal plan."`

---

### ❌ TC-FB03: Validation — Rating = 0 → 400
```
POST /api/meal-plans/{mealPlanId_new}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 0
}
```
**Expected**: 400 Bad Request — `"Rating must be between 1 and 5."`

---

### ❌ TC-FB04: Validation — Rating = 6 → 400
```
POST /api/meal-plans/{mealPlanId_new}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 6
}
```
**Expected**: 400 Bad Request — `"Rating must be between 1 and 5."`

---

### ❌ TC-FB05: Validation — Comment > 500 ký tự → 400
```
POST /api/meal-plans/{mealPlanId_new}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 4,
  "comment": "AAAA... (> 500 characters)"
}
```
**Expected**: 400 Bad Request — `"Comment cannot exceed 500 characters."`

---

### ❌ TC-FB06: Feedback — Plan không tồn tại → 404
```
POST /api/meal-plans/00000000-0000-0000-0000-000000000000/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "rating": 5
}
```
**Expected**: 404 Not Found — `"Meal plan not found."`

---

### ✅ TC-FB07: Tạo meal item feedback — Like
> Sử dụng `{mealItemId}` lấy từ TC-DD01 response (meals[0].id).
```
POST /api/meal-items/{mealItemId}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": true,
  "comment": "Món này rất ngon và dễ nấu"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Feedback submitted successfully",
  "data": {
    "id": "<guid>",
    "mealItemId": "{mealItemId}",
    "userId": "<current_user_id>",
    "liked": true,
    "comment": "Món này rất ngon và dễ nấu",
    "createdAt": "..."
  }
}
```
**Verify**:
- [ ] `liked` = true
- [ ] `mealItemId` đúng

---

### ✅ TC-FB08: Tạo meal item feedback — Dislike
> Sử dụng mealItemId khác (meals[1].id).
```
POST /api/meal-items/{mealItemId_2}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": false,
  "comment": "Không hợp khẩu vị"
}
```
**Expected**: 201 Created
**Verify**:
- [ ] `liked` = false

---

### ❌ TC-FB09: Duplicate meal item feedback → 409
```
POST /api/meal-items/{mealItemId}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": false,
  "comment": "Thay đổi ý kiến"
}
```
**Expected**: 409 Conflict — `"You have already rated this meal item."`

---

### ❌ TC-FB10: Validation — Comment > 300 ký tự → 400
```
POST /api/meal-items/{mealItemId}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": true,
  "comment": "AAAA... (> 300 characters)"
}
```
**Expected**: 400 Bad Request — `"Comment cannot exceed 300 characters."`

---

### ❌ TC-FB11: Feedback — Meal item không tồn tại → 404
```
POST /api/meal-items/00000000-0000-0000-0000-000000000000/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": true
}
```
**Expected**: 404 Not Found — `"Meal item not found."`

---

### ✅ TC-FB12: Feedback — Comment là optional
```
POST /api/meal-items/{mealItemId_3}/feedback
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "liked": true
}
```
**Expected**: 201 Created — `comment` = `null`

---

## 🔟 AUTHORIZATION — Kiểm tra quyền truy cập

### ❌ TC-A01: Không có token → 401 cho tất cả endpoints
```
POST /api/pregnancies/{pregnancyId}/food-preferences
(No Authorization header)
```
**Expected**: 401 Unauthorized

---

### ❌ TC-A02: User không có permission → 403
> Dùng user/role **không** được gán `food_preference.write`.
```
POST /api/pregnancies/{pregnancyId}/food-preferences
Authorization: Bearer {token_no_permission}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010001-0000-0000-0000-000000000001",
  "preferenceType": "Dislike"
}
```
**Expected**: 403 Forbidden

---

### ❌ TC-A03: DOCTOR không được generate meal plan
> DOCTOR có 11 nutrition permissions nhưng **KHÔNG** có `meal_plan.generate`.
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
Authorization: Bearer {doctor_token}
Content-Type: application/json
```
```json
{
  "startDate": "2026-03-09",
  "durationWeeks": 1
}
```
**Expected**: 403 Forbidden — DOCTOR **chỉ** có thể xem (read), không generate.

---

### ✅ TC-A04: DOCTOR có thể đọc meal plans
```
GET /api/pregnancies/{pregnancyId}/meal-plans
Authorization: Bearer {doctor_token}
```
**Expected**: 200 OK — DOCTOR có `meal_plan.read` permission.

---

## 1️⃣1️⃣ EDGE CASES — Các trường hợp biên

### ✅ TC-E01: Unicode Vietnamese — Food preference notes
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "foodItemId": "c7010004-0000-0000-0000-000000000001",
  "preferenceType": "Dislike",
  "notes": "Không thích mùi sầu riêng 🤢, nhất là khi mang thai"
}
```
**Expected**: 201 Created — Unicode + emoji được lưu đúng.
**Verify**:
- [ ] GET lại → `notes` vẫn đầy đủ emoji + Vietnamese

---

### ✅ TC-E02: Tạo preference cho nhiều loại thực phẩm
> Tạo nhiều preferences (Allergy + Dislike) và verify AI meal plan tránh tất cả.
```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
```
Tạo thêm:
1. Allergy: SHELLFISH (`c7010002-0000-0000-0000-000000000007`), severity High
2. Dislike: BITTER_MELON (`c7010003-0000-0000-0000-000000000002`)

Rồi:
```
POST /api/pregnancies/{pregnancyId}/meal-plans/generate
```
```json
{
  "startDate": "2026-04-06",
  "durationWeeks": 1
}
```
**Expected**: Meal plan KHÔNG chứa peanut, shrimp, shellfish (Allergy). Có thể chứa bitter melon (Dislike là suggestion, AI có thể tránh hoặc không).

---

### ✅ TC-E03: Nutrition notes ảnh hưởng AI context
> Tạo nutrition note trước khi generate:
```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
```
```json
{
  "noteType": "Diet",
  "valueText": "Ăn chay, không ăn thịt đỏ (bò, heo)"
}
```
Rồi generate meal plan → AI nên tránh thịt bò, heo.

---

### ✅ TC-E04: Empty lists — Pregnancy chưa có food preference
```
GET /api/pregnancies/{pregnancyId_clean}/food-preferences?lang=vi
Authorization: Bearer {token}
```
**Expected**: 200 OK — `data: []` (empty array, không lỗi)

---

### ✅ TC-E05: Empty lists — Pregnancy chưa có nutrition notes
```
GET /api/pregnancies/{pregnancyId_clean}/nutrition-notes
Authorization: Bearer {token}
```
**Expected**: 200 OK — `data: []` (empty array)

---

### ✅ TC-E06: Empty lists — Pregnancy chưa có meal plans
```
GET /api/pregnancies/{pregnancyId_clean}/meal-plans
Authorization: Bearer {token}
```
**Expected**: 200 OK — `data: { items: [], page: 1, pageSize: 10, totalItems: 0 }`

---

## 📊 BẢNG TỔNG HỢP TEST CASES

| Section | Nhóm | ✅ Happy | ❌ Error | Tổng |
|---------|------|----------|---------|------|
| 0 | Pre-test (startup, enums) | 3 | 0 | 3 |
| 1 | Reference Data (food items, nutrients) | 4 | 0 | 4 |
| 2 | Food Preferences (CRUD) | 11 | 8 | 19 |
| 3 | Nutrition Notes (CRUD) | 7 | 5 | 12 |
| 4 | Query Specs | 1 | 0 | 1 |
| 5 | Meal Plan Generate (AI) | 3 | 6 | 9 |
| 6 | Meal Plan List/Detail/Delete | 5 | 3 | 8 |
| 7 | Meal Day Detail | 2 | 2 | 4 |
| 8 | Recipe Detail | 1 | 2 | 3 |
| 9 | Feedback (Plan + Item) | 4 | 8 | 12 |
| 10 | Authorization | 1 | 3 | 4 |
| 11 | Edge Cases | 6 | 0 | 6 |
| **TỔNG** | | **48** | **37** | **85** |

---

## 📌 PERMISSION MATRIX — Week 7

| Permission | USER | DOCTOR | Endpoints |
|-----------|------|--------|-----------|
| `food_preference.read` | ✅ | ✅ | GET food-preferences |
| `food_preference.write` | ✅ | ✅ | POST + PUT food-preferences |
| `food_preference.delete` | ✅ | ✅ | DELETE food-preferences |
| `nutrition_note.read` | ✅ | ✅ | GET nutrition-notes |
| `nutrition_note.write` | ✅ | ✅ | POST + PUT nutrition-notes |
| `nutrition_note.delete` | ✅ | ✅ | DELETE nutrition-notes |
| `meal_plan.read` | ✅ | ✅ | GET meal-plans (list, detail, day-detail) |
| `meal_plan.generate` | ✅ | ❌ | POST meal-plans/generate |
| `meal_plan.delete` | ✅ | ✅ | DELETE meal-plans |
| `recipe.read` | ✅ | ✅ | GET recipes |
| `meal_plan_feedback.write` | ✅ | ✅ | POST meal-plans/{id}/feedback |
| `meal_item_feedback.write` | ✅ | ✅ | POST meal-items/{id}/feedback |

> **Lưu ý**: DOCTOR **KHÔNG** có `meal_plan.generate` — chỉ có thể xem, không generate.

---

## 📌 SEEDED DATA — Tham khảo

### Food Items (45 items, 6 categories)
| Category | Items | Example Codes |
|----------|-------|---------------|
| Proteins (13) | Thịt, cá, hải sản, đậu phụ | CHICKEN, PORK, BEEF, FISH_SALMON, SHRIMP, TOFU |
| Allergens (8) | Dị ứng phổ biến | SEAFOOD_GENERAL, PEANUT, MILK_COW, GLUTEN |
| Vegetables (9) | Rau củ | CILANTRO, BITTER_MELON, SPINACH, BOK_CHOY |
| Fruits (4) | Trái cây | DURIAN, JACKFRUIT, PINEAPPLE, PAPAYA_GREEN |
| Condiments (7) | Gia vị, nội tạng | SHRIMP_PASTE, MSG, ORGAN_MEAT_LIVER, CAFFEINE |
| Pregnancy-avoid (4) | Thực phẩm cần tránh | RAW_FISH, SOFT_CHEESE, RAW_EGG, DELI_MEAT |

### Nutrients (15)
| Code | Vietnamese | Unit |
|------|-----------|------|
| PROTEIN | Chất đạm | g |
| FAT | Chất béo | g |
| CARBS | Carbohydrate | g |
| FIBER | Chất xơ | g |
| IRON | Sắt | mg |
| CALCIUM | Canxi | mg |
| FOLIC_ACID | Acid folic | µg |
| VITAMIN_A | Vitamin A | µg |
| VITAMIN_C | Vitamin C | mg |
| VITAMIN_D | Vitamin D | µg |
| ZINC | Kẽm | mg |
| OMEGA3 | Omega-3 | mg |
| IODINE | I-ốt | µg |
| DHA | DHA | mg |
| MAGNESIUM | Magiê | mg |

---

## ✅ COMPLETION CHECKLIST

Khi hoàn thành test, đánh dấu:
- [ ] Tất cả 48 happy-path tests PASS
- [ ] Tất cả 37 error-path tests PASS
- [ ] Reference data endpoints trả về đúng 45 food items + 15 nutrients
- [ ] Food preferences CRUD hoàn chỉnh (create, read, update, delete, restore)
- [ ] Nutrition notes CRUD hoàn chỉnh
- [ ] AI meal plan generate hoạt động (1-4 tuần)
- [ ] Overlap auto-delete hoạt động
- [ ] Rate limiting hoạt động đúng (15 calls/day)
- [ ] Day detail trả đầy đủ meals + nutrients + recipes
- [ ] Recipe detail trả đầy đủ instructions
- [ ] Feedback unique constraint hoạt động (1 feedback/user/plan, 1 feedback/user/item)
- [ ] Soft-delete + restore hoạt động cho food preferences
- [ ] DOCTOR không thể generate (403), chỉ read
- [ ] Unicode tiếng Việt lưu và hiển thị đúng
- [ ] Paging, sorting, searching hoạt động cho meal plans
