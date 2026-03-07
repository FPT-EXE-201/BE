# Nutrition & Meal Planning Workflow — AI-Powered Meal Generation

> **Mục đích**: Tài liệu workflow đầy đủ cho chức năng Nutrition & Meal Planning — giúp developer mới hoặc AI hiểu flow trước khi implement hoặc maintain code.  
> **Cập nhật**: 2026-03-04  
> **Trạng thái**: Week 7 ✅  
> **Xem thêm**: `WEEK_7_PROMPTS_GUIDE.md` (chi tiết code), `FEATURES_WORKFLOW_GUIDE.md` (tổng quan tất cả features), `TESTING_CHECKLIST_WEEK_7.md` (test cases)

---

## 1. TỔNG QUAN

Chức năng Nutrition & Meal Planning cho phép mẹ bầu:
1. **Khai báo sở thích / dị ứng** thực phẩm (Allergy, Dislike) → pick từ 45 food items seeded
2. **Ghi chú dinh dưỡng** dạng tự do (Diet / Note / Other) → AI sẽ đọc khi generate
3. **Generate thực đơn bằng AI** (Google Gemini 2.5 Flash) → 1-4 tuần, 4 bữa/ngày, có recipe + nutrients
4. **Xem chi tiết** thực đơn theo ngày (meals, nutrients localized, recipes)
5. **Đánh giá feedback** meal plan (1-5 sao) và từng meal item (like/dislike)

> **⚠️ KEY PRINCIPLES**:
> - AI generate thực đơn ĐỒNG BỘ (synchronous) — không dùng background job (khác với OCR ở Week 5)
> - Mỗi meal item bắt buộc có Recipe (REQUIRED — không optional)
> - BMI + target calories tính tự động từ profile → AI tuân theo
> - Rate limit: **15 AI calls/ngày/user** (mỗi tuần = 1 call)
> - Overlap auto-delete: plan mới trùng ngày → plan cũ tự soft-delete

---

## 2. ENTITY RELATIONSHIP DIAGRAM

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        ENTITY RELATIONSHIPS                              │
│                                                                          │
│  User                                                                    │
│    │                                                                     │
│    │ 1:N (owns)                                                          │
│    ▼                                                                     │
│  Pregnancy                                                               │
│    │                                                                     │
│    ├──── 1:N ──── PregnancyFoodPreference ──── N:1 ──── RefFoodItem     │
│    │                   │                                    │             │
│    │                   └── Allergy/Dislike                   │             │
│    │                       + Severity                  1:N   │             │
│    │                                              RefFoodItemTranslation  │
│    │                                                   (vi, en)           │
│    │                                                                     │
│    ├──── 1:N ──── PregnancyNutritionNote                                │
│    │                   │                                                 │
│    │                   └── NoteType (Diet/Note/Other) + ValueText        │
│    │                                                                     │
│    ├──── 1:N ──── Recipe ────────── 1:N ──── MealItem (via RecipeId)    │
│    │                                                                     │
│    ├──── 1:N ──── MealPlan                                              │
│    │                 │                                                   │
│    │                 ├── 1:N ──── MealPlanDay                            │
│    │                 │               │                                   │
│    │                 │               └── 1:N ──── MealItem               │
│    │                 │                               │                   │
│    │                 │                    ┌───────────┼───────────┐       │
│    │                 │                    │           │           │       │
│    │                 │               N:1  │      1:N  │      1:N │       │
│    │                 │             Recipe │  MealItem │   MealItem│       │
│    │                 │                    │  Nutrient │   Feedback│       │
│    │                 │                    │    ↕ N:1  │           │       │
│    │                 │                    │ RefNutrient│           │       │
│    │                 │                    │     │      │           │       │
│    │                 │                    │  1:N│      │           │       │
│    │                 │                    │ RefNutrient│           │       │
│    │                 │                    │ Translation│           │       │
│    │                 │                    │  (vi, en)  │           │       │
│    │                 │                    └───────────┘           │       │
│    │                 │                                            │       │
│    │                 └── 1:N ──── MealPlanFeedback                │       │
│    │                                                              │       │
│    └──── 1:N ──── AiRequestLog ──── N:1 ──── AiPromptTemplate   │       │
│                                                                   │       │
│  ⚠️ MealPlan.AiRequestLogId (nullable FK, link first week only)  │       │
└──────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Entity Summary

| Entity | Table | Vai trò | BaseEntity | Soft Delete |
|--------|-------|---------|:----------:|:-----------:|
| `RefFoodItem` | `ref_food_items` | Master data thực phẩm (45 items seeded) | ✅ | ✅ |
| `RefFoodItemTranslation` | `ref_food_item_translations` | i18n tên thực phẩm (vi, en) | ❌ composite PK | ❌ |
| `RefNutrient` | `ref_nutrients` | Master data dưỡng chất (15 items) | ❌ custom | ❌ |
| `RefNutrientTranslation` | `ref_nutrient_translations` | i18n tên dưỡng chất | ❌ composite PK | ❌ |
| `PregnancyFoodPreference` | `pregnancy_food_preferences` | Dị ứng / không thích (user picks) | ✅ | ✅ |
| `PregnancyNutritionNote` | `pregnancy_nutrition_notes` | Ghi chú dinh dưỡng tự do | ✅ | ✅ |
| `Recipe` | `recipes` | Công thức nấu ăn (AI-generated) | ✅ | ✅ |
| `MealPlan` | `meal_plans` | Kế hoạch bữa ăn 1-4 tuần | ✅ | ✅ |
| `MealPlanDay` | `meal_plan_days` | 1 ngày trong plan | ✅ | ✅ |
| `MealItem` | `meal_items` | 1 món ăn trong 1 bữa | ✅ | ✅ |
| `MealItemNutrient` | `meal_item_nutrients` | Bridge: meal_item ↔ nutrient + amount | ❌ composite PK | ❌ |
| `MealPlanFeedback` | `meal_plan_feedback` | Đánh giá plan (1-5 sao) | ✅ | ✅ |
| `MealItemFeedback` | `meal_item_feedback` | Đánh giá món ăn (like/dislike) | ✅ | ✅ |
| `AiRequestLog` | `ai_request_logs` | Log mỗi AI API call | ✅ | ✅ |

### 2.2 Key Relationships

