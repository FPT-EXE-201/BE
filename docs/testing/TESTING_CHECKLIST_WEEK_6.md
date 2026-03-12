# 📋 TESTING CHECKLIST — WEEK 6: Weight Tracking & Motivational Messages

> **Prerequisite**: Đã hoàn thành TESTING_CHECKLIST_WEEK_3 + WEEK_4 + WEEK_5. Có JWT Bearer Token, có pregnancy Active.
> **Tool**: Postman / Thunder Client / Swagger UI.
> **Base URL**: `https://localhost:{PORT}/api`

---

## ⚠️ QUAN TRỌNG — Cấu hình trước khi test

Week 6 sử dụng Azure Document Intelligence (OCR) cho tính năng Weight OCR Extraction.
**PHẢI đảm bảo cấu hình từ Week 5 vẫn đúng** trước khi test:

| Service | Config Key | Ghi chú |
|---------|-----------|---------|
| Azure Doc Intelligence | `AI:AzureDocumentIntelligence:Endpoint` | Đã cấu hình từ Week 5 |
| Azure Doc Intelligence | `AI:AzureDocumentIntelligence:ApiKey` | Đã cấu hình từ Week 5 |

> **Lưu ý**: Nếu **KHÔNG** test phần OCR (Section 6), có thể bỏ qua bước cấu hình này. Các tính năng Weight Log, Weight Goal, Weight Alert, Motivational Messages hoạt động **độc lập** không cần Azure.

---

## 0️⃣ PRE-TEST: Xác nhận hệ thống khởi động đúng

### ✅ TC-W00: Application startup
```
dotnet run --project src/FPT.EXE201.Api
```
**Kiểm tra Console Output**:
- [ ] Không có exception khi khởi động
- [ ] EF Migration / Database update không lỗi
- [ ] Motivational templates seeded (30 templates × 2 languages)

**Expected**: App khởi động thành công, không crash.

---

### ✅ TC-W01: Chuẩn bị — Đảm bảo có pregnancy Active
```
GET /api/pregnancies/active
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có pregnancy đang Active.
**Lưu lại**: `{pregnancyId}`, `{lastMenstrualPeriodDate}` (nếu có, dùng để verify gestationalWeek ở chart).

> **✅ Ghi chú**: WeightSource, WeightAlertType, MotivationalCategory đã được thêm vào `RefDataController.GetEnums()`. Có thể lấy qua:
> - `GET /api/ref/enums` → trả về tất cả enums (bao gồm `weightSource`, `weightAlertType`, `motivationalCategory`)
> - `GET /api/ref/enums/weightSource` → chỉ WeightSource
> - `GET /api/ref/enums/weightAlertType` → chỉ WeightAlertType
> - `GET /api/ref/enums/motivationalCategory` → chỉ MotivationalCategory

| Enum | Values |
|------|--------|
| `WeightSource` | Manual(0), OCR(1) |
| `WeightAlertType` | RapidGain(0), RapidLoss(1), AboveRange(2), BelowRange(3) |
| `MotivationalCategory` | BabySize(0), Milestone(1), Tip(2) |

---

## 1️⃣ WEIGHT GOAL — Thiết lập mục tiêu cân nặng

### ✅ TC-G01: Tạo weight goal cho pregnancy
```
POST /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 160,
  "prePregnancyWeightKg": 55.5,
  "recommendedTotalGainMin": 11.5,
  "recommendedTotalGainMax": 16.0,
  "notes": "BMI bình thường trước mang thai"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Weight goal set successfully",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "heightCm": 160.0,
    "prePregnancyWeightKg": 55.5,
    "bmi": 21.68,
    "bmiCategory": "Normal",
    "recommendedTotalGainMin": 11.5,
    "recommendedTotalGainMax": 16.0,
    "notes": "BMI bình thường trước mang thai",
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `bmi` được tính tự động: `55.5 / (1.60 * 1.60) ≈ 21.68`
- [ ] `bmiCategory` là `"Normal"` (18.5 ≤ BMI < 25.0)
- [ ] `id`, `pregnancyId` là GUID hợp lệ
- [ ] `createdAt`, `updatedAt` có giá trị

**Lưu lại**: `{weightGoalId}`.

---

### ✅ TC-G02: Tạo weight goal — Không gửi height/weight → Fallback từ Pregnancy
```
POST /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "notes": "Test fallback to pregnancy data"
}
```
**Expected**: 201 Created
- Nếu Pregnancy **có** `heightCm` / `prePregnancyWeightKg` → BMI được tính từ Pregnancy data
- Nếu Pregnancy **không có** → `bmi: null`, `bmiCategory: null`
- `recommendedTotalGainMin/Max` auto-calculated theo IOM guidelines dựa trên BMI:

| BMI | Category | Recommended Gain (kg) |
|-----|----------|----------------------|
| null | — | 11.5 – 16.0 (default) |
| < 18.5 | Underweight | 12.5 – 18.0 |
| 18.5 – 24.99 | Normal | 11.5 – 16.0 |
| 25.0 – 29.99 | Overweight | 7.0 – 11.5 |
| ≥ 30.0 | Obese | 5.0 – 9.0 |

> **⚠️ Lưu ý**: Nếu user gửi cả `recommendedTotalGainMin` VÀ `recommendedTotalGainMax` → dùng giá trị user gửi, KHÔNG auto-calculate.

---

### ❌ TC-G03: Tạo weight goal — Duplicate (đã có goal) → 409
```
POST /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 165,
  "prePregnancyWeightKg": 60
}
```
**Expected**: 409 Conflict
```json
{
  "success": false,
  "statusCode": 409,
  "message": "Weight goal already exists for this pregnancy. Use PUT to update."
}
```

---

### ✅ TC-G04: Đọc weight goal
```
GET /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
```
**Expected**: 200 OK — Trả về WeightGoalDto đã tạo ở TC-G01.
**Verify**:
- [ ] `bmi`, `bmiCategory` đầy đủ
- [ ] Dữ liệu khớp với input

---

### ✅ TC-G05: Đọc weight goal — Chưa có goal
> Dùng pregnancy chưa tạo weight goal.
```
GET /api/pregnancies/{pregnancyId_no_goal}/weight-goals
Authorization: Bearer {token}
```
**Expected**: 200 OK — `data: null` (trả về null, không lỗi).

