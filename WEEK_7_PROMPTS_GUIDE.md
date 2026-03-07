# WEEK 7 PROMPTS GUIDE — Nutrition + Meal Planning (Part 1: Prompts 1–5)

> **Scope**: Domain layer + Infrastructure layer (Entities, Enums, EF Configs, Seeds, DTOs, Validators).
> Prompts 6–10 (Repositories, UoW, Services, Controllers, DI) trong **Part 2**.
> **Tham chiếu**: `WEEK_7_NUTRITION_DECISIONS.md`, `DEVELOPMENT_WORKFLOW_GUIDE.md`, `DATABASE_SCHEMA.sql` Section 8.
> **Cập nhật**: 2026-03-03

---

## ⚠️ CONVENTIONS (PHẢI ĐỌC TRƯỚC KHI CODE)

1. ⚠️ **BaseEntity**: Kế thừa `BaseEntity` → có `Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `IsDeleted` (computed, KHÔNG map DB). Config PHẢI có `builder.Ignore(x => x.IsDeleted)`.
2. ⚠️ **DTOs PHẢI là `record`** — KHÔNG dùng `class`.
3. ⚠️ **Exceptions**: Service throw `NotFoundException` / `BadRequestException` / `ConflictException` / `ForbiddenException`. Controller KHÔNG try-catch — `GlobalExceptionFilter` tự xử lý.
4. ⚠️ **RBAC**: `[RequirePermission("module.action")]` trên mỗi endpoint.
5. ⚠️ **Soft delete**: Entities kế thừa `BaseEntity` → auto query filter `WHERE deleted_at IS NULL`.
6. ⚠️ **Seed data**: Dùng anonymous types + fixed `DateTime`. GUIDs phải hardcoded string.
7. ⚠️ **GUID columns**: `CHAR(36)` — KHÔNG dùng `BINARY(16)`.
8. ⚠️ **Enums**: `HasConversion<string>()` — KHÔNG dùng int.
9. ⚠️ **Translation entities**: Composite PK `(FK_Id, LanguageCode)`, KHÔNG kế thừa `BaseEntity`.
10. ⚠️ **Property naming**: C# PascalCase → DB snake_case. Map via `.HasColumnName()`.
11. ⚠️ **UnitOfWork**: Lazy `??=` pattern. `IUnitOfWork` trong Application, `UnitOfWork` trong Infrastructure.
12. ⚠️ **File-scoped namespace** cho tất cả file mới (`namespace X;`).
13. ⚠️ **Navigation properties**: Luôn init `= new List<>()` hoặc `= null!`.

---

## 📋 CONTEXT

### Week 7 Objectives
- Dietary preferences management (allergies, dislikes, free-text notes)
- AI-generated meal plans (1–4 weeks) with recipes + nutrient tracking
- User feedback system (plan ratings + item like/dislike)
- Calorie calculation based on BMI + trimester (IOM guidelines)
- Rate limiting (15 AI calls/day per user)

### Prerequisites (Đã có từ weeks trước)
- `pregnancies` table + `Pregnancy` entity — BMI, conditions, gestational week
- `weight_logs` table + `WeightLog` entity — latest weight
- `pregnancy_conditions` — health conditions (diabetes, preeclampsia...)
- `ai_prompt_templates` + `AiPromptTemplate` entity — prompt template system
- `IAiProvider` + `GeminiAiProvider` — Gemini API integration
- `PromptBuilder` — 3-layer rule system (System, Domain, Feature)
- `IGenericRepository<T> where T : BaseEntity` — generic CRUD

### 8 Decisions Applied

| # | Change | Detail |
|---|--------|--------|
| 1 | Schema | Added `updated_at` + `deleted_at` to `meal_plan_days`, `meal_items`, `meal_plan_feedback`, `meal_item_feedback` → inherit BaseEntity |
| 2 | RefNutrient | Custom entity (no BaseEntity, no soft delete) — separate pattern |
| 3 | Rate limit | 15 AI calls/day per user, checked via `ai_request_logs` count |
| 4 | BMI fallback | `bmi_weight = pre_pregnancy_weight_kg ?? current_weight_kg`, throw if both null |
| 5 | Overlap | Auto-replace — soft-delete overlapping plans silently, no ConflictException |
| 6 | Consolidation | 5 services + 5 controllers (was 8 each) |
| 7 | max_output_tokens | 8192 (was 4096) |
| 8 | meal_items.notes | Added to AI output schema + parse logic (nullable) |

### Entity Inheritance Strategy

| Entity | BaseEntity? | Soft Delete? | Reason |
|--------|:-----------:|:------------:|--------|
| RefFoodItem | ✅ | ✅ | Ref data, full lifecycle |
| RefFoodItemTranslation | ❌ | ❌ | Composite PK |
| RefNutrient | ❌ custom | ❌ | Decision #2: no soft delete |
| RefNutrientTranslation | ❌ | ❌ | Composite PK |
| PregnancyFoodPreference | ✅ | ✅ | |
| PregnancyNutritionNote | ✅ | ✅ | |
| Recipe | ✅ | ✅ | |
| MealPlan | ✅ | ✅ | |
| MealPlanDay | ✅ | ✅ | Decision #1 |
| MealItem | ✅ | ✅ | Decision #1 |
| MealItemNutrient | ❌ | ❌ | Composite PK, join table |
| MealPlanFeedback | ✅ | ✅ | Decision #1 |
| MealItemFeedback | ✅ | ✅ | Decision #1 |
| AiRequestLog | ✅ | ✅ | Week 5 schema, new entity |

### Services (5) — Decision #6

| # | Service | Responsibilities |
|---|---------|-----------------|
| 1 | `IRefDataService` (EXTEND existing) | `GetActiveFoodItemsAsync`, `GetActiveNutrientsAsync` |
| 2 | `IFoodPreferenceService` | CRUD food preferences + CRUD nutrition notes |
| 3 | `IMealPlanService` | Generate, List, GetDetail, Delete, GetDayDetail |
| 4 | `IRecipeService` | GetRecipeById |
| 5 | `INutritionFeedbackService` | CreatePlanFeedback, CreateItemFeedback |

### Controllers (5) — Decision #6

| # | Controller | Endpoints |
|---|-----------|-----------|
| 1 | `RefDataController` (EXTEND) | `GET /api/ref/food-items`, `GET /api/ref/nutrients` |
| 2 | `FoodPreferencesController` | 8 endpoints: CRUD prefs + CRUD notes |
| 3 | `MealPlansController` | Generate, List, Detail, Delete, DayDetail, PlanFeedback |
| 4 | `RecipesController` | `GET /api/recipes/{recipeId}` |
| 5 | `MealItemsController` | `POST /api/meal-items/{itemId}/feedback` |

### API Endpoints (Full List)

```
REF DATA (extend existing RefDataController):
  GET  /api/ref/food-items?lang=vi
  GET  /api/ref/nutrients?lang=vi

FOOD PREFERENCES (FoodPreferencesController):
  GET    /api/pregnancies/{id}/food-preferences
  POST   /api/pregnancies/{id}/food-preferences
  PUT    /api/pregnancies/{id}/food-preferences/{prefId}
  DELETE /api/pregnancies/{id}/food-preferences/{prefId}
  GET    /api/pregnancies/{id}/nutrition-notes
  POST   /api/pregnancies/{id}/nutrition-notes
  PUT    /api/pregnancies/{id}/nutrition-notes/{noteId}
  DELETE /api/pregnancies/{id}/nutrition-notes/{noteId}

MEAL PLANS (MealPlansController):
  POST   /api/pregnancies/{id}/meal-plans/generate
  GET    /api/pregnancies/{id}/meal-plans
  GET    /api/pregnancies/{id}/meal-plans/{planId}
  DELETE /api/pregnancies/{id}/meal-plans/{planId}
  GET    /api/meal-plans/{planId}/days/{date}
  POST   /api/meal-plans/{planId}/feedback

RECIPES (RecipesController):
  GET    /api/recipes/{recipeId}

MEAL ITEMS (MealItemsController):
  POST   /api/meal-items/{itemId}/feedback