| From | To | Type | FK | Ghi chú |
|------|-----|------|-----|---------|
| PregnancyFoodPreference | Pregnancy | N:1 | `pregnancy_id` NOT NULL | Cascade delete |
| PregnancyFoodPreference | RefFoodItem | N:1 | `food_item_id` NOT NULL | Restrict delete |
| PregnancyNutritionNote | Pregnancy | N:1 | `pregnancy_id` NOT NULL | Cascade delete |
| MealPlan | Pregnancy | N:1 | `pregnancy_id` NOT NULL | Cascade delete |
| MealPlan | AiRequestLog | N:1 | `ai_request_log_id` NULL | First week's log |
| MealPlanDay | MealPlan | N:1 | `meal_plan_id` NOT NULL | Cascade delete |
| MealItem | MealPlanDay | N:1 | `meal_day_id` NOT NULL | Cascade delete |
| MealItem | Recipe | N:1 | `recipe_id` NULL | Set null on delete |
| MealItemNutrient | MealItem | N:1 | `meal_item_id` NOT NULL | Cascade delete |
| MealItemNutrient | RefNutrient | N:1 | `nutrient_id` NOT NULL | Restrict delete |
| MealPlanFeedback | MealPlan | N:1 | `meal_plan_id` NOT NULL | Cascade delete |
| MealPlanFeedback | User | N:1 | `user_id` NOT NULL | Cascade delete |
| MealItemFeedback | MealItem | N:1 | `meal_item_id` NOT NULL | Cascade delete |
| MealItemFeedback | User | N:1 | `user_id` NOT NULL | Cascade delete |
| Recipe | Pregnancy | N:1 | `pregnancy_id` NOT NULL | Cascade delete |

### 2.3 Unique Constraints

| Table | Columns | Index Name | Ghi chú |
|-------|---------|-----------|---------|
| `pregnancy_food_preferences` | `(pregnancy_id, food_item_id, preference_type)` | `uk_food_pref_pregnancy` | IgnoreQueryFilters for soft delete |
| `meal_plan_days` | `(meal_plan_id, plan_date)` | `uk_meal_plan_days` | 1 date per plan |
| `meal_plan_feedback` | `(meal_plan_id, user_id)` | `uk_meal_plan_feedback` | 1 feedback/user/plan |
| `meal_item_feedback` | `(meal_item_id, user_id)` | `uk_meal_item_feedback` | 1 feedback/user/item |

> **⚠️ Soft Delete + Unique Constraint**: MySQL không hỗ trợ filtered indexes. Do đó, repository dùng `IgnoreQueryFilters()` để tìm cả records đã soft-delete → service layer restore (set `DeletedAt = null`) thay vì insert mới. Pattern này giống `WeightLogRepository`.

---

## 3. FULL PIPELINE FLOW — 3 PHASES

### 3.1 Phase 1: SETUP — Khai báo sở thích dinh dưỡng