---

### ✅ TC-G06: Cập nhật weight goal
```
PUT /api/weight-goals/{weightGoalId}
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 162,
  "prePregnancyWeightKg": 57.0,
  "recommendedTotalGainMin": 11.5,
  "recommendedTotalGainMax": 16.0,
  "notes": "Đã cập nhật chiều cao"
}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Weight goal updated successfully",
  "data": {
    "id": "{weightGoalId}",
    "bmi": 21.72,
    "bmiCategory": "Normal",
    "notes": "Đã cập nhật chiều cao",
    ...
  }
}
```
**Verify**:
- [ ] `bmi` = `57.0 / (1.62 * 1.62) ≈ 21.72` (tính lại)
- [ ] `notes` = `"Đã cập nhật chiều cao"`
- [ ] `updatedAt` thay đổi

---

### ❌ TC-G07: Validation — heightCm ngoài range
```
POST /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 30,
  "prePregnancyWeightKg": -5
}
```
**Expected**: 400 Bad Request — Validation errors:
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": [
    "Height must be greater than 50 cm.",
    "Pre-pregnancy weight must be greater than 0."
  ]
}
```
> **Validation Rules**: `heightCm > 50 AND < 250`, `prePregnancyWeightKg > 0 AND < 500`

---

### ❌ TC-G08: Validation — recommendedTotalGainMin > Max
```
POST /api/pregnancies/{pregnancyId}/weight-goals
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 160,
  "prePregnancyWeightKg": 55,
  "recommendedTotalGainMin": 20.0,
  "recommendedTotalGainMax": 10.0
}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": ["Maximum gain must be >= minimum gain."]
}
```

---

### ❌ TC-G09: Weight goal — Weight goal không tồn tại
```
PUT /api/weight-goals/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "heightCm": 160
}
```
**Expected**: 404 Not Found — `"Weight goal not found."`

---

## 2️⃣ WEIGHT LOG — Ghi nhận cân nặng

### ✅ TC-L01: Tạo weight log — Manual
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-02-21",
  "weightKg": 58.5,
  "note": "Cân buổi sáng trước ăn",
  "source": "Manual"
}
```
**Expected**: 201 Created
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Weight log recorded successfully",
  "data": {
    "id": "<guid>",
    "pregnancyId": "{pregnancyId}",
    "loggedOn": "2026-02-21",
    "weightKg": 58.5,
    "note": "Cân buổi sáng trước ăn",
    "source": "Manual",
    "weightGainFromBaseline": 3.0,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```
**Verify**:
- [ ] `weightGainFromBaseline` = `58.5 - 55.5 = 3.0` (nếu có prePregnancyWeightKg từ goal/pregnancy)
- [ ] `weightGainFromBaseline` = `null` nếu chưa có prePregnancyWeightKg
- [ ] `source` = `"Manual"`

**Lưu lại**: `{weightLogId_1}`.

---

### ✅ TC-L02: Tạo weight log — Không gửi source (default = Manual)
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-02-28",
  "weightKg": 59.0
}
```
**Expected**: 201 Created — `source: "Manual"` (default).

**Lưu lại**: `{weightLogId_2}`.

---

### ❌ TC-L03: Tạo weight log — Duplicate ngày → 409
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-02-21",
  "weightKg": 60.0,
  "source": "Manual"
}
```
**Expected**: 409 Conflict
```json
{
  "success": false,
  "statusCode": 409,
  "message": "A weight log already exists for 2026-02-21."
}
```
> **⚠️ Note**: Error message chứa ngày cụ thể (format `yyyy-MM-dd`).

---

### ✅ TC-L04: Tạo weight log — Tăng nhanh → Auto-trigger alert RapidGain
> **Điều kiện**: Tạo entry tăng > 0.7 kg/tuần so với entry gần nhất **trong cửa sổ 7–14 ngày**.
> **Alert cooldown**: Mỗi loại alert chỉ được tạo tối đa **1 lần / 7 ngày** cho mỗi pregnancy.
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-07",
  "weightKg": 61.5,
  "note": "Test rapid gain alert",
  "source": "Manual"
}
```
**Expected**: 201 Created — Entry tạo thành công.
- Tăng từ 59.0 (28/02) → 61.5 (07/03) = +2.5 kg / 7 ngày = **2.5 kg/week** > 0.7 → Alert **RapidGain** auto-created.

**Verify** (ở TC-A01 bên dưới): Kiểm tra `GET /weight-alerts` có RapidGain alert.
**Alert DetailsJson expected**: `{"weeklyGain":2.50,"currentWeight":61.5,"previousWeight":59.0,"daysBetween":7}`

**Lưu lại**: `{weightLogId_3}`.

---

### ❌ TC-L05: Validation — weightKg ngoài range
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-10",
  "weightKg": 0,
  "source": "Manual"
}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": ["Weight must be greater than 0."]
}
```

**Thêm test case**: `"weightKg": 501` → `"Weight must be less than 500 kg."`

> **Validation Rules**: `weightKg > 0 AND < 500`

---

### ⚠️ TC-L06: Validation — loggedOn trong tương lai (DISABLED)
> **⚠️ LƯU Ý**: Validation rule cho future date hiện đang **bị comment out** trong `CreateWeightLogDtoValidator.cs`. Test case này sẽ **KHÔNG trigger 400** — thay vào đó sẽ trả về **201 Created** bình thường.
> Nếu muốn enable lại, uncomment `.LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))` trong validator.
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2099-01-01",
  "weightKg": 60.0,
  "source": "Manual"
}
```
**Expected (HIỆN TẠI)**: 201 Created — Future date **KHÔNG** bị từ chối.
**Expected (NẾU enable validation)**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": ["Logged date cannot be in the future."]
}
```

---

### ✅ TC-L07: Cập nhật weight log (partial update)
```
PUT /api/weight-logs/{weightLogId_1}
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "weightKg": 58.8,
  "note": "Đã cập nhật - cân lại chính xác hơn"
}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Weight log updated successfully",
  "data": {
    "id": "{weightLogId_1}",
    "weightKg": 58.8,
    "note": "Đã cập nhật - cân lại chính xác hơn",
    "source": "Manual",
    ...
  }
}
```
**Verify**:
- [ ] `weightKg` = 58.8
- [ ] `note` = `"Đã cập nhật - cân lại chính xác hơn"`
- [ ] `source` giữ nguyên `"Manual"` (không gửi thì không đổi)
- [ ] `updatedAt` thay đổi

---

### ✅ TC-L08: Xóa weight log (soft delete)
```
DELETE /api/weight-logs/{weightLogId_3}
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Weight log deleted successfully",
  "data": null
}
```
**Verify**: `GET /api/pregnancies/{pregnancyId}/weight-logs` → không còn entry `{weightLogId_3}`.

---

### ❌ TC-L09: Xóa weight log không tồn tại → 404
```
DELETE /api/weight-logs/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Weight log not found."`