```

### Permissions

```
food_preference.read  — USER, ADMIN
food_preference.write — USER, ADMIN
food_preference.delete — USER, ADMIN
nutrition_note.read   — USER, ADMIN
nutrition_note.write  — USER, ADMIN
nutrition_note.delete — USER, ADMIN
meal_plan.read        — USER, ADMIN
meal_plan.generate    — USER
meal_plan.delete      — USER, ADMIN
recipe.read           — USER, ADMIN
meal_plan_feedback.write  — USER
meal_item_feedback.write  — USER
```

---

## 🎯 PROMPT 1/10 — Context + SQL Schema Reference

### Task
Đọc hiểu context cho Week 7 Nutrition + Meal Planning. **Không tạo file nào** trong prompt này — chỉ hiểu schema + decisions.

### SQL Schema Reference (DATABASE_SCHEMA.sql Section 8)

> ⚠️ Decision #1: Bốn table `meal_plan_days`, `meal_items`, `meal_plan_feedback`, `meal_item_feedback` được **thêm `updated_at` + `deleted_at`** so với schema gốc → cho phép kế thừa `BaseEntity`.

> ⚠️ Decision #2: `ref_nutrients` **KHÔNG có `deleted_at`** → entity riêng, không kế thừa BaseEntity.

```sql
-- 14 tables for Week 7:
-- ref_food_items (BaseEntity)
-- ref_food_item_translations (composite PK)
-- ref_nutrients (custom entity: created_at + updated_at only)
-- ref_nutrient_translations (composite PK)
-- pregnancy_food_preferences (BaseEntity)
-- pregnancy_nutrition_notes (BaseEntity)
-- recipes (BaseEntity)
-- meal_plans (BaseEntity, FK → ai_request_logs)
-- meal_plan_days (BaseEntity — Decision #1)
-- meal_items (BaseEntity — Decision #1)
-- meal_item_nutrients (composite PK, join table)
-- meal_plan_feedback (BaseEntity — Decision #1)
-- meal_item_feedback (BaseEntity — Decision #1)
-- ai_request_logs (BaseEntity — Week 5 schema, implemented now)
```

### Business Rules
1. Overlap check (Decision #5): Auto soft-delete overlapping plans — NO ConflictException
2. Rate limit (Decision #3): 15 AI calls/day/user via `ai_request_logs` count → throw `BadRequestException`
3. BMI fallback (Decision #4): `bmi_weight = pre_pregnancy_weight_kg ?? current_weight_kg`, throw if both null
4. Recipe REQUIRED: Every meal item MUST have a recipe. Validate in parse logic.
5. Multi-week failure: Rollback entire transaction nếu bất kỳ week nào fail
6. Unknown nutrient code: Skip + log warning — KHÔNG crash
7. Duration: 1–4 weeks, user chọn start date (today or future)
8. 1 Gemini API call per week — week 2+ includes previous week summary

### ✅ Checkpoint
- [ ] Đã đọc hiểu 14 tables, 8 decisions, entity inheritance strategy
- [ ] Đã hiểu 5 services + 5 controllers consolidation
- [ ] Đã hiểu business rules (overlap, rate limit, BMI fallback, rollback)

---

## 🎯 PROMPT 2/10 — Domain: Enums + Entities

### Task
Tạo **7 enums** + **14 entities** + update `Pregnancy.cs` thêm navigation properties.

### ⚠️ Convention Reminders
- Enums: file-scoped namespace, simple values (no explicit int backing)
- Entities kế thừa BaseEntity: KHÔNG khai báo `Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt` (đã có từ base)
- RefNutrient: entity riêng (Decision #2) — khai báo `Id`, `CreatedAt`, `UpdatedAt` thủ công
- Translation + Join table entities: KHÔNG kế thừa BaseEntity, composite PK
- Navigation properties: `= null!` cho single, `= new List<>()` cho collection

### Files to create:

#### 1. `src/FPT.EXE201.Domain/Enums/FoodPreferenceType.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

public enum FoodPreferenceType
{
    Allergy,
    Dislike
}
```

#### 2. `src/FPT.EXE201.Domain/Enums/AllergySeverity.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

public enum AllergySeverity
{
    Low,
    Medium,
    High
}
```

#### 3. `src/FPT.EXE201.Domain/Enums/MealType.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}
```

#### 4. `src/FPT.EXE201.Domain/Enums/MealPlanSource.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

public enum MealPlanSource
{
    AI,
    Manual
}
```

#### 5. `src/FPT.EXE201.Domain/Enums/NutritionNoteType.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

public enum NutritionNoteType
{
    Diet,
    Note,
    Other
}
```

#### 6. `src/FPT.EXE201.Domain/Enums/AiFeature.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Feature identifier for ai_request_logs.feature column.
/// </summary>
public enum AiFeature
{
    MedicalExtraction,
    NutritionMealPlan,
    NutritionChat,
    DoctorChat
}
```

#### 7. `src/FPT.EXE201.Domain/Enums/AiRequestStatus.cs`
```csharp
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Status for ai_request_logs.status column.
/// </summary>
public enum AiRequestStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed
}
```

---

#### 8. `src/FPT.EXE201.Domain/Entities/RefFoodItem.cs`
```csharp
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Reference food item catalog (~60-80 items).
/// Used for preference/allergy picker UI only — NOT an ingredient database.
/// </summary>
public class RefFoodItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<RefFoodItemTranslation> Translations { get; set; }
        = new List<RefFoodItemTranslation>();
    public ICollection<PregnancyFoodPreference> FoodPreferences { get; set; }
        = new List<PregnancyFoodPreference>();
}
```

#### 9. `src/FPT.EXE201.Domain/Entities/RefFoodItemTranslation.cs`
```csharp
namespace FPT.EXE201.Domain.Entities;

/// ⚠️ KHÔNG kế thừa BaseEntity — composite primary key.
public class RefFoodItemTranslation
{
    public Guid FoodItemId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Navigation
    public RefFoodItem FoodItem { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
```

#### 10. `src/FPT.EXE201.Domain/Entities/RefNutrient.cs`
```csharp
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Reference nutrient catalog (15 items: PROTEIN, IRON, CALCIUM...).
/// ⚠️ Decision #2: Custom entity — KHÔNG kế thừa BaseEntity, KHÔNG có soft delete.
/// Has created_at + updated_at only (no deleted_at).
/// </summary>
public class RefNutrient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<RefNutrientTranslation> Translations { get; set; }
        = new List<RefNutrientTranslation>();
    public ICollection<MealItemNutrient> MealItemNutrients { get; set; }
        = new List<MealItemNutrient>();
}
```

#### 11. `src/FPT.EXE201.Domain/Entities/RefNutrientTranslation.cs`
```csharp
namespace FPT.EXE201.Domain.Entities;

/// ⚠️ KHÔNG kế thừa BaseEntity — composite primary key.
public class RefNutrientTranslation
{
    public Guid NutrientId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Navigation
    public RefNutrient Nutrient { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
```

#### 12. `src/FPT.EXE201.Domain/Entities/PregnancyFoodPreference.cs`
```csharp
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User allergens/dislikes per pregnancy. FK → ref_food_items.
/// Unique constraint: (pregnancy_id, food_item_id, preference_type).
/// </summary>
public class PregnancyFoodPreference : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public Guid FoodItemId { get; set; }
    public FoodPreferenceType PreferenceType { get; set; }
    public AllergySeverity? Severity { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public RefFoodItem FoodItem { get; set; } = null!;
}
```

#### 13. `src/FPT.EXE201.Domain/Entities/PregnancyNutritionNote.cs`
```csharp
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Free-text dietary notes per pregnancy (e.g., "thích món miền Tây").
/// Supplements structured food preferences with flexible text notes.
/// </summary>
public class PregnancyNutritionNote : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public NutritionNoteType NoteType { get; set; } = NutritionNoteType.Note;
    public string ValueText { get; set; } = string.Empty;

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
}
```

#### 14. `src/FPT.EXE201.Domain/Entities/Recipe.cs`
```csharp
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// AI-generated recipe, pregnancy-scoped.
/// 1 recipe per meal item (REQUIRED — Decision #8).
/// </summary>
public class Recipe : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int? Servings { get; set; }
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
}
```

#### 15. `src/FPT.EXE201.Domain/Entities/MealPlan.cs`
```csharp
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// AI-generated or manual meal plan. 1 record per generation request.
/// User chọn 3 weeks → 1 record (start_date → end_date), không tách per week.
/// ai_request_log_id links to first AI call log (convenience FK).
/// </summary>
public class MealPlan : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public Guid? AiRequestLogId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MealPlanSource Source { get; set; } = MealPlanSource.AI;
    public string? Title { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public AiRequestLog? AiRequestLog { get; set; }
    public ICollection<MealPlanDay> Days { get; set; } = new List<MealPlanDay>();
    public ICollection<MealPlanFeedback> Feedbacks { get; set; } = new List<MealPlanFeedback>();
}
```

#### 16. `src/FPT.EXE201.Domain/Entities/MealPlanDay.cs`
```csharp
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// 1 record per day in meal plan.
/// ⚠️ Decision #1: Added updated_at + deleted_at → inherits BaseEntity.
/// </summary>
public class MealPlanDay : BaseEntity
{
    public Guid MealPlanId { get; set; }
    public DateOnly PlanDate { get; set; }

    // Navigation
    public MealPlan MealPlan { get; set; } = null!;
    public ICollection<MealItem> Items { get; set; } = new List<MealItem>();
}
```

#### 17. `src/FPT.EXE201.Domain/Entities/MealItem.cs`
```csharp
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Individual meal item within a day (BREAKFAST, LUNCH, DINNER, SNACK).
/// recipe_id is REQUIRED by business rule (Decision #8) — DB allows NULL for flexibility.
/// item_name is ALWAYS filled alongside recipe.
/// ⚠️ Decision #1: Added updated_at + deleted_at → inherits BaseEntity.
/// </summary>
public class MealItem : BaseEntity
{
    public Guid MealDayId { get; set; }
    public MealType MealType { get; set; }
    public Guid? RecipeId { get; set; }
    public string? ItemName { get; set; }
    public string? PortionText { get; set; }
    public int? CaloriesKcal { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public MealPlanDay MealDay { get; set; } = null!;
    public Recipe? Recipe { get; set; }
    public ICollection<MealItemNutrient> Nutrients { get; set; } = new List<MealItemNutrient>();
    public ICollection<MealItemFeedback> Feedbacks { get; set; } = new List<MealItemFeedback>();
}
```

#### 18. `src/FPT.EXE201.Domain/Entities/MealItemNutrient.cs`
```csharp
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Nutrient breakdown per meal item. Composite PK (meal_item_id, nutrient_id).
/// ⚠️ KHÔNG kế thừa BaseEntity — join table, no timestamps.
/// </summary>
public class MealItemNutrient
{
    public Guid MealItemId { get; set; }
    public Guid NutrientId { get; set; }
    public decimal Amount { get; set; }

    // Navigation
    public MealItem MealItem { get; set; } = null!;
    public RefNutrient Nutrient { get; set; } = null!;
}
```

#### 19. `src/FPT.EXE201.Domain/Entities/MealPlanFeedback.cs`
```csharp
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User rates overall meal plan (1-5 stars). Unique per (meal_plan_id, user_id).
/// ⚠️ Decision #1: Added updated_at + deleted_at → inherits BaseEntity.
/// </summary>
public class MealPlanFeedback : BaseEntity
{
    public Guid MealPlanId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public MealPlan MealPlan { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

#### 20. `src/FPT.EXE201.Domain/Entities/MealItemFeedback.cs`
```csharp
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User likes/dislikes individual meal items. Unique per (meal_item_id, user_id).
/// ⚠️ Decision #1: Added updated_at + deleted_at → inherits BaseEntity.
/// </summary>
public class MealItemFeedback : BaseEntity
{
    public Guid MealItemId { get; set; }
    public Guid UserId { get; set; }
    public bool Liked { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public MealItem MealItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

#### 21. `src/FPT.EXE201.Domain/Entities/AiRequestLog.cs`
```csharp
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Logs every AI API call. From Week 5 schema (ai_request_logs table).
/// Entity created in Week 7 for meal plan generation tracking + rate limiting.
/// 1 record per week-chunk per generation.
/// </summary>
public class AiRequestLog : BaseEntity
{
    public AiFeature Feature { get; set; }
    public Guid? PregnancyId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TemplateId { get; set; }
    public AiRequestStatus Status { get; set; } = AiRequestStatus.Pending;
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public int? TokensInput { get; set; }
    public int? TokensOutput { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public Pregnancy? Pregnancy { get; set; }
    public User? User { get; set; }
    public AiPromptTemplate? Template { get; set; }
}
```

---

#### 22. Update `src/FPT.EXE201.Domain/Entities/Pregnancy.cs` — Add navigation properties

Thêm vào cuối class (sau `ICollection<PrenatalTest> Tests`):

```csharp
    // Week 7 — Nutrition + Meal Planning
    public ICollection<PregnancyFoodPreference> FoodPreferences { get; set; }
        = new List<PregnancyFoodPreference>();
    public ICollection<PregnancyNutritionNote> NutritionNotes { get; set; }
        = new List<PregnancyNutritionNote>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    public ICollection<AiRequestLog> AiRequestLogs { get; set; }
        = new List<AiRequestLog>();
```

### ✅ Checkpoint — Prompt 2
- [ ] 7 enum files created in `Domain/Enums/`
- [ ] 14 entity files created in `Domain/Entities/`
- [ ] `Pregnancy.cs` updated with 5 new navigation properties
- [ ] `RefNutrient` does NOT inherit BaseEntity (Decision #2)
- [ ] `MealPlanDay`, `MealItem`, `MealPlanFeedback`, `MealItemFeedback` inherit BaseEntity (Decision #1)
- [ ] `MealItemNutrient`, translations do NOT inherit BaseEntity
- [ ] Project builds without errors

---

## 🎯 PROMPT 3/10 — Infrastructure: EF Configurations

### Task
Tạo **14 EF configuration files** trong `src/FPT.EXE201.Infrastructure/Configurations/`.

### ⚠️ Convention Reminders
- `CHAR(36)` cho tất cả GUID columns
- `HasConversion<string>()` cho enums
- `builder.Ignore(x => x.IsDeleted)` cho mọi entity kế thừa BaseEntity
- Translation config: composite PK, `UseCollation("utf8mb4_unicode_ci")` cho LanguageCode
- FK to Language dùng `.HasPrincipalKey(l => l.Code)` (Language PK là Id, FK theo Code)
- Index naming: `idx_{table}_{columns}`, Unique: `uk_{table}_{columns}`, FK: `fk_{table}_{ref_table}`

### Files to create:

#### 1. `RefFoodItemConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefFoodItemConfiguration : IEntityTypeConfiguration<RefFoodItem>
{
    public void Configure(EntityTypeBuilder<RefFoodItem> builder)
    {
        builder.ToTable("ref_food_items");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.Code)
            .IsRequired().HasColumnName("code").HasMaxLength(80);
        builder.HasIndex(r => r.Code)
            .IsUnique().HasDatabaseName("uk_ref_food_items_code");

        builder.Property(r => r.IsActive)
            .IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(r => r.IsDeleted);

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.FoodItem).HasForeignKey(t => t.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 2. `RefFoodItemTranslationConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefFoodItemTranslationConfiguration
    : IEntityTypeConfiguration<RefFoodItemTranslation>
{
    public void Configure(EntityTypeBuilder<RefFoodItemTranslation> builder)
    {
        builder.ToTable("ref_food_item_translations");

        builder.HasKey(t => new { t.FoodItemId, t.LanguageCode });

        builder.Property(t => t.FoodItemId)
            .HasColumnName("food_item_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode)
            .IsRequired().HasColumnName("language_code")
            .HasMaxLength(10).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName)
            .IsRequired().HasColumnName("display_name").HasMaxLength(120);

        builder.HasOne(t => t.FoodItem)
            .WithMany(f => f.Translations).HasForeignKey(t => t.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### 3. `RefNutrientConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

/// <summary>
/// ⚠️ Decision #2: RefNutrient does NOT inherit BaseEntity.
/// No soft-delete filter, no DeletedAt column.
/// </summary>
public class RefNutrientConfiguration : IEntityTypeConfiguration<RefNutrient>
{
    public void Configure(EntityTypeBuilder<RefNutrient> builder)
    {
        builder.ToTable("ref_nutrients");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.Code)
            .IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code)
            .IsUnique().HasDatabaseName("uk_ref_nutrients_code");

        builder.Property(r => r.Unit)
            .IsRequired().HasColumnName("unit").HasMaxLength(20);

        builder.Property(r => r.IsActive)
            .IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");

        // ⚠️ NO DeletedAt, NO Ignore(IsDeleted) — custom entity

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.Nutrient).HasForeignKey(t => t.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 4. `RefNutrientTranslationConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefNutrientTranslationConfiguration
    : IEntityTypeConfiguration<RefNutrientTranslation>
{
    public void Configure(EntityTypeBuilder<RefNutrientTranslation> builder)
    {
        builder.ToTable("ref_nutrient_translations");

        builder.HasKey(t => new { t.NutrientId, t.LanguageCode });

        builder.Property(t => t.NutrientId)
            .HasColumnName("nutrient_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode)
            .IsRequired().HasColumnName("language_code")
            .HasMaxLength(10).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName)
            .IsRequired().HasColumnName("display_name").HasMaxLength(120);

        builder.HasOne(t => t.Nutrient)
            .WithMany(n => n.Translations).HasForeignKey(t => t.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### 5. `PregnancyFoodPreferenceConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyFoodPreferenceConfiguration
    : IEntityTypeConfiguration<PregnancyFoodPreference>
{
    public void Configure(EntityTypeBuilder<PregnancyFoodPreference> builder)
    {
        builder.ToTable("pregnancy_food_preferences");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(p => p.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.FoodItemId)
            .IsRequired().HasColumnName("food_item_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PreferenceType)
            .IsRequired().HasColumnName("preference_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Severity)
            .HasColumnName("severity")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Notes)
            .HasColumnName("notes").HasMaxLength(255);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(p => p.IsDeleted);

        // Unique: 1 preference per pregnancy + food + type
        builder.HasIndex(p => new { p.PregnancyId, p.FoodItemId, p.PreferenceType })
            .IsUnique().HasDatabaseName("uk_food_pref_pregnancy");

        // Relationships
        builder.HasOne(p => p.Pregnancy)
            .WithMany(preg => preg.FoodPreferences).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.FoodItem)
            .WithMany(f => f.FoodPreferences).HasForeignKey(p => p.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### 6. `PregnancyNutritionNoteConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyNutritionNoteConfiguration
    : IEntityTypeConfiguration<PregnancyNutritionNote>
{
    public void Configure(EntityTypeBuilder<PregnancyNutritionNote> builder)
    {
        builder.ToTable("pregnancy_nutrition_notes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(n => n.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(n => n.NoteType)
            .IsRequired().HasColumnName("note_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.ValueText)
            .IsRequired().HasColumnName("value_text").HasMaxLength(200);

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(n => n.IsDeleted);

        builder.HasIndex(n => new { n.PregnancyId, n.CreatedAt })
            .HasDatabaseName("idx_nutrition_notes_pregnancy");

        builder.HasOne(n => n.Pregnancy)
            .WithMany(preg => preg.NutritionNotes).HasForeignKey(n => n.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 7. `RecipeConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Title)
            .IsRequired().HasColumnName("title").HasMaxLength(200);
        builder.Property(r => r.Instructions)
            .HasColumnName("instructions").HasColumnType("LONGTEXT");
        builder.Property(r => r.Servings)
            .HasColumnName("servings");
        builder.Property(r => r.PrepMinutes)
            .HasColumnName("prep_minutes");
        builder.Property(r => r.CookMinutes)
            .HasColumnName("cook_minutes");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(r => r.IsDeleted);

        builder.HasIndex(r => new { r.PregnancyId, r.CreatedAt })
            .HasDatabaseName("idx_recipes_pregnancy");

        builder.HasOne(r => r.Pregnancy)
            .WithMany(preg => preg.Recipes).HasForeignKey(r => r.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 8. `MealPlanConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("meal_plans");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(m => m.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.AiRequestLogId)
            .HasColumnName("ai_request_log_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.StartDate)
            .IsRequired().HasColumnName("start_date").HasColumnType("DATE");
        builder.Property(m => m.EndDate)
            .IsRequired().HasColumnName("end_date").HasColumnType("DATE");
        builder.Property(m => m.Source)
            .IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Title)
            .HasColumnName("title").HasMaxLength(200);
        builder.Property(m => m.Notes)
            .HasColumnName("notes").HasColumnType("TEXT");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(m => m.IsDeleted);

        builder.HasIndex(m => new { m.PregnancyId, m.StartDate })
            .HasDatabaseName("idx_meal_plans_pregnancy");

        builder.HasCheckConstraint("chk_meal_plan_dates", "end_date >= start_date");

        // Relationships
        builder.HasOne(m => m.Pregnancy)
            .WithMany(preg => preg.MealPlans).HasForeignKey(m => m.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.AiRequestLog)
            .WithMany().HasForeignKey(m => m.AiRequestLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

#### 9. `MealPlanDayConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay>
{
    public void Configure(EntityTypeBuilder<MealPlanDay> builder)
    {
        builder.ToTable("meal_plan_days");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(d => d.MealPlanId)
            .IsRequired().HasColumnName("meal_plan_id").HasColumnType("CHAR(36)");
        builder.Property(d => d.PlanDate)
            .IsRequired().HasColumnName("plan_date").HasColumnType("DATE");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(d => d.IsDeleted);

        builder.HasIndex(d => new { d.MealPlanId, d.PlanDate })
            .IsUnique().HasDatabaseName("uk_meal_plan_days");

        builder.HasOne(d => d.MealPlan)
            .WithMany(m => m.Days).HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 10. `MealItemConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> builder)
    {
        builder.ToTable("meal_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(i => i.MealDayId)
            .IsRequired().HasColumnName("meal_day_id").HasColumnType("CHAR(36)");
        builder.Property(i => i.MealType)
            .IsRequired().HasColumnName("meal_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.RecipeId)
            .HasColumnName("recipe_id").HasColumnType("CHAR(36)");
        builder.Property(i => i.ItemName)
            .HasColumnName("item_name").HasMaxLength(200);
        builder.Property(i => i.PortionText)
            .HasColumnName("portion_text").HasMaxLength(120);
        builder.Property(i => i.CaloriesKcal)
            .HasColumnName("calories_kcal");
        builder.Property(i => i.Notes)
            .HasColumnName("notes").HasMaxLength(255);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(i => i.IsDeleted);

        builder.HasIndex(i => new { i.MealDayId, i.MealType })
            .HasDatabaseName("idx_meal_items_day_type");

        builder.HasCheckConstraint("chk_meal_item_name",
            "recipe_id IS NOT NULL OR item_name IS NOT NULL");

        // Relationships
        builder.HasOne(i => i.MealDay)
            .WithMany(d => d.Items).HasForeignKey(i => i.MealDayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Recipe)
            .WithMany(r => r.MealItems).HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

#### 11. `MealItemNutrientConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemNutrientConfiguration : IEntityTypeConfiguration<MealItemNutrient>
{
    public void Configure(EntityTypeBuilder<MealItemNutrient> builder)
    {
        builder.ToTable("meal_item_nutrients");

        builder.HasKey(min => new { min.MealItemId, min.NutrientId });

        builder.Property(min => min.MealItemId)
            .HasColumnName("meal_item_id").HasColumnType("CHAR(36)");
        builder.Property(min => min.NutrientId)
            .HasColumnName("nutrient_id").HasColumnType("CHAR(36)");
        builder.Property(min => min.Amount)
            .IsRequired().HasColumnName("amount").HasColumnType("DECIMAL(10,3)");

        builder.HasCheckConstraint("chk_nutrient_amount", "amount >= 0");

        builder.HasOne(min => min.MealItem)
            .WithMany(i => i.Nutrients).HasForeignKey(min => min.MealItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(min => min.Nutrient)
            .WithMany(n => n.MealItemNutrients).HasForeignKey(min => min.NutrientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### 12. `MealPlanFeedbackConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanFeedbackConfiguration : IEntityTypeConfiguration<MealPlanFeedback>
{
    public void Configure(EntityTypeBuilder<MealPlanFeedback> builder)
    {
        builder.ToTable("meal_plan_feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(f => f.MealPlanId)
            .IsRequired().HasColumnName("meal_plan_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.Rating)
            .IsRequired().HasColumnName("rating").HasColumnType("TINYINT");
        builder.Property(f => f.Comment)
            .HasColumnName("comment").HasMaxLength(500);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(f => f.IsDeleted);

        builder.HasIndex(f => new { f.MealPlanId, f.UserId })
            .IsUnique().HasDatabaseName("uk_meal_plan_feedback");

        builder.HasCheckConstraint("chk_plan_rating", "rating BETWEEN 1 AND 5");

        builder.HasOne(f => f.MealPlan)
            .WithMany(m => m.Feedbacks).HasForeignKey(f => f.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.User)
            .WithMany().HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 13. `MealItemFeedbackConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemFeedbackConfiguration : IEntityTypeConfiguration<MealItemFeedback>
{
    public void Configure(EntityTypeBuilder<MealItemFeedback> builder)
    {
        builder.ToTable("meal_item_feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(f => f.MealItemId)
            .IsRequired().HasColumnName("meal_item_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.Liked)
            .IsRequired().HasColumnName("liked").HasColumnType("TINYINT(1)");
        builder.Property(f => f.Comment)
            .HasColumnName("comment").HasMaxLength(300);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(f => f.IsDeleted);

        builder.HasIndex(f => new { f.MealItemId, f.UserId })
            .IsUnique().HasDatabaseName("uk_meal_item_feedback");

        builder.HasOne(f => f.MealItem)
            .WithMany(i => i.Feedbacks).HasForeignKey(f => f.MealItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.User)
            .WithMany().HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 14. `AiRequestLogConfiguration.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>
{
    public void Configure(EntityTypeBuilder<AiRequestLog> builder)
    {
        builder.ToTable("ai_request_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(a => a.Feature)
            .IsRequired().HasColumnName("feature")
            .HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.PregnancyId)
            .HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.UserId)
            .HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.TemplateId)
            .HasColumnName("template_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Model)
            .HasColumnName("model").HasMaxLength(80);
        builder.Property(a => a.PromptVersion)
            .HasColumnName("prompt_version").HasMaxLength(64);
        builder.Property(a => a.RequestPayload)
            .HasColumnName("request_payload").HasColumnType("JSON");
        builder.Property(a => a.ResponsePayload)
            .HasColumnName("response_payload").HasColumnType("JSON");
        builder.Property(a => a.TokensInput)
            .HasColumnName("tokens_input");
        builder.Property(a => a.TokensOutput)
            .HasColumnName("tokens_output");
        builder.Property(a => a.ProcessingTimeMs)
            .HasColumnName("processing_time_ms");
        builder.Property(a => a.ErrorMessage)
            .HasColumnName("error_message").HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(a => a.IsDeleted);

        // Indexes
        builder.HasIndex(a => new { a.Feature, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_feature");
        builder.HasIndex(a => new { a.PregnancyId, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_pregnancy");
        builder.HasIndex(a => new { a.Status, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_status");

        // Relationships
        builder.HasOne(a => a.Pregnancy)
            .WithMany(preg => preg.AiRequestLogs).HasForeignKey(a => a.PregnancyId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.User)
            .WithMany().HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Template)
            .WithMany().HasForeignKey(a => a.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### ✅ Checkpoint — Prompt 3
- [ ] 14 configuration files created in `Infrastructure/Configurations/`
- [ ] All BaseEntity configs have `builder.Ignore(x => x.IsDeleted)`
- [ ] `RefNutrientConfiguration` does NOT have DeletedAt/IsDeleted
- [ ] All GUID columns use `CHAR(36)`
- [ ] All enums use `HasConversion<string>()`
- [ ] Translation configs have composite PK + `UseCollation` + `HasPrincipalKey(l => l.Code)`
- [ ] All indexes match naming convention `idx_/uk_/fk_`
- [ ] Check constraints added where schema defines them
- [ ] Project builds without errors

---

## 🎯 PROMPT 4/10 — Infrastructure: AppDbContext + Seeders + Migration

### Task
1. Update `AppDbContext.cs` — add `DbSet<>` for 14 new entities + handle `RefNutrient` timestamps
2. Create `NutritionFoodItemSeeder.cs` — seed 45 food items + vi/en translations
3. Create `NutrientSeeder.cs` — seed 15 nutrients + vi/en translations
4. Update `AiPromptTemplateSeed.cs` — add nutrition.meal_plan template
5. Register seeders in `AppDbContext.OnModelCreating`
6. Run `dotnet ef migrations add Week7_NutritionMealPlanning`

### ⚠️ Convention Reminders
- Seed data uses anonymous types + fixed `DateTime`
- GUIDs are hardcoded strings
- Seeders called from `OnModelCreating`
- `RefNutrient` needs manual timestamp handling in `UpdateTimestamps()`

### 1. Update `AppDbContext.cs`

Add these `DbSet<>` properties (after Week 6 section):

```csharp
        // Week 7 — Nutrition + Meal Planning
        public DbSet<RefFoodItem> RefFoodItems { get; set; }
        public DbSet<RefFoodItemTranslation> RefFoodItemTranslations { get; set; }
        public DbSet<RefNutrient> RefNutrients { get; set; }
        public DbSet<RefNutrientTranslation> RefNutrientTranslations { get; set; }
        public DbSet<PregnancyFoodPreference> PregnancyFoodPreferences { get; set; }
        public DbSet<PregnancyNutritionNote> PregnancyNutritionNotes { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanDay> MealPlanDays { get; set; }
        public DbSet<MealItem> MealItems { get; set; }
        public DbSet<MealItemNutrient> MealItemNutrients { get; set; }
        public DbSet<MealPlanFeedback> MealPlanFeedbacks { get; set; }
        public DbSet<MealItemFeedback> MealItemFeedbacks { get; set; }
        public DbSet<AiRequestLog> AiRequestLogs { get; set; }
```

Add to `OnModelCreating` (after existing seeders):
```csharp
            // Week 7 — Nutrition Seeders
            NutritionFoodItemSeeder.Seed(modelBuilder);
            NutrientSeeder.Seed(modelBuilder);
```

Add to `UpdateTimestamps()` method — handle `RefNutrient` (does NOT inherit BaseEntity):
```csharp
                // Week 7 — RefNutrient (custom entity, not BaseEntity)
                else if (entry.Entity is RefNutrient nutrient)
                {
                    if (entry.State == EntityState.Added)
                    {
                        nutrient.CreatedAt = DateTime.UtcNow;
                    }
                    nutrient.UpdatedAt = DateTime.UtcNow;
                }
```

### 2. Create `src/FPT.EXE201.Infrastructure/Persistence/Seeders/NutritionFoodItemSeeder.cs`

```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class NutritionFoodItemSeeder
{
    private static readonly DateTime SeedDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        // ═══ FOOD ITEMS ═══
        var items = new (string id, string code)[]
        {
            // Proteins
            ("c7010001-0000-0000-0000-000000000001", "CHICKEN"),
            ("c7010001-0000-0000-0000-000000000002", "PORK"),
            ("c7010001-0000-0000-0000-000000000003", "BEEF"),
            ("c7010001-0000-0000-0000-000000000004", "FISH_SALMON"),
            ("c7010001-0000-0000-0000-000000000005", "FISH_TUNA"),
            ("c7010001-0000-0000-0000-000000000006", "FISH_MACKEREL"),
            ("c7010001-0000-0000-0000-000000000007", "SHRIMP"),
            ("c7010001-0000-0000-0000-000000000008", "CRAB"),
            ("c7010001-0000-0000-0000-000000000009", "SQUID"),
            ("c7010001-0000-0000-0000-00000000000a", "CLAM"),
            ("c7010001-0000-0000-0000-00000000000b", "EGG"),
            ("c7010001-0000-0000-0000-00000000000c", "TOFU"),
            ("c7010001-0000-0000-0000-00000000000d", "TEMPEH"),
            // Allergens
            ("c7010002-0000-0000-0000-000000000001", "SEAFOOD_GENERAL"),
            ("c7010002-0000-0000-0000-000000000002", "PEANUT"),
            ("c7010002-0000-0000-0000-000000000003", "TREE_NUT"),
            ("c7010002-0000-0000-0000-000000000004", "MILK_COW"),
            ("c7010002-0000-0000-0000-000000000005", "GLUTEN"),
            ("c7010002-0000-0000-0000-000000000006", "SOYBEAN"),
            ("c7010002-0000-0000-0000-000000000007", "SHELLFISH"),
            ("c7010002-0000-0000-0000-000000000008", "SESAME"),
            // Vegetables
            ("c7010003-0000-0000-0000-000000000001", "CILANTRO"),
            ("c7010003-0000-0000-0000-000000000002", "BITTER_MELON"),
            ("c7010003-0000-0000-0000-000000000003", "MORNING_GLORY"),
            ("c7010003-0000-0000-0000-000000000004", "SPINACH"),
            ("c7010003-0000-0000-0000-000000000005", "BOK_CHOY"),
            ("c7010003-0000-0000-0000-000000000006", "BEAN_SPROUT"),
            ("c7010003-0000-0000-0000-000000000007", "ONION"),
            ("c7010003-0000-0000-0000-000000000008", "GARLIC"),
            ("c7010003-0000-0000-0000-000000000009", "GINGER"),
            // Fruits
            ("c7010004-0000-0000-0000-000000000001", "DURIAN"),
            ("c7010004-0000-0000-0000-000000000002", "JACKFRUIT"),
            ("c7010004-0000-0000-0000-000000000003", "PINEAPPLE"),
            ("c7010004-0000-0000-0000-000000000004", "PAPAYA_GREEN"),
            // Condiments/Others
            ("c7010005-0000-0000-0000-000000000001", "SHRIMP_PASTE"),
            ("c7010005-0000-0000-0000-000000000002", "FISH_SAUCE_STRONG"),
            ("c7010005-0000-0000-0000-000000000003", "MSG"),
            ("c7010005-0000-0000-0000-000000000004", "ORGAN_MEAT_LIVER"),
            ("c7010005-0000-0000-0000-000000000005", "ORGAN_MEAT_GENERAL"),
            ("c7010005-0000-0000-0000-000000000006", "CAFFEINE"),
            ("c7010005-0000-0000-0000-000000000007", "ALCOHOL"),
            // Pregnancy-avoid
            ("c7010006-0000-0000-0000-000000000001", "RAW_FISH"),
            ("c7010006-0000-0000-0000-000000000002", "SOFT_CHEESE"),
            ("c7010006-0000-0000-0000-000000000003", "RAW_EGG"),
            ("c7010006-0000-0000-0000-000000000004", "DELI_MEAT"),
        };

        foreach (var (id, code) in items)
        {
            builder.Entity<RefFoodItem>().HasData(new
            {
                Id = new Guid(id),
                Code = code,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══ TRANSLATIONS — Vietnamese ═══
        var translationsVi = new (string id, string vi)[]
        {
            ("c7010001-0000-0000-0000-000000000001", "Thịt gà"),
            ("c7010001-0000-0000-0000-000000000002", "Thịt heo"),
            ("c7010001-0000-0000-0000-000000000003", "Thịt bò"),
            ("c7010001-0000-0000-0000-000000000004", "Cá hồi"),
            ("c7010001-0000-0000-0000-000000000005", "Cá ngừ"),
            ("c7010001-0000-0000-0000-000000000006", "Cá thu"),
            ("c7010001-0000-0000-0000-000000000007", "Tôm"),
            ("c7010001-0000-0000-0000-000000000008", "Cua"),
            ("c7010001-0000-0000-0000-000000000009", "Mực"),
            ("c7010001-0000-0000-0000-00000000000a", "Nghêu / Sò"),
            ("c7010001-0000-0000-0000-00000000000b", "Trứng"),
            ("c7010001-0000-0000-0000-00000000000c", "Đậu phụ"),
            ("c7010001-0000-0000-0000-00000000000d", "Tempeh"),
            ("c7010002-0000-0000-0000-000000000001", "Hải sản (chung)"),
            ("c7010002-0000-0000-0000-000000000002", "Đậu phộng"),
            ("c7010002-0000-0000-0000-000000000003", "Hạt cây (óc chó, hạnh nhân...)"),
            ("c7010002-0000-0000-0000-000000000004", "Sữa bò"),
            ("c7010002-0000-0000-0000-000000000005", "Gluten (lúa mì)"),
            ("c7010002-0000-0000-0000-000000000006", "Đậu nành"),
            ("c7010002-0000-0000-0000-000000000007", "Động vật có vỏ"),
            ("c7010002-0000-0000-0000-000000000008", "Mè (vừng)"),
            ("c7010003-0000-0000-0000-000000000001", "Rau mùi (ngò)"),
            ("c7010003-0000-0000-0000-000000000002", "Khổ qua (mướp đắng)"),
            ("c7010003-0000-0000-0000-000000000003", "Rau muống"),
            ("c7010003-0000-0000-0000-000000000004", "Rau bina (cải bó xôi)"),
            ("c7010003-0000-0000-0000-000000000005", "Cải thìa"),
            ("c7010003-0000-0000-0000-000000000006", "Giá đỗ"),
            ("c7010003-0000-0000-0000-000000000007", "Hành"),
            ("c7010003-0000-0000-0000-000000000008", "Tỏi"),
            ("c7010003-0000-0000-0000-000000000009", "Gừng"),
            ("c7010004-0000-0000-0000-000000000001", "Sầu riêng"),
            ("c7010004-0000-0000-0000-000000000002", "Mít"),
            ("c7010004-0000-0000-0000-000000000003", "Dứa (thơm)"),
            ("c7010004-0000-0000-0000-000000000004", "Đu đủ xanh"),
            ("c7010005-0000-0000-0000-000000000001", "Mắm tôm"),
            ("c7010005-0000-0000-0000-000000000002", "Nước mắm nặng mùi"),
            ("c7010005-0000-0000-0000-000000000003", "Bột ngọt (MSG)"),
            ("c7010005-0000-0000-0000-000000000004", "Gan"),
            ("c7010005-0000-0000-0000-000000000005", "Nội tạng (chung)"),
            ("c7010005-0000-0000-0000-000000000006", "Caffeine"),
            ("c7010005-0000-0000-0000-000000000007", "Rượu bia"),
            ("c7010006-0000-0000-0000-000000000001", "Cá sống / Sashimi"),
            ("c7010006-0000-0000-0000-000000000002", "Phô mai mềm"),
            ("c7010006-0000-0000-0000-000000000003", "Trứng sống"),
            ("c7010006-0000-0000-0000-000000000004", "Thịt nguội (deli meat)"),
        };

        foreach (var (id, vi) in translationsVi)
        {
            builder.Entity<RefFoodItemTranslation>().HasData(new
            {
                FoodItemId = new Guid(id),
                LanguageCode = "vi",
                DisplayName = vi
            });
        }

        // ═══ TRANSLATIONS — English ═══
        var translationsEn = new (string id, string en)[]
        {
            ("c7010001-0000-0000-0000-000000000001", "Chicken"),
            ("c7010001-0000-0000-0000-000000000002", "Pork"),
            ("c7010001-0000-0000-0000-000000000003", "Beef"),
            ("c7010001-0000-0000-0000-000000000004", "Salmon"),
            ("c7010001-0000-0000-0000-000000000005", "Tuna"),
            ("c7010001-0000-0000-0000-000000000006", "Mackerel"),
            ("c7010001-0000-0000-0000-000000000007", "Shrimp"),
            ("c7010001-0000-0000-0000-000000000008", "Crab"),
            ("c7010001-0000-0000-0000-000000000009", "Squid"),
            ("c7010001-0000-0000-0000-00000000000a", "Clam / Mussel"),
            ("c7010001-0000-0000-0000-00000000000b", "Egg"),
            ("c7010001-0000-0000-0000-00000000000c", "Tofu"),
            ("c7010001-0000-0000-0000-00000000000d", "Tempeh"),
            ("c7010002-0000-0000-0000-000000000001", "Seafood (general)"),
            ("c7010002-0000-0000-0000-000000000002", "Peanut"),
            ("c7010002-0000-0000-0000-000000000003", "Tree nuts (walnut, almond...)"),
            ("c7010002-0000-0000-0000-000000000004", "Cow's milk"),
            ("c7010002-0000-0000-0000-000000000005", "Gluten (wheat)"),
            ("c7010002-0000-0000-0000-000000000006", "Soybean"),
            ("c7010002-0000-0000-0000-000000000007", "Shellfish"),
            ("c7010002-0000-0000-0000-000000000008", "Sesame"),
            ("c7010003-0000-0000-0000-000000000001", "Cilantro (coriander)"),
            ("c7010003-0000-0000-0000-000000000002", "Bitter melon"),
            ("c7010003-0000-0000-0000-000000000003", "Morning glory (water spinach)"),
            ("c7010003-0000-0000-0000-000000000004", "Spinach"),
            ("c7010003-0000-0000-0000-000000000005", "Bok choy"),
            ("c7010003-0000-0000-0000-000000000006", "Bean sprouts"),
            ("c7010003-0000-0000-0000-000000000007", "Onion"),
            ("c7010003-0000-0000-0000-000000000008", "Garlic"),
            ("c7010003-0000-0000-0000-000000000009", "Ginger"),
            ("c7010004-0000-0000-0000-000000000001", "Durian"),
            ("c7010004-0000-0000-0000-000000000002", "Jackfruit"),
            ("c7010004-0000-0000-0000-000000000003", "Pineapple"),
            ("c7010004-0000-0000-0000-000000000004", "Green papaya"),
            ("c7010005-0000-0000-0000-000000000001", "Shrimp paste"),
            ("c7010005-0000-0000-0000-000000000002", "Strong fish sauce"),
            ("c7010005-0000-0000-0000-000000000003", "MSG"),
            ("c7010005-0000-0000-0000-000000000004", "Liver"),
            ("c7010005-0000-0000-0000-000000000005", "Organ meat (general)"),
            ("c7010005-0000-0000-0000-000000000006", "Caffeine"),
            ("c7010005-0000-0000-0000-000000000007", "Alcohol"),
            ("c7010006-0000-0000-0000-000000000001", "Raw fish / Sashimi"),
            ("c7010006-0000-0000-0000-000000000002", "Soft cheese"),
            ("c7010006-0000-0000-0000-000000000003", "Raw egg"),
            ("c7010006-0000-0000-0000-000000000004", "Deli meat"),
        };

        foreach (var (id, en) in translationsEn)
        {
            builder.Entity<RefFoodItemTranslation>().HasData(new
            {
                FoodItemId = new Guid(id),
                LanguageCode = "en",
                DisplayName = en
            });
        }
    }
}
```

### 3. Create `src/FPT.EXE201.Infrastructure/Persistence/Seeders/NutrientSeeder.cs`

```csharp
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class NutrientSeeder
{
    private static readonly DateTime SeedDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        var nutrients = new (string id, string code, string unit, string vi, string en)[]
        {
            ("c7020001-0000-0000-0000-000000000001", "CALORIES",       "kcal", "Năng lượng",   "Calories"),
            ("c7020001-0000-0000-0000-000000000002", "PROTEIN",        "g",    "Chất đạm",     "Protein"),
            ("c7020001-0000-0000-0000-000000000003", "CARBOHYDRATES",  "g",    "Tinh bột",     "Carbohydrates"),
            ("c7020001-0000-0000-0000-000000000004", "FAT",            "g",    "Chất béo",     "Fat"),
            ("c7020001-0000-0000-0000-000000000005", "FIBER",          "g",    "Chất xơ",      "Fiber"),
            ("c7020001-0000-0000-0000-000000000006", "IRON",           "mg",   "Sắt",          "Iron"),
            ("c7020001-0000-0000-0000-000000000007", "CALCIUM",        "mg",   "Canxi",        "Calcium"),
            ("c7020001-0000-0000-0000-000000000008", "FOLIC_ACID",     "mcg",  "Axit folic",   "Folic acid"),
            ("c7020001-0000-0000-0000-000000000009", "VITAMIN_D",      "mcg",  "Vitamin D",    "Vitamin D"),
            ("c7020001-0000-0000-0000-00000000000a", "VITAMIN_C",      "mg",   "Vitamin C",    "Vitamin C"),
            ("c7020001-0000-0000-0000-00000000000b", "VITAMIN_A",      "mcg",  "Vitamin A",    "Vitamin A"),
            ("c7020001-0000-0000-0000-00000000000c", "VITAMIN_B12",    "mcg",  "Vitamin B12",  "Vitamin B12"),
            ("c7020001-0000-0000-0000-00000000000d", "OMEGA_3",        "mg",   "Omega-3",      "Omega-3"),
            ("c7020001-0000-0000-0000-00000000000e", "DHA",            "mg",   "DHA",          "DHA"),
            ("c7020001-0000-0000-0000-00000000000f", "ZINC",           "mg",   "Kẽm",          "Zinc"),
        };

        foreach (var (id, code, unit, vi, en) in nutrients)
        {
            var guid = new Guid(id);

            // RefNutrient (custom entity — NOT BaseEntity, no DeletedAt)
            builder.Entity<RefNutrient>().HasData(new
            {
                Id = guid,
                Code = code,
                Unit = unit,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });

            // Vietnamese translation
            builder.Entity<RefNutrientTranslation>().HasData(new
            {
                NutrientId = guid,
                LanguageCode = "vi",
                DisplayName = vi
            });

            // English translation
            builder.Entity<RefNutrientTranslation>().HasData(new
            {
                NutrientId = guid,
                LanguageCode = "en",
                DisplayName = en
            });
        }
    }
}
```

### 4. Update `AiPromptTemplateSeed.cs` — Add nutrition template

Thêm một `HasData` entry thứ hai cho `nutrition.meal_plan` template. Chèn vào cuối method `Configure`, sau entry `medical_record.extraction`:

```csharp
        // Week 7 — Nutrition Meal Plan Template
        builder.HasData(
            new
            {
                Id = Guid.Parse("a1000002-0000-0000-0000-000000000001"),
                TemplateKey = "nutrition.meal_plan",
                Version = 1,
                DisplayName = "Nutrition Meal Plan Generator",
                Description = "Generate 7-day AI meal plans with Vietnamese dishes, recipes, and nutrients for pregnant women.",

                SystemRules = @"You are a certified prenatal nutritionist AI assistant.
Respond in Vietnamese.
Output ONLY valid JSON matching the provided schema.
No markdown, no explanation, no extra text outside JSON.",

                DomainRules = @"Pregnancy nutrition guidelines (IOM):
- Trimester 1 (week 1-12): Focus folic acid (600mcg/day), no extra calories.
- Trimester 2 (week 13-26): +340 kcal/day, iron (27mg/day), calcium (1000mg/day).
- Trimester 3 (week 27-40): +450 kcal/day, increase protein, DHA.
- Daily water: 2.3L minimum.
- Avoid: raw fish, high-mercury fish, unpasteurized dairy, alcohol.
- Gestational diabetes: low GI foods, split meals, limit sugar.
- Preeclampsia: reduce sodium, increase potassium.",

                FeatureRules = @"Generate a 7-day meal plan with exactly 4 meals per day: BREAKFAST, LUNCH, DINNER, SNACK.

For EVERY meal item, you MUST provide:
- itemName: Vietnamese dish name (concise)
- portionText: serving size in Vietnamese
- caloriesKcal: integer
- notes: brief nutrition note in Vietnamese (nullable)
- recipe: REQUIRED object with:
  - title: dish name
  - instructions: step-by-step cooking instructions in Vietnamese
  - servings: integer
  - prepMinutes: integer
  - cookMinutes: integer
- nutrients: array of objects, ONLY use these codes:
  PROTEIN, CARBOHYDRATES, FAT, FIBER, IRON, CALCIUM,
  FOLIC_ACID, VITAMIN_D, VITAMIN_C, VITAMIN_A,
  VITAMIN_B12, OMEGA_3, DHA, ZINC
  Each: { ""code"": ""PROTEIN"", ""amount"": 12.5 }

Ensure variety: do not repeat the same dish within 3 days.
Each day's total calories should be close to {targetCalories} kcal.",

                OutputSchema = @"{
  ""title"": ""string"",
  ""totalDailyCalories"": ""number"",
  ""notes"": ""string"",
  ""days"": [
    {
      ""date"": ""YYYY-MM-DD"",
      ""meals"": [
        {
          ""mealType"": ""BREAKFAST|LUNCH|DINNER|SNACK"",
          ""itemName"": ""string"",
          ""portionText"": ""string"",
          ""caloriesKcal"": ""number"",
          ""notes"": ""string|null"",
          ""recipe"": {
            ""title"": ""string"",
            ""instructions"": ""string"",
            ""servings"": ""number"",
            ""prepMinutes"": ""number"",
            ""cookMinutes"": ""number""
          },
          ""nutrients"": [
            { ""code"": ""string"", ""amount"": ""number"" }
          ]
        }
      ]
    }
  ]
}",

                ModelName = "gemini-2.5-flash",
                Temperature = 0.7,
                MaxOutputTokens = 8192,
                IsActive = true,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
```

### 5. Permission Seed Data — Update `DatabaseSeeder.cs`

Permission seeding dùng **runtime `DatabaseSeeder`** (KHÔNG dùng `HasData`). Cần thêm vào file `src/FPT.EXE201.Infrastructure/Persistence/DatabaseSeeder.cs`.

**5a. Thêm permissions** — chèn vào cuối Section 2 (sau dòng `pregnancy.test.delete`, trước `await context.SaveChangesAsync()`):

```csharp
            // Week 7 — Nutrition + Meal Planning
            await SeedPermissionIfNotExists(context, "food_preference.read", "Read Food Preferences", "User can view their food preferences and allergies");
            await SeedPermissionIfNotExists(context, "food_preference.write", "Write Food Preferences", "User can add/update food preferences and allergies");
            await SeedPermissionIfNotExists(context, "food_preference.delete", "Delete Food Preferences", "User can remove food preferences");
            await SeedPermissionIfNotExists(context, "nutrition_note.read", "Read Nutrition Notes", "User can view their nutrition notes");
            await SeedPermissionIfNotExists(context, "nutrition_note.write", "Write Nutrition Notes", "User can add/update nutrition notes");
            await SeedPermissionIfNotExists(context, "nutrition_note.delete", "Delete Nutrition Notes", "User can remove nutrition notes");
            await SeedPermissionIfNotExists(context, "meal_plan.read", "Read Meal Plans", "User can view their meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan.generate", "Generate Meal Plan", "User can generate AI meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan.delete", "Delete Meal Plans", "User can delete their meal plans");
            await SeedPermissionIfNotExists(context, "recipe.read", "Read Recipes", "User can view recipes in their meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan_feedback.write", "Write Meal Plan Feedback", "User can rate meal plans");
            await SeedPermissionIfNotExists(context, "meal_item_feedback.write", "Write Meal Item Feedback", "User can like/dislike meal items");
```

> ⚠️ Không seed `ref_food_item.read` và `ref_nutrient.read` — 2 endpoint này nằm trong `RefDataController` không cần `[RequirePermission]` (public ref data).

**5b. Assign permissions cho USER role** — thêm vào mảng `userPermissionCodes`:

```csharp
                // Week 7 — Nutrition
                "food_preference.read", "food_preference.write", "food_preference.delete",
                "nutrition_note.read", "nutrition_note.write", "nutrition_note.delete",
                "meal_plan.read", "meal_plan.generate", "meal_plan.delete",
                "recipe.read",
                "meal_plan_feedback.write", "meal_item_feedback.write",
```

**5c. Assign permissions cho DOCTOR role** — thêm vào mảng `doctorPermissionCodes`:

```csharp
                // Week 7 — Nutrition (same as USER except meal_plan.generate)
                "food_preference.read", "food_preference.write", "food_preference.delete",
                "nutrition_note.read", "nutrition_note.write", "nutrition_note.delete",
                "meal_plan.read", "meal_plan.delete",
                "recipe.read",
                "meal_plan_feedback.write", "meal_item_feedback.write",
```

> ⚠️ ADMIN nhận **tất cả** permissions tự động (dùng `permissions.Select(p => p.Id)`) — không cần thêm.
> ⚠️ `meal_plan.generate` chỉ gán cho USER role (không gán cho DOCTOR) — doctor chỉ xem, không generate.

### 6. Run Migration

```bash
cd src/FPT.EXE201.Infrastructure
dotnet ef migrations add Week7_NutritionMealPlanning --startup-project ../FPT.EXE201.Api
dotnet ef database update --startup-project ../FPT.EXE201.Api
```

### ✅ Checkpoint — Prompt 4
- [ ] `AppDbContext` has 14 new `DbSet<>` properties
- [ ] `UpdateTimestamps()` handles `RefNutrient` (custom entity)
- [ ] `NutritionFoodItemSeeder` seeds 45 food items + vi/en translations
- [ ] `NutrientSeeder` seeds 15 nutrients + vi/en translations
- [ ] `AiPromptTemplateSeed` has `nutrition.meal_plan` template with `max_output_tokens = 8192`
- [ ] Seeders registered in `OnModelCreating`
- [ ] 12 permissions seeded in `DatabaseSeeder.cs` (Section 2)
- [ ] USER role assigned 12 nutrition permissions (including `meal_plan.generate`)
- [ ] DOCTOR role assigned 11 nutrition permissions (excluding `meal_plan.generate`)
- [ ] ADMIN role gets all permissions automatically
- [ ] Migration created and applied successfully
- [ ] All 14 tables exist in database

---

## 🎯 PROMPT 5/10 — Application: DTOs + FluentValidation

### Task
Tạo DTOs (record pattern) + FluentValidation validators trong folder `Application/DTOs/Nutrition/` và `Application/Validations/Nutrition/`.

### ⚠️ Convention Reminders
- DTOs PHẢI là `record` — KHÔNG dùng class
- Enums trong response DTOs serialized as `string` (không dùng enum type)
- Create/Update DTOs dùng primary constructor với default values
- Mỗi Create DTO cần FluentValidation validator

### Folder: `src/FPT.EXE201.Application/DTOs/Nutrition/`

#### 1. `RefFoodItemDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record RefFoodItemDto(
    Guid Id,
    string Code,
    string DisplayName
);
```

#### 2. `RefNutrientDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record RefNutrientDto(
    Guid Id,
    string Code,
    string Unit,
    string DisplayName
);
```

#### 3. `CreateFoodPreferenceDto.cs`
```csharp
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateFoodPreferenceDto(
    Guid FoodItemId,
    FoodPreferenceType PreferenceType,
    AllergySeverity? Severity = null,
    string? Notes = null
);
```

#### 4. `UpdateFoodPreferenceDto.cs`
```csharp
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record UpdateFoodPreferenceDto(
    AllergySeverity? Severity = null,
    string? Notes = null
);
```

#### 5. `FoodPreferenceDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record FoodPreferenceDto(
    Guid Id,
    Guid PregnancyId,
    Guid FoodItemId,
    string FoodItemCode,
    string FoodItemDisplayName,
    string PreferenceType,
    string? Severity,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

#### 6. `CreateNutritionNoteDto.cs`
```csharp
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateNutritionNoteDto(
    NutritionNoteType NoteType,
    string ValueText
);
```

#### 7. `UpdateNutritionNoteDto.cs`
```csharp
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record UpdateNutritionNoteDto(
    NutritionNoteType? NoteType = null,
    string? ValueText = null
);
```

#### 8. `NutritionNoteDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record NutritionNoteDto(
    Guid Id,
    Guid PregnancyId,
    string NoteType,
    string ValueText,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

#### 9. `GenerateMealPlanDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record GenerateMealPlanDto(
    DateOnly StartDate,
    int DurationWeeks,
    string? AdditionalNotes = null
);
```

#### 10. `MealPlanSummaryDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanSummaryDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Source,
    string? Title,
    int TotalDays,
    DateTime CreatedAt
);
```

#### 11. `MealPlanDetailDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanDetailDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Source,
    string? Title,
    string? Notes,
    List<MealPlanDaySummaryDto> Days,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record MealPlanDaySummaryDto(
    Guid Id,
    DateOnly PlanDate,
    int TotalCalories,
    int MealCount
);
```

#### 12. `MealDayDetailDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealDayDetailDto(
    Guid Id,
    Guid MealPlanId,
    DateOnly PlanDate,
    int TotalCalories,
    List<MealItemDto> Meals
);

public record MealItemDto(
    Guid Id,
    string MealType,
    Guid? RecipeId,
    string? ItemName,
    string? PortionText,
    int? CaloriesKcal,
    string? Notes,
    List<MealItemNutrientDto> Nutrients
);

public record MealItemNutrientDto(
    string NutrientCode,
    string NutrientName,
    string Unit,
    decimal Amount
);
```

#### 13. `RecipeDetailDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record RecipeDetailDto(
    Guid Id,
    Guid PregnancyId,
    string Title,
    string? Instructions,
    int? Servings,
    int? PrepMinutes,
    int? CookMinutes,
    DateTime CreatedAt
);
```

#### 14. `CreateMealPlanFeedbackDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateMealPlanFeedbackDto(
    int Rating,
    string? Comment = null
);
```

#### 15. `CreateMealItemFeedbackDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateMealItemFeedbackDto(
    bool Liked,
    string? Comment = null
);
```

#### 16. `MealPlanFeedbackDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanFeedbackDto(
    Guid Id,
    Guid MealPlanId,
    Guid UserId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
```

#### 17. `MealItemFeedbackDto.cs`
```csharp
namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealItemFeedbackDto(
    Guid Id,
    Guid MealItemId,
    Guid UserId,
    bool Liked,
    string? Comment,
    DateTime CreatedAt
);
```

---

### Folder: `src/FPT.EXE201.Application/Validations/Nutrition/`

#### 1. `CreateFoodPreferenceDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateFoodPreferenceDtoValidator : AbstractValidator<CreateFoodPreferenceDto>
{
    public CreateFoodPreferenceDtoValidator()
    {
        RuleFor(x => x.FoodItemId)
            .NotEmpty().WithMessage("Food item ID is required.");

        RuleFor(x => x.PreferenceType)
            .IsInEnum().WithMessage("Preference type must be Allergy or Dislike.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Severity must be Low, Medium, or High.")
            .When(x => x.Severity.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters.");
    }
}
```

#### 2. `CreateNutritionNoteDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateNutritionNoteDtoValidator : AbstractValidator<CreateNutritionNoteDto>
{
    public CreateNutritionNoteDtoValidator()
    {
        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Note type must be Diet, Note, or Other.");

        RuleFor(x => x.ValueText)
            .NotEmpty().WithMessage("Value text is required.")
            .MaximumLength(200).WithMessage("Value text cannot exceed 200 characters.");
    }
}
```

#### 3. `GenerateMealPlanDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class GenerateMealPlanDtoValidator : AbstractValidator<GenerateMealPlanDto>
{
    public GenerateMealPlanDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date must be today or in the future.");

        RuleFor(x => x.DurationWeeks)
            .InclusiveBetween(1, 4)
            .WithMessage("Duration must be between 1 and 4 weeks.");

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(500).WithMessage("Additional notes cannot exceed 500 characters.");
    }
}
```

#### 4. `CreateMealPlanFeedbackDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateMealPlanFeedbackDtoValidator : AbstractValidator<CreateMealPlanFeedbackDto>
{
    public CreateMealPlanFeedbackDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
    }
}
```

#### 5. `CreateMealItemFeedbackDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class CreateMealItemFeedbackDtoValidator : AbstractValidator<CreateMealItemFeedbackDto>
{
    public CreateMealItemFeedbackDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(300).WithMessage("Comment cannot exceed 300 characters.");
    }
}
```

#### 6. `UpdateFoodPreferenceDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class UpdateFoodPreferenceDtoValidator : AbstractValidator<UpdateFoodPreferenceDto>
{
    public UpdateFoodPreferenceDtoValidator()
    {
        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Severity must be Low, Medium, or High.")
            .When(x => x.Severity.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters.");
    }
}
```

#### 7. `UpdateNutritionNoteDtoValidator.cs`
```csharp
using FluentValidation;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.Validations.Nutrition;

public class UpdateNutritionNoteDtoValidator : AbstractValidator<UpdateNutritionNoteDto>
{
    public UpdateNutritionNoteDtoValidator()
    {
        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Note type must be Diet, Note, or Other.")
            .When(x => x.NoteType.HasValue);

        RuleFor(x => x.ValueText)
            .MaximumLength(200).WithMessage("Value text cannot exceed 200 characters.")
            .When(x => x.ValueText != null);
    }
}
```

### ✅ Checkpoint — Prompt 5
- [ ] 17 DTO files created in `Application/DTOs/Nutrition/`
- [ ] All DTOs are `record` — no classes
- [ ] Enums in response DTOs are `string` type
- [ ] 7 validator files created in `Application/Validations/Nutrition/`
- [ ] All Create DTOs have corresponding validators
- [ ] Validators auto-registered by FluentValidation assembly scanning
- [ ] Project builds without errors

---

## 📌 Part 1 Complete — Summary

| Prompt | Layer | Files Created |
|--------|-------|---------------|
| 1/10 | Context | — (reference only) |
| 2/10 | Domain | 7 enums + 14 entities + 1 modification = **22 files** |
| 3/10 | Infrastructure | 14 EF configurations = **14 files** |
| 4/10 | Infrastructure | AppDbContext update + 2 seeders + 1 template seed + permissions + migration = **3 new + 3 modified** |
| 5/10 | Application | 17 DTOs + 7 validators = **24 files** |
| **Total** | | **~63 files** |

### Remaining in Part 2 (Prompts 6–10):
- **Prompt 6/10**: Repository interfaces + implementations + UoW updates
- **Prompt 7/10**: Service interfaces (5 services)
- **Prompt 8/10**: Service implementations — RefData, FoodPreference, Recipe, Feedback
- **Prompt 9/10**: MealPlanService — AI generation logic (main business logic)
- **Prompt 10/10**: Controllers (5) + DI registration + Final checklist

---

## 🎯 END OF PART 1 — Tiếp tục xem `WEEK_7_PROMPTS_GUIDE_PART2.md` cho Prompts 6–10.