```
┌─────────────────────────────────────────────────────────────────────────┐
│                PHASE 1: NUTRITION PROFILE SETUP                         │
│                                                                         │
│  ⬇ Flutter app                                                          │
│                                                                         │
│  Step A: Lấy danh sách thực phẩm tham khảo                             │
│  GET /api/ref/food-items?lang=vi                                        │
│  → 45 food items (Thịt gà, Đậu phộng, Tôm, Sầu riêng...)             │
│                                                                         │
│  Step B: User chọn dị ứng / không thích                                 │
│  POST /api/pregnancies/{id}/food-preferences?lang=vi                    │
│  body: { foodItemId, preferenceType, severity?, notes? }                │
│       │                                                                 │
│       ├── Allergy + severity (Low/Medium/High) → AI PHẢI tránh         │
│       └── Dislike (không cần severity) → AI nên tránh                   │
│       │                                                                 │
│       ▼                                                                 │
│  FoodPreferenceService.CreatePreferenceAsync                            │
│       │                                                                 │
│       ├── VerifyPregnancyOwnership (userId == pregnancy.UserId)         │
│       ├── Validate food item exists (RefFoodItems)                      │
│       ├── FindByKeyIncludingDeletedAsync (IgnoreQueryFilters)           │
│       │   ├── Active exists → 409 ConflictException                    │
│       │   ├── Soft-deleted exists → RESTORE (set DeletedAt=null)       │
│       │   └── Not exists → CREATE new entity                           │
│       └── SaveChanges → return FoodPreferenceDto                        │
│                                                                         │
│  Step C: User thêm ghi chú dinh dưỡng (optional)                       │
│  POST /api/pregnancies/{id}/nutrition-notes                             │
│  body: { noteType: "Diet", valueText: "Ăn chay, không thịt đỏ" }      │
│       │                                                                 │
│       └── PregnancyNutritionNote entity (no unique constraint)          │
│                                                                         │
│  ✅ Profile Setup DONE — ready for AI generation                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Phase 2: AI GENERATE — Tạo thực đơn

```
┌─────────────────────────────────────────────────────────────────────────┐
│            PHASE 2: AI MEAL PLAN GENERATION                             │
│                                                                         │
│  POST /api/pregnancies/{id}/meal-plans/generate                         │
│  body: { startDate, durationWeeks (1-4), additionalNotes? }             │
│                                                                         │
│  ┌─ MealPlanService.GenerateAsync ─────────────────────────────────┐    │
│  │                                                                  │   │
│  │  Step 1: VerifyPregnancyOwnership                                │   │
│  │       └── pregnancy.UserId == currentUserId?                     │   │
│  │                                                                  │   │
│  │  Step 2: Validate durationWeeks (1–4)                            │   │
│  │       └── Cũng validate ở FluentValidation (double check)        │   │
│  │                                                                  │   │
│  │  Step 3: Rate Limit Check                                        │   │
│  │       │  AiRequestLogs.CountTodayByUserAsync(userId)             │   │
│  │       │  remaining = 15 - todayCount                             │   │
│  │       │  if (remaining < durationWeeks) → 400 Bad Request        │   │
│  │       │  "Daily AI limit: need X calls, remaining Y/15"          │   │
│  │       ▼                                                          │   │
│  │  Step 4: Calculate BMI + Target Calories                         │   │
│  │       │  bmiWeight = PrePregnancyWeightKg ?? currentWeight       │   │
│  │       │  (currentWeight = latest WeightLog)                      │   │
│  │       │  heightM = HeightCm / 100                                │   │
│  │       │  bmi = weight / (height²)                                │   │
│  │       │                                                          │   │
│  │       │  ┌─ IOM Base Calories ──────────────────────┐            │   │
│  │       │  │  BMI < 18.5 (Underweight) → 2400 kcal   │            │   │
│  │       │  │  BMI 18.5–24.9 (Normal)   → 2200 kcal   │            │   │
│  │       │  │  BMI 25.0–29.9 (Overweight)→ 2000 kcal  │            │   │
│  │       │  │  BMI ≥ 30.0 (Obese)       → 1800 kcal   │            │   │
│  │       │  └─────────────────────────────────────────┘            │   │
│  │       │                                                          │   │
│  │       │  ┌─ Trimester Bonus ────────────────────────┐            │   │
│  │       │  │  T1 (week 1–12)  → +0 kcal              │            │   │
│  │       │  │  T2 (week 13–27) → +340 kcal             │            │   │
│  │       │  │  T3 (week 28+)   → +450 kcal             │            │   │
│  │       │  └─────────────────────────────────────────┘            │   │
│  │       │                                                          │   │
│  │       │  targetCalories = baseCalories + trimesterBonus          │   │
│  │       ▼                                                          │   │
│  │  Step 5: Handle Overlap — Auto Soft-Delete                       │   │
│  │       │  endDate = startDate + (durationWeeks * 7 - 1)           │   │
│  │       │  overlapping = MealPlans.GetOverlappingAsync(...)        │   │
│  │       │  → Soft delete ALL overlapping plans                     │   │
│  │       │  → Log: "Auto-deleted overlapping meal plan {PlanId}"    │   │
│  │       ▼                                                          │   │
│  │  Step 6: Collect Nutrition Context (cho AI prompt)               │   │
│  │       │  ├── FoodPreferences (allergies + dislikes)              │   │
│  │       │  ├── NutritionNotes (dietary notes)                      │   │
│  │       │  └── PregnancyConditions (GDM, preeclampsia...)          │   │
│  │       ▼                                                          │   │
│  │  Step 7: Load AI Template + Nutrient Cache                       │   │
│  │       │  ├── AiPromptTemplate key="nutrition.meal_plan"          │   │
│  │       │  └── RefNutrients → Dictionary<Code, Guid>               │   │
│  │       ▼                                                          │   │
│  │  Step 8: Create MealPlan Entity                                  │   │
│  │       │  { PregnancyId, StartDate, EndDate, Source = AI }        │   │
│  │       ▼                                                          │   │
│  │  ┌─ Step 9: Transaction — Week-by-Week Generation ───────────┐  │   │
│  │  │                                                             │  │   │
│  │  │  BeginTransactionAsync                                      │  │   │
│  │  │  AddAsync(mealPlan)                                         │  │   │
│  │  │                                                             │  │   │
│  │  │  for (week = 0; week < durationWeeks; week++)               │  │   │
│  │  │  {                                                          │  │   │
│  │  │     ├── Create AiRequestLog (Status = Processing)           │  │   │
│  │  │     │   └── Link first log to mealPlan.AiRequestLogId      │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── Build Prompt (PromptBuilder pipeline)               │  │   │
│  │  │     │   ├── FromTemplate(template)                          │  │   │
│  │  │     │   ├── WithContext("NUTRITION PROFILE", contextText)   │  │   │
│  │  │     │   ├── WithUserMessage(weekPrompt)                     │  │   │
│  │  │     │   └── Build()                                         │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── IAiProvider.GenerateAsync(prompt)  ← SYNCHRONOUS   │  │   │
│  │  │     │   └── Google Gemini 2.5 Flash                         │  │   │
│  │  │     │       (Temperature=0.7, MaxOutputTokens=8192)         │  │   │
│  │  │     │       ⏱️ 15-60 giây mỗi week                         │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── ParseMealPlanResponse(aiResponse.Content)           │  │   │
│  │  │     │   ├── CleanAiJsonResponse (remove markdown fences)    │  │   │
│  │  │     │   ├── RepairTruncatedJson (fix unbalanced braces)     │  │   │
│  │  │     │   └── Deserialize → AiWeekResponse                   │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── Set plan.Title from first week                      │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── CreateWeekEntities (7 days × 4 meals each)          │  │   │
│  │  │     │   ├── MealPlanDay (planDate)                          │  │   │
│  │  │     │   ├── Recipe (title, instructions, servings...)       │  │   │
│  │  │     │   ├── MealItem (mealType, itemName, calories...)      │  │   │
│  │  │     │   └── MealItemNutrient (nutrientId, amount)           │  │   │
│  │  │     │       └── Match by Code → Guid from nutrientMap      │  │   │
│  │  │     │                                                       │  │   │
│  │  │     ├── Update AiRequestLog → Succeeded                    │  │   │
│  │  │     │   (model, tokensIn/Out, processingTime, response)     │  │   │
│  │  │     │                                                       │  │   │
│  │  │     └── BuildWeekSummary → previousWeekSummary              │  │   │
│  │  │         (cho prompt tuần tiếp theo: "đa dạng, ko lặp lại") │  │   │
│  │  │  }                                                          │  │   │
│  │  │                                                             │  │   │
│  │  │  CommitTransactionAsync ← ALL-OR-NOTHING                   │  │   │
│  │  │                                                             │  │   │
│  │  │  ┌─ catch (Exception) ──────────────────────────────────┐   │  │   │
│  │  │  │  RollbackTransactionAsync                             │   │  │   │
│  │  │  │  Log error + completedWeeks                           │   │  │   │
│  │  │  │  Persist Failed AiRequestLog (AFTER rollback)        │   │  │   │
│  │  │  │  → Đảm bảo analytics không bị mất                   │   │  │   │
│  │  │  └──────────────────────────────────────────────────────┘   │  │   │
│  │  └─────────────────────────────────────────────────────────────┘  │   │
│  │                                                                  │   │
│  │  Step 10: Return GetDetailAsync(mealPlan.Id)                     │   │
│  │       → MealPlanDetailDto (plan + days summary)                  │   │
│  │                                                                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Response: 201 Created → MealPlanDetailDto                              │
│  ⏱️ Total time: durationWeeks × 15-60s (1 week ≈ 30s average)          │
│  ⚠️ Client timeout phải ≥ 120s                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Phase 3: CONSUME — Xem / Đánh giá / Xóa