---

## 3️⃣ WEIGHT LOG — Danh sách & Phân trang

### ✅ TC-P01: Lấy danh sách weight logs (mặc định)
```
GET /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
```
**Expected**: 200 OK — Trả về danh sách paged, sắp xếp theo `loggedOn` giảm dần (mới nhất trước).
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "<guid>",
        "pregnancyId": "{pregnancyId}",
        "loggedOn": "2026-02-28",
        "weightKg": 59.0,
        "note": null,
        "source": "Manual",
        "weightGainFromBaseline": 3.5,
        "createdAt": "...",
        "updatedAt": "..."
      },
      {
        "id": "<guid>",
        "loggedOn": "2026-02-21",
        "weightKg": 58.8,
        ...
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 2,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```
**Verify**:
- [ ] `totalItems` khớp số entry đã tạo (trừ entry đã xóa)
- [ ] Thứ tự: ngày mới nhất trước (default sort: `loggedOn DESC`)
- [ ] Mỗi item có `weightGainFromBaseline` tính đúng

---

### ✅ TC-P02: Phân trang + Sort
```
GET /api/pregnancies/{pregnancyId}/weight-logs?page=1&pageSize=1&sortBy=weightKg&sortDir=asc
Authorization: Bearer {token}
```
**Expected**: 200 OK — 1 item, sắp xếp theo `weightKg` tăng dần.
**Verify**:
- [ ] `pageSize` = 1
- [ ] `totalItems` > 1
- [ ] `hasNextPage` = true
- [ ] Item có `weightKg` nhỏ nhất

---

### ✅ TC-P03: Tìm kiếm theo note
```
GET /api/pregnancies/{pregnancyId}/weight-logs?search=buổi sáng
Authorization: Bearer {token}
```
**Expected**: 200 OK — Chỉ trả về entries có `note` chứa `"buổi sáng"`.
**Verify**:
- [ ] `totalItems` ít hơn tổng số entries
- [ ] Mọi item trong results đều có `note` chứa keyword

> **Searchable fields**: `note` (theo WeightLogListQuerySpec).

---

## 4️⃣ WEIGHT CHART — Biểu đồ cân nặng

### ✅ TC-C01: Lấy dữ liệu biểu đồ
```
GET /api/pregnancies/{pregnancyId}/weight-logs/chart
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "prePregnancyWeightKg": 55.5,
    "recommendedGainMin": 11.5,
    "recommendedGainMax": 16.0,
    "currentWeightKg": 59.0,
    "totalGainKg": 3.5,
    "totalEntries": 2,
    "dataPoints": [
      {
        "date": "2026-02-21",
        "weightKg": 58.8,
        "gestationalWeek": 20
      },
      {
        "date": "2026-02-28",
        "weightKg": 59.0,
        "gestationalWeek": 21
      }
    ]
  }
}
```
**Verify**:
- [ ] `prePregnancyWeightKg` lấy từ `Pregnancy.PrePregnancyWeightKg` (KHÔNG phải WeightGoalRange)
- [ ] `recommendedGainMin/Max` lấy từ WeightGoalRange
- [ ] `currentWeightKg` = cân nặng entry mới nhất
- [ ] `totalGainKg` = `currentWeightKg - prePregnancyWeightKg` (null nếu thiếu data)
- [ ] `totalEntries` = số lượng weight logs
- [ ] `dataPoints` sắp xếp theo `date` tăng dần (ngày cũ trước)
- [ ] `gestationalWeek` tính từ `lastMenstrualPeriodDate`: `(date - LMP).TotalDays / 7`
- [ ] Mỗi data point dùng field `date` (KHÔNG phải `loggedOn`)

---

### ✅ TC-C02: Chart — Không có LMP → gestationalWeek = null
Nếu pregnancy không có `lastMenstrualPeriodDate`:
```
GET /api/pregnancies/{pregnancyId_no_lmp}/weight-logs/chart
Authorization: Bearer {token}
```
**Expected**: `gestationalWeek` = `null` cho tất cả data points.

---

### ✅ TC-C03: Chart — Không có weight goal → fields null
Nếu pregnancy chưa tạo weight goal:

**Expected**: `prePregnancyWeightKg`, `recommendedGainMin`, `recommendedGainMax`, `totalGainKg` có thể = `null`.

---

## 5️⃣ WEIGHT ALERTS — Cảnh báo cân nặng

> **⚠️ ALERT LOGIC (đã cập nhật)**:
>
> | Rule | Giá trị | Giải thích |
> |------|---------|------------|
> | **Comparison window** | 7–14 ngày | Chỉ so sánh với log cách **7–14 ngày** (bỏ qua log < 7 ngày hoặc > 14 ngày) |
> | **RapidGain threshold** | > 0.7 kg/week | `weeklyGain = (currentWeight - previousWeight) / (daysDiff / 7.0)` |
> | **RapidLoss threshold** | < -0.3 kg/week | Dùng cùng công thức, giá trị âm |
> | **Cooldown** | 7 ngày (RapidGain/RapidLoss only) | Chỉ áp dụng cho **RapidGain** và **RapidLoss** — mỗi loại tối đa 1 alert / 7 ngày. **AboveRange** và **BelowRange** KHÔNG có cooldown (trigger mỗi lần điều kiện đúng). |
> | **AboveRange** | totalGain > max | Trigger bất kỳ lúc nào, không cần comparison window |
> | **BelowRange** | totalGain < min | Chỉ trigger khi `gestationalWeek ≥ 37` |
>
> **detailsJson fields**:
> - RapidGain: `weeklyGain`, `currentWeight`, `previousWeight`, `daysBetween`
> - RapidLoss: `weeklyChange`, `currentWeight`, `previousWeight`, `daysBetween`
> - AboveRange: `currentWeight`, `totalGain`, `maxRecommended`
> - BelowRange: `currentWeight`, `totalGain`, `minRecommended`

### ✅ TC-A01: Lấy danh sách alerts
```
GET /api/pregnancies/{pregnancyId}/weight-alerts
Authorization: Bearer {token}
```
**Expected**: 200 OK — Có ít nhất 1 alert `RapidGain` (auto-trigger từ TC-L04).
```json
{
  "success": true,
  "data": [
    {
      "id": "<guid>",
      "pregnancyId": "{pregnancyId}",
      "alertType": "RapidGain",
      "triggeredAt": "...",
      "detailsJson": "{\"weeklyGain\":2.50,\"currentWeight\":61.5,\"previousWeight\":59.0,\"daysBetween\":7}",
      "resolvedAt": null,
      "isResolved": false
    }
  ]
}
```
**Verify**:
- [ ] `alertType` = `"RapidGain"`
- [ ] `isResolved` = `false`
- [ ] `resolvedAt` = `null`
- [ ] `detailsJson` chứa `weeklyGain`, `currentWeight`, `previousWeight`, `daysBetween`

**Lưu lại**: `{alertId}`.

---

### ✅ TC-A02: Resolve alert
```
PUT /api/weight-alerts/{alertId}/resolve
Authorization: Bearer {token}
```
**Expected**: 200 OK
```json
{
  "success": true,
  "message": "Alert resolved successfully",
  "data": {
    "id": "{alertId}",
    "alertType": "RapidGain",
    "triggeredAt": "...",
    "resolvedAt": "2026-02-22T...",
    "isResolved": true
  }
}
```
**Verify**:
- [ ] `resolvedAt` có giá trị (UTC now)
- [ ] `isResolved` = `true`

---

### ❌ TC-A03: Resolve alert đã resolved → 400
```
PUT /api/weight-alerts/{alertId}/resolve
Authorization: Bearer {token}
```
**Expected**: 400 Bad Request
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Alert is already resolved."
}
```
> **⚠️ Lưu ý**: Trả về **400 Bad Request**, KHÔNG phải 409 Conflict.

