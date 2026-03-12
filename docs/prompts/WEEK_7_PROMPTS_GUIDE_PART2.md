# WEEK 7 PROMPTS GUIDE — Nutrition + Meal Planning (Part 2: Prompts 6–10)

> **Scope**: Application layer (Repositories, UoW, Services) + API layer (Controllers, DI).
> Part 1 (Prompts 1–5): Domain + Infrastructure → xem `WEEK_7_PROMPTS_GUIDE.md`.
> **Tham chiếu**: `WEEK_7_NUTRITION_DECISIONS.md`, `DEVELOPMENT_WORKFLOW_GUIDE.md`.

---

## ⚠️ CONVENTIONS (RECAP — xem đầy đủ ở Part 1)

1. ⚠️ **BaseEntity**: Entities kế thừa → có `Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `IsDeleted` (computed, KHÔNG map DB).
2. ⚠️ **Repository**: `IGenericRepository<T> where T : BaseEntity`. Custom entity (RefNutrient) dùng standalone repo pattern.
3. ⚠️ **UnitOfWork**: Lazy `??=` pattern. Interface trong Application, Implementation trong Infrastructure.
4. ⚠️ **Services**: Throw exceptions — KHÔNG return null cho lỗi. `VerifyPregnancyOwnership` pattern.
5. ⚠️ **Controllers**: `[Authorize]`, `[RequirePermission]`, extends `BaseApiController`. KHÔNG try-catch.
6. ⚠️ **DTOs là `record`**, enums trong response là `string`.
7. ⚠️ **AI Pipeline**: `PromptBuilder.FromTemplate(template).WithContext().WithUserMessage().Build()` → `_aiProvider.GenerateAsync()`.
8. ⚠️ **File-scoped namespace** cho tất cả file mới.

---

## 🎯 PROMPT 6/10 — Repository Interfaces + Implementations + UoW Updates

### Task
Tạo **11 repository interfaces** + **11 implementations** + update `IUnitOfWork` và `UnitOfWork`.

### ⚠️ Convention Reminders
- GenericRepository-based repos: `: IGenericRepository<T>` → impl `: GenericRepository<T>, IXRepository`
- `RefNutrientRepository`: standalone (RefNutrient không kế thừa BaseEntity) → pattern giống `WeightAlertRepository`
- Constructor pattern: `public XRepository(AppDbContext context) : base(context) { }`
- UoW: Lazy `??=` pattern, nhóm theo week, private nullable field

---

### Repository Interfaces — `src/FPT.EXE201.Application/IRepositories/`

#### 1. `IRefFoodItemRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefFoodItemRepository : IGenericRepository<RefFoodItem>
{
    Task<List<RefFoodItem>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default);
}
```

#### 2. `IRefNutrientRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

/// ⚠️ Standalone interface — RefNutrient KHÔNG kế thừa BaseEntity (Decision #2).
/// Pattern giống IWeightAlertRepository.
public interface IRefNutrientRepository
{
    Task<List<RefNutrient>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default);
    Task<List<RefNutrient>> GetByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default);
}
```

#### 3. `IPregnancyFoodPreferenceRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyFoodPreferenceRepository : IGenericRepository<PregnancyFoodPreference>
{
    Task<List<PregnancyFoodPreference>> GetByPregnancyIdAsync(
        Guid pregnancyId, string langCode, CancellationToken ct = default);
    Task<bool> ExistsByPregnancyFoodItemTypeAsync(
        Guid pregnancyId, Guid foodItemId, FoodPreferenceType type,
        CancellationToken ct = default);
}
```

#### 4. `IPregnancyNutritionNoteRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyNutritionNoteRepository : IGenericRepository<PregnancyNutritionNote>
{
    Task<List<PregnancyNutritionNote>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);
}
```

#### 5. `IRecipeRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRecipeRepository : IGenericRepository<Recipe>
{
    Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
}
```

#### 6. `IMealPlanRepository.cs`
```csharp
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanRepository : IGenericRepository<MealPlan>
{
    Task<PagedResult<MealPlan>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default);
    Task<MealPlan?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken ct = default);
    Task<List<MealPlan>> GetOverlappingAsync(
        Guid pregnancyId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default);
}
```

#### 7. `IMealPlanDayRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanDayRepository : IGenericRepository<MealPlanDay>
{
    Task<MealPlanDay?> GetByPlanIdAndDateAsync(
        Guid planId, DateOnly date, CancellationToken ct = default);
}
```

#### 8. `IMealItemRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealItemRepository : IGenericRepository<MealItem> { }
```

#### 9. `IMealPlanFeedbackRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanFeedbackRepository : IGenericRepository<MealPlanFeedback>
{
    Task<bool> ExistsByPlanAndUserAsync(
        Guid mealPlanId, Guid userId, CancellationToken ct = default);
}
```

#### 10. `IMealItemFeedbackRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealItemFeedbackRepository : IGenericRepository<MealItemFeedback>
{
    Task<bool> ExistsByItemAndUserAsync(
        Guid mealItemId, Guid userId, CancellationToken ct = default);
}
```

#### 11. `IAiRequestLogRepository.cs`
```csharp
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IAiRequestLogRepository : IGenericRepository<AiRequestLog>
{
    Task<int> CountTodayByUserAsync(Guid userId, CancellationToken ct = default);
}
```

---

### Repository Implementations — `src/FPT.EXE201.Infrastructure/Repositories/`

#### 1. `RefFoodItemRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefFoodItemRepository : GenericRepository<RefFoodItem>, IRefFoodItemRepository
{
    public RefFoodItemRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefFoodItem>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(f => f.IsActive)
            .Include(f => f.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(f => f.Code)
            .ToListAsync(ct);
    }
}
```

#### 2. `RefNutrientRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