```
┌─────────────────────────────────────────────────────────────────────────┐
│                PHASE 3: CONSUME — View + Feedback + Delete              │
│                                                                         │
│  ┌─ 3A. LIST MEAL PLANS ─────────────────────────────────────────────┐ │
│  │  GET /api/pregnancies/{id}/meal-plans?page=1&pageSize=10           │ │
│  │      &search=keyword&searchBy=title,notes                          │ │
│  │      &sort=startdate_asc                                           │ │
│  │                                                                    │ │
│  │  → PagedResult<MealPlanSummaryDto>                                 │ │
│  │    { id, startDate, endDate, source, title, totalDays, createdAt } │ │
│  │                                                                    │ │
│  │  ⚠️ Supports: search (title, notes), sort (startdate, enddate,    │ │
│  │     createdat), paging. Default: createdat_desc                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3B. PLAN DETAIL ─────────────────────────────────────────────────┐ │
│  │  GET /api/pregnancies/{id}/meal-plans/{planId}                     │ │
│  │                                                                    │ │
│  │  → MealPlanDetailDto                                               │ │
│  │    { id, startDate, endDate, source, title, notes,                 │ │
│  │      days: [ { id, planDate, totalCalories, mealCount } ],         │ │
│  │      createdAt, updatedAt }                                        │ │
│  │                                                                    │ │
│  │  Days sorted by planDate ascending                                 │ │
│  │  totalCalories = SUM(items.caloriesKcal)                           │ │
│  │  mealCount = COUNT(items)                                          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3C. DAY DETAIL ──────────────────────────────────────────────────┐ │
│  │  GET /api/meal-plans/{planId}/days/{date}?lang=vi                  │ │
│  │                                                                    │ │
│  │  → MealDayDetailDto                                                │ │
│  │    { id, mealPlanId, planDate, totalCalories,                      │ │
│  │      meals: [                                                      │ │
│  │        { id, mealType, recipeId, itemName, portionText,            │ │
│  │          caloriesKcal, notes,                                      │ │
│  │          nutrients: [                                               │ │
│  │            { nutrientCode, nutrientName, unit, amount }            │ │
│  │          ]                                                         │ │
│  │        }                                                           │ │
│  │      ] }                                                           │ │
│  │                                                                    │ │
│  │  ⚠️ nutrientName localized via lang parameter (vi/en)              │ │
│  │  ⚠️ Meals sorted by mealType (Breakfast < Lunch < Dinner < Snack) │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3D. RECIPE DETAIL ───────────────────────────────────────────────┐ │
│  │  GET /api/recipes/{recipeId}                                       │ │
│  │                                                                    │ │
│  │  → RecipeDetailDto                                                 │ │
│  │    { id, pregnancyId, title, instructions, servings,               │ │
│  │      prepMinutes, cookMinutes, createdAt }                         │ │
│  │                                                                    │ │
│  │  ⚠️ Ownership verified: recipe.Pregnancy.UserId == currentUserId  │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3E. MEAL PLAN FEEDBACK ──────────────────────────────────────────┐ │
│  │  POST /api/meal-plans/{planId}/feedback                            │ │
│  │  body: { rating (1-5), comment? }                                  │ │
│  │                                                                    │ │
│  │  → MealPlanFeedbackDto                                             │ │
│  │    { id, mealPlanId, userId, rating, comment, createdAt }          │ │
│  │                                                                    │ │
│  │  ⚠️ 1 feedback per user per plan (unique constraint)               │ │
│  │  ⚠️ Soft-deleted feedback → restored on re-submit                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3F. MEAL ITEM FEEDBACK ──────────────────────────────────────────┐ │
│  │  POST /api/meal-items/{itemId}/feedback                            │ │
│  │  body: { liked (bool), comment? }                                  │ │
│  │                                                                    │ │
│  │  → MealItemFeedbackDto                                             │ │
│  │    { id, mealItemId, userId, liked, comment, createdAt }           │ │
│  │                                                                    │ │
│  │  ⚠️ 1 feedback per user per item (unique constraint)               │ │
│  │  ⚠️ Ownership verified: item → day → plan → pregnancy → user      │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─ 3G. DELETE MEAL PLAN ─────────────────────────────────────────────┐ │
│  │  DELETE /api/pregnancies/{id}/meal-plans/{planId}                   │ │
│  │                                                                    │ │
│  │  → Soft delete (set DeletedAt, không xóa vật lý)                   │ │
│  │  → AiRequestLog / Recipes VẪN GIỮ                                 │ │
│  │  → Plan không xuất hiện trong list nữa                             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. AI PROMPT PIPELINE — Chi tiết

### 4.1 PromptBuilder Architecture (4-Layer)

```
┌────────────────────────────────────────────────────────────────────┐
│                   AI PROMPT — 4 LAYERS                             │
│                                                                    │
│  Layer 1: SYSTEM RULES (from AiPromptTemplate.SystemRules)         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  "You are a certified prenatal nutritionist AI assistant.    │  │
│  │   Respond in Vietnamese.                                     │  │
│  │   Output ONLY valid JSON matching the provided schema."      │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  Layer 2: DOMAIN RULES (from AiPromptTemplate.DomainRules)         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  IOM pregnancy nutrition guidelines:                         │  │
│  │  - T1: folic acid 600mcg/day, no extra calories              │  │
│  │  - T2: +340 kcal, iron 27mg, calcium 1000mg                  │  │
│  │  - T3: +450 kcal, increase protein, DHA                      │  │
│  │  - Daily water 2.3L, avoid raw fish/alcohol/soft cheese      │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  Layer 3: FEATURE RULES (from AiPromptTemplate.FeatureRules)       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  "Generate 7-day meal plan with 4 meals/day:                 │  │
│  │   BREAKFAST, LUNCH, DINNER, SNACK.                            │  │
│  │   EVERY meal MUST have recipe + nutrients.                    │  │
│  │   Use ONLY nutrient codes: PROTEIN, IRON, CALCIUM..."        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  Layer 4: RAG CONTEXT (dynamic, per-request)                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  NUTRITION PROFILE:                                          │  │
│  │  - Tuần thai: 24                                             │  │
│  │  - BMI: 22.5                                                 │  │
│  │  - Cân nặng: 62.0 kg                                        │  │
│  │  - Calories mục tiêu: 2540 kcal/ngày                        │  │
│  │  - Dị ứng: Đậu phộng, Tôm                                   │  │
│  │  - Không thích: Rau mùi                                      │  │
│  │  - Ghi chú: [Diet] Ăn chay linh hoạt                        │  │
│  │  - Bệnh lý: Tiểu đường thai kỳ                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  + USER MESSAGE (week-specific)                                    │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  "Tạo thực đơn 7 ngày từ 2026-03-09 đến 2026-03-15.        │  │
│  │   Mục tiêu: ~2540 kcal/ngày.                                │  │
│  │   Mỗi ngày cần 4 bữa: BREAKFAST, LUNCH, DINNER, SNACK.     │  │
│  │   Mỗi món PHẢI có recipe đầy đủ.                            │  │
│  │   Yêu cầu thêm: Ưu tiên món Việt Nam"                       │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  → IAiProvider.GenerateAsync(prompt) → AiResponse                  │
│    { Content, PromptTokens, CompletionTokens, ModelUsed, Time }    │
└────────────────────────────────────────────────────────────────────┘
```

### 4.2 AI Response JSON Schema

```json
{
  "title": "Thực đơn tuần 20 thai kỳ",
  "totalDailyCalories": 2540,
  "notes": "Thực đơn phù hợp cho mẹ bầu tam cá nguyệt 2",
  "days": [
    {
      "date": "2026-03-09",
      "meals": [
        {
          "mealType": "BREAKFAST",
          "itemName": "Cháo yến mạch với trứng và rau bina",
          "portionText": "1 bát lớn (350ml)",
          "caloriesKcal": 450,
          "notes": "Giàu sắt và acid folic cho tam cá nguyệt 2",
          "recipe": {
            "title": "Cháo yến mạch với trứng",
            "instructions": "1. Nấu yến mạch...\n2. Đánh trứng...",
            "servings": 1,
            "prepMinutes": 5,
            "cookMinutes": 15
          },
          "nutrients": [
            { "code": "PROTEIN", "amount": 15.5 },
            { "code": "IRON", "amount": 3.2 },
            { "code": "FOLIC_ACID", "amount": 180.0 }
          ]
        }
      ]
    }
  ]
}
```

### 4.3 JSON Cleanup Pipeline

```
AI raw response
     │
     ▼