---

### ❌ TC-A04: Resolve alert không tồn tại → 404
```
PUT /api/weight-alerts/00000000-0000-0000-0000-000000000000/resolve
Authorization: Bearer {token}
```
**Expected**: 404 Not Found — `"Weight alert not found."`

---

## 6️⃣ WEIGHT OCR — Trích xuất cân nặng từ ảnh

> **⚠️ Flow**: Upload ảnh cân → OCR extract text → Regex parse số kg → Trả về cho FE confirm → FE gọi `POST /weight-logs` (Source=OCR) nếu đồng ý.
> OCR sử dụng `IOcrProvider` (Azure Document Intelligence) — cùng provider với Week 5.

---

### ✅ TC-O01: Upload ảnh cân → OCR trích xuất thành công
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `image` | File | Ảnh chụp từ cân (có hiển thị số kg, vd: "59.5 kg") |

**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "success": true,
    "extractedWeightKg": 59.5,
    "rawOcrText": "59.5 kg  ...",
    "confidenceScore": 0.85,
    "message": "Trích xuất thành công: 59.5 kg. Vui lòng xác nhận."
  }
}
```
**Verify**:
- [ ] `data.success` = `true`
- [ ] `extractedWeightKg` là số hợp lệ (30–200 kg)
- [ ] `confidenceScore` trong khoảng 0–1
- [ ] `rawOcrText` chứa text OCR thô (từ Azure Document Intelligence)
- [ ] `message` = `"Trích xuất thành công: {value} kg. Vui lòng xác nhận."`
- [ ] Field names: `extractedWeightKg` (KHÔNG phải `extractedWeight`), `rawOcrText` (KHÔNG phải `rawText`), `confidenceScore` (KHÔNG phải `confidence`)

**Lưu ý**: Kết quả phụ thuộc vào chất lượng ảnh và Azure Document Intelligence.

---

### ✅ TC-O02: Upload ảnh — Có text nhưng không có số cân hợp lệ
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `image` | File | Ảnh có text nhưng KHÔNG có số cân (vd: ảnh menu, screenshot) |

**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "success": false,
    "extractedWeightKg": null,
    "rawOcrText": "...",
    "confidenceScore": null,
    "message": "Nhận diện được text nhưng không tìm thấy giá trị cân nặng hợp lệ (30–200 kg). Vui lòng chụp lại."
  }
}
```
**Verify**:
- [ ] `data.success` = `false`
- [ ] `extractedWeightKg` = `null`
- [ ] `rawOcrText` có text (OCR hoạt động, nhưng không tìm được số cân)
- [ ] `message` gợi ý chụp lại

---

### ✅ TC-O03: Upload ảnh — Không nhận diện được text nào
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `image` | File | Ảnh mờ / trắng / không có text |

**Expected**: 200 OK
```json
{
  "success": true,
  "data": {
    "success": false,
    "extractedWeightKg": null,
    "rawOcrText": null,
    "confidenceScore": null,
    "message": "Không nhận diện được text từ ảnh. Vui lòng chụp rõ hơn."
  }
}
```

---

### ❌ TC-O04: Upload file không phải ảnh → 400
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `image` | File | File `.pdf` hoặc `.txt` |

**Expected**: 400 Bad Request — `"Only JPEG and PNG images are allowed."`

---

### ❌ TC-O05: Upload ảnh quá lớn (>5MB) → 400
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
| Field | Type | Value |
|-------|------|-------|
| `image` | File | Ảnh > 5 MB |

**Expected**: 400 Bad Request — `"Image size must not exceed 5 MB."`

---

### ❌ TC-O06: Upload không có file → 400
```
POST /api/pregnancies/{pregnancyId}/weight-logs/extract-weight
Authorization: Bearer {token}
Content-Type: multipart/form-data
```
(Không gửi field `image`)

**Expected**: 400 Bad Request — `"Image file is required."`