/// ⚠️ Standalone repository — RefNutrient KHÔNG kế thừa BaseEntity.
/// Pattern giống WeightAlertRepository.
public class RefNutrientRepository : IRefNutrientRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<RefNutrient> _dbSet;

    public RefNutrientRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<RefNutrient>();
    }

    public async Task<List<RefNutrient>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => n.IsActive)
            .Include(n => n.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(n => n.Code)
            .ToListAsync(ct);
    }

    public async Task<List<RefNutrient>> GetByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => codes.Contains(n.Code) && n.IsActive)
            .ToListAsync(ct);
    }
}
```

#### 3. `PregnancyFoodPreferenceRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyFoodPreferenceRepository
    : GenericRepository<PregnancyFoodPreference>, IPregnancyFoodPreferenceRepository
{
    public PregnancyFoodPreferenceRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyFoodPreference>> GetByPregnancyIdAsync(
        Guid pregnancyId, string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(p => p.PregnancyId == pregnancyId)
            .Include(p => p.FoodItem)
                .ThenInclude(fi => fi.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(p => p.PreferenceType)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByPregnancyFoodItemTypeAsync(
        Guid pregnancyId, Guid foodItemId, FoodPreferenceType type,
        CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(
            p => p.PregnancyId == pregnancyId
                 && p.FoodItemId == foodItemId
                 && p.PreferenceType == type, ct);
    }
}
```

#### 4. `PregnancyNutritionNoteRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyNutritionNoteRepository
    : GenericRepository<PregnancyNutritionNote>, IPregnancyNutritionNoteRepository
{
    public PregnancyNutritionNoteRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyNutritionNote>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => n.PregnancyId == pregnancyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }
}
```

#### 5. `RecipeRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RecipeRepository : GenericRepository<Recipe>, IRecipeRepository
{
    public RecipeRepository(AppDbContext context) : base(context) { }

    public async Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(r => r.MealItems)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
```

#### 6. `MealPlanRepository.cs`
```csharp
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.MealPlans;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanRepository : GenericRepository<MealPlan>, IMealPlanRepository
{
    public MealPlanRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<MealPlan>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default)
    {
        return await GetPagedAsync(
            options,
            predicate: m => m.PregnancyId == pregnancyId,
            include: q => q.Include(m => m.Days),
            searchBuilder: SearchHelper.CreateSearchBuilder(
                MealPlanListQuerySpec.SearchMap,
                MealPlanListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: MealPlanListQuerySpec.SortMap,
            defaultSort: MealPlanListQuerySpec.DefaultSort,
            cancellationToken: ct);
    }

    public async Task<MealPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(m => m.Days.OrderBy(d => d.PlanDate))
                .ThenInclude(d => d.Items.OrderBy(i => i.MealType))
                    .ThenInclude(i => i.Nutrients)
                        .ThenInclude(n => n.Nutrient)
                            .ThenInclude(rn => rn.Translations)
            .Include(m => m.Days)
                .ThenInclude(d => d.Items)
                    .ThenInclude(i => i.Recipe)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<List<MealPlan>> GetOverlappingAsync(
        Guid pregnancyId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default)
    {
        return await _dbSet
            .Where(m => m.PregnancyId == pregnancyId
                        && m.StartDate <= endDate
                        && m.EndDate >= startDate)
            .ToListAsync(ct);
    }
}
```

#### 7. `MealPlanDayRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanDayRepository : GenericRepository<MealPlanDay>, IMealPlanDayRepository
{
    public MealPlanDayRepository(AppDbContext context) : base(context) { }

    public async Task<MealPlanDay?> GetByPlanIdAndDateAsync(
        Guid planId, DateOnly date, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(d => d.MealPlanId == planId && d.PlanDate == date)
            .Include(d => d.Items.OrderBy(i => i.MealType))
                .ThenInclude(i => i.Nutrients)
                    .ThenInclude(n => n.Nutrient)
                        .ThenInclude(rn => rn.Translations)
            .Include(d => d.Items)
                .ThenInclude(i => i.Recipe)
            .FirstOrDefaultAsync(ct);
    }
}
```

#### 8. `MealItemRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealItemRepository : GenericRepository<MealItem>, IMealItemRepository
{
    public MealItemRepository(AppDbContext context) : base(context) { }
}
```

#### 9. `MealPlanFeedbackRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanFeedbackRepository
    : GenericRepository<MealPlanFeedback>, IMealPlanFeedbackRepository
{
    public MealPlanFeedbackRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsByPlanAndUserAsync(
        Guid mealPlanId, Guid userId, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(
            f => f.MealPlanId == mealPlanId && f.UserId == userId, ct);
    }
}
```

#### 10. `MealItemFeedbackRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealItemFeedbackRepository
    : GenericRepository<MealItemFeedback>, IMealItemFeedbackRepository
{
    public MealItemFeedbackRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsByItemAndUserAsync(
        Guid mealItemId, Guid userId, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(
            f => f.MealItemId == mealItemId && f.UserId == userId, ct);
    }
}
```

#### 11. `AiRequestLogRepository.cs`
```csharp
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class AiRequestLogRepository
    : GenericRepository<AiRequestLog>, IAiRequestLogRepository
{
    public AiRequestLogRepository(AppDbContext context) : base(context) { }

    public async Task<int> CountTodayByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _dbSet.CountAsync(
            l => l.UserId == userId
                 && l.Feature == AiFeature.NutritionMealPlan
                 && l.CreatedAt >= todayUtc, ct);
    }
}
```

---

### UoW Updates

#### Update `src/FPT.EXE201.Application/IUnitOfWork.cs`

Thêm vào cuối (sau Week 6 section):

```csharp
        // Week 7 — Nutrition + Meal Planning
        IRefFoodItemRepository RefFoodItems { get; }
        IRefNutrientRepository RefNutrients { get; }
        IPregnancyFoodPreferenceRepository FoodPreferences { get; }
        IPregnancyNutritionNoteRepository NutritionNotes { get; }
        IRecipeRepository Recipes { get; }
        IMealPlanRepository MealPlans { get; }
        IMealPlanDayRepository MealPlanDays { get; }
        IMealItemRepository MealItems { get; }
        IMealPlanFeedbackRepository MealPlanFeedbacks { get; }
        IMealItemFeedbackRepository MealItemFeedbacks { get; }
        IAiRequestLogRepository AiRequestLogs { get; }
```

#### Update `src/FPT.EXE201.Infrastructure/Repositories/UnitOfWork.cs`

**1. Thêm private fields** (sau Week 6 fields):

```csharp
        // Week 7
        private IRefFoodItemRepository? _refFoodItems;
        private IRefNutrientRepository? _refNutrients;
        private IPregnancyFoodPreferenceRepository? _foodPreferences;
        private IPregnancyNutritionNoteRepository? _nutritionNotes;
        private IRecipeRepository? _recipes;
        private IMealPlanRepository? _mealPlans;
        private IMealPlanDayRepository? _mealPlanDays;
        private IMealItemRepository? _mealItems;
        private IMealPlanFeedbackRepository? _mealPlanFeedbacks;
        private IMealItemFeedbackRepository? _mealItemFeedbacks;
        private IAiRequestLogRepository? _aiRequestLogs;
```

**2. Thêm public properties** (sau Week 6 properties):

```csharp
        // Week 7
        public IRefFoodItemRepository RefFoodItems => _refFoodItems ??= new RefFoodItemRepository(_context);
        public IRefNutrientRepository RefNutrients => _refNutrients ??= new RefNutrientRepository(_context);
        public IPregnancyFoodPreferenceRepository FoodPreferences => _foodPreferences ??= new PregnancyFoodPreferenceRepository(_context);
        public IPregnancyNutritionNoteRepository NutritionNotes => _nutritionNotes ??= new PregnancyNutritionNoteRepository(_context);
        public IRecipeRepository Recipes => _recipes ??= new RecipeRepository(_context);
        public IMealPlanRepository MealPlans => _mealPlans ??= new MealPlanRepository(_context);
        public IMealPlanDayRepository MealPlanDays => _mealPlanDays ??= new MealPlanDayRepository(_context);
        public IMealItemRepository MealItems => _mealItems ??= new MealItemRepository(_context);
        public IMealPlanFeedbackRepository MealPlanFeedbacks => _mealPlanFeedbacks ??= new MealPlanFeedbackRepository(_context);
        public IMealItemFeedbackRepository MealItemFeedbacks => _mealItemFeedbacks ??= new MealItemFeedbackRepository(_context);
        public IAiRequestLogRepository AiRequestLogs => _aiRequestLogs ??= new AiRequestLogRepository(_context);
```

### ✅ Checkpoint — Prompt 6
- [ ] 11 repository interfaces created in `Application/IRepositories/`
- [ ] 11 repository implementations created in `Infrastructure/Repositories/`
- [ ] `IRefNutrientRepository` is standalone (no `IGenericRepository`) — Decision #2
- [ ] `IUnitOfWork.cs` has 11 new properties in Week 7 section
- [ ] `UnitOfWork.cs` has 11 private fields + 11 lazy `??=` properties
- [ ] Project builds without errors

---

## 🎯 PROMPT 7/10 — Service Interfaces

### Task
Tạo/extend **5 service interfaces** theo Decision #6.

### ⚠️ Convention Reminders
- Interface trong `Application/IServices/`
- Method signatures: `Task<T> MethodAsync(..., CancellationToken ct = default)`
- `userId` parameter cho ownership verification
- Dùng DTOs đã tạo ở Prompt 5

---

### Files:

#### 1. Update `src/FPT.EXE201.Application/IServices/IRefDataService.cs`

Thêm 2 methods vào interface hiện tại:

```csharp
    // Week 7 — Nutrition
    Task<List<RefFoodItemDto>> GetActiveFoodItemsAsync(
        string langCode, CancellationToken cancellationToken = default);
    Task<List<RefNutrientDto>> GetActiveNutrientsAsync(
        string langCode, CancellationToken cancellationToken = default);
```

> ⚠️ Cần thêm `using FPT.EXE201.Application.DTOs.Nutrition;` vào file.

#### 2. `src/FPT.EXE201.Application/IServices/IFoodPreferenceService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IFoodPreferenceService
{
    // Food Preferences
    Task<List<FoodPreferenceDto>> GetPreferencesAsync(
        Guid pregnancyId, Guid userId, string langCode = "vi",
        CancellationToken ct = default);
    Task<FoodPreferenceDto> CreatePreferenceAsync(
        Guid pregnancyId, Guid userId, CreateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default);
    Task<FoodPreferenceDto> UpdatePreferenceAsync(
        Guid prefId, Guid userId, UpdateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default);
    Task DeletePreferenceAsync(
        Guid prefId, Guid userId, CancellationToken ct = default);

    // Nutrition Notes
    Task<List<NutritionNoteDto>> GetNotesAsync(
        Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<NutritionNoteDto> CreateNoteAsync(
        Guid pregnancyId, Guid userId, CreateNutritionNoteDto dto,
        CancellationToken ct = default);
    Task<NutritionNoteDto> UpdateNoteAsync(
        Guid noteId, Guid userId, UpdateNutritionNoteDto dto,
        CancellationToken ct = default);
    Task DeleteNoteAsync(
        Guid noteId, Guid userId, CancellationToken ct = default);
}
```

#### 3. `src/FPT.EXE201.Application/IServices/IMealPlanService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IMealPlanService
{
    Task<MealPlanDetailDto> GenerateAsync(
        Guid pregnancyId, Guid userId, GenerateMealPlanDto dto,
        CancellationToken ct = default);
    Task<PagedResult<MealPlanSummaryDto>> ListAsync(
        Guid pregnancyId, Guid userId, QueryOptions options,
        CancellationToken ct = default);
    Task<MealPlanDetailDto> GetDetailAsync(
        Guid planId, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(
        Guid planId, Guid userId, CancellationToken ct = default);
    Task<MealDayDetailDto> GetDayDetailAsync(
        Guid planId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default);
}
```

#### 4. `src/FPT.EXE201.Application/IServices/IRecipeService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IRecipeService
{
    Task<RecipeDetailDto> GetByIdAsync(
        Guid recipeId, Guid userId, CancellationToken ct = default);
}
```

#### 5. `src/FPT.EXE201.Application/IServices/INutritionFeedbackService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface INutritionFeedbackService
{
    Task<MealPlanFeedbackDto> CreatePlanFeedbackAsync(
        Guid planId, Guid userId, CreateMealPlanFeedbackDto dto,
        CancellationToken ct = default);
    Task<MealItemFeedbackDto> CreateItemFeedbackAsync(
        Guid itemId, Guid userId, CreateMealItemFeedbackDto dto,
        CancellationToken ct = default);
}
```

### ✅ Checkpoint — Prompt 7
- [ ] `IRefDataService` extended with 2 new methods
- [ ] 4 new service interfaces created in `Application/IServices/`
- [ ] All methods return DTOs defined in Prompt 5
- [ ] All methods accept `CancellationToken ct = default`
- [ ] Project builds without errors

---

## 🎯 PROMPT 8/10 — Services: RefData + FoodPreference + Recipe + Feedback

### Task
Implement 4 services đơn giản (không phải MealPlanService — dành cho Prompt 9).

### ⚠️ Convention Reminders
- Services inject `IUnitOfWork`, dùng `VerifyPregnancyOwnership` pattern
- Throw exceptions: `NotFoundException`, `BadRequestException`, `ConflictException`, `ForbiddenException`
- Manual mapping (static helper methods) — pattern giống `WeightLogService`
- KHÔNG try-catch trừ khi xử lý AI pipeline

---

### QuerySpec: `src/FPT.EXE201.Application/Features/MealPlans/MealPlanListQuerySpec.cs`

> Tạo trước khi implement service — cần cho `MealPlanRepository.GetByPregnancyIdPagedAsync`.

```csharp
using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.MealPlans;

public static class MealPlanListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<MealPlan, string?>>> SearchMap = new()
    {
        ["title"] = m => m.Title,
        ["notes"] = m => m.Notes
    };
    public static readonly string[] DefaultSearchKeys = ["title"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["startdate"] = (Expression<Func<MealPlan, DateOnly>>)(m => m.StartDate),
        ["enddate"]   = (Expression<Func<MealPlan, DateOnly>>)(m => m.EndDate),
        ["createdat"] = (Expression<Func<MealPlan, DateTime>>)(m => m.CreatedAt)
    };
    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<MealPlan, DateTime>>)(m => m.CreatedAt);

    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "createdat",
        DefaultSortDir = "desc"
    };
}
```

---

### Files:

#### 1. Update `src/FPT.EXE201.Application/Services/RefDataService.cs`

Thêm 2 methods vào class hiện tại:

```csharp
    // Week 7 — Nutrition

    public async Task<List<RefFoodItemDto>> GetActiveFoodItemsAsync(
        string langCode, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.RefFoodItems
            .GetActiveWithTranslationsAsync(langCode, cancellationToken);
        return items.Select(f =>
        {
            var t = f.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefFoodItemDto(f.Id, f.Code, t?.DisplayName ?? f.Code);
        }).ToList();
    }

    public async Task<List<RefNutrientDto>> GetActiveNutrientsAsync(
        string langCode, CancellationToken cancellationToken = default)
    {
        var nutrients = await _unitOfWork.RefNutrients
            .GetActiveWithTranslationsAsync(langCode, cancellationToken);
        return nutrients.Select(n =>
        {
            var t = n.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefNutrientDto(n.Id, n.Code, n.Unit, t?.DisplayName ?? n.Code);
        }).ToList();
    }