CleanAiJsonResponse()
     ├── Remove markdown code fences (```json ... ```)
     ├── Find JSON start (first { or [)
     └── Find JSON end (last } or ])
     │
     ▼
RepairTruncatedJson()
     ├── Count unbalanced { } [ ]
     ├── Strip trailing incomplete entry after last comma
     └── Append missing ] then }
     │
     ▼
JsonSerializer.Deserialize<AiWeekResponse>()
     ├── Success → proceed
     └── Failure → BadRequestException("AI returned invalid meal plan format")
```

### 4.4 Multi-Week Generation

```
Week 1:
  prompt = base context + "Tạo thực đơn 7 ngày từ 2026-03-09 đến 2026-03-15"
  → previousWeekSummary = "Các món đã có: Cháo yến mạch, Canh chua cá lóc, ..."

Week 2:
  prompt = base context + "Tiếp tục thực đơn tuần 2, 2026-03-16 đến 2026-03-22"
         + "Tóm tắt tuần trước: Các món đã có: ..."
         + "Đảm bảo đa dạng, không lặp lại món ăn tuần trước."
  → previousWeekSummary = updated

Week 3, 4: tương tự...
```

---

## 5. API ENDPOINTS

### 5.1 Reference Data (Public — No Auth)

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/api/ref/food-items?lang=vi` | 45 food items (picker UI) |
| GET | `/api/ref/nutrients?lang=vi` | 15 nutrients (info display) |
| GET | `/api/ref/enums` | All enums including Week 7 additions |
| GET | `/api/ref/enums/{name}` | Single enum by name |
| GET | `/api/ref/query-specs/mealPlans` | Search/sort spec for meal plans |

### 5.2 Food Preferences CRUD

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| GET | `/api/pregnancies/{id}/food-preferences?lang=vi` | `food_preference.read` | List preferences (with localized food names) |
| POST | `/api/pregnancies/{id}/food-preferences?lang=vi` | `food_preference.write` | Create preference (restore if soft-deleted) |
| PUT | `/api/pregnancies/{id}/food-preferences/{prefId}?lang=vi` | `food_preference.write` | Update severity/notes |
| DELETE | `/api/pregnancies/{id}/food-preferences/{prefId}` | `food_preference.delete` | Soft delete |

### 5.3 Nutrition Notes CRUD

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| GET | `/api/pregnancies/{id}/nutrition-notes` | `nutrition_note.read` | List notes |
| POST | `/api/pregnancies/{id}/nutrition-notes` | `nutrition_note.write` | Create note |
| PUT | `/api/pregnancies/{id}/nutrition-notes/{noteId}` | `nutrition_note.write` | Update note |
| DELETE | `/api/pregnancies/{id}/nutrition-notes/{noteId}` | `nutrition_note.delete` | Soft delete |

### 5.4 Meal Plan Operations

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| POST | `/api/pregnancies/{id}/meal-plans/generate` | `meal_plan.generate` | AI generate (1-4 weeks) |
| GET | `/api/pregnancies/{id}/meal-plans` | `meal_plan.read` | List with search/sort/paging |
| GET | `/api/pregnancies/{id}/meal-plans/{planId}` | `meal_plan.read` | Plan detail (days summary) |
| DELETE | `/api/pregnancies/{id}/meal-plans/{planId}` | `meal_plan.delete` | Soft delete |
| GET | `/api/meal-plans/{planId}/days/{date}?lang=vi` | `meal_plan.read` | Day detail (meals + nutrients) |

### 5.5 Recipe & Feedback

| Method | Endpoint | Permission | Mô tả |
|--------|----------|------------|--------|
| GET | `/api/recipes/{recipeId}` | `recipe.read` | Recipe detail |
| POST | `/api/meal-plans/{planId}/feedback` | `meal_plan_feedback.write` | Rate plan (1-5 stars) |
| POST | `/api/meal-items/{itemId}/feedback` | `meal_item_feedback.write` | Like/dislike meal item |

---

## 6. SEEDED DATA

### 6.1 Food Items (45 items, 6 categories)

| Category | Count | Example Codes |
|----------|-------|---------------|
| Proteins | 13 | CHICKEN, PORK, BEEF, FISH_SALMON, SHRIMP, TOFU, TEMPEH |
| Allergens | 8 | SEAFOOD_GENERAL, PEANUT, MILK_COW, GLUTEN, SOYBEAN |
| Vegetables | 9 | CILANTRO, BITTER_MELON, SPINACH, BOK_CHOY, GARLIC |
| Fruits | 4 | DURIAN, JACKFRUIT, PINEAPPLE, PAPAYA_GREEN |
| Condiments | 7 | SHRIMP_PASTE, MSG, ORGAN_MEAT_LIVER, CAFFEINE, ALCOHOL |
| Pregnancy-avoid | 4 | RAW_FISH, SOFT_CHEESE, RAW_EGG, DELI_MEAT |

Mỗi item có translations: Vietnamese (`vi`) + English (`en`).

### 6.2 Nutrients (15 items)

| Code | Vietnamese | English | Unit |
|------|-----------|---------|------|
| PROTEIN | Chất đạm | Protein | g |
| FAT | Chất béo | Fat | g |
| CARBS | Carbohydrate | Carbohydrates | g |
| FIBER | Chất xơ | Fiber | g |
| IRON | Sắt | Iron | mg |
| CALCIUM | Canxi | Calcium | mg |
| FOLIC_ACID | Acid folic | Folic Acid | µg |
| VITAMIN_A | Vitamin A | Vitamin A | µg |
| VITAMIN_C | Vitamin C | Vitamin C | mg |
| VITAMIN_D | Vitamin D | Vitamin D | µg |
| ZINC | Kẽm | Zinc | mg |
| OMEGA3 | Omega-3 | Omega-3 | mg |
| IODINE | I-ốt | Iodine | µg |
| DHA | DHA | DHA | mg |
| MAGNESIUM | Magiê | Magnesium | mg |

### 6.3 AI Prompt Template

| Field | Value |
|-------|-------|
| Key | `nutrition.meal_plan` |
| Model | `gemini-2.5-flash` |
| Temperature | 0.7 |
| MaxOutputTokens | 8192 |
| Version | 1 |

### 6.4 Permissions (12)