---

### ✅ TC-O07: OCR thành công → Confirm bằng POST weight-log Source=OCR
> **Flow hoàn chỉnh**: OCR extract → FE hiển thị → User confirm → Tạo weight log.
1. OCR extract (TC-O01) → lấy `extractedWeightKg` = 59.5
2. Confirm:
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-10",
  "weightKg": 59.5,
  "note": "Từ OCR extract, đã xác nhận",
  "source": "OCR"
}
```
**Expected**: 201 Created — `source: "OCR"`.

---

## 7️⃣ MOTIVATIONAL MESSAGES — Thông điệp động viên (Public)

> **⚠️ Endpoint public** — KHÔNG cần Authorization header. Giống `RefDataController`.

---

### ✅ TC-M01: Lấy motivational messages — Tiếng Việt (default)
```
GET /api/motivational?week=20
```
**Expected**: 200 OK
```json
{
  "success": true,
  "data": [
    {
      "id": "<guid>",
      "category": "BabySize",
      "weekStart": 20,
      "weekEnd": 21,
      "variablesJson": "{\"fruitVi\":\"quả chuối\",\"fruitEn\":\"banana\",\"sizeCm\":\"25.6\"}",
      "title": "Bé to bằng quả chuối!",
      "message": "Tuần 20-21: Bé dài 25.6 cm — nửa chặng đường rồi mẹ ơi! Bé đã có lông mày và mi mắt."
    }
  ]
}
```
**Verify**:
- [ ] Trả về templates có `weekStart ≤ 20 ≤ weekEnd`
- [ ] `title`, `message` là tiếng Việt
- [ ] DTO fields: `id`, `category`, `weekStart`, `weekEnd`, `variablesJson`, `title`, `message`
- [ ] **KHÔNG** có field `isActive`, `languageCode` trong response

---

### ✅ TC-M02: Lấy motivational messages — Tiếng Anh
```
GET /api/motivational?week=20&lang=en
```
**Expected**: 200 OK — `title`, `message` là tiếng Anh.
**Verify**: Nội dung khác với kết quả TC-M01 (cùng template, khác ngôn ngữ).

---

### ✅ TC-M03: Lấy motivational messages — Lọc theo category
```
GET /api/motivational?week=20&category=BabySize
```
**Expected**: 200 OK — Chỉ trả về templates có `category = "BabySize"`.
**Verify**: Không có item nào có category khác.

---

### ✅ TC-M04: Lấy motivational messages — Milestone category
```
GET /api/motivational?week=12&category=Milestone
```
**Expected**: 200 OK — Templates về milestones.
**Verify**: Tất cả items có `category = "Milestone"`.

---

### ✅ TC-M05: Lấy motivational messages — Tuần không có template
```
GET /api/motivational?week=1
```
**Expected**: 200 OK — `data: []` (danh sách rỗng, không lỗi).

---

### ✅ TC-M06: Lấy motivational messages — Ngôn ngữ không tồn tại
```
GET /api/motivational?week=20&lang=ja
```
**Expected**: 200 OK — `data: []` (không có bản dịch tiếng Nhật → danh sách rỗng).

---

## 8️⃣ OWNERSHIP & PERMISSIONS — Kiểm tra phân quyền

### ❌ TC-SEC01: Weight log — Truy cập pregnancy của user khác → 403
> Cần 2 tài khoản user A và user B. User B có pregnancy riêng.
```
POST /api/pregnancies/{pregnancyId_of_user_B}/weight-logs
Authorization: Bearer {token_of_user_A}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-01",
  "weightKg": 60.0,
  "source": "Manual"
}
```
**Expected**: 403 Forbidden — `"Access denied."`

---

### ❌ TC-SEC02: Weight goal — Truy cập pregnancy của user khác → 403
```
GET /api/pregnancies/{pregnancyId_of_user_B}/weight-goals
Authorization: Bearer {token_of_user_A}
```
**Expected**: 403 Forbidden — `"Access denied."`

---

### ❌ TC-SEC03: Weight log — Không có token → 401
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-01",
  "weightKg": 60.0,
  "source": "Manual"
}
```
**Expected**: 401 Unauthorized — Thiếu Bearer Token.

---

### ✅ TC-SEC04: Motivational — Không cần token → 200
```
GET /api/motivational?week=20
```
(Không gửi Authorization header)
**Expected**: 200 OK — Endpoint public, không cần auth.

---

### ❌ TC-SEC05: Weight log — User không có permission weight_log.write → 403
> Cần tài khoản KHÔNG có permission `weight_log.write` (vd: tạo role mới chỉ có read).
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token_no_permission}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-01",
  "weightKg": 60.0
}
```
**Expected**: 403 Forbidden.

---

### ❌ TC-SEC06: Cập nhật weight log user khác → 403/404
```
PUT /api/weight-logs/{weightLogId_of_user_B}
Authorization: Bearer {token_of_user_A}
Content-Type: application/json
```
```json
{
  "weightKg": 99.9
}
```
**Expected**: 403 Forbidden (`"Access denied."`) hoặc 404 Not Found (`"Weight log not found."`).

---

## 9️⃣ EDGE CASES & INTEGRATION

### ❌ TC-E01: Weight log — Pregnancy không tồn tại → 404
```
POST /api/pregnancies/00000000-0000-0000-0000-000000000000/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-01",
  "weightKg": 60.0,
  "source": "Manual"
}
```
**Expected**: 404 Not Found — `"Pregnancy not found."`

---

### ✅ TC-E02: Weight log — Source = OCR
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-15",
  "weightKg": 60.5,
  "note": "Từ OCR extract",
  "source": "OCR"
}
```
**Expected**: 201 Created — `source: "OCR"`.

---

### ✅ TC-E03: Nhiều weight logs → Chart data ordered
Tạo thêm vài weight logs với các ngày khác nhau, sau đó:
```
GET /api/pregnancies/{pregnancyId}/weight-logs/chart
Authorization: Bearer {token}
```
**Expected**: `dataPoints` sắp xếp theo `date` tăng dần (ngày cũ trước, ngày mới sau).

---