```

> ⚠️ Cần thêm `using FPT.EXE201.Application.DTOs.Nutrition;` vào file.

#### 2. `src/FPT.EXE201.Application/Services/FoodPreferenceService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class FoodPreferenceService : IFoodPreferenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public FoodPreferenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ═══ Food Preferences ═══

    public async Task<List<FoodPreferenceDto>> GetPreferencesAsync(
        Guid pregnancyId, Guid userId, string langCode = "vi",
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var prefs = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pregnancyId, langCode, ct);

        return prefs.Select(p => MapToPreferenceDto(p, langCode)).ToList();
    }

    public async Task<FoodPreferenceDto> CreatePreferenceAsync(
        Guid pregnancyId, Guid userId, CreateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Validate food item exists
        var foodItem = await _unitOfWork.RefFoodItems.GetByIdAsync(dto.FoodItemId, cancellationToken: ct)
            ?? throw new NotFoundException("Food item not found.");

        // Check unique constraint: (pregnancy_id, food_item_id, preference_type)
        var exists = await _unitOfWork.FoodPreferences
            .ExistsByPregnancyFoodItemTypeAsync(pregnancyId, dto.FoodItemId, dto.PreferenceType, ct);
        if (exists)
            throw new ConflictException(
                $"A {dto.PreferenceType} preference for this food item already exists.");

        var pref = new PregnancyFoodPreference
        {
            PregnancyId = pregnancyId,
            FoodItemId = dto.FoodItemId,
            PreferenceType = dto.PreferenceType,
            Severity = dto.Severity,
            Notes = dto.Notes
        };

        await _unitOfWork.FoodPreferences.AddAsync(pref, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with translations for response
        var loaded = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pregnancyId, langCode, ct);
        var created = loaded.First(p => p.Id == pref.Id);
        return MapToPreferenceDto(created, langCode);
    }

    public async Task<FoodPreferenceDto> UpdatePreferenceAsync(
        Guid prefId, Guid userId, UpdateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default)
    {
        var pref = await _unitOfWork.FoodPreferences.GetByIdAsync(prefId, cancellationToken: ct)
            ?? throw new NotFoundException("Food preference not found.");

        await VerifyPregnancyOwnership(pref.PregnancyId, userId, ct);

        if (dto.Severity != null) pref.Severity = dto.Severity;
        if (dto.Notes != null) pref.Notes = dto.Notes;

        _unitOfWork.FoodPreferences.Update(pref);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with translations
        var loaded = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pref.PregnancyId, langCode, ct);
        var updated = loaded.First(p => p.Id == prefId);
        return MapToPreferenceDto(updated, langCode);
    }

    public async Task DeletePreferenceAsync(
        Guid prefId, Guid userId, CancellationToken ct = default)
    {
        var pref = await _unitOfWork.FoodPreferences.GetByIdAsync(prefId, cancellationToken: ct)
            ?? throw new NotFoundException("Food preference not found.");

        await VerifyPregnancyOwnership(pref.PregnancyId, userId, ct);

        await _unitOfWork.FoodPreferences.SoftDeleteAsync(pref, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══ Nutrition Notes ═══

    public async Task<List<NutritionNoteDto>> GetNotesAsync(
        Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var notes = await _unitOfWork.NutritionNotes
            .GetByPregnancyIdAsync(pregnancyId, ct);

        return notes.Select(MapToNoteDto).ToList();
    }

    public async Task<NutritionNoteDto> CreateNoteAsync(
        Guid pregnancyId, Guid userId, CreateNutritionNoteDto dto,
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var note = new PregnancyNutritionNote
        {
            PregnancyId = pregnancyId,
            NoteType = dto.NoteType,
            ValueText = dto.ValueText
        };

        await _unitOfWork.NutritionNotes.AddAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToNoteDto(note);
    }

    public async Task<NutritionNoteDto> UpdateNoteAsync(
        Guid noteId, Guid userId, UpdateNutritionNoteDto dto,
        CancellationToken ct = default)
    {
        var note = await _unitOfWork.NutritionNotes.GetByIdAsync(noteId, cancellationToken: ct)
            ?? throw new NotFoundException("Nutrition note not found.");

        await VerifyPregnancyOwnership(note.PregnancyId, userId, ct);

        if (dto.NoteType.HasValue) note.NoteType = dto.NoteType.Value;
        if (dto.ValueText != null) note.ValueText = dto.ValueText;

        _unitOfWork.NutritionNotes.Update(note);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToNoteDto(note);
    }

    public async Task DeleteNoteAsync(
        Guid noteId, Guid userId, CancellationToken ct = default)
    {
        var note = await _unitOfWork.NutritionNotes.GetByIdAsync(noteId, cancellationToken: ct)
            ?? throw new NotFoundException("Nutrition note not found.");

        await VerifyPregnancyOwnership(note.PregnancyId, userId, ct);

        await _unitOfWork.NutritionNotes.SoftDeleteAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══ Private Helpers ═══

    private async Task<Pregnancy> VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
        return pregnancy;
    }

    private static FoodPreferenceDto MapToPreferenceDto(
        PregnancyFoodPreference p, string langCode) => new(
        p.Id, p.PregnancyId, p.FoodItemId,
        p.FoodItem?.Code ?? "",
        p.FoodItem?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode)?.DisplayName
            ?? p.FoodItem?.Code ?? "",
        p.PreferenceType.ToString(),
        p.Severity?.ToString(),
        p.Notes,
        p.CreatedAt, p.UpdatedAt);

    private static NutritionNoteDto MapToNoteDto(PregnancyNutritionNote n) => new(
        n.Id, n.PregnancyId, n.NoteType.ToString(), n.ValueText,
        n.CreatedAt, n.UpdatedAt);
}
```

#### 3. `src/FPT.EXE201.Application/Services/RecipeService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class RecipeService : IRecipeService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RecipeDetailDto> GetByIdAsync(
        Guid recipeId, Guid userId, CancellationToken ct = default)
    {
        var recipe = await _unitOfWork.Recipes.GetByIdWithDetailsAsync(recipeId, ct)
            ?? throw new NotFoundException("Recipe not found.");

        // Verify ownership through pregnancy
        await VerifyPregnancyOwnership(recipe.PregnancyId, userId, ct);

        return new RecipeDetailDto(
            recipe.Id, recipe.PregnancyId, recipe.Title,
            recipe.Instructions, recipe.Servings,
            recipe.PrepMinutes, recipe.CookMinutes,
            recipe.CreatedAt);
    }

    private async Task VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
    }
}
```

#### 4. `src/FPT.EXE201.Application/Services/NutritionFeedbackService.cs`
```csharp
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class NutritionFeedbackService : INutritionFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;

    public NutritionFeedbackService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealPlanFeedbackDto> CreatePlanFeedbackAsync(
        Guid planId, Guid userId, CreateMealPlanFeedbackDto dto,
        CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        // Check unique: one feedback per user per plan
        var exists = await _unitOfWork.MealPlanFeedbacks
            .ExistsByPlanAndUserAsync(planId, userId, ct);
        if (exists)
            throw new ConflictException("You have already rated this meal plan.");

        var feedback = new MealPlanFeedback
        {
            MealPlanId = planId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        await _unitOfWork.MealPlanFeedbacks.AddAsync(feedback, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new MealPlanFeedbackDto(
            feedback.Id, feedback.MealPlanId, feedback.UserId,
            feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }

    public async Task<MealItemFeedbackDto> CreateItemFeedbackAsync(
        Guid itemId, Guid userId, CreateMealItemFeedbackDto dto,
        CancellationToken ct = default)
    {
        var item = await _unitOfWork.MealItems.GetByIdAsync(itemId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal item not found.");

        // Verify ownership through meal day → meal plan → pregnancy
        var day = await _unitOfWork.MealPlanDays
            .GetByIdAsync(item.MealDayId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan day not found.");

        var plan = await _unitOfWork.MealPlans.GetByIdAsync(day.MealPlanId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        // Check unique: one feedback per user per item
        var exists = await _unitOfWork.MealItemFeedbacks
            .ExistsByItemAndUserAsync(itemId, userId, ct);
        if (exists)
            throw new ConflictException("You have already rated this meal item.");

        var feedback = new MealItemFeedback
        {
            MealItemId = itemId,
            UserId = userId,
            Liked = dto.Liked,
            Comment = dto.Comment
        };

        await _unitOfWork.MealItemFeedbacks.AddAsync(feedback, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new MealItemFeedbackDto(
            feedback.Id, feedback.MealItemId, feedback.UserId,
            feedback.Liked, feedback.Comment, feedback.CreatedAt);
    }

    private async Task VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
    }
}
```

### ✅ Checkpoint — Prompt 8
- [ ] `RefDataService` extended with `GetActiveFoodItemsAsync` + `GetActiveNutrientsAsync`
- [ ] `FoodPreferenceService` created with 8 CRUD methods (4 prefs + 4 notes)
- [ ] `RecipeService` created with `GetByIdAsync`
- [ ] `NutritionFeedbackService` created with plan + item feedback
- [ ] `MealPlanListQuerySpec` created in `Features/MealPlans/`
- [ ] All services use `VerifyPregnancyOwnership` pattern
- [ ] All services throw proper exceptions (not return null)
- [ ] Project builds without errors

---

## 🎯 PROMPT 9/10 — MealPlanService (AI Generation Core)

### Task
Implement `MealPlanService` — service chính, xử lý AI meal plan generation.

### ⚠️ Key Business Rules
1. **Rate limit** (Decision #3): 15 AI calls/day per user. Mỗi week = 1 call. 4-week plan = 4 calls. Check trước khi bắt đầu.
2. **BMI fallback** (Decision #4): `bmiWeight = PrePregnancyWeightKg ?? currentWeight`. Throw nếu cả hai null.
3. **Overlap** (Decision #5): Auto soft-delete overlapping plans. KHÔNG throw `ConflictException`.
4. **Transaction**: Begin → Generate all weeks → Commit. Nếu bất kỳ week nào fail → Rollback toàn bộ.
5. **1 Gemini call per week**: Week 2+ includes previous week summary.
6. **Recipe REQUIRED**: Every meal item MUST have a recipe in AI output.
7. **Unknown nutrient code**: Skip + log warning — KHÔNG crash.
8. **max_output_tokens**: 8192 (Decision #7).

### ⚠️ Convention Reminders
- AI pipeline: `PromptBuilder.FromTemplate(template).WithContext("LABEL", data).WithUserMessage(msg).Build()` → `_aiProvider.GenerateAsync(prompt, ct)`
- `AiResponse` has: `Content`, `PromptTokens`, `CompletionTokens`, `TotalTokens`, `ModelUsed`, `ProcessingTime`
- JSON parsing: Dùng `System.Text.Json` + private record models
- Reuse `CleanAiJsonResponse` pattern từ `MedicalRecordAiService`

---

### File: `src/FPT.EXE201.Application/Services/MealPlanService.cs`

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiProvider _aiProvider;
    private readonly ILogger<MealPlanService> _logger;

    private const string TemplateKey = "nutrition.meal_plan";
    private const int DailyRateLimit = 15;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MealPlanService(
        IUnitOfWork unitOfWork,
        IAiProvider aiProvider,
        ILogger<MealPlanService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: Generate Meal Plan (AI Pipeline)
    // ═══════════════════════════════════════════════════

    public async Task<MealPlanDetailDto> GenerateAsync(
        Guid pregnancyId, Guid userId, GenerateMealPlanDto dto,
        CancellationToken ct = default)
    {
        // Step 1: Verify ownership
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Step 2: Validate duration
        if (dto.DurationWeeks < 1 || dto.DurationWeeks > 4)
            throw new BadRequestException("Duration must be between 1 and 4 weeks.");

        // Step 3: Rate limit check (Decision #3)
        var todayCount = await _unitOfWork.AiRequestLogs.CountTodayByUserAsync(userId, ct);
        var remaining = DailyRateLimit - todayCount;
        if (remaining < dto.DurationWeeks)
            throw new BadRequestException(
                $"Daily AI limit: need {dto.DurationWeeks} calls, remaining {remaining}/{DailyRateLimit}. Try again tomorrow.");

        // Step 4: Calculate BMI + target calories (Decision #4)
        var currentWeight = await GetCurrentWeight(pregnancyId, ct);
        var bmiWeight = pregnancy.PrePregnancyWeightKg ?? currentWeight;
        if (bmiWeight == null || pregnancy.HeightCm == null || pregnancy.HeightCm == 0)
            throw new BadRequestException(
                "Pre-pregnancy weight (or current weight) and height are required for calorie calculation.");

        var heightM = pregnancy.HeightCm.Value / 100m;
        var bmi = Math.Round(bmiWeight.Value / (heightM * heightM), 1);
        var gestWeek = pregnancy.CurrentGestationalWeek
                       ?? CalculateGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        var targetCalories = CalculateTargetCalories(bmi, gestWeek ?? 20);

        // Step 5: Handle overlap (Decision #5) — auto soft-delete
        var endDate = dto.StartDate.AddDays(dto.DurationWeeks * 7 - 1);
        var overlapping = await _unitOfWork.MealPlans
            .GetOverlappingAsync(pregnancyId, dto.StartDate, endDate, ct);
        foreach (var plan in overlapping)
        {
            await _unitOfWork.MealPlans.SoftDeleteAsync(plan, ct);
            _logger.LogInformation("Auto-deleted overlapping meal plan {PlanId}", plan.Id);
        }

        // Step 6: Collect nutrition context
        var foodPrefs = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pregnancyId, "vi", ct);
        var nutritionNotes = await _unitOfWork.NutritionNotes
            .GetByPregnancyIdAsync(pregnancyId, ct);
        var conditions = await _unitOfWork.PregnancyConditions
            .GetByPregnancyIdAsync(pregnancyId, "vi", ct);

        // Step 7: Load AI template + nutrient cache
        var template = await _unitOfWork.AiPromptTemplates
            .GetActiveByKeyAsync(TemplateKey, ct)
            ?? throw new NotFoundException($"AI prompt template '{TemplateKey}' not found.");

        var allNutrients = await _unitOfWork.RefNutrients
            .GetActiveWithTranslationsAsync("vi", ct);
        var nutrientMap = allNutrients.ToDictionary(n => n.Code, n => n.Id);

        // Step 8: Create MealPlan entity
        var mealPlan = new MealPlan
        {
            PregnancyId = pregnancyId,
            StartDate = dto.StartDate,
            EndDate = endDate,
            Source = MealPlanSource.AI,
            Notes = dto.AdditionalNotes
        };

        // Step 9: Transaction — Generate week by week
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _unitOfWork.MealPlans.AddAsync(mealPlan, ct);
            string? previousWeekSummary = null;
            int week = 0;

            for (week = 0; week < dto.DurationWeeks; week++)
            {
                var weekStart = dto.StartDate.AddDays(week * 7);
                var weekEnd = weekStart.AddDays(6);

                // Create AiRequestLog
                var aiLog = new AiRequestLog
                {
                    Feature = AiFeature.NutritionMealPlan,
                    PregnancyId = pregnancyId,
                    UserId = userId,
                    TemplateId = template.Id,
                    Status = AiRequestStatus.Processing
                };
                await _unitOfWork.AiRequestLogs.AddAsync(aiLog, ct);

                // Link first AI log to MealPlan
                if (week == 0) mealPlan.AiRequestLogId = aiLog.Id;

                // Build prompt
                var contextText = FormatNutritionContext(
                    pregnancy, foodPrefs, nutritionNotes, conditions,
                    currentWeight, bmi, gestWeek, targetCalories);
                var userMessage = BuildWeekPrompt(
                    week, weekStart, weekEnd, targetCalories,
                    previousWeekSummary, dto.AdditionalNotes);

                var prompt = PromptBuilder.FromTemplate(template)
                    .WithContext("NUTRITION PROFILE", contextText)
                    .WithUserMessage(userMessage)
                    .Build();

                _logger.LogInformation(
                    "Generating meal plan week {Week}/{Total} for pregnancy {Id}",
                    week + 1, dto.DurationWeeks, pregnancyId);

                // Call Gemini
                var aiResponse = await _aiProvider.GenerateAsync(prompt, ct);

                // Parse JSON response
                var weekPlan = ParseMealPlanResponse(aiResponse.Content);

                // Set plan title from first week
                if (week == 0 && !string.IsNullOrEmpty(weekPlan.Title))
                    mealPlan.Title = weekPlan.Title;

                // Create entities from parsed response
                CreateWeekEntities(mealPlan, weekPlan, weekStart, nutrientMap);

                // Update AiRequestLog → Succeeded
                aiLog.Status = AiRequestStatus.Succeeded;
                aiLog.Model = aiResponse.ModelUsed;
                aiLog.TokensInput = aiResponse.PromptTokens;
                aiLog.TokensOutput = aiResponse.CompletionTokens;
                aiLog.ProcessingTimeMs = (int)aiResponse.ProcessingTime.TotalMilliseconds;
                aiLog.ResponsePayload = aiResponse.Content;

                _logger.LogInformation(
                    "Week {Week} generated. Tokens: {In}+{Out}={Total}",
                    week + 1, aiResponse.PromptTokens,
                    aiResponse.CompletionTokens, aiResponse.TotalTokens);

                // Build summary for next week
                previousWeekSummary = BuildWeekSummary(weekPlan);
            }

            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Meal plan {PlanId} generated successfully ({Weeks} weeks)",
                mealPlan.Id, dto.DurationWeeks);

            // Return full detail
            return await GetDetailAsync(mealPlan.Id, userId, ct);
        }
        catch (Exception ex)
        {
            // Log failed AiRequestLog BEFORE rollback (so we don't lose debug info)
            _logger.LogError(ex,
                "Meal plan generation failed for pregnancy {Id}. " +
                "Completed {CompletedWeeks}/{TotalWeeks} weeks before failure.",
                pregnancyId, week, dto.DurationWeeks);

            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: List / Detail / Delete / Day Detail
    // ═══════════════════════════════════════════════════

    public async Task<PagedResult<MealPlanSummaryDto>> ListAsync(
        Guid pregnancyId, Guid userId, QueryOptions options,
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var paged = await _unitOfWork.MealPlans
            .GetByPregnancyIdPagedAsync(pregnancyId, options, ct);

        var dtos = paged.Items.Select(m => new MealPlanSummaryDto(
            m.Id, m.PregnancyId, m.StartDate, m.EndDate,
            m.Source.ToString(), m.Title,
            m.Days?.Count ?? 0, m.CreatedAt
        )).ToList();

        return new PagedResult<MealPlanSummaryDto>(
            dtos, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<MealPlanDetailDto> GetDetailAsync(
        Guid planId, Guid userId, CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdWithDetailsAsync(planId, ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        return MapToDetailDto(plan);
    }

    public async Task DeleteAsync(
        Guid planId, Guid userId, CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        await _unitOfWork.MealPlans.SoftDeleteAsync(plan, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<MealDayDetailDto> GetDayDetailAsync(
        Guid planId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        var day = await _unitOfWork.MealPlanDays
            .GetByPlanIdAndDateAsync(planId, date, ct)
            ?? throw new NotFoundException(
                $"No meal plan data for date {date:yyyy-MM-dd}.");

        return MapToDayDetailDto(day, langCode);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Calorie Calculation (IOM Guidelines)
    // ═══════════════════════════════════════════════════

    private async Task<decimal?> GetCurrentWeight(
        Guid pregnancyId, CancellationToken ct)
    {
        var latestLog = await _unitOfWork.WeightLogs
            .GetLatestByPregnancyIdAsync(pregnancyId, ct);
        return latestLog?.WeightKg;
    }

    private static int? CalculateGestationalWeek(DateOnly? lmpDate)
    {
        if (!lmpDate.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDays = today.DayNumber - lmpDate.Value.DayNumber;
        return totalDays >= 0 && totalDays <= 315 ? totalDays / 7 : null;
    }

    /// <summary>
    /// IOM-based calorie target: base from BMI category + trimester bonus.
    /// </summary>
    private static int CalculateTargetCalories(decimal bmi, int gestationalWeek)
    {
        var baseCalories = bmi switch
        {
            < 18.5m => 2400,       // Underweight
            < 25.0m => 2200,       // Normal
            < 30.0m => 2000,       // Overweight
            _       => 1800        // Obese
        };

        var trimesterBonus = gestationalWeek switch
        {
            <= 12 => 0,            // T1
            <= 27 => 340,          // T2
            _     => 450           // T3
        };

        return baseCalories + trimesterBonus;
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: AI Prompt Building
    // ═══════════════════════════════════════════════════

    private static string FormatNutritionContext(
        Pregnancy pregnancy,
        List<PregnancyFoodPreference> foodPrefs,
        List<PregnancyNutritionNote> nutritionNotes,
        List<PregnancyCondition> conditions,
        decimal? currentWeight, decimal bmi,
        int? gestationalWeek, int targetCalories)
    {
        var parts = new List<string>();

        if (gestationalWeek.HasValue)
            parts.Add($"Tuần thai: {gestationalWeek}");
        parts.Add($"BMI: {bmi:F1}");
        if (currentWeight.HasValue)
            parts.Add($"Cân nặng hiện tại: {currentWeight:F1} kg");
        parts.Add($"Calories mục tiêu: {targetCalories} kcal/ngày");

        var allergies = foodPrefs
            .Where(p => p.PreferenceType == FoodPreferenceType.Allergy).ToList();
        if (allergies.Any())
        {
            var names = allergies.Select(a =>
                a.FoodItem?.Translations?.FirstOrDefault()?.DisplayName
                ?? a.FoodItem?.Code ?? "N/A");
            parts.Add("Dị ứng: " + string.Join(", ", names));
        }

        var dislikes = foodPrefs
            .Where(p => p.PreferenceType == FoodPreferenceType.Dislike).ToList();
        if (dislikes.Any())
        {
            var names = dislikes.Select(d =>
                d.FoodItem?.Translations?.FirstOrDefault()?.DisplayName
                ?? d.FoodItem?.Code ?? "N/A");
            parts.Add("Không thích: " + string.Join(", ", names));
        }

        if (nutritionNotes.Any())
        {
            parts.Add("Ghi chú dinh dưỡng:");
            foreach (var note in nutritionNotes)
                parts.Add($"  - [{note.NoteType}] {note.ValueText}");
        }

        var conditionNames = conditions
            .Select(c => c.Condition?.Translations?.FirstOrDefault()?.DisplayName
                         ?? c.Condition?.Code ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();
        if (conditionNames.Any())
            parts.Add("Bệnh lý: " + string.Join(", ", conditionNames));

        return parts.Any() ? string.Join("\n", parts) : "Không có thông tin đặc biệt.";
    }

    private static string BuildWeekPrompt(
        int weekIndex, DateOnly weekStart, DateOnly weekEnd,
        int targetCalories, string? previousWeekSummary,
        string? additionalNotes)
    {
        var sb = new System.Text.StringBuilder();

        if (weekIndex == 0)
        {
            sb.AppendLine($"Tạo thực đơn 7 ngày từ {weekStart:yyyy-MM-dd} đến {weekEnd:yyyy-MM-dd}.");
        }
        else
        {
            sb.AppendLine($"Tiếp tục thực đơn tuần {weekIndex + 1}, từ {weekStart:yyyy-MM-dd} đến {weekEnd:yyyy-MM-dd}.");
            if (!string.IsNullOrEmpty(previousWeekSummary))
            {
                sb.AppendLine();
                sb.AppendLine("Tóm tắt tuần trước:");
                sb.AppendLine(previousWeekSummary);
                sb.AppendLine("Đảm bảo đa dạng, không lặp lại món ăn tuần trước.");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Mục tiêu: ~{targetCalories} kcal/ngày.");
        sb.AppendLine("Mỗi ngày cần 4 bữa: BREAKFAST, LUNCH, DINNER, SNACK.");
        sb.AppendLine("Mỗi món PHẢI có recipe đầy đủ (title, instructions, servings, prepMinutes, cookMinutes).");

        if (!string.IsNullOrEmpty(additionalNotes))
        {
            sb.AppendLine();
            sb.AppendLine($"Yêu cầu thêm từ người dùng: {additionalNotes}");
        }

        return sb.ToString();
    }

    private static string BuildWeekSummary(AiWeekResponse weekPlan)
    {
        if (weekPlan.Days == null || !weekPlan.Days.Any())
            return "Tuần trước không có dữ liệu.";

        var dishes = weekPlan.Days
            .SelectMany(d => d.Meals ?? Enumerable.Empty<AiMealResponse>())
            .Select(m => m.ItemName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .Take(15);

        return "Các món đã có: " + string.Join(", ", dishes);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: JSON Parsing + Entity Creation
    // ═══════════════════════════════════════════════════

    private AiWeekResponse ParseMealPlanResponse(string content)
    {
        var cleaned = CleanAiJsonResponse(content);
        cleaned = RepairTruncatedJson(cleaned);

        try
        {
            var parsed = JsonSerializer.Deserialize<AiWeekResponse>(cleaned, JsonOptions);
            if (parsed?.Days == null || !parsed.Days.Any())
                throw new BadRequestException("AI returned empty meal plan.");
            return parsed;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to parse AI meal plan response. First 300 chars: {Preview}",
                cleaned.Length > 300 ? cleaned[..300] : cleaned);
            throw new BadRequestException(
                "AI returned invalid meal plan format. Please try again.");
        }
    }

    private void CreateWeekEntities(
        MealPlan mealPlan,
        AiWeekResponse weekPlan,
        DateOnly weekStart,
        Dictionary<string, Guid> nutrientMap)
    {
        for (int dayIndex = 0; dayIndex < weekPlan.Days.Count; dayIndex++)
        {
            var dayResponse = weekPlan.Days[dayIndex];

            // Parse date from AI response, fallback to sequential
            DateOnly planDate;
            if (DateOnly.TryParse(dayResponse.Date, out var parsed))
                planDate = parsed;
            else
                planDate = weekStart.AddDays(dayIndex);

            var planDay = new MealPlanDay
            {
                MealPlanId = mealPlan.Id,
                PlanDate = planDate
            };
            mealPlan.Days.Add(planDay);

            if (dayResponse.Meals == null) continue;

            foreach (var mealResponse in dayResponse.Meals)
            {
                // Create Recipe (REQUIRED by business rule)
                Recipe? recipe = null;
                if (mealResponse.Recipe != null)
                {
                    recipe = new Recipe
                    {
                        PregnancyId = mealPlan.PregnancyId,
                        Title = mealResponse.Recipe.Title ?? mealResponse.ItemName ?? "Untitled",
                        Instructions = mealResponse.Recipe.Instructions,
                        Servings = mealResponse.Recipe.Servings,
                        PrepMinutes = mealResponse.Recipe.PrepMinutes,
                        CookMinutes = mealResponse.Recipe.CookMinutes
                    };
                }

                // Parse MealType
                if (!Enum.TryParse<MealType>(mealResponse.MealType, true, out var mealType))
                    mealType = MealType.Snack; // Fallback

                var mealItem = new MealItem
                {
                    MealDayId = planDay.Id,
                    MealType = mealType,
                    RecipeId = recipe?.Id,
                    ItemName = mealResponse.ItemName,
                    PortionText = mealResponse.PortionText,
                    CaloriesKcal = mealResponse.CaloriesKcal,
                    Notes = mealResponse.Notes,
                    Recipe = recipe
                };
                planDay.Items.Add(mealItem);

                // Create MealItemNutrients
                if (mealResponse.Nutrients != null)
                {
                    foreach (var nutrientResponse in mealResponse.Nutrients)
                    {
                        if (!nutrientMap.TryGetValue(nutrientResponse.Code, out var nutrientId))
                        {
                            _logger.LogWarning(
                                "Unknown nutrient code '{Code}' — skipping",
                                nutrientResponse.Code);
                            continue;
                        }

                        mealItem.Nutrients.Add(new MealItemNutrient
                        {
                            NutrientId = nutrientId,
                            Amount = nutrientResponse.Amount
                        });
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: JSON Cleanup (reuse pattern from MedicalRecordAiService)
    // ═══════════════════════════════════════════════════

    private static string CleanAiJsonResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        var cleaned = content.Trim();

        // Remove markdown code fences
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > 0)
                cleaned = cleaned[(firstNewline + 1)..];
            if (cleaned.EndsWith("```"))
                cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();
        }

        // Find JSON start
        var jsonStart = Math.Min(
            cleaned.IndexOf('{') is var ib && ib >= 0 ? ib : int.MaxValue,
            cleaned.IndexOf('[') is var ia && ia >= 0 ? ia : int.MaxValue);
        if (jsonStart > 0 && jsonStart < int.MaxValue)
            cleaned = cleaned[jsonStart..];

        // Find JSON end
        var jsonEnd = Math.Max(cleaned.LastIndexOf('}'), cleaned.LastIndexOf(']'));
        if (jsonEnd > 0 && jsonEnd < cleaned.Length - 1)
            cleaned = cleaned[..(jsonEnd + 1)];

        return cleaned;
    }

    /// <summary>
    /// Repair truncated JSON from AI response (max_output_tokens may cut off).
    /// Counts unbalanced braces/brackets and appends missing closers.
    /// Reuse pattern from MedicalRecordAiService.RepairTruncatedJson.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        var openBraces = 0;
        var openBrackets = 0;
        var inString = false;
        var escaped = false;

        foreach (var c in json)
        {
            if (escaped) { escaped = false; continue; }
            if (c == '\\') { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            switch (c)
            {
                case '{': openBraces++; break;
                case '}': openBraces--; break;
                case '[': openBrackets++; break;
                case ']': openBrackets--; break;
            }
        }

        if (openBraces == 0 && openBrackets == 0)
            return json;

        // Strip trailing incomplete entry (partial key-value after last comma)
        var repaired = json.TrimEnd();
        if (repaired.Length > 0)
        {
            var lastValid = repaired.LastIndexOfAny(['}', ']', '"']);
            if (lastValid > 0)
            {
                var afterLast = repaired[(lastValid + 1)..].Trim();
                if (afterLast.Length > 0 && afterLast[0] == ',')
                    repaired = repaired[..(lastValid + 1)];
            }
        }

        // Append missing closers
        for (int i = 0; i < openBrackets; i++) repaired += "]";
        for (int i = 0; i < openBraces; i++) repaired += "}";

        return repaired;
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Ownership + Mapping
    // ═══════════════════════════════════════════════════

    private async Task<Pregnancy> VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
        return pregnancy;
    }

    private static MealPlanDetailDto MapToDetailDto(MealPlan plan) => new(
        plan.Id, plan.PregnancyId, plan.StartDate, plan.EndDate,
        plan.Source.ToString(), plan.Title, plan.Notes,
        plan.Days.OrderBy(d => d.PlanDate).Select(d => new MealPlanDaySummaryDto(
            d.Id, d.PlanDate,
            d.Items.Sum(i => i.CaloriesKcal ?? 0),
            d.Items.Count
        )).ToList(),
        plan.CreatedAt, plan.UpdatedAt);

    private static MealDayDetailDto MapToDayDetailDto(MealPlanDay day, string langCode = "vi") => new(
        day.Id, day.MealPlanId, day.PlanDate,
        day.Items.Sum(i => i.CaloriesKcal ?? 0),
        day.Items.OrderBy(i => i.MealType).Select(i => new MealItemDto(
            i.Id, i.MealType.ToString(), i.RecipeId,
            i.ItemName, i.PortionText, i.CaloriesKcal, i.Notes,
            i.Nutrients.Select(n => new MealItemNutrientDto(
                n.Nutrient.Code,
                n.Nutrient.Translations.FirstOrDefault(t => t.LanguageCode == langCode)?.DisplayName
                    ?? n.Nutrient.Code,
                n.Nutrient.Unit,
                n.Amount
            )).ToList()
        )).ToList());

    // ═══════════════════════════════════════════════════
    // PRIVATE: AI Response JSON Models
    // ═══════════════════════════════════════════════════

    private record AiWeekResponse(
        string? Title,
        int? TotalDailyCalories,
        string? Notes,
        List<AiDayResponse> Days);

    private record AiDayResponse(
        string Date,
        List<AiMealResponse> Meals);

    private record AiMealResponse(
        string MealType,
        string ItemName,
        string? PortionText,
        int? CaloriesKcal,
        string? Notes,
        AiRecipeResponse? Recipe,
        List<AiNutrientResponse>? Nutrients);

    private record AiRecipeResponse(
        string Title,
        string? Instructions,
        int? Servings,
        int? PrepMinutes,
        int? CookMinutes);

    private record AiNutrientResponse(
        string Code,
        decimal Amount);
}
```

### ✅ Checkpoint — Prompt 9
- [ ] `MealPlanService` created with 5 public methods
- [ ] `GenerateAsync` implements full AI pipeline with transaction
- [ ] Rate limit check before generation (Decision #3)
- [ ] BMI fallback logic (Decision #4)
- [ ] Auto-soft-delete overlapping plans (Decision #5)
- [ ] 1 Gemini call per week, week 2+ includes previous summary
- [ ] Unknown nutrient codes are skipped with warning (not crash)
- [ ] AiRequestLog created per week-chunk; error logged BEFORE rollback
- [ ] `CleanAiJsonResponse` strips markdown fences from AI output
- [ ] `RepairTruncatedJson` fixes truncated JSON from max_output_tokens cutoff
- [ ] `MapToDayDetailDto` filters translations by `langCode`
- [ ] `CreateWeekEntities` uses for loop (NOT IndexOf) for sequential day indexing
- [ ] Private JSON records match AI template OutputSchema
- [ ] Project builds without errors

---

## 🎯 PROMPT 10/10 — Controllers + DI Registration + Final Checklist

### Task
Tạo/extend **5 controllers** + update **DI registration** + **QuerySpecRegistry** + **RefDataController enums**.

### ⚠️ Convention Reminders
- `[Route("api")]`, `[Authorize]`, extends `BaseApiController`
- `[RequirePermission("module.action")]` trên mỗi endpoint
- Constructor inject service. KHÔNG inject multiple services (1 controller → 1 primary service, trừ `RefDataController` và `MealPlansController` — xem note trong code)
- KHÔNG try-catch — `GlobalExceptionFilter` xử lý
- Return `Success(data)`, `Created(data, msg)`, hoặc `Success<object?>(null, msg)`

---

### Files:

#### 1. Update `src/FPT.EXE201.Api/Controllers/RefDataController.cs`

**1a. Thêm 2 endpoints** (sau endpoint `GetDocumentTypes`):

```csharp
    /// <summary>
    /// Lấy danh mục thực phẩm cho UI chọn dị ứng/không thích.
    /// </summary>
    [HttpGet("food-items")]
    public async Task<IActionResult> GetFoodItems(
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveFoodItemsAsync(lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Lấy danh mục dưỡng chất (PROTEIN, IRON, CALCIUM...).
    /// </summary>
    [HttpGet("nutrients")]
    public async Task<IActionResult> GetNutrients(
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveNutrientsAsync(lang, ct);
        return Success(result);
    }
```

**1b. Thêm enums vào `GetEnums()`** (thêm vào dictionary):

```csharp
            // Week 7 — Nutrition
            ["foodPreferenceType"] = ToEnumList<FoodPreferenceType>(),
            ["allergySeverity"] = ToEnumList<AllergySeverity>(),
            ["mealType"] = ToEnumList<MealType>(),
            ["mealPlanSource"] = ToEnumList<MealPlanSource>(),
            ["nutritionNoteType"] = ToEnumList<NutritionNoteType>(),
            ["aiFeature"] = ToEnumList<AiFeature>(),
            ["aiRequestStatus"] = ToEnumList<AiRequestStatus>(),
```

> ⚠️ Cần thêm `using FPT.EXE201.Domain.Enums;` nếu chưa có (các enum mới).
> ⚠️ Phải thêm vào CẢ HAI dictionaries — `GetEnums()` và `GetEnumByName()`.

#### 2. `src/FPT.EXE201.Api/Controllers/FoodPreferencesController.cs`
```csharp
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
[Authorize]
public class FoodPreferencesController : BaseApiController
{
    private readonly IFoodPreferenceService _service;

    public FoodPreferencesController(IFoodPreferenceService service)
    {
        _service = service;
    }

    // ═══ Food Preferences ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/food-preferences")]
    [RequirePermission("food_preference.read")]
    public async Task<IActionResult> GetPreferences(
        Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.GetPreferencesAsync(
            pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPost("pregnancies/{pregnancyId:guid}/food-preferences")]
    [RequirePermission("food_preference.write")]
    public async Task<IActionResult> CreatePreference(
        Guid pregnancyId, [FromBody] CreateFoodPreferenceDto dto,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.CreatePreferenceAsync(
            pregnancyId, GetCurrentUserId(), dto, lang, ct);
        return Created(result, "Food preference created successfully");
    }

    [HttpPut("pregnancies/{pregnancyId:guid}/food-preferences/{prefId:guid}")]
    [RequirePermission("food_preference.write")]
    public async Task<IActionResult> UpdatePreference(
        Guid pregnancyId, Guid prefId,
        [FromBody] UpdateFoodPreferenceDto dto,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.UpdatePreferenceAsync(prefId, GetCurrentUserId(), dto, lang, ct);
        return Success(result, "Food preference updated successfully");
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/food-preferences/{prefId:guid}")]
    [RequirePermission("food_preference.delete")]
    public async Task<IActionResult> DeletePreference(
        Guid pregnancyId, Guid prefId, CancellationToken ct)
    {
        await _service.DeletePreferenceAsync(prefId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Food preference deleted successfully");
    }

    // ═══ Nutrition Notes ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/nutrition-notes")]
    [RequirePermission("nutrition_note.read")]
    public async Task<IActionResult> GetNotes(
        Guid pregnancyId, CancellationToken ct)
    {
        var result = await _service.GetNotesAsync(
            pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPost("pregnancies/{pregnancyId:guid}/nutrition-notes")]
    [RequirePermission("nutrition_note.write")]
    public async Task<IActionResult> CreateNote(
        Guid pregnancyId, [FromBody] CreateNutritionNoteDto dto, CancellationToken ct)
    {
        var result = await _service.CreateNoteAsync(
            pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Nutrition note created successfully");
    }

    [HttpPut("pregnancies/{pregnancyId:guid}/nutrition-notes/{noteId:guid}")]
    [RequirePermission("nutrition_note.write")]
    public async Task<IActionResult> UpdateNote(
        Guid pregnancyId, Guid noteId,
        [FromBody] UpdateNutritionNoteDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateNoteAsync(noteId, GetCurrentUserId(), dto, ct);
        return Success(result, "Nutrition note updated successfully");
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/nutrition-notes/{noteId:guid}")]
    [RequirePermission("nutrition_note.delete")]
    public async Task<IActionResult> DeleteNote(
        Guid pregnancyId, Guid noteId, CancellationToken ct)
    {
        await _service.DeleteNoteAsync(noteId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Nutrition note deleted successfully");
    }
}
```

#### 3. `src/FPT.EXE201.Api/Controllers/MealPlansController.cs`
```csharp
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

/// ⚠️ Exception: Controller này inject 2 services (MealPlanService + FeedbackService)
/// vì plan feedback là sub-resource của meal plan, tách controller riêng không hợp lý.
[Route("api")]
[Authorize]
public class MealPlansController : BaseApiController
{
    private readonly IMealPlanService _mealPlanService;
    private readonly INutritionFeedbackService _feedbackService;

    public MealPlansController(
        IMealPlanService mealPlanService,
        INutritionFeedbackService feedbackService)
    {
        _mealPlanService = mealPlanService;
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Generate AI meal plan (1-4 weeks). Rate limited: 15 calls/day.
    /// </summary>
    [HttpPost("pregnancies/{pregnancyId:guid}/meal-plans/generate")]
    [RequirePermission("meal_plan.generate")]
    public async Task<IActionResult> Generate(
        Guid pregnancyId, [FromBody] GenerateMealPlanDto dto, CancellationToken ct)
    {
        var result = await _mealPlanService.GenerateAsync(
            pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Meal plan generated successfully");
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/meal-plans")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> List(
        Guid pregnancyId, [FromQuery] QueryOptions options, CancellationToken ct)
    {
        var result = await _mealPlanService.ListAsync(
            pregnancyId, GetCurrentUserId(), options, ct);
        return Success(result);
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/meal-plans/{planId:guid}")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetDetail(
        Guid pregnancyId, Guid planId, CancellationToken ct)
    {
        var result = await _mealPlanService.GetDetailAsync(
            planId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/meal-plans/{planId:guid}")]
    [RequirePermission("meal_plan.delete")]
    public async Task<IActionResult> Delete(
        Guid pregnancyId, Guid planId, CancellationToken ct)
    {
        await _mealPlanService.DeleteAsync(planId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Meal plan deleted successfully");
    }

    /// <summary>
    /// Get meal plan detail for a specific date.
    /// </summary>
    [HttpGet("meal-plans/{planId:guid}/days/{date}")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetDayDetail(
        Guid planId, DateOnly date,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _mealPlanService.GetDayDetailAsync(
            planId, date, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Rate overall meal plan (1-5 stars). One feedback per user per plan.
    /// </summary>
    [HttpPost("meal-plans/{planId:guid}/feedback")]
    [RequirePermission("meal_plan_feedback.write")]
    public async Task<IActionResult> CreatePlanFeedback(
        Guid planId, [FromBody] CreateMealPlanFeedbackDto dto, CancellationToken ct)
    {
        var result = await _feedbackService.CreatePlanFeedbackAsync(
            planId, GetCurrentUserId(), dto, ct);
        return Created(result, "Feedback submitted successfully");
    }
}
```

#### 4. `src/FPT.EXE201.Api/Controllers/RecipesController.cs`
```csharp
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/recipes")]
[Authorize]
public class RecipesController : BaseApiController
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet("{recipeId:guid}")]
    [RequirePermission("recipe.read")]
    public async Task<IActionResult> GetById(Guid recipeId, CancellationToken ct)
    {
        var result = await _recipeService.GetByIdAsync(
            recipeId, GetCurrentUserId(), ct);
        return Success(result);
    }
}
```

#### 5. `src/FPT.EXE201.Api/Controllers/MealItemsController.cs`
```csharp
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/meal-items")]
[Authorize]
public class MealItemsController : BaseApiController
{
    private readonly INutritionFeedbackService _feedbackService;

    public MealItemsController(INutritionFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Like/dislike a meal item. One feedback per user per item.
    /// </summary>
    [HttpPost("{itemId:guid}/feedback")]
    [RequirePermission("meal_item_feedback.write")]
    public async Task<IActionResult> CreateItemFeedback(
        Guid itemId, [FromBody] CreateMealItemFeedbackDto dto, CancellationToken ct)
    {
        var result = await _feedbackService.CreateItemFeedbackAsync(
            itemId, GetCurrentUserId(), dto, ct);
        return Created(result, "Feedback submitted successfully");
    }
}
```

---

### DI Registration

#### Update `src/FPT.EXE201.Application/DependencyInjection.cs`

Thêm vào sau Week 6 section:

```csharp
        // Week 7 — Nutrition + Meal Planning
        services.AddScoped<IFoodPreferenceService, FoodPreferenceService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<INutritionFeedbackService, NutritionFeedbackService>();
```

> ⚠️ `IRefDataService` đã registered (`AddScoped<IRefDataService, RefDataService>()`) — chỉ extend implementation, KHÔNG thêm registration mới.

---

### QuerySpecRegistry Update

Update `src/FPT.EXE201.Application/Common/Querying/QuerySpecRegistry.cs`:

```csharp
// Thêm vào dictionary:
["mealPlans"] = MealPlanListQuerySpec.Metadata,
```

> ⚠️ Cần thêm `using FPT.EXE201.Application.Features.MealPlans;`.

---

### ✅ Checkpoint — Prompt 10
- [ ] `RefDataController` extended with `food-items` + `nutrients` endpoints
- [ ] `RefDataController` enums dictionary updated with 7 new enums (BOTH `GetEnums()` AND `GetEnumByName()`)
- [ ] `FoodPreferencesController` created with 8 endpoints (Create/Update pass `lang` query param)
- [ ] `MealPlansController` created with 6 endpoints (plan feedback + `GetDayDetail` with `lang` param)
- [ ] `RecipesController` created with 1 endpoint
- [ ] `MealItemsController` created with 1 endpoint (item feedback)
- [ ] All endpoints have `[RequirePermission]` attributes
- [ ] All endpoints use `GetCurrentUserId()`
- [ ] `Application/DependencyInjection.cs` updated with 4 new services
- [ ] `QuerySpecRegistry` updated with `mealPlans` entry
- [ ] No try-catch in any controller
- [ ] Project builds without errors

---

## 📌 Full Summary — Week 7 Prompts (All 10)

| Prompt | Layer | Files Created/Modified |
|--------|-------|----------------------|
| 1/10 | Context | — (reference only) |
| 2/10 | Domain | 7 enums + 14 entities + 1 modification = **22 files** |
| 3/10 | Infrastructure | 14 EF configurations = **14 files** |
| 4/10 | Infrastructure | AppDbContext + 2 seeders + 1 template seed + permissions + migration = **3 new + 3 modified** |
| 5/10 | Application | 17 DTOs + 7 validators = **24 files** |
| 6/10 | Application + Infra | 11 repo interfaces + 11 repo impls + 2 UoW updates = **22 new + 2 modified** |
| 7/10 | Application | 4 new service interfaces + 1 modified = **4 new + 1 modified** |
| 8/10 | Application | 4 service impls + 1 QuerySpec = **4 new + 1 modified** |
| 9/10 | Application | MealPlanService = **1 file** |
| 10/10 | API + Application | 4 controllers + 1 extended + 2 DI updates = **4 new + 3 modified** |
| **Total** | | **~94 new files + ~11 modifications** |

---

## 🎯 Final Verification Checklist

### Architecture
- [ ] Clean Architecture respected: Domain ← Application → Infrastructure → API
- [ ] No direct DbContext access in Application layer
- [ ] All repos accessed through UnitOfWork

### Domain
- [ ] 7 new enums in `Domain/Enums/`
- [ ] 14 new entities in `Domain/Entities/`
- [ ] RefNutrient is custom entity (no BaseEntity) — Decision #2
- [ ] MealPlanDay, MealItem, MealPlanFeedback, MealItemFeedback inherit BaseEntity — Decision #1
- [ ] Pregnancy.cs has 5 new navigation properties

### Infrastructure
- [ ] 14 EF configurations in `Infrastructure/Configurations/`
- [ ] All configs: `CHAR(36)`, `HasConversion<string>()`, `Ignore(IsDeleted)`
- [ ] AppDbContext has 14 new DbSets + RefNutrient timestamp handling
- [ ] 2 seeders (food items + nutrients) + AI template seed updated
- [ ] Migration created and applied

### Application
- [ ] 17 DTOs (all `record`) + 7 validators
- [ ] `GenerateMealPlanDtoValidator` uses `DateTime.UtcNow` (NOT `DateTime.Today`)
- [ ] 11 repository interfaces + 11 implementations
- [ ] IUnitOfWork + UnitOfWork updated with Week 7 repos
- [ ] 5 service interfaces (1 extended + 4 new)
- [ ] 5 service implementations (1 extended + 4 new)
- [ ] `FoodPreferenceService` Create/Update accept `langCode` parameter (not hardcoded)
- [ ] MealPlanService: rate limit, BMI fallback, overlap delete, transaction, AI pipeline
- [ ] MealPlanService: `RepairTruncatedJson` handles max_output_tokens cutoff
- [ ] MealPlanService: `CreateWeekEntities` uses for loop (not IndexOf)
- [ ] MealPlanService: error logged BEFORE transaction rollback
- [ ] MealPlanService: `MapToDayDetailDto` filters translations by `langCode`
- [ ] MealPlanListQuerySpec registered in QuerySpecRegistry

### API
- [ ] RefDataController extended (2 endpoints + 7 enums)
- [ ] 4 new controllers: FoodPreferences, MealPlans, Recipes, MealItems
- [ ] `MealPlansController` injects 2 services (documented exception)
- [ ] All 17 endpoints have `[RequirePermission]`
- [ ] DependencyInjection.cs updated with 4 service registrations
- [ ] 12 permissions seeded in `DatabaseSeeder.cs` + assigned to USER/DOCTOR roles

### Business Rules
- [ ] Rate limit: 15 AI calls/day checked before generation — Decision #3
- [ ] BMI fallback: `PrePregnancyWeightKg ?? currentWeight` — Decision #4
- [ ] Overlap: auto soft-delete, no ConflictException — Decision #5
- [ ] max_output_tokens: 8192 in AI template (model: `gemini-2.5-flash`) — Decision #7
- [ ] meal_items.notes: included in AI output + parse logic — Decision #8
- [ ] Unknown nutrient codes: skip + log warning
- [ ] Multi-week failure: error logged before transaction rollback
- [ ] Truncated JSON: `RepairTruncatedJson` fixes max_output_tokens cutoff
- [ ] 1 Gemini call per week, week 2+ includes previous summary

---

## 🎯 END OF WEEK 7 PROMPTS GUIDE