| Permission | USER | DOCTOR | Ghi chú |
|-----------|:----:|:------:|---------|
| `food_preference.read` | ✅ | ✅ | |
| `food_preference.write` | ✅ | ✅ | |
| `food_preference.delete` | ✅ | ✅ | |
| `nutrition_note.read` | ✅ | ✅ | |
| `nutrition_note.write` | ✅ | ✅ | |
| `nutrition_note.delete` | ✅ | ✅ | |
| `meal_plan.read` | ✅ | ✅ | |
| `meal_plan.generate` | ✅ | ❌ | **DOCTOR không generate** |
| `meal_plan.delete` | ✅ | ✅ | |
| `recipe.read` | ✅ | ✅ | |
| `meal_plan_feedback.write` | ✅ | ✅ | |
| `meal_item_feedback.write` | ✅ | ✅ | |

---

## 7. ENUMS

```csharp
public enum FoodPreferenceType   // Loại sở thích
{
    Allergy,    // Dị ứng → AI PHẢI tránh
    Dislike     // Không thích → AI nên tránh
}

public enum AllergySeverity      // Mức độ dị ứng
{
    Low,        // Nhẹ
    Medium,     // Trung bình
    High        // Nặng (sốc phản vệ)
}

public enum NutritionNoteType    // Loại ghi chú
{
    Diet,       // Chế độ ăn (ăn chay, keto, ...)
    Note,       // Ghi chú y tế (bác sĩ khuyên)
    Other       // Khác
}

public enum MealType             // Bữa ăn
{
    Breakfast,  // Sáng
    Lunch,      // Trưa
    Dinner,     // Tối
    Snack       // Ăn nhẹ
}

public enum MealPlanSource       // Nguồn tạo plan
{
    AI,         // AI-generated (Gemini)
    Manual      // User tạo tay (future)
}

public enum AiFeature            // Feature ID cho log
{
    MedicalExtraction,    // Week 5 (OCR)
    NutritionMealPlan,    // Week 7
    NutritionChat,        // Future
    DoctorChat            // Future
}

public enum AiRequestStatus      // Trạng thái AI request
{
    Pending,       // Chờ xử lý
    Processing,    // Đang xử lý
    Succeeded,     // Thành công
    Failed         // Thất bại
}
```

---

## 8. BUSINESS RULES

| Rule | Chi tiết |
|------|----------|
| **Ownership** | User chỉ access resources của own pregnancies (VerifyPregnancyOwnership) |
| **BMI required** | Pre-pregnancy weight (hoặc latest weight log) + height phải có để tính calories |
| **Rate limit** | 15 AI calls/ngày/user. Mỗi tuần = 1 call. 4 tuần = 4 calls. Reset hàng ngày (UTC) |
| **Overlap** | Plan mới trùng date range → plan cũ auto soft-delete |
| **ALL-OR-NOTHING** | DB transaction: nếu bất kỳ week nào fail → rollback toàn bộ plan |
| **Recipe required** | Mỗi meal item bắt buộc có recipe (AI rule). Fallback: nếu AI không provide → skip |
| **Nutrient matching** | AI trả nutrient CODE → match với RefNutrient.Code → lấy Guid. Unknown code → warning log, skip |
| **Unique feedback** | 1 feedback per user per plan. 1 feedback per user per item. Soft-deleted → restored |
| **Unique preference** | 1 preference per (pregnancy, food_item, preference_type). Soft-deleted → restored |
| **Soft delete** | `deleted_at` column + global query filter. IgnoreQueryFilters cho unique checks |
| **i18n** | Food items, nutrients có translations (vi, en). Query param `lang` cho display name |
| **Calorie calculation** | IOM guidelines: base from BMI category + trimester bonus |
| **Gestational week** | CurrentGestationalWeek ?? calculated from LastMenstrualPeriodDate ?? fallback 20 |
| **AI synchronous** | Generate call blocks until complete (15-60s/week). KHÔNG dùng background job |
| **Failed log persist** | Khi AI fail, AiRequestLog (Status=Failed) được persist SAU rollback (separate save) |

---

## 9. FILE STRUCTURE — Code liên quan