### ✅ TC-E04: RapidLoss alert
> Tạo entry giảm < -0.3 kg/tuần so với entry trước **trong cửa sổ 7–14 ngày**.
> **Lưu ý**: Alert chỉ trigger nếu chưa có RapidLoss alert nào trong 7 ngày gần nhất (cooldown).
```
POST /api/pregnancies/{pregnancyId}/weight-logs
Authorization: Bearer {token}
Content-Type: application/json
```
```json
{
  "loggedOn": "2026-03-22",
  "weightKg": 57.0,
  "note": "Test rapid loss",
  "source": "Manual"
}
```
> Giảm từ 60.5 (15/03) → 57.0 (22/03) = -3.5 kg / 7 ngày = **-3.5 kg/week** → Alert **RapidLoss** (< -0.3 kg/week).

**Verify**:
```
GET /api/pregnancies/{pregnancyId}/weight-alerts
```
- [ ] Có alert mới với `alertType = "RapidLoss"`
- [ ] `detailsJson` chứa `weeklyChange`, `currentWeight`, `previousWeight`, `daysBetween`

---

### ✅ TC-E05: AboveRange alert
> **Điều kiện**: `totalGain > recommendedTotalGainMax` (trigger ở bất kỳ tuần nào).

Ví dụ: `prePregnancyWeightKg = 55.5`, `recommendedTotalGainMax = 16.0`
→ Tạo weight log `weightKg = 72.0` (totalGain = 16.5 > 16.0)

**Verify**:
```
GET /api/pregnancies/{pregnancyId}/weight-alerts
```
- [ ] Có alert `alertType = "AboveRange"`
- [ ] `detailsJson` chứa `currentWeight`, `totalGain`, `maxRecommended`

---

### ✅ TC-E06: BelowRange alert
> **Điều kiện**: `totalGain < recommendedTotalGainMin` VÀ `CurrentGestationalWeek >= 37`.

> **⚠️ Khó test**: Cần pregnancy ở tuần 37+ để trigger. Chỉ verify logic nếu có pregnancy phù hợp.

---

### ✅ TC-E07: BMI Category ranges — Verify auto-calculate
Tạo weight goals với các BMI khác nhau (trên pregnancy khác nhau hoặc xóa goal cũ):

| Input | BMI | Category | Auto Gain Range |
|-------|-----|----------|----------------|
| `{ "heightCm": 160, "prePregnancyWeightKg": 45 }` | 17.58 | `"Underweight"` | 12.5 – 18.0 |
| `{ "heightCm": 160, "prePregnancyWeightKg": 55 }` | 21.48 | `"Normal"` | 11.5 – 16.0 |
| `{ "heightCm": 160, "prePregnancyWeightKg": 65 }` | 25.39 | `"Overweight"` | 7.0 – 11.5 |
| `{ "heightCm": 160, "prePregnancyWeightKg": 80 }` | 31.25 | `"Obese"` | 5.0 – 9.0 |

> **Chỉ verify auto-gain khi KHÔNG gửi** `recommendedTotalGainMin/Max` trong request body.

---

## 📊 CHECKLIST SUMMARY

| # | Test Case | Type | Result |
|---|-----------|------|--------|
| **0. Pre-test** | | | |
| W00 | App startup | ✅ | ☐ |
| W01 | Chuẩn bị — Có pregnancy Active | ✅ | ☐ |
| **1. Weight Goal** | | | |
| G01 | Tạo weight goal — Happy path | ✅ | ☐ |
| G02 | Tạo goal — Fallback từ Pregnancy | ✅ | ☐ |
| G03 | Tạo goal — Duplicate | ❌ 409 | ☐ |
| G04 | Đọc weight goal | ✅ | ☐ |
| G05 | Đọc goal — Chưa có goal | ✅ | ☐ |
| G06 | Cập nhật goal | ✅ | ☐ |
| G07 | Validation — Height/Weight ngoài range | ❌ 400 | ☐ |
| G08 | Validation — GainMin > GainMax | ❌ 400 | ☐ |
| G09 | Goal không tồn tại | ❌ 404 | ☐ |
| **2. Weight Log** | | | |
| L01 | Tạo weight log — Manual | ✅ | ☐ |
| L02 | Tạo log — Default source | ✅ | ☐ |
| L03 | Tạo log — Duplicate ngày | ❌ 409 | ☐ |
| L04 | Tạo log — Auto-trigger RapidGain alert | ✅ | ☐ |
| L05 | Validation — weightKg ngoài range | ❌ 400 | ☐ |
| L06 | Validation — loggedOn future (**DISABLED**) | ⚠️ N/A | ☐ |
| L07 | Cập nhật log — Partial update | ✅ | ☐ |
| L08 | Xóa log — Soft delete | ✅ | ☐ |
| L09 | Xóa log — Không tồn tại | ❌ 404 | ☐ |
| **3. Paging & Sort** | | | |
| P01 | Danh sách weight logs (default) | ✅ | ☐ |
| P02 | Phân trang + Sort | ✅ | ☐ |
| P03 | Tìm kiếm theo note | ✅ | ☐ |
| **4. Weight Chart** | | | |
| C01 | Chart data — Happy path | ✅ | ☐ |
| C02 | Chart — Không có LMP | ✅ | ☐ |
| C03 | Chart — Không có goal | ✅ | ☐ |
| **5. Weight Alerts** | | | |
| A01 | Lấy alerts | ✅ | ☐ |
| A02 | Resolve alert | ✅ | ☐ |
| A03 | Resolve alert đã resolved | ❌ 400 | ☐ |
| A04 | Resolve alert không tồn tại | ❌ 404 | ☐ |
| **6. Weight OCR** | | | |
| O01 | OCR trích xuất thành công | ✅ | ☐ |
| O02 | OCR — Text nhưng không có cân | ✅ | ☐ |
| O03 | OCR — Không nhận diện text | ✅ | ☐ |
| O04 | Upload file không phải ảnh | ❌ 400 | ☐ |
| O05 | Upload ảnh quá lớn | ❌ 400 | ☐ |
| O06 | Upload không có file | ❌ 400 | ☐ |
| O07 | OCR → Confirm POST weight-log | ✅ | ☐ |
| **7. Motivational** | | | |
| M01 | Messages — Tiếng Việt (default) | ✅ | ☐ |
| M02 | Messages — Tiếng Anh | ✅ | ☐ |
| M03 | Messages — Lọc category | ✅ | ☐ |
| M04 | Messages — Milestone | ✅ | ☐ |
| M05 | Messages — Tuần không có template | ✅ | ☐ |
| M06 | Messages — Ngôn ngữ không tồn tại | ✅ | ☐ |
| **8. Ownership & Permissions** | | | |
| SEC01 | Truy cập pregnancy user khác | ❌ 403 | ☐ |
| SEC02 | Goal — Pregnancy user khác | ❌ 403 | ☐ |
| SEC03 | Không có token | ❌ 401 | ☐ |
| SEC04 | Motivational — Public endpoint | ✅ | ☐ |
| SEC05 | Không có permission | ❌ 403 | ☐ |
| SEC06 | Update log user khác | ❌ 403/404 | ☐ |
| **9. Edge Cases** | | | |
| E01 | Pregnancy không tồn tại | ❌ 404 | ☐ |
| E02 | Source = OCR | ✅ | ☐ |
| E03 | Chart data ordered | ✅ | ☐ |
| E04 | RapidLoss alert | ✅ | ☐ |
| E05 | AboveRange alert | ✅ | ☐ |
| E06 | BelowRange alert | ✅ | ☐ |
| E07 | BMI Category ranges | ✅ | ☐ |

