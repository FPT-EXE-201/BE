# Nutrition & Meal Planning — Hướng dẫn API cho Frontend

> **Mục đích**: Hướng dẫn Flutter/FE team nối API cho tính năng **Nutrition & Meal Planning** (khai báo dị ứng/sở thích thực phẩm, AI generate thực đơn, xem chi tiết ngày/món/recipe, đánh giá feedback).  
> **Base URL**: `https://{domain}/api`  
> **Auth**: Tất cả API (trừ ref data) yêu cầu header `Authorization: Bearer {accessToken}`  
> **Cập nhật**: 2026-03-11

---

## MỤC LỤC

1. [Tổng quan luồng](#1-tổng-quan-luồng)
2. [Enums & Reference Data](#2-enums--reference-data)
3. [API: Food Preferences (Dị ứng / Không thích)](#3-api-food-preferences)
4. [API: Nutrition Notes (Ghi chú dinh dưỡng)](#4-api-nutrition-notes)
5. [API: Meal Plan — Generate (AI)](#5-api-meal-plan--generate-ai)
6. [API: Meal Plan — List / Detail / Delete](#6-api-meal-plan--list--detail--delete)
7. [API: Day Detail (Chi tiết 1 ngày)](#7-api-day-detail)
8. [API: Recipe Detail (Công thức nấu ăn)](#8-api-recipe-detail)
9. [API: Feedback (Đánh giá)](#9-api-feedback)
10. [DTOs — Chi tiết tất cả fields](#10-dtos--chi-tiết-tất-cả-fields)
11. [Luồng tích hợp từng bước](#11-luồng-tích-hợp-từng-bước)
12. [Search / Sort / Paging cho Meal Plans](#12-search--sort--paging-cho-meal-plans)
13. [Response Format chuẩn](#13-response-format-chuẩn)
14. [Error Handling](#14-error-handling)

---

## 1. TỔNG QUAN LUỒNG

Tính năng Nutrition & Meal Planning có **3 giai đoạn** (phases):

```
 ┌──────────────────────────────────────────────────────────────────────┐
 │              PHASE 1: SETUP — Khai báo sở thích dinh dưỡng          │
 │                                                                      │
 │  1. GET /api/ref/food-items?lang=vi      → Lấy 45 thực phẩm        │
 │  2. POST .../food-preferences            → Chọn dị ứng / không thích│
 │  3. POST .../nutrition-notes             → Ghi chú chế độ ăn        │
 │                                                                      │
 │  ✅ Setup xong — sẵn sàng generate AI                               │
 └──────────────────────────────────────────┬───────────────────────────┘
                                            │
 ┌──────────────────────────────────────────▼───────────────────────────┐
 │         PHASE 2: AI GENERATE — Tạo thực đơn (Background Job)        │
 │                                                                      │
 │  1. POST .../meal-plans/generate         → Queue job (202 Accepted)  │
 │  2. GET /api/meal-plans/{id}/status      → Poll mỗi 3-5s            │
 │     Status: Pending → Generating → Succeeded ✅ / Failed ❌          │
 │                                                                      │
 │  ⏱️ Background: 15-60 giây mỗi tuần (1 tuần = 1 AI call)           │
 └──────────────────────────────────────────┬───────────────────────────┘
                                            │
 ┌──────────────────────────────────────────▼───────────────────────────┐
 │           PHASE 3: CONSUME — Xem / Đánh giá / Xóa                   │
 │                                                                      │
 │  1. GET .../meal-plans                   → List thực đơn (paging)    │
 │  2. GET .../meal-plans/{planId}          → Chi tiết plan (days)      │
 │  3. GET /api/meal-plans/{id}/days/{date} → Chi tiết 1 ngày (meals)  │
 │  4. GET /api/recipes/{recipeId}          → Công thức nấu ăn         │
 │  5. POST /api/meal-plans/{id}/feedback   → Đánh giá plan (1-5 sao)  │
 │  6. POST /api/meal-items/{id}/feedback   → Like/dislike món ăn      │
 │  7. DELETE .../meal-plans/{planId}       → Xóa plan (soft delete)   │
 └──────────────────────────────────────────────────────────────────────┘
```

### Tóm tắt tổng các API

| Nhóm | Endpoints | Auth |
|------|:---------:|:----:|
| Reference Data (food-items, nutrients, enums) | 5 | ❌ Public |
| Food Preferences CRUD | 4 | ✅ |
| Nutrition Notes CRUD | 4 | ✅ |
| Meal Plan (generate, status, list, detail, delete) | 5 | ✅ |
| Day Detail | 1 | ✅ |
| Recipe Detail | 1 | ✅ |
| Feedback (plan + item) | 2 | ✅ |
| **Tổng** | **22** | |

---

## 2. ENUMS & REFERENCE DATA

### 2.1 Enums quan trọng

#### MealPlanStatus — Trạng thái generate AI

```
Pending      → Đang chờ xử lý (vừa queue)
Generating   → AI đang tạo thực đơn (tuần theo tuần)
Succeeded    → Hoàn tất ✅ (FE có thể xem chi tiết)
Failed       → Thất bại ❌ (hiện lỗi + cho thử lại)
```

**FE cần xử lý:**
- `Pending`, `Generating` → Hiện loading spinner + progress (completedWeeks/totalWeeks), tiếp tục polling
- `Succeeded` → Dừng polling, navigate sang màn chi tiết plan
- `Failed` → Dừng polling, hiện `errorMessage` + nút "Thử lại"

#### FoodPreferenceType — Loại sở thích thực phẩm

```
Allergy    → Dị ứng (AI PHẢI tránh hoàn toàn)
Dislike    → Không thích (AI nên tránh nhưng có thể thay thế)
```

#### AllergySeverity — Mức độ dị ứng (chỉ dùng khi PreferenceType = Allergy)

```
Low        → Nhẹ (ngứa, phát ban nhẹ)
Medium     → Trung bình (sưng, nổi mề đay)
High       → Nặng (sốc phản vệ, nguy hiểm tính mạng)
```

#### NutritionNoteType — Loại ghi chú dinh dưỡng

```
Diet       → Chế độ ăn (ăn chay, keto, low-carb...)
Note       → Ghi chú y tế (bác sĩ khuyên tránh/bổ sung gì)
Other      → Khác (sở thích cá nhân)
```

#### MealType — Loại bữa ăn

```
Breakfast  → Sáng
Lunch      → Trưa
Dinner     → Tối
Snack      → Ăn nhẹ / Bữa phụ
```

**⚠️ Thứ tự hiển thị:** FE nên sort theo thứ tự trên (Breakfast → Lunch → Dinner → Snack).

#### MealPlanSource — Nguồn tạo plan

```
AI         → AI-generated (Gemini) — hiện tại chỉ có loại này
Manual     → User tạo tay (future, chưa implement)
```

### 2.2 API Lấy danh sách Food Items (PUBLIC — không cần auth)

```
GET /api/ref/food-items?lang=vi
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
    { "id": "f0000001-...", "code": "CHICKEN", "displayName": "Thịt gà" },
    { "id": "f0000002-...", "code": "PEANUT", "displayName": "Đậu phộng" },
    { "id": "f0000003-...", "code": "SHRIMP", "displayName": "Tôm" },
    { "id": "f0000004-...", "code": "DURIAN", "displayName": "Sầu riêng" }
  ]
}
```

**FE sử dụng:**
- Cache danh sách này (ít thay đổi)
- Dùng `id` khi tạo food preference (`foodItemId`)
- Dùng `displayName` để hiển thị tên thực phẩm cho user chọn
- Render UI dạng picker / multi-select

**45 food items — 6 danh mục:**

| Danh mục | Số lượng | Ví dụ codes |
|----------|:--------:|-------------|
| Proteins | 13 | `CHICKEN`, `PORK`, `BEEF`, `FISH_SALMON`, `SHRIMP`, `TOFU`, `TEMPEH` |
| Allergens | 8 | `SEAFOOD_GENERAL`, `PEANUT`, `MILK_COW`, `GLUTEN`, `SOYBEAN` |
| Vegetables | 9 | `CILANTRO`, `BITTER_MELON`, `SPINACH`, `BOK_CHOY`, `GARLIC` |
| Fruits | 4 | `DURIAN`, `JACKFRUIT`, `PINEAPPLE`, `PAPAYA_GREEN` |
| Condiments | 7 | `SHRIMP_PASTE`, `MSG`, `ORGAN_MEAT_LIVER`, `CAFFEINE`, `ALCOHOL` |
| Pregnancy-avoid | 4 | `RAW_FISH`, `SOFT_CHEESE`, `RAW_EGG`, `DELI_MEAT` |

### 2.3 API Lấy danh sách Nutrients (PUBLIC)

```
GET /api/ref/nutrients?lang=vi
```

**Response:**
```json
{
  "success": true,
  "data": [
    { "id": "n0000001-...", "code": "PROTEIN", "unit": "g", "displayName": "Chất đạm" },
    { "id": "n0000002-...", "code": "IRON", "unit": "mg", "displayName": "Sắt" },
    { "id": "n0000003-...", "code": "CALCIUM", "unit": "mg", "displayName": "Canxi" },
    { "id": "n0000004-...", "code": "FOLIC_ACID", "unit": "µg", "displayName": "Acid folic" }
  ]
}
```

**15 nutrients:**

| Code | Tiếng Việt | Đơn vị |
|------|------------|:------:|
| `PROTEIN` | Chất đạm | g |
| `FAT` | Chất béo | g |
| `CARBS` | Carbohydrate | g |
| `FIBER` | Chất xơ | g |
| `IRON` | Sắt | mg |
| `CALCIUM` | Canxi | mg |
| `FOLIC_ACID` | Acid folic | µg |
| `VITAMIN_A` | Vitamin A | µg |
| `VITAMIN_C` | Vitamin C | mg |
| `VITAMIN_D` | Vitamin D | µg |
| `ZINC` | Kẽm | mg |
| `OMEGA3` | Omega-3 | mg |
| `IODINE` | I-ốt | µg |
| `DHA` | DHA | mg |
| `MAGNESIUM` | Magiê | mg |

**FE sử dụng:** Hiển thị bảng dinh dưỡng cho mỗi món ăn (trong Day Detail). Nutrient name + unit đã localized theo `lang`.

### 2.4 API Lấy Enum values

```
GET /api/ref/enums                          → Tất cả enums (dict)
GET /api/ref/enums/mealPlanStatus           → 1 enum cụ thể
GET /api/ref/enums/foodPreferenceType
GET /api/ref/enums/allergySeverity
GET /api/ref/enums/mealType
GET /api/ref/enums/nutritionNoteType
GET /api/ref/enums/mealPlanSource
```

**Response (ví dụ `/api/ref/enums/mealPlanStatus`):**
```json
{
  "success": true,
  "data": [
    { "value": 0, "name": "Pending" },
    { "value": 1, "name": "Generating" },
    { "value": 2, "name": "Succeeded" },
    { "value": 3, "name": "Failed" }
  ]
}
```

> **Lưu ý:** BE trả enum dạng **string name** trong response DTOs (`"Pending"`, `"Breakfast"`, `"Allergy"`...), KHÔNG trả int value. FE gửi request body cũng dùng **string name** hoặc **int value** đều được (JSON converter hỗ trợ cả hai).

### 2.5 API Lấy Query Specs (cho Search/Sort metadata)

```
GET /api/ref/query-specs/mealPlans
```

**Response:**
```json
{
  "success": true,
  "data": {
    "searchableFields": ["title", "notes"],
    "defaultSearchFields": ["title"],
    "sortableFields": ["startdate", "enddate", "createdat"],
    "defaultSortBy": "createdat",
    "defaultSortDir": "desc"
  }
}
```

---

## 3. API: FOOD PREFERENCES

### 3.1 Danh sách sở thích / dị ứng

```
GET /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `pregnancyId` | Guid | (path) | ID thai kỳ |
| `lang` | string | `"vi"` | Ngôn ngữ hiển thị tên thực phẩm |

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "pref-001-...",
      "pregnancyId": "a0000001-...",
      "foodItemId": "f0000002-...",
      "foodItemCode": "PEANUT",
      "foodItemDisplayName": "Đậu phộng",
      "preferenceType": "Allergy",
      "severity": "High",
      "notes": "Sốc phản vệ, tuyệt đối không dùng",
      "createdAt": "2026-03-10T10:00:00Z",
      "updatedAt": "2026-03-10T10:00:00Z"
    },
    {
      "id": "pref-002-...",
      "pregnancyId": "a0000001-...",
      "foodItemId": "f0000003-...",
      "foodItemCode": "CILANTRO",
      "foodItemDisplayName": "Rau mùi",
      "preferenceType": "Dislike",
      "severity": null,
      "notes": null,
      "createdAt": "2026-03-10T10:01:00Z",
      "updatedAt": "2026-03-10T10:01:00Z"
    }
  ]
}
```

**DTO: `FoodPreferenceDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID preference |
| `pregnancyId` | Guid | ID thai kỳ |
| `foodItemId` | Guid | ID thực phẩm |
| `foodItemCode` | string | Code thực phẩm (VD: `"PEANUT"`) |
| `foodItemDisplayName` | string | Tên hiển thị (localized theo `lang`) |
| `preferenceType` | string | `"Allergy"` hoặc `"Dislike"` |
| `severity` | string? | `"Low"` / `"Medium"` / `"High"` — null nếu Dislike |
| `notes` | string? | Ghi chú |
| `createdAt` | DateTime | Ngày tạo |
| `updatedAt` | DateTime | Ngày cập nhật |

---

### 3.2 Tạo sở thích / dị ứng mới

```
POST /api/pregnancies/{pregnancyId}/food-preferences?lang=vi
```

**Body (JSON):**
```json
{
  "foodItemId": "f0000002-0000-0000-0000-000000000000",
  "preferenceType": "Allergy",
  "severity": "High",
  "notes": "Sốc phản vệ"
}
```

**DTO: `CreateFoodPreferenceDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `foodItemId` | Guid | ✅ | ID thực phẩm (lấy từ `/api/ref/food-items`) |
| `preferenceType` | string | ✅ | `"Allergy"` hoặc `"Dislike"` |
| `severity` | string? | ❌ | Mức độ dị ứng: `"Low"` / `"Medium"` / `"High"`. **Chỉ dùng khi `preferenceType` = `"Allergy"`** |
| `notes` | string? | ❌ | Ghi chú tùy chọn |

**Response (201 Created):** `FoodPreferenceDto` (cùng format ở trên)

**⚠️ Business Rules:**
- **Unique constraint**: 1 preference per (pregnancy, food_item, preference_type). Nếu đã tồn tại → **409 Conflict**
- **Soft-delete restore**: Nếu đã từng tạo rồi xóa → BE tự restore (không cần FE xử lý)
- `severity` chỉ có ý nghĩa khi `preferenceType = "Allergy"`. Nếu `Dislike` → gửi `severity: null`

---

### 3.3 Cập nhật sở thích

```
PUT /api/pregnancies/{pregnancyId}/food-preferences/{prefId}?lang=vi
```

**Body (JSON):**
```json
{
  "severity": "Medium",
  "notes": "Đã giảm mức độ sau điều trị"
}
```

**DTO: `UpdateFoodPreferenceDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `severity` | string? | ❌ | Cập nhật mức độ dị ứng |
| `notes` | string? | ❌ | Cập nhật ghi chú |

**Response:** `FoodPreferenceDto`

> **Lưu ý:** Chỉ update `severity` và `notes`. KHÔNG thể đổi `foodItemId` hoặc `preferenceType` — cần xóa rồi tạo mới.

---

### 3.4 Xóa sở thích

```
DELETE /api/pregnancies/{pregnancyId}/food-preferences/{prefId}
```

**Response:**
```json
{
  "success": true,
  "message": "Food preference deleted successfully",
  "data": null
}
```

Soft delete — preference bị ẩn khỏi danh sách. Nếu FE tạo lại cùng (foodItem + preferenceType) → BE tự restore.

---

## 4. API: NUTRITION NOTES

### 4.1 Danh sách ghi chú dinh dưỡng

```
GET /api/pregnancies/{pregnancyId}/nutrition-notes
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "note-001-...",
      "pregnancyId": "a0000001-...",
      "noteType": "Diet",
      "valueText": "Ăn chay linh hoạt - không thịt đỏ, có cá và trứng",
      "createdAt": "2026-03-10T10:05:00Z",
      "updatedAt": "2026-03-10T10:05:00Z"
    },
    {
      "id": "note-002-...",
      "pregnancyId": "a0000001-...",
      "noteType": "Note",
      "valueText": "Bác sĩ khuyên bổ sung thêm sắt và acid folic giai đoạn tam cá nguyệt 2",
      "createdAt": "2026-03-10T10:06:00Z",
      "updatedAt": "2026-03-10T10:06:00Z"
    }
  ]
}
```

**DTO: `NutritionNoteDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID ghi chú |
| `pregnancyId` | Guid | ID thai kỳ |
| `noteType` | string | `"Diet"` / `"Note"` / `"Other"` |
| `valueText` | string | Nội dung ghi chú |
| `createdAt` | DateTime | Ngày tạo |
| `updatedAt` | DateTime | Ngày cập nhật |

---

### 4.2 Tạo ghi chú mới

```
POST /api/pregnancies/{pregnancyId}/nutrition-notes
```

**Body (JSON):**
```json
{
  "noteType": "Diet",
  "valueText": "Ăn chay linh hoạt - không thịt đỏ, có cá và trứng"
}
```

**DTO: `CreateNutritionNoteDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `noteType` | string | ✅ | `"Diet"` / `"Note"` / `"Other"` |
| `valueText` | string | ✅ | Nội dung ghi chú |

**Response (201 Created):** `NutritionNoteDto`

> **Không có unique constraint** — user có thể tạo nhiều notes cùng loại.

---

### 4.3 Cập nhật ghi chú

```
PUT /api/pregnancies/{pregnancyId}/nutrition-notes/{noteId}
```

**Body (JSON):**
```json
{
  "noteType": "Diet",
  "valueText": "Ăn chay hoàn toàn - không sản phẩm động vật"
}
```

**DTO: `UpdateNutritionNoteDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `noteType` | string? | ❌ | Đổi loại (null = giữ nguyên) |
| `valueText` | string? | ❌ | Cập nhật nội dung (null = giữ nguyên) |

**Response:** `NutritionNoteDto`

---

### 4.4 Xóa ghi chú

```
DELETE /api/pregnancies/{pregnancyId}/nutrition-notes/{noteId}
```

**Response:** `data` = null, `message` = "Nutrition note deleted successfully"

---

## 5. API: MEAL PLAN — GENERATE (AI)

### `POST /api/pregnancies/{pregnancyId}/meal-plans/generate`

Queue AI meal plan generation. Trả về **202 Accepted** ngay lập tức. AI chạy background.

**Body (JSON):**
```json
{
  "startDate": "2026-03-11",
  "durationWeeks": 2,
  "additionalNotes": "Ưu tiên món Việt Nam, đa dạng nguyên liệu"
}
```

**DTO: `GenerateMealPlanDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `startDate` | DateOnly | ✅ | Ngày bắt đầu (format: `yyyy-MM-dd`). **Phải ≥ ngày hôm nay** |
| `durationWeeks` | int | ✅ | Số tuần: **1 đến 4** |
| `additionalNotes` | string? | ❌ | Yêu cầu thêm cho AI (VD: "Ưu tiên món Việt Nam") |

**Response (202 Accepted):**
```json
{
  "success": true,
  "statusCode": 202,
  "message": "Meal plan generation queued. Poll /status for progress.",
  "data": {
    "id": "mp-001-...",
    "pregnancyId": "a0000001-...",
    "status": "Pending",
    "completedWeeks": 0,
    "totalWeeks": 2,
    "title": null,
    "errorMessage": null,
    "createdAt": "2026-03-11T08:00:00Z"
  }
}
```

**⚠️ Sau khi gọi thành công:**
- Lưu `data.id` (mealPlanId) để polling status
- Bắt đầu polling ngay (bước tiếp theo)

**⚠️ Business Rules:**
- **Rate limit**: 15 AI calls/ngày/user. Mỗi tuần = 1 AI call → 4 tuần = 4 calls. Nếu vượt → 429 Too Many Requests
- **Overlap auto-delete**: Nếu plan mới trùng date range với plan cũ → plan cũ tự động bị soft-delete
- **BMI required**: User phải có pre-pregnancy weight (hoặc latest weight log) + height trong profile. Nếu thiếu → 400 Bad Request
- **startDate validation**: Phải ≥ ngày hôm nay. Nếu quá khứ → 400 validation error

---

### Polling trạng thái

```
GET /api/meal-plans/{planId}/status
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "mp-001-...",
    "pregnancyId": "a0000001-...",
    "status": "Generating",
    "completedWeeks": 1,
    "totalWeeks": 2,
    "title": null,
    "errorMessage": null,
    "createdAt": "2026-03-11T08:00:00Z"
  }
}
```

**DTO: `MealPlanStatusDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID meal plan |
| `pregnancyId` | Guid | ID thai kỳ |
| `status` | string | `"Pending"` / `"Generating"` / `"Succeeded"` / `"Failed"` |
| `completedWeeks` | int | Số tuần đã generate xong (0 → totalWeeks) |
| `totalWeeks` | int | Tổng số tuần yêu cầu |
| `title` | string? | Tiêu đề plan (AI tạo sau khi xong) |
| `errorMessage` | string? | Lỗi nếu Failed |
| `createdAt` | DateTime | Ngày tạo |

**Polling logic (Flutter pseudo-code):**
```dart
Timer.periodic(Duration(seconds: 4), (timer) async {
  final response = await dio.get('/api/meal-plans/$planId/status');
  final data = response.data['data'];
  final status = data['status'];

  switch (status) {
    case 'Pending':
      updateUI('Đang chuẩn bị tạo thực đơn...');
      break;
    case 'Generating':
      final completed = data['completedWeeks'];
      final total = data['totalWeeks'];
      updateUI('Đang tạo thực đơn... ($completed/$total tuần)');
      // Hiện progress bar: completed / total
      break;
    case 'Succeeded':
      timer.cancel();
      navigateToMealPlanDetail(data['id']);
      break;
    case 'Failed':
      timer.cancel();
      showError(data['errorMessage'] ?? 'Tạo thực đơn thất bại');
      break;
  }
});
```

**Loading message gợi ý:**

| Status | completedWeeks | Message |
|--------|:--------------:|---------|
| `Pending` | 0 | "Đang chuẩn bị tạo thực đơn..." |
| `Generating` | 0/2 | "Đang tạo thực đơn tuần 1..." |
| `Generating` | 1/2 | "Đang tạo thực đơn tuần 2..." |
| `Succeeded` | 2/2 | "Hoàn tất! Xem thực đơn..." |
| `Failed` | — | "Tạo thực đơn thất bại. Thử lại?" |

---

## 6. API: MEAL PLAN — LIST / DETAIL / DELETE

### 6.1 Danh sách meal plans (có Search/Sort/Paging)

```
GET /api/pregnancies/{pregnancyId}/meal-plans?page=1&pageSize=10&search=tuần 28&sortBy=startdate&sortDir=desc
```

| Query Param | Type | Default | Mô tả |
|-------------|------|---------|-------|
| `page` | int | `1` | Trang (1-based) |
| `pageSize` | int | `20` | Số items/trang (max 100) |
| `search` | string? | null | Từ khóa tìm kiếm |
| `searchIn` | string? | `"title"` | Fields tìm kiếm, phân cách bằng dấu `,` (VD: `"title,notes"`) |
| `sortBy` | string? | `"createdat"` | Field sort: `startdate`, `enddate`, `createdat` |
| `sortDir` | string? | `"desc"` | `"asc"` hoặc `"desc"` |

**Response (paged):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "mp-001-...",
        "pregnancyId": "a0000001-...",
        "startDate": "2026-03-11",
        "endDate": "2026-03-24",
        "source": "AI",
        "status": "Succeeded",
        "title": "Thực đơn dinh dưỡng tam cá nguyệt 2",
        "totalDays": 14,
        "createdAt": "2026-03-11T08:00:00Z"
      }
    ],
    "totalCount": 3,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

**DTO: `MealPlanSummaryDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID meal plan |
| `pregnancyId` | Guid | ID thai kỳ |
| `startDate` | DateOnly | Ngày bắt đầu |
| `endDate` | DateOnly | Ngày kết thúc |
| `source` | string | `"AI"` hoặc `"Manual"` |
| `status` | string | `"Pending"` / `"Generating"` / `"Succeeded"` / `"Failed"` |
| `title` | string? | Tiêu đề (AI tạo) |
| `totalDays` | int | Tổng số ngày |
| `createdAt` | DateTime | Ngày tạo |

---

### 6.2 Chi tiết 1 meal plan

```
GET /api/pregnancies/{pregnancyId}/meal-plans/{planId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "mp-001-...",
    "pregnancyId": "a0000001-...",
    "startDate": "2026-03-11",
    "endDate": "2026-03-24",
    "source": "AI",
    "title": "Thực đơn dinh dưỡng tam cá nguyệt 2",
    "notes": "Thực đơn phù hợp cho mẹ bầu tuần 24, BMI 22.5",
    "days": [
      {
        "id": "day-001-...",
        "planDate": "2026-03-11",
        "totalCalories": 2540,
        "mealCount": 4
      },
      {
        "id": "day-002-...",
        "planDate": "2026-03-12",
        "totalCalories": 2480,
        "mealCount": 4
      }
    ],
    "createdAt": "2026-03-11T08:00:00Z",
    "updatedAt": "2026-03-11T08:05:00Z"
  }
}
```

**DTO: `MealPlanDetailDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID meal plan |
| `pregnancyId` | Guid | ID thai kỳ |
| `startDate` | DateOnly | Ngày bắt đầu |
| `endDate` | DateOnly | Ngày kết thúc |
| `source` | string | `"AI"` / `"Manual"` |
| `title` | string? | Tiêu đề plan |
| `notes` | string? | Ghi chú tổng plan |
| `days` | MealPlanDaySummaryDto[] | Danh sách ngày (sorted by planDate ascending) |
| `createdAt` | DateTime | Ngày tạo |
| `updatedAt` | DateTime | Ngày cập nhật |

**DTO: `MealPlanDaySummaryDto`** (nested trong `days[]`)

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID ngày |
| `planDate` | DateOnly | Ngày (yyyy-MM-dd) |
| `totalCalories` | int | Tổng calo ngày = SUM(items.caloriesKcal) |
| `mealCount` | int | Số món ăn = COUNT(items) |

**FE hiển thị:** Render danh sách ngày dạng list/calendar. Mỗi ngày hiện `planDate`, `totalCalories` kcal, `mealCount` bữa. Tap vào ngày → gọi Day Detail API.

---

### 6.3 Xóa meal plan

```
DELETE /api/pregnancies/{pregnancyId}/meal-plans/{planId}
```

**Response:**
```json
{
  "success": true,
  "message": "Meal plan deleted successfully",
  "data": null
}
```

**⚠️ Lưu ý:**
- Soft delete — plan bị ẩn khỏi danh sách
- Recipes vẫn giữ nguyên trong DB (pregnancy-scoped)
- AiRequestLog vẫn giữ nguyên (audit trail)
- MealPlanFeedback, MealItems/Days cũng bị ẩn theo (global query filter)

---

## 7. API: DAY DETAIL

### Chi tiết 1 ngày — bao gồm tất cả meals + nutrients

```
GET /api/meal-plans/{planId}/days/{date}?lang=vi
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `planId` | Guid | (path) | ID meal plan |
| `date` | DateOnly | (path) | Ngày cần xem (format: `yyyy-MM-dd`) |
| `lang` | string | `"vi"` | Ngôn ngữ tên dưỡng chất |

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "day-001-...",
    "mealPlanId": "mp-001-...",
    "planDate": "2026-03-11",
    "totalCalories": 2540,
    "meals": [
      {
        "id": "item-001-...",
        "mealType": "Breakfast",
        "recipeId": "recipe-001-...",
        "itemName": "Cháo yến mạch với trứng và rau bina",
        "portionText": "1 bát lớn (350ml)",
        "caloriesKcal": 450,
        "notes": "Giàu sắt và acid folic cho tam cá nguyệt 2",
        "nutrients": [
          { "nutrientCode": "PROTEIN", "nutrientName": "Chất đạm", "unit": "g", "amount": 15.5 },
          { "nutrientCode": "IRON", "nutrientName": "Sắt", "unit": "mg", "amount": 3.2 },
          { "nutrientCode": "FOLIC_ACID", "nutrientName": "Acid folic", "unit": "µg", "amount": 180.0 },
          { "nutrientCode": "CALCIUM", "nutrientName": "Canxi", "unit": "mg", "amount": 120.0 },
          { "nutrientCode": "FIBER", "nutrientName": "Chất xơ", "unit": "g", "amount": 4.5 }
        ]
      },
      {
        "id": "item-002-...",
        "mealType": "Lunch",
        "recipeId": "recipe-002-...",
        "itemName": "Cơm gạo lứt với cá hồi nướng và rau xanh",
        "portionText": "1 đĩa vừa",
        "caloriesKcal": 720,
        "notes": "Giàu omega-3 và DHA tốt cho phát triển não thai nhi",
        "nutrients": [
          { "nutrientCode": "PROTEIN", "nutrientName": "Chất đạm", "unit": "g", "amount": 35.0 },
          { "nutrientCode": "OMEGA3", "nutrientName": "Omega-3", "unit": "mg", "amount": 1200.0 },
          { "nutrientCode": "DHA", "nutrientName": "DHA", "unit": "mg", "amount": 500.0 }
        ]
      },
      {
        "id": "item-003-...",
        "mealType": "Dinner",
        "recipeId": "recipe-003-...",
        "itemName": "Canh chua cá lóc",
        "portionText": "1 tô + 1 bát cơm",
        "caloriesKcal": 680,
        "notes": null,
        "nutrients": [
          { "nutrientCode": "PROTEIN", "nutrientName": "Chất đạm", "unit": "g", "amount": 28.0 },
          { "nutrientCode": "VITAMIN_C", "nutrientName": "Vitamin C", "unit": "mg", "amount": 45.0 }
        ]
      },
      {
        "id": "item-004-...",
        "mealType": "Snack",
        "recipeId": "recipe-004-...",
        "itemName": "Sinh tố bơ sữa chua",
        "portionText": "1 ly 300ml",
        "caloriesKcal": 320,
        "notes": "Bổ sung canxi buổi chiều",
        "nutrients": [
          { "nutrientCode": "CALCIUM", "nutrientName": "Canxi", "unit": "mg", "amount": 250.0 },
          { "nutrientCode": "VITAMIN_D", "nutrientName": "Vitamin D", "unit": "µg", "amount": 5.0 }
        ]
      }
    ]
  }
}
```

**DTO: `MealDayDetailDto`** (root)

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID ngày |
| `mealPlanId` | Guid | ID meal plan |
| `planDate` | DateOnly | Ngày |
| `totalCalories` | int | Tổng calo ngày |
| `meals` | MealItemDto[] | Danh sách món ăn (sorted: Breakfast → Lunch → Dinner → Snack) |

**DTO: `MealItemDto`** (nested trong `meals[]`)

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID meal item — **dùng cho feedback API** |
| `mealType` | string | `"Breakfast"` / `"Lunch"` / `"Dinner"` / `"Snack"` |
| `recipeId` | Guid? | ID recipe — **dùng cho recipe detail API** (null nếu AI không tạo recipe) |
| `itemName` | string? | Tên món ăn |
| `portionText` | string? | Phần ăn (VD: "1 bát lớn", "2 lát") |
| `caloriesKcal` | int? | Calo (kcal) |
| `notes` | string? | Ghi chú dinh dưỡng cho món |
| `nutrients` | MealItemNutrientDto[] | Danh sách dưỡng chất |

**DTO: `MealItemNutrientDto`** (nested trong `nutrients[]`)

| Field | Type | Mô tả |
|-------|------|-------|
| `nutrientCode` | string | Mã dưỡng chất (VD: `"PROTEIN"`, `"IRON"`) |
| `nutrientName` | string | Tên hiển thị (localized: `"Chất đạm"`, `"Sắt"`) |
| `unit` | string | Đơn vị (`"g"`, `"mg"`, `"µg"`) |
| `amount` | decimal | Hàm lượng |

**⚠️ FE lưu ý:**
- Meals sorted theo mealType: `Breakfast` → `Lunch` → `Dinner` → `Snack`
- Mỗi meal item **nên** có recipe (`recipeId != null`). Tap vào recipe → gọi Recipe Detail API
- `nutrientName` + `unit` đã localized theo `lang` parameter
- `id` của mỗi meal item dùng để gọi feedback API (like/dislike)

---

## 8. API: RECIPE DETAIL

### Chi tiết công thức nấu ăn

```
GET /api/recipes/{recipeId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "recipe-001-...",
    "pregnancyId": "a0000001-...",
    "title": "Cháo yến mạch với trứng và rau bina",
    "instructions": "1. Rửa sạch rau bina, cắt nhỏ.\n2. Đun sôi 400ml nước, thêm 60g yến mạch.\n3. Nấu trên lửa nhỏ 10 phút, khuấy đều.\n4. Đánh 1 quả trứng gà, đổ vào cháo, khuấy nhanh.\n5. Thêm rau bina, nấu thêm 2 phút.\n6. Nêm 1/4 muỗng cà phê muối, 1 muỗng dầu mè.\n7. Múc ra bát, rắc hành lá.",
    "servings": 1,
    "prepMinutes": 5,
    "cookMinutes": 15,
    "createdAt": "2026-03-11T08:03:00Z"
  }
}
```

**DTO: `RecipeDetailDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID recipe |
| `pregnancyId` | Guid | ID thai kỳ (ownership check) |
| `title` | string | Tên công thức |
| `instructions` | string? | Hướng dẫn nấu (multi-line text, dùng `\n` để xuống dòng) |
| `servings` | int? | Số phần ăn |
| `prepMinutes` | int? | Thời gian chuẩn bị (phút) |
| `cookMinutes` | int? | Thời gian nấu (phút) |
| `createdAt` | DateTime | Ngày tạo |

**FE hiển thị:**
- `instructions` là text thuần, phân cách bằng `\n`. FE split theo newline để render từng bước
- Hiện `prepMinutes` + `cookMinutes` ở header (VD: "Chuẩn bị: 5 phút | Nấu: 15 phút")
- Hiện `servings` (VD: "Cho 1 người")

---

## 9. API: FEEDBACK

### 9.1 Đánh giá meal plan (1-5 sao)

```
POST /api/meal-plans/{planId}/feedback
```

**Body (JSON):**
```json
{
  "rating": 4,
  "comment": "Thực đơn đa dạng, phù hợp khẩu vị"
}
```

**DTO: `CreateMealPlanFeedbackDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `rating` | int | ✅ | Điểm đánh giá: **1 đến 5** |
| `comment` | string? | ❌ | Nhận xét (tối đa 300 ký tự) |

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "fb-001-...",
    "mealPlanId": "mp-001-...",
    "userId": "user-001-...",
    "rating": 4,
    "comment": "Thực đơn đa dạng, phù hợp khẩu vị",
    "createdAt": "2026-03-11T12:00:00Z"
  }
}
```

**DTO: `MealPlanFeedbackDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID feedback |
| `mealPlanId` | Guid | ID meal plan |
| `userId` | Guid | ID user |
| `rating` | int | Điểm (1-5) |
| `comment` | string? | Nhận xét |
| `createdAt` | DateTime | Ngày tạo |

**⚠️ Business rules:**
- **1 feedback per user per plan** — nếu đã đánh giá → **409 Conflict** ("You have already rated this meal plan")
- Soft-deleted feedback → tự restore khi gửi lại (BE xử lý, FE không cần quan tâm)
- `rating` ngoài 1-5 → 400 validation error

---

### 9.2 Like/Dislike món ăn

```
POST /api/meal-items/{itemId}/feedback
```

| Param | Type | Mô tả |
|-------|------|-------|
| `itemId` | Guid | ID meal item (lấy từ Day Detail response `meals[].id`) |

**Body (JSON):**
```json
{
  "liked": true,
  "comment": "Món này rất ngon, muốn ăn lại"
}
```

**DTO: `CreateMealItemFeedbackDto`**

| Field | Type | Required | Mô tả |
|-------|------|:--------:|-------|
| `liked` | bool | ✅ | `true` = thích, `false` = không thích |
| `comment` | string? | ❌ | Nhận xét (tối đa 300 ký tự) |

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "ifb-001-...",
    "mealItemId": "item-001-...",
    "userId": "user-001-...",
    "liked": true,
    "comment": "Món này rất ngon, muốn ăn lại",
    "createdAt": "2026-03-11T12:05:00Z"
  }
}
```

**DTO: `MealItemFeedbackDto`**

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID feedback |
| `mealItemId` | Guid | ID meal item |
| `userId` | Guid | ID user |
| `liked` | bool | Thích/không thích |
| `comment` | string? | Nhận xét |
| `createdAt` | DateTime | Ngày tạo |

**⚠️ Business rules:**
- **1 feedback per user per item** — trùng → 409 Conflict
- Ownership verified: item → day → plan → pregnancy → user (deep chain)

---

## 10. DTOs — CHI TIẾT TẤT CẢ FIELDS

### Tóm tắt tất cả DTOs

| DTO | Loại | Dùng cho |
|-----|------|----------|
| `RefFoodItemDto` | Response | Danh sách thực phẩm tham khảo |
| `RefNutrientDto` | Response | Danh sách dưỡng chất tham khảo |
| `CreateFoodPreferenceDto` | Request | Tạo sở thích thực phẩm |
| `UpdateFoodPreferenceDto` | Request | Cập nhật sở thích |
| `FoodPreferenceDto` | Response | Sở thích thực phẩm (full) |
| `CreateNutritionNoteDto` | Request | Tạo ghi chú dinh dưỡng |
| `UpdateNutritionNoteDto` | Request | Cập nhật ghi chú |
| `NutritionNoteDto` | Response | Ghi chú dinh dưỡng (full) |
| `GenerateMealPlanDto` | Request | Generate AI meal plan |
| `MealPlanStatusDto` | Response | Trạng thái polling |
| `MealPlanSummaryDto` | Response | Danh sách plan (list item) |
| `MealPlanDetailDto` | Response | Chi tiết plan (+ days summary) |
| `MealPlanDaySummaryDto` | Response (nested) | 1 ngày trong plan detail |
| `MealDayDetailDto` | Response | Chi tiết 1 ngày (+ meals) |
| `MealItemDto` | Response (nested) | 1 món ăn |
| `MealItemNutrientDto` | Response (nested) | 1 dưỡng chất của món ăn |
| `RecipeDetailDto` | Response | Công thức nấu ăn |
| `CreateMealPlanFeedbackDto` | Request | Đánh giá plan |
| `MealPlanFeedbackDto` | Response | Feedback plan (full) |
| `CreateMealItemFeedbackDto` | Request | Like/dislike món ăn |
| `MealItemFeedbackDto` | Response | Feedback item (full) |

### DTO Field Reference — Request DTOs

#### `CreateFoodPreferenceDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `foodItemId` | Guid | ✅ | Must exist in RefFoodItems |
| `preferenceType` | FoodPreferenceType | ✅ | `"Allergy"` hoặc `"Dislike"` |
| `severity` | AllergySeverity? | ❌ | `"Low"` / `"Medium"` / `"High"` (chỉ khi Allergy) |
| `notes` | string? | ❌ | Tối đa 500 ký tự |

#### `UpdateFoodPreferenceDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `severity` | AllergySeverity? | ❌ | `"Low"` / `"Medium"` / `"High"` |
| `notes` | string? | ❌ | Tối đa 500 ký tự |

#### `CreateNutritionNoteDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `noteType` | NutritionNoteType | ✅ | `"Diet"` / `"Note"` / `"Other"` |
| `valueText` | string | ✅ | Không rỗng, tối đa 1000 ký tự |

#### `UpdateNutritionNoteDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `noteType` | NutritionNoteType? | ❌ | `"Diet"` / `"Note"` / `"Other"` |
| `valueText` | string? | ❌ | Tối đa 1000 ký tự |

#### `GenerateMealPlanDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `startDate` | DateOnly | ✅ | ≥ hôm nay (yyyy-MM-dd) |
| `durationWeeks` | int | ✅ | 1 – 4 |
| `additionalNotes` | string? | ❌ | Tối đa 500 ký tự |

#### `CreateMealPlanFeedbackDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `rating` | int | ✅ | 1 – 5 |
| `comment` | string? | ❌ | Tối đa 300 ký tự |

#### `CreateMealItemFeedbackDto`

| Field | Type | Required | Validation |
|-------|------|:--------:|-----------|
| `liked` | bool | ✅ | `true` / `false` |
| `comment` | string? | ❌ | Tối đa 300 ký tự |

### DTO Field Reference — Response DTOs

#### `RefFoodItemDto`

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID thực phẩm |
| `code` | string | Mã (VD: `"PEANUT"`, `"CHICKEN"`) |
| `displayName` | string | Tên hiển thị (localized) |

#### `RefNutrientDto`

| Field | Type | Mô tả |
|-------|------|-------|
| `id` | Guid | ID dưỡng chất |
| `code` | string | Mã (VD: `"PROTEIN"`, `"IRON"`) |
| `unit` | string | Đơn vị (VD: `"g"`, `"mg"`, `"µg"`) |
| `displayName` | string | Tên hiển thị (localized) |

---

## 11. LUỒNG TÍCH HỢP TỪNG BƯỚC

### Luồng chính: Từ setup đến xem thực đơn

```
Bước 1   GET /api/ref/food-items?lang=vi
          → Cache danh sách 45 thực phẩm
          → Render UI picker cho user chọn dị ứng/không thích

Bước 2   (Lặp) POST /api/pregnancies/{id}/food-preferences?lang=vi
          → body: { foodItemId, preferenceType: "Allergy", severity: "High" }
          → Mỗi thực phẩm user chọn = 1 request
          → VD: Đậu phộng (Allergy/High), Tôm (Allergy/Medium), Rau mùi (Dislike)

Bước 3   (Tùy chọn) POST /api/pregnancies/{id}/nutrition-notes
          → body: { noteType: "Diet", valueText: "Ăn chay linh hoạt" }
          → body: { noteType: "Note", valueText: "BS khuyên bổ sung sắt" }

Bước 4   POST /api/pregnancies/{id}/meal-plans/generate
          → body: { startDate: "2026-03-11", durationWeeks: 2 }
          → Lưu response.data.id (planId)
          → Response: 202 Accepted

Bước 5   POLL: GET /api/meal-plans/{planId}/status
          → Mỗi 3-5 giây
          → Hiện progress bar: completedWeeks / totalWeeks
          → Dừng khi status = "Succeeded" hoặc "Failed"

          ⏱️ Thời gian: ~30s/tuần × durationWeeks

Bước 6   GET /api/pregnancies/{id}/meal-plans/{planId}
          → Nhận MealPlanDetailDto (days summary)
          → Render calendar/list với ngày + calo

Bước 7   Khi user tap vào 1 ngày:
          GET /api/meal-plans/{planId}/days/2026-03-11?lang=vi
          → Nhận MealDayDetailDto (4 bữa + nutrients)
          → Render: Sáng | Trưa | Tối | Ăn nhẹ

Bước 8   Khi user tap vào 1 món:
          GET /api/recipes/{recipeId}
          → Nhận RecipeDetailDto (instructions)
          → Render: công thức nấu ăn step-by-step

Bước 9   (Tùy chọn) Feedback:
          POST /api/meal-plans/{planId}/feedback
          → body: { rating: 5, comment: "Rất tốt!" }

          POST /api/meal-items/{itemId}/feedback
          → body: { liked: true, comment: "Ngon!" }
```

### Luồng phụ: Quản lý sở thích (Settings screen)

```
GET  /api/pregnancies/{id}/food-preferences?lang=vi    → List hiện tại
POST /api/pregnancies/{id}/food-preferences?lang=vi    → Thêm mới
PUT  /api/pregnancies/{id}/food-preferences/{prefId}   → Sửa severity/notes
DEL  /api/pregnancies/{id}/food-preferences/{prefId}   → Xóa

GET  /api/pregnancies/{id}/nutrition-notes              → List ghi chú
POST /api/pregnancies/{id}/nutrition-notes              → Thêm ghi chú
PUT  /api/pregnancies/{id}/nutrition-notes/{noteId}     → Sửa ghi chú
DEL  /api/pregnancies/{id}/nutrition-notes/{noteId}     → Xóa ghi chú
```

> **Lưu ý:** Thay đổi food preferences / nutrition notes **KHÔNG ảnh hưởng** plan đã generate. AI chỉ đọc preferences mới nhất khi generate plan mới.

### Luồng phụ: Xem danh sách + xóa plan

```
GET /api/pregnancies/{id}/meal-plans?page=1&pageSize=10&sortBy=startdate&sortDir=desc
    → List plans (paged)

DELETE /api/pregnancies/{id}/meal-plans/{planId}
    → Soft delete
```

---

## 12. SEARCH / SORT / PAGING CHO MEAL PLANS

### Query Parameters

```
GET /api/pregnancies/{id}/meal-plans?page=1&pageSize=10&search=tuần 28&searchIn=title,notes&sortBy=startdate&sortDir=desc
```

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `page` | int | `1` | Trang hiện tại (1-based) |
| `pageSize` | int | `20` | Số items mỗi trang (1–100) |
| `search` | string? | null | Từ khóa tìm kiếm (case-insensitive, LIKE %search%) |
| `searchIn` | string? | `"title"` | Fields để tìm, phân cách `,`. Options: `title`, `notes` |
| `sortBy` | string? | `"createdat"` | Field sort. Options: `startdate`, `enddate`, `createdat` |
| `sortDir` | string? | `"desc"` | Hướng sort: `"asc"` hoặc `"desc"` |

### Paged Response Format

```json
{
  "success": true,
  "data": {
    "items": [ ... MealPlanSummaryDto[] ... ],
    "totalCount": 15,
    "page": 1,
    "pageSize": 10,
    "totalPages": 2
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `items` | array | Danh sách items trang hiện tại |
| `totalCount` | int | Tổng số items (tất cả trang) |
| `page` | int | Trang hiện tại |
| `pageSize` | int | Số items/trang |
| `totalPages` | int | Tổng số trang |

### Ví dụ FE

```dart
// Lấy trang 1, sort theo ngày bắt đầu mới nhất, tìm "tuần 28"
final response = await dio.get(
  '/api/pregnancies/$pregnancyId/meal-plans',
  queryParameters: {
    'page': 1,
    'pageSize': 10,
    'search': 'tuần 28',
    'searchIn': 'title,notes',
    'sortBy': 'startdate',
    'sortDir': 'desc',
  },
);

final pagedResult = response.data['data'];
final plans = pagedResult['items'] as List;    // MealPlanSummaryDto[]
final totalPages = pagedResult['totalPages'];  // For pagination UI
```

---

## 13. RESPONSE FORMAT CHUẨN

Tất cả API đều trả về format thống nhất:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "data": { ... },
  "errors": null,
  "timestamp": "2026-03-11T08:00:00Z"
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

## 14. ERROR HANDLING

### HTTP Status Codes

| Code | Ý nghĩa | Khi nào xảy ra |
|------|---------|----------------|
| 200 | OK | GET, PUT, DELETE thành công |
| 201 | Created | POST tạo mới thành công |
| 202 | Accepted | Meal plan generation đã queue |
| 400 | Bad Request | Validation fail (startDate quá khứ, durationWeeks > 4, BMI thiếu...) |
| 401 | Unauthorized | Token hết hạn hoặc không hợp lệ |
| 403 | Forbidden | Không có quyền (permission hoặc không phải owner pregnancy) |
| 404 | Not Found | Resource không tồn tại (plan, day, recipe, food item...) |
| 409 | Conflict | Duplicate (food preference đã tồn tại, feedback đã đánh giá) |
| 429 | Too Many Requests | Rate limit exceeded (15 AI calls/ngày) |

### Ví dụ Error Responses

**Validation Error (400):**
```json
{
  "success": false,
  "message": "Validation failed",
  "statusCode": 400,
  "data": null,
  "errors": [
    "Start date must be today or in the future.",
    "Duration must be between 1 and 4 weeks."
  ]
}
```

**Conflict — Duplicate Preference (409):**
```json
{
  "success": false,
  "message": "A Allergy preference for this food item already exists.",
  "statusCode": 409,
  "data": null,
  "errors": null
}
```

**Conflict — Duplicate Feedback (409):**
```json
{
  "success": false,
  "message": "You have already rated this meal plan.",
  "statusCode": 409,
  "data": null,
  "errors": null
}
```

**Rate Limit Exceeded (429):**
```json
{
  "success": false,
  "message": "Daily AI request limit exceeded. Please try again tomorrow.",
  "statusCode": 429,
  "data": null,
  "errors": null
}
```

**BMI Missing (400):**
```json
{
  "success": false,
  "message": "Please update your weight and height before generating a meal plan.",
  "statusCode": 400,
  "data": null,
  "errors": null
}
```

---

## PHỤ LỤC A: DATA MODEL HIERARCHY

```
MealPlan
├── id, pregnancyId, startDate, endDate
├── source ("AI"), status, title, notes
├── completedWeeks, totalWeeks
│
├── Days[] (MealPlanDay)
│   ├── id, planDate
│   │
│   └── Items[] (MealItem)
│       ├── id, mealType, itemName, portionText, caloriesKcal, notes
│       ├── recipeId → Recipe
│       │               ├── title, instructions
│       │               ├── servings, prepMinutes, cookMinutes
│       │               └── (1 recipe per meal item)
│       │
│       ├── Nutrients[] (MealItemNutrient)
│       │   ├── nutrientCode, nutrientName (localized), unit
│       │   └── amount (decimal)
│       │
│       └── Feedbacks[] (MealItemFeedback)
│           ├── liked (bool), comment
│           └── (1 per user per item)
│
└── Feedbacks[] (MealPlanFeedback)
    ├── rating (1-5), comment
    └── (1 per user per plan)
```

## PHỤ LỤC B: API ROUTE CHEAT SHEET

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/ref/food-items?lang=vi` | 45 food items |
| 2 | GET | `/api/ref/nutrients?lang=vi` | 15 nutrients |
| 3 | GET | `/api/ref/enums` | All enums |
| 4 | GET | `/api/ref/enums/{name}` | 1 enum |
| 5 | GET | `/api/ref/query-specs/mealPlans` | Search/sort spec |
| 6 | GET | `/api/pregnancies/{id}/food-preferences?lang=vi` | List prefs |
| 7 | POST | `/api/pregnancies/{id}/food-preferences?lang=vi` | Create pref |
| 8 | PUT | `/api/pregnancies/{id}/food-preferences/{prefId}?lang=vi` | Update pref |
| 9 | DELETE | `/api/pregnancies/{id}/food-preferences/{prefId}` | Delete pref |
| 10 | GET | `/api/pregnancies/{id}/nutrition-notes` | List notes |
| 11 | POST | `/api/pregnancies/{id}/nutrition-notes` | Create note |
| 12 | PUT | `/api/pregnancies/{id}/nutrition-notes/{noteId}` | Update note |
| 13 | DELETE | `/api/pregnancies/{id}/nutrition-notes/{noteId}` | Delete note |
| 14 | POST | `/api/pregnancies/{id}/meal-plans/generate` | **Generate AI** (202) |
| 15 | GET | `/api/meal-plans/{planId}/status` | **Poll status** |
| 16 | GET | `/api/pregnancies/{id}/meal-plans` | List plans (paged) |
| 17 | GET | `/api/pregnancies/{id}/meal-plans/{planId}` | Plan detail |
| 18 | DELETE | `/api/pregnancies/{id}/meal-plans/{planId}` | Delete plan |
| 19 | GET | `/api/meal-plans/{planId}/days/{date}?lang=vi` | **Day detail** |
| 20 | GET | `/api/recipes/{recipeId}` | Recipe detail |
| 21 | POST | `/api/meal-plans/{planId}/feedback` | Rate plan (1-5) |
| 22 | POST | `/api/meal-items/{itemId}/feedback` | Like/dislike item |