```
src/
├── FPT.EXE201.Domain/
│   ├── Entities/
│   │   ├── RefFoodItem.cs                ← Master food item (Code, IsActive)
│   │   ├── RefFoodItemTranslation.cs     ← i18n (composite PK)
│   │   ├── RefNutrient.cs                ← Custom entity (NO BaseEntity)
│   │   ├── RefNutrientTranslation.cs     ← i18n (composite PK)
│   │   ├── PregnancyFoodPreference.cs    ← User allergy/dislike
│   │   ├── PregnancyNutritionNote.cs     ← Free-text dietary notes
│   │   ├── Recipe.cs                     ← AI-generated recipe
│   │   ├── MealPlan.cs                   ← Weekly plan (1-4 weeks)
│   │   ├── MealPlanDay.cs                ← 1 day in plan
│   │   ├── MealItem.cs                   ← 1 food item in 1 meal
│   │   ├── MealItemNutrient.cs           ← Bridge: item ↔ nutrient (composite PK)
│   │   ├── MealPlanFeedback.cs           ← Plan rating (1-5)
│   │   ├── MealItemFeedback.cs           ← Item like/dislike
│   │   └── AiRequestLog.cs              ← AI call log (shared with Week 5)
│   └── Enums/
│       ├── FoodPreferenceType.cs         ← Allergy, Dislike
│       ├── AllergySeverity.cs            ← Low, Medium, High
│       ├── NutritionNoteType.cs          ← Diet, Note, Other
│       ├── MealType.cs                   ← Breakfast, Lunch, Dinner, Snack
│       ├── MealPlanSource.cs             ← AI, Manual
│       ├── AiFeature.cs                  ← MedicalExtraction, NutritionMealPlan...
│       └── AiRequestStatus.cs            ← Pending, Processing, Succeeded, Failed
│
├── FPT.EXE201.Application/
│   ├── DTOs/Nutrition/
│   │   ├── CreateFoodPreferenceDto.cs    ← foodItemId, preferenceType, severity?, notes?
│   │   ├── UpdateFoodPreferenceDto.cs    ← severity?, notes? (partial update)
│   │   ├── FoodPreferenceDto.cs          ← Full response with localized food name
│   │   ├── CreateNutritionNoteDto.cs     ← noteType, valueText
│   │   ├── UpdateNutritionNoteDto.cs     ← noteType?, valueText? (partial)
│   │   ├── NutritionNoteDto.cs           ← Full response
│   │   ├── GenerateMealPlanDto.cs        ← startDate, durationWeeks, additionalNotes?
│   │   ├── MealPlanSummaryDto.cs         ← List item (id, dates, title, totalDays)
│   │   ├── MealPlanDetailDto.cs          ← Plan + days summary + MealPlanDaySummaryDto
│   │   ├── MealDayDetailDto.cs           ← Day detail + MealItemDto + MealItemNutrientDto
│   │   ├── RecipeDetailDto.cs            ← Recipe with instructions
│   │   ├── CreateMealPlanFeedbackDto.cs  ← rating (1-5), comment?
│   │   ├── MealPlanFeedbackDto.cs        ← Full response
│   │   ├── CreateMealItemFeedbackDto.cs  ← liked (bool), comment?
│   │   └── MealItemFeedbackDto.cs        ← Full response
│   ├── IRepositories/
│   │   ├── IPregnancyFoodPreferenceRepository.cs  ← FindByKeyIncludingDeletedAsync
│   │   ├── IPregnancyNutritionNoteRepository.cs   ← GetByPregnancyIdAsync
│   │   ├── IRefFoodItemRepository.cs               ← (existing GenericRepo)
│   │   ├── IRefNutrientRepository.cs               ← GetActiveWithTranslationsAsync (standalone)
│   │   ├── IRecipeRepository.cs                     ← GetByIdWithDetailsAsync
│   │   ├── IMealPlanRepository.cs                   ← GetOverlappingAsync, paged query
│   │   ├── IMealPlanDayRepository.cs                ← GetByPlanIdAndDateAsync (deep include)
│   │   ├── IMealItemRepository.cs                   ← (existing GenericRepo)
│   │   ├── IMealPlanFeedbackRepository.cs           ← FindByKeyIncludingDeletedAsync
│   │   ├── IMealItemFeedbackRepository.cs           ← FindByKeyIncludingDeletedAsync
│   │   └── IAiRequestLogRepository.cs              ← CountTodayByUserAsync
│   ├── IServices/
│   │   ├── IFoodPreferenceService.cs     ← 4 CRUD food prefs + 4 CRUD notes
│   │   ├── IMealPlanService.cs           ← GenerateAsync, ListAsync, GetDetailAsync, etc.
│   │   ├── IRecipeService.cs             ← GetByIdAsync
│   │   ├── INutritionFeedbackService.cs  ← Plan feedback + Item feedback
│   │   └── IRefDataService.cs            ← +GetActiveFoodItemsAsync, +GetActiveNutrientsAsync
│   ├── Services/
│   │   ├── FoodPreferenceService.cs      ← CRUD + restore-from-soft-delete pattern
│   │   ├── MealPlanService.cs            ← ~700 lines, AI pipeline, calorie calc, JSON parse
│   │   ├── RecipeService.cs              ← GetById + ownership through pregnancy
│   │   ├── NutritionFeedbackService.cs   ← Plan + Item feedback with uniqueness
│   │   └── RefDataService.cs             ← +2 methods (food items, nutrients with translations)
│   ├── Features/MealPlans/
│   │   └── MealPlanListQuerySpec.cs      ← search: title,notes | sort: startdate,enddate,createdat
│   └── Validations/Nutrition/
│       ├── CreateFoodPreferenceDtoValidator.cs
│       ├── UpdateFoodPreferenceDtoValidator.cs
│       ├── CreateNutritionNoteDtoValidator.cs
│       ├── UpdateNutritionNoteDtoValidator.cs
│       ├── GenerateMealPlanDtoValidator.cs    ← startDate ≥ today, duration 1-4
│       ├── CreateMealPlanFeedbackDtoValidator.cs  ← rating 1-5
│       └── CreateMealItemFeedbackDtoValidator.cs  ← comment ≤ 300
│
├── FPT.EXE201.Infrastructure/
│   ├── Configurations/
│   │   ├── RefFoodItemConfiguration.cs
│   │   ├── RefFoodItemTranslationConfiguration.cs
│   │   ├── RefNutrientConfiguration.cs          ← Custom timestamps (no global filter)
│   │   ├── RefNutrientTranslationConfiguration.cs
│   │   ├── PregnancyFoodPreferenceConfiguration.cs ← uk_food_pref_pregnancy
│   │   ├── PregnancyNutritionNoteConfiguration.cs
│   │   ├── RecipeConfiguration.cs
│   │   ├── MealPlanConfiguration.cs
│   │   ├── MealPlanDayConfiguration.cs           ← uk_meal_plan_days
│   │   ├── MealItemConfiguration.cs
│   │   ├── MealItemNutrientConfiguration.cs      ← Composite PK
│   │   ├── MealPlanFeedbackConfiguration.cs      ← uk_meal_plan_feedback + chk_plan_rating
│   │   ├── MealItemFeedbackConfiguration.cs      ← uk_meal_item_feedback
│   │   └── AiRequestLogConfiguration.cs
│   ├── Repositories/
│   │   ├── PregnancyFoodPreferenceRepository.cs  ← IgnoreQueryFilters for soft delete
│   │   ├── PregnancyNutritionNoteRepository.cs
│   │   ├── RefFoodItemRepository.cs
│   │   ├── RefNutrientRepository.cs              ← Standalone (not GenericRepository)
│   │   ├── RecipeRepository.cs
│   │   ├── MealPlanRepository.cs                  ← SearchHelper + QuerySpec
│   │   ├── MealPlanDayRepository.cs               ← Deep include (Items → Nutrients → Translations)
│   │   ├── MealItemRepository.cs
│   │   ├── MealPlanFeedbackRepository.cs          ← IgnoreQueryFilters
│   │   ├── MealItemFeedbackRepository.cs          ← IgnoreQueryFilters
│   │   └── AiRequestLogRepository.cs
│   ├── Persistence/
│   │   ├── AppDbContext.cs                ← +14 DbSets, +2 seeder calls
│   │   ├── UnitOfWork.cs                  ← +11 lazy ??= properties
│   │   └── Seeders/
│   │       ├── NutritionFoodItemSeeder.cs ← 45 items × 2 langs = 90 translations
│   │       ├── NutrientSeeder.cs          ← 15 items × 2 langs = 30 translations
│   │       └── DatabaseSeeder.cs          ← +12 permissions, USER=12, DOCTOR=11
│   └── Configurations/Seeds/
│       └── AiPromptTemplateSeed.cs        ← nutrition.meal_plan template
│
└── FPT.EXE201.Api/Controllers/
    ├── FoodPreferencesController.cs       ← 8 endpoints (4 prefs + 4 notes)
    ├── MealPlansController.cs             ← 6 endpoints (2 services injected)
    ├── RecipesController.cs               ← 1 endpoint
    ├── MealItemsController.cs             ← 1 endpoint (item feedback)
    └── RefDataController.cs               ← +2 endpoints (food-items, nutrients)
                                             +7 enums in GetEnums/GetEnumByName
```

---

## 10. DI REGISTRATION