**Tổng: 56 test cases** (37 happy path ✅ + 18 error/negative cases ❌ + 1 disabled ⚠️)

---

## ⚙️ RECOMMENDED TEST ORDER

1. **Pre-test** (W00 → W01) — Verify app chạy, có pregnancy Active
2. **Weight Goal** (G01 → G09) — Tạo goal trước để có `prePregnancyWeightKg` cho các test sau
3. **Weight Log** (L01 → L09) — Tạo weight logs, test CRUD + auto-alert
4. **Paging & Sort** (P01 → P03) — Test search/sort/paging
5. **Weight Chart** (C01 → C03) — Verify chart data (phụ thuộc có weight logs)
6. **Weight Alerts** (A01 → A04) — Verify alerts từ bước 3 + resolve
7. **Weight OCR** (O01 → O07) — Test OCR flow (cần Azure config)
8. **Motivational** (M01 → M06) — Test public endpoint (không cần auth)
9. **Ownership & Permissions** (SEC01 → SEC06) — Test phân quyền (cần 2 accounts)
10. **Edge Cases** (E01 → E07) — Integration + alert trigger edge cases

> **⚡ Tip**: Chạy G01 đầu tiên để có `prePregnancyWeightKg` → `weightGainFromBaseline` sẽ tính đúng cho weight logs. OCR tests (Section 6) có thể bỏ qua nếu chưa có Azure config.

---

## 📝 DTO FIELD REFERENCE

### WeightLogDto (response)
| Field | Type | Ghi chú |
|-------|------|---------|
| `id` | Guid | |
| `pregnancyId` | Guid | |
| `loggedOn` | DateOnly | `yyyy-MM-dd` |
| `weightKg` | decimal | |
| `note` | string? | |
| `source` | string | `"Manual"` \| `"OCR"` |
| `weightGainFromBaseline` | decimal? | `weightKg - prePregnancyWeightKg` |
| `createdAt` | DateTime | |
| `updatedAt` | DateTime | |

### WeightGoalDto (response)
| Field | Type | Ghi chú |
|-------|------|---------|
| `id` | Guid | |
| `pregnancyId` | Guid | |
| `heightCm` | decimal? | |
| `prePregnancyWeightKg` | decimal? | |
| `bmi` | decimal? | Auto-calculated |
| `bmiCategory` | string? | `"Underweight"` \| `"Normal"` \| `"Overweight"` \| `"Obese"` |
| `recommendedTotalGainMin` | decimal? | IOM-based hoặc user-supplied |
| `recommendedTotalGainMax` | decimal? | |
| `notes` | string? | |
| `createdAt` | DateTime | |
| `updatedAt` | DateTime | |

### WeightChartDataDto (response)
| Field | Type | Ghi chú |
|-------|------|---------|
| `prePregnancyWeightKg` | decimal? | |
| `recommendedGainMin` | decimal? | |
| `recommendedGainMax` | decimal? | |
| `currentWeightKg` | decimal? | Latest weight log |
| `totalGainKg` | decimal? | `currentWeight - prePregnancyWeight` |
| `totalEntries` | int | Count of weight logs |
| `dataPoints` | List | Sorted by `date` ASC |

### WeightChartPointDto (trong dataPoints)
| Field | Type | Ghi chú |
|-------|------|---------|
| `date` | DateOnly | **KHÔNG phải** `loggedOn` |
| `weightKg` | decimal | |
| `gestationalWeek` | int? | `(date - LMP).TotalDays / 7`, null nếu không có LMP |

### WeightOcrExtractResultDto (response)
| Field | Type | Ghi chú |
|-------|------|---------|
| `success` | bool | OCR tìm được cân hay không |
| `extractedWeightKg` | decimal? | 30–200 kg, null nếu không tìm được |
| `rawOcrText` | string? | Text thô từ Azure OCR |
| `confidenceScore` | decimal? | 0–1 |
| `message` | string | Vietnamese message |

### MotivationalTemplateDto (response)
| Field | Type | Ghi chú |
|-------|------|---------|
| `id` | Guid | |
| `category` | string | `"BabySize"` \| `"Milestone"` \| `"Tip"` |
| `weekStart` | int | |
| `weekEnd` | int | |
| `variablesJson` | string? | |
| `title` | string? | Ngôn ngữ theo `lang` param |
| `message` | string | Ngôn ngữ theo `lang` param |

> **⚠️**: KHÔNG có field `isActive`, `languageCode` trong response DTO.

---

## 🔑 PERMISSIONS REFERENCE

| Permission Code | Endpoints | Roles |
|----------------|-----------|-------|
| `weight_log.read` | GET weight-logs, GET chart | USER, DOCTOR |
| `weight_log.write` | POST weight-log, POST extract-weight, PUT weight-log | USER, DOCTOR |
| `weight_log.delete` | DELETE weight-log | USER, DOCTOR |
| `weight_goal.read` | GET weight-goals | USER, DOCTOR |
| `weight_goal.write` | POST weight-goal, PUT weight-goal | USER, DOCTOR |
| `weight_alert.read` | GET weight-alerts | USER, DOCTOR |
| `weight_alert.resolve` | PUT weight-alert resolve | USER, DOCTOR |

> Tất cả permissions đã được gán cho role **USER** và **DOCTOR** trong `DatabaseSeeder.cs`.
> Motivational endpoint (`GET /api/motivational`) là **public** — KHÔNG cần permission.

---

## 🔧 TROUBLESHOOTING

### OCR extract-weight trả về 500 Internal Server Error
- **Nguyên nhân**: Azure Doc Intelligence API key sai hoặc endpoint sai
- **Fix**: Kiểm tra `AI:AzureDocumentIntelligence:Endpoint` và `:ApiKey` trong `appsettings.json`

### OCR trả về "Không nhận diện được text từ ảnh"
- **Nguyên nhân**: Ảnh quá mờ hoặc không có text
- **Fix**: Dùng ảnh rõ nét, độ phân giải tối thiểu 300 DPI, số cân hiển thị rõ

### OCR trả về text nhưng không tìm được cân
- **Nguyên nhân**: Số cân ngoài range 30–200 kg, hoặc format không nhận diện được
- **Fix**: Đảm bảo ảnh hiển thị số cân dạng: `"59.5 kg"`, `"Weight: 59.5"`, `"Cân nặng: 59.5"`, hoặc `"59.5"` (standalone decimal)

### Weight log trả về 409 khi tạo
- **Nguyên nhân**: Đã có weight log cùng ngày cho pregnancy này
- **Fix**: Dùng ngày khác, hoặc PUT để update entry hiện có

### Alert không auto-trigger
- **Nguyên nhân 1**: Không có log nào cách log mới **7–14 ngày** (quá gần < 7 ngày hoặc quá xa > 14 ngày)
- **Nguyên nhân 2**: Weekly gain ≤ 0.7 kg (hoặc weekly loss ≥ -0.3 kg) — chưa vượt ngưỡng
- **Nguyên nhân 3**: **Cooldown** (chỉ RapidGain/RapidLoss) — đã có alert cùng loại trong 7 ngày gần nhất
- **Fix**: Tạo 2 entries cách nhau **7–14 ngày** với chênh lệch lớn (vd: +3 kg/week), đảm bảo không có alert cùng loại trong 7 ngày trước

### Chart gestationalWeek = null cho tất cả
- **Nguyên nhân**: Pregnancy không có `lastMenstrualPeriodDate`
- **Fix**: Update pregnancy để có LMP date trước khi xem chart

### Weight goal BMI = null dù có height/weight
- **Nguyên nhân**: Height hoặc weight = null (cả dto VÀ pregnancy)
- **Fix**: Gửi cả `heightCm` VÀ `prePregnancyWeightKg` trong request body

### Motivational trả về array rỗng
- **Nguyên nhân**: Không có template cho tuần đã chọn, hoặc không có translation cho language
- **Fix**: Kiểm tra seed data (30 templates, weeks 4–42), thử `lang=vi` hoặc `lang=en`

---

## 📁 FILES CREATED/MODIFIED IN WEEK 6

### Domain Layer
- `src/FPT.EXE201.Domain/Enums/WeightSource.cs`
- `src/FPT.EXE201.Domain/Enums/WeightAlertType.cs`
- `src/FPT.EXE201.Domain/Enums/MotivationalCategory.cs`
- `src/FPT.EXE201.Domain/Entities/WeightLog.cs`
- `src/FPT.EXE201.Domain/Entities/WeightGoalRange.cs`
- `src/FPT.EXE201.Domain/Entities/WeightAlert.cs` *(NO BaseEntity — immutable audit log)*
- `src/FPT.EXE201.Domain/Entities/MotivationalTemplate.cs`
- `src/FPT.EXE201.Domain/Entities/MotivationalTemplateTranslation.cs` *(NO BaseEntity — composite PK)*

### Application Layer
- `src/FPT.EXE201.Application/DTOs/WeightTracking/` — 9 DTO files (record types)
- `src/FPT.EXE201.Application/Validations/WeightTracking/` — 3 FluentValidation validators
- `src/FPT.EXE201.Application/IRepositories/` — 4 repository interfaces *(IWeightAlertRepository: added HasRecentAlertAsync)*
- `src/FPT.EXE201.Application/IServices/` — 3 service interfaces
- `src/FPT.EXE201.Application/Services/WeightLogService.cs` *(modified — alert logic: 7-14 day window + 7-day cooldown)*
- `src/FPT.EXE201.Application/Services/MotivationalService.cs`
- `src/FPT.EXE201.Application/Features/WeightLogs/WeightLogListQuerySpec.cs`
- `src/FPT.EXE201.Application/IUnitOfWork.cs` *(modified — 4 new repos)*
- `src/FPT.EXE201.Application/DependencyInjection.cs` *(modified)*
- `src/FPT.EXE201.Application/Common/Querying/QuerySpecRegistry.cs` *(modified)*

### Infrastructure Layer
- `src/FPT.EXE201.Infrastructure/Configurations/` — 5 EF configurations
- `src/FPT.EXE201.Infrastructure/Persistence/AppDbContext.cs` *(modified — 5 DbSets)*
- `src/FPT.EXE201.Infrastructure/Persistence/Seeders/MotivationalTemplateSeeder.cs`
- `src/FPT.EXE201.Infrastructure/Persistence/DatabaseSeeder.cs` *(modified — 7 permissions)*
- `src/FPT.EXE201.Infrastructure/Repositories/` — 4 repository implementations *(WeightAlertRepository: added HasRecentAlertAsync)*
- `src/FPT.EXE201.Infrastructure/Repositories/UnitOfWork.cs` *(modified)*
- `src/FPT.EXE201.Infrastructure/Services/WeightOcrService.cs`
- `src/FPT.EXE201.Infrastructure/DependencyInjection.cs` *(modified)*

### API Layer
- `src/FPT.EXE201.Api/Controllers/WeightLogsController.cs` *(11 endpoints)*
- `src/FPT.EXE201.Api/Controllers/MotivationalController.cs` *(1 public endpoint)*