```csharp
// Application/DependencyInjection.cs
services.AddScoped<IFoodPreferenceService, FoodPreferenceService>();
services.AddScoped<IMealPlanService, MealPlanService>();
services.AddScoped<IRecipeService, RecipeService>();
services.AddScoped<INutritionFeedbackService, NutritionFeedbackService>();
// + IRefDataService already registered (extended with GetActiveFoodItemsAsync, GetActiveNutrientsAsync)

// Infrastructure/DependencyInjection.cs
// All 11 repositories registered via UnitOfWork lazy ??= pattern (no separate DI)

// QuerySpecRegistry.cs
{ "mealPlans", new QuerySpecInfo("mealPlans", ...) }
```

---

## 11. DATA FLOW — Request cycle

### 11.1 Food Preference Create → Restore Pattern

```
POST /api/pregnancies/{id}/food-preferences
     │
     ▼
FoodPreferenceService.CreatePreferenceAsync
     │
     ├── VerifyPregnancyOwnership
     ├── Validate RefFoodItem exists
     ├── FindByKeyIncludingDeletedAsync (IgnoreQueryFilters)
     │   │
     │   ├── null → CREATE new PregnancyFoodPreference
     │   │
     │   ├── DeletedAt != null → RESTORE
     │   │   └── Set DeletedAt = null
     │   │       Update Severity, Notes
     │   │       _unitOfWork.FoodPreferences.Update(existing)
     │   │
     │   └── DeletedAt == null → 409 ConflictException
     │       "A {type} preference for this food item already exists."
     │
     ├── SaveChangesAsync
     └── Reload with translations → return FoodPreferenceDto
```

### 11.2 Meal Plan Generate → Entity Creation

```
AI JSON Response
     │
     ▼
ParseMealPlanResponse(content)
     │
     ▼
CreateWeekEntities(mealPlan, weekPlan, weekStart, nutrientMap)
     │
     for each day in weekPlan.Days:
     │   │
     │   ├── MealPlanDay { MealPlanId, PlanDate }
     │   │   └── mealPlan.Days.Add(planDay)
     │   │
     │   for each meal in day.Meals:
     │       │
     │       ├── Recipe { PregnancyId, Title, Instructions, Servings, ... }
     │       │   └── Created if meal.Recipe != null
     │       │
     │       ├── MealItem { MealDayId, MealType, RecipeId, ItemName, Calories, ... }
     │       │   └── planDay.Items.Add(mealItem)
     │       │
     │       └── for each nutrient in meal.Nutrients:
     │           │
     │           ├── nutrientMap.TryGetValue(code, out nutrientId)
     │           │   └── Unknown code → LogWarning, skip
     │           │
     │           └── MealItemNutrient { NutrientId, Amount }
     │               └── mealItem.Nutrients.Add(nutrient)
     │
     ▼ 
CommitTransactionAsync → ALL entities saved at once (EF change tracker)
```

### 11.3 Feedback → Unique + Restore Pattern

```
POST /api/meal-plans/{planId}/feedback
     │
     ▼
NutritionFeedbackService.CreatePlanFeedbackAsync
     │
     ├── Verify plan exists
     ├── VerifyPregnancyOwnership
     ├── FindByKeyIncludingDeletedAsync (IgnoreQueryFilters)
     │   │
     │   ├── null → CREATE new MealPlanFeedback
     │   ├── DeletedAt != null → RESTORE (update Rating, Comment)
     │   └── DeletedAt == null → 409 "You have already rated this meal plan."
     │
     ├── SaveChangesAsync
     └── return MealPlanFeedbackDto
```

---

## 12. DIFFERENCES FROM MEDICAL RECORD (Week 5)

| Aspect | Medical Record (Week 5) | Nutrition (Week 7) |
|--------|------------------------|---------------------|
| **AI mode** | **Asynchronous** (background job via Channel + BackgroundService) | **Synchronous** (blocks until complete) |
| **OCR** | Azure Document Intelligence → Gemini AI | Gemini AI only (no OCR) |
| **User review** | User reviews AI extracted data → manually confirms | No review — AI output directly saved |
| **Data creation** | AI extracts → user confirms → PrenatalVisit/Test created | AI generates → entities auto-created in transaction |
| **Entity scope** | Per-document (1 OcrResult per run) | Per-plan (1 AiRequestLog per week-chunk) |
| **Reuse** | PromptBuilder pipeline (shared) | PromptBuilder pipeline (shared) |
| **Template key** | `medical_record.extraction` | `nutrition.meal_plan` |
| **AI Provider** | `IAiProvider` (Gemini) | `IAiProvider` (Gemini) — same interface |
| **Rate limit** | None (per-document) | 15 calls/day/user |

---

## 13. FAQ

**Q: Tại sao AI generate synchronous thay vì background job như OCR?**  
A: OCR cần upload ảnh → Azure OCR → AI extract (3 bước, tổng ~30s) → user cần review. Meal plan chỉ cần AI generate → trực tiếp trả về kết quả. User cần xem thực đơn ngay sau khi generate.

**Q: Tại sao RefNutrient không kế thừa BaseEntity?**  
A: RefNutrient là reference data (master data) — KHÔNG cần soft delete. Có `created_at` + `updated_at` nhưng KHÔNG có `deleted_at`. Custom entity để tránh global filter.

**Q: User thêm/xóa food preference rồi generate lại, có ảnh hưởng plan cũ không?**  
A: KHÔNG. Plan cũ đã generated với context cũ, giữ nguyên. Plan mới sẽ dùng preferences mới nhất.

**Q: Rate limit reset khi nào?**  
A: Reset hàng ngày theo UTC. `CountTodayByUserAsync` đếm từ đầu ngày UTC.

**Q: Overlapping plans xử lý thế nào?**  
A: Auto soft-delete plan cũ trước khi tạo plan mới. User không cần xóa thủ công.

**Q: AI trả về nutrient code không tồn tại trong hệ thống?**  
A: Skip + log warning. Không tạo MealItemNutrient cho unknown codes.

**Q: Có thể tạo MealPlan manually (không qua AI)?**  
A: Enum `MealPlanSource.Manual` đã có sẵn nhưng chưa implement. Future feature.

**Q: MealPlan.AiRequestLogId link log nào?**  
A: Link AiRequestLog của **tuần đầu tiên** (week 0). Các tuần sau có log riêng nhưng không FK vào MealPlan.

**Q: Soft delete plan → recipes, feedbacks bị sao?**  
A: Recipes tồn tại độc lập (pregnancy-scoped). MealPlanFeedback cascade delete theo global filter. MealItems/Days cũng bị ẩn bởi global filter.

**Q: Sao MealItem.RecipeId nullable nếu recipe là REQUIRED?**  
A: AI rule ALWAYS provides recipe nhưng parse có thể fail. Nullable FK cho defensive coding.

**Q: Maximum meal plan duration?**  
A: 4 tuần (28 ngày). Validator block > 4 weeks + service double-check.
