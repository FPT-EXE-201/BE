# Development Workflow Guide — FPT EXE 201

> **Mục đích**: Tổng hợp tất cả conventions, patterns và workflow chuẩn để AI code đúng ngay lần đầu.  
> **Cập nhật**: 2026-02-15 · Đồng bộ với codebase qua Week 5  
> **Xem thêm**: `DATABASE_SCHEMA.sql` (DDL), `WEEK_*_PROMPTS_GUIDE.md` (chi tiết từng week)

---

## 1. PROJECT OVERVIEW

| Layer | Project | Vai trò |
|-------|---------|---------|
| **API** | `FPT.EXE201.Api` | Controllers, Filters, Middlewares, Program.cs |
| **Application** | `FPT.EXE201.Application` | DTOs (record), Services, IRepositories, Validations, Exceptions, Authorization |
| **Domain** | `FPT.EXE201.Domain` | Entities, Enums, Common (BaseEntity) — KHÔNG dependency |
| **Infrastructure** | `FPT.EXE201.Infrastructure` | EF Core (AppDbContext, Configurations, Migrations), Repositories, UnitOfWork, External Services |

```
API → Application → Domain ← Infrastructure
```

- **API** depends on Application only
- **Application** depends on Domain only (NO Infrastructure)
- **Infrastructure** implements Application interfaces, depends on Domain
- **Domain** has ZERO dependencies

---

## 2. CONVENTIONS (⚠️ BẮT BUỘC)

### 2.1 Database — MySQL 8 / Pomelo EF Core

| Convention | Giá trị | Ghi chú |
|-----------|---------|---------|
| Guid storage | `CHAR(36)` | KHÔNG dùng BINARY(16) |
| Naming | `snake_case` | Table: `pregnancies`, Column: `lmp_date` |
| Charset | `utf8mb4` | Global `HasCharSet("utf8mb4")` trong AppDbContext |
| Enum storage | `VARCHAR` + `HasConversion<string>()` | KHÔNG dùng int |
| Soft delete | `deleted_at` DATETIME(6) NULL | Global query filter `DeletedAt == null` |
| Index naming | `idx_{table}_{columns}` | Unique: `uk_{table}_{columns}` |
| FK naming | `fk_{table}_{ref_table}` | — |
| Timestamps | `DATETIME(6)` | Microsecond precision |

### 2.2 BaseEntity

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt != null; // computed, KHÔNG map DB
}
```

- `CreatedAt` / `UpdatedAt` — set tự động bởi `AppDbContext.SaveChangesAsync`
- **KHÔNG** có `CreatedBy` / `UpdatedBy`
- Translation entities (composite PK) **KHÔNG** kế thừa BaseEntity
- Join tables (N:N) **KHÔNG** kế thừa BaseEntity, chỉ có `created_at`

### 2.3 Entity Property Naming

C# property dùng **tên rõ nghĩa**, DB column dùng **tên ngắn**. Mapping qua `.HasColumnName()`:

```
C# Property               → DB Column
──────────────────────────────────────
LastMenstrualPeriodDate    → lmp_date
ExpectedDeliveryDate       → edd_date
CurrentGestationalWeek     → current_week
PregnancyNumber            → pregnancy_no
VisitDateTime              → visit_at
TestDate                   → test_date
IsAbnormalResult           → abnormal_flag
LanguageCode               → lang_code
DisplayName                → name
```

### 2.4 DTOs — Record Pattern

```csharp
// ✅ ĐÚNG — dùng record với primary constructor
public record CreatePregnancyDto(
    DateOnly? LastMenstrualPeriodDate,
    string? Notes,
    BabyGender BabyGender = BabyGender.Unknown,
    PregnancyType PregnancyType = PregnancyType.Singleton
);

// ❌ SAI — không dùng class DTO
public class CreatePregnancyDto { public string? Notes { get; set; } }
```

### 2.5 Enum Convention

```csharp
// ✅ ĐÚNG — enum đơn giản, KHÔNG gắn int value
public enum PregnancyStatus
{
    Active,
    Ended,
    Miscarriage,
    Delivered
}

// ❌ SAI — không dùng int backing
public enum PregnancyStatus { Active = 1, Ended = 2 }
```

EF Config:

```csharp
builder.Property(p => p.Status)
    .IsRequired().HasColumnName("status")
    .HasConversion<string>().HasMaxLength(20);
```

### 2.6 JSON Columns

`VitalsJson`, `ResultJson`, `StructuredJson`, `MetadataJson` — dùng `JSON` column type để lưu dữ liệu linh hoạt:

```csharp
// Entity
public string? VitalsJson { get; set; }

// EF Config
builder.Property(v => v.VitalsJson)
    .HasColumnName("vitals_json").HasColumnType("JSON");
```

---

## 3. CODING PATTERNS (Trích từ codebase thật)

### 3.1 EF Configuration

```csharp
public class PregnancyConfiguration : IEntityTypeConfiguration<Pregnancy>
{
    public void Configure(EntityTypeBuilder<Pregnancy> builder)
    {
        builder.ToTable("pregnancies");

        // CHAR(36) — KHÔNG dùng BINARY(16)
        builder.Property(p => p.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(p => p.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");

        // Enum → string
        builder.Property(p => p.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);

        // Tên rõ nghĩa → column ngắn
        builder.Property(p => p.LastMenstrualPeriodDate)
            .HasColumnName("lmp_date").HasColumnType("DATE");

        // Timestamps
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        // LUÔN ignore computed property
        builder.Ignore(p => p.IsDeleted);

        // Indexes
        builder.HasIndex(p => new { p.UserId, p.PregnancyNumber })
            .IsUnique().HasDatabaseName("uk_pregnancies_user_no");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany().HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Conditions)
            .WithOne(c => c.Pregnancy).HasForeignKey(c => c.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**⚠️ Configurations auto-applied**: `AppDbContext.OnModelCreating` gọi `ApplyConfigurationsFromAssembly` — chỉ cần đặt file đúng namespace.

### 3.2 Repository Interface

```csharp
public interface IPregnancyRepository : IGenericRepository<Pregnancy>
{
    Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Pregnancy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

### 3.3 Repository Implementation

```csharp
public class PregnancyRepository : GenericRepository<Pregnancy>, IPregnancyRepository
{
    public PregnancyRepository(AppDbContext context) : base(context) { }

    public async Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.Status == PregnancyStatus.Active && p.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var maxNo = await _dbSet
            .IgnoreQueryFilters()  // Include deleted to avoid collision
            .Where(p => p.UserId == userId)
            .MaxAsync(p => (int?)p.PregnancyNumber, cancellationToken);
        return (maxNo ?? 0) + 1;
    }
}
```

### 3.4 UnitOfWork — Lazy `??=` Pattern

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IPregnancyRepository? _pregnancies;

    public UnitOfWork(AppDbContext context) { _context = context; }

    // Lazy initialization — repository chỉ tạo khi được dùng
    public IPregnancyRepository Pregnancies => _pregnancies ??= new PregnancyRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
```

### 3.5 Service — Exception-based

```csharp
public class PregnancyService : IPregnancyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PregnancyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PregnancyDto> CreateAsync(Guid userId, CreatePregnancyDto dto, CancellationToken ct = default)
    {
        // Business rule: 1 active pregnancy per user
        var existing = await _unitOfWork.Pregnancies.GetActiveByUserIdAsync(userId, ct);
        if (existing != null)
            throw new ConflictException("You already have an active pregnancy.");

        var nextNo = await _unitOfWork.Pregnancies.GetNextPregnancyNumberAsync(userId, ct);

        var pregnancy = new Pregnancy
        {
            UserId = userId,
            PregnancyNumber = nextNo,
            Status = PregnancyStatus.Active,
            LastMenstrualPeriodDate = dto.LastMenstrualPeriodDate,
            // ... map remaining fields
        };

        await _unitOfWork.Pregnancies.AddAsync(pregnancy, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(pregnancy);
    }
}
```

**Exceptions có sẵn**: `NotFoundException` (404), `BadRequestException` (400), `ConflictException` (409), `ForbiddenException` (403), `UnauthorizedException` (401)

### 3.6 Controller — NO try-catch

```csharp
[Route("api/pregnancies")]
[Authorize]
public class PregnanciesController : BaseApiController
{
    private readonly IPregnancyService _pregnancyService;

    public PregnanciesController(IPregnancyService pregnancyService)
    {
        _pregnancyService = pregnancyService;
    }

    [HttpPost]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> Create([FromBody] CreatePregnancyDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.CreateAsync(GetCurrentUserId(), dto, ct);
        return Created(result, "Pregnancy created successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("pregnancy.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _pregnancyService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Pregnancy deleted successfully");
    }
}
```

**BaseApiController helpers**: `Success<T>(data, message)` → 200, `Created<T>(data, message)` → 201, `GetCurrentUserId()` → Guid

### 3.7 DI Registration

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);
    services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
    services.AddScoped<IPregnancyService, PregnancyService>();
    return services;
}
```

### 3.8 Seed Data — Anonymous Type + Fixed DateTime

```csharp
public static class PregnancyConditionSeeder
{
    private static readonly DateTime SeedDate = new(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        var id = new Guid("...");
        // ✅ Anonymous type
        builder.Entity<RefPregnancyCondition>().HasData(new
        {
            Id = id,
            Code = "GESTATIONAL_DIABETES",
            IsActive = true,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate
        });
        // ❌ KHÔNG dùng entity instance, KHÔNG dùng DateTime.UtcNow
    }
}
```

---

## 4. DEVELOPMENT STEPS — Implement Feature Mới

Thứ tự bắt buộc khi implement 1 module/feature mới:

```
Step 1:  Domain     → Entities + Enums
Step 2:  Infra      → EF Configuration files
Step 3:  Infra      → AppDbContext — thêm DbSet<>
Step 4:  Infra      → Migration + Seed Data (nếu có ref tables)
Step 5:  App        → DTOs (record) + FluentValidation
Step 6:  App        → IRepository interface (kế thừa IGenericRepository<T>)
Step 7:  Infra      → Repository implementation
Step 8:  App        → IUnitOfWork — thêm property
Step 9:  Infra      → UnitOfWork — thêm lazy field + property
Step 10: App        → IService interface
Step 11: App        → Service implementation (business logic, throw exceptions)
Step 12: Infra      → AutoMapper Profile
Step 13: App        → DependencyInjection.cs — register service
Step 14: API        → Controller (kế thừa BaseApiController)
Step 15: Test       → Swagger testing
```

**Migration commands** (từ `src/FPT.EXE201.Api`):

```bash
dotnet ef migrations add YourMigrationName --project ../FPT.EXE201.Infrastructure --startup-project .
dotnet ef database update --project ../FPT.EXE201.Infrastructure --startup-project .
```

---

## 5. AUTHORIZATION PATTERNS

### 5.1 Permission-based (RBAC)

```csharp
[RequirePermission("pregnancy.write")]
public async Task<IActionResult> Create(...) { }
```

Permissions nạp vào JWT claims khi login → `PermissionAuthorizationHandler` check từ claims, KHÔNG query DB mỗi request.

### 5.2 Ownership Check (trong Service)

```csharp
var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(id, ct);
if (pregnancy == null)
    throw new NotFoundException("Pregnancy not found");
if (pregnancy.UserId != currentUserId)
    throw new ForbiddenException("Access denied");
```

### 5.3 Permission Naming Convention

`{module}.{action}` hoặc `{module}.{action}.{scope}`

```
pregnancy.read      pregnancy.write      pregnancy.delete
prenatal_visit.read  prenatal_visit.write
document.create      document.view        document.update
document.delete      document.favorite    ocr.trigger        ocr.view
```

---

## 6. ROADMAP

> Chi tiết implementation xem `WEEK_*_PROMPTS_GUIDE.md`. Schema DDL xem `DATABASE_SCHEMA.sql`.

| Week | Module | Status | Tables | Prompt Guide |
|------|--------|--------|--------|-------------|
| 1-2 | Auth + RBAC + Audit | ✅ Done | `users`, `user_profiles`, `roles`, `permissions`, `role_permissions`, `user_roles`, `auth_refresh_tokens`, `audit_events`, `languages` | — |
| 3 | Pregnancy Core | ✅ Done | `pregnancies`, `pregnancy_conditions`, `prenatal_visits`, `prenatal_tests`, `ref_pregnancy_conditions` + translations, `ref_test_types` + translations | `WEEK_3_PROMPTS_GUIDE.md` |
| 4 | Storage + Medical Documents + OCR Stub | ✅ Done | `storage_files`, `ref_document_types` + translations, `medical_documents`, `ocr_results` | `WEEK_4_PROMPTS_GUIDE.md` |
| 5 | Third-party: Supabase Storage + Azure OCR + Gemini AI | ⬜ | `ai_prompt_templates`, `ai_request_logs` + ALTER `ocr_results` | `WEEK_5_PROMPTS_GUIDE.md` |
| 5.5 | Auto-Fill: Review & Confirm AI Extraction → Visit/Test | ⬜ | ALTER `ocr_results` (4 confirm fields) | `WEEK_5.5_PROMPTS_GUIDE.md` |
| 6 | Weight Tracking + Motivational | ⬜ | `weight_logs`, `weight_goal_ranges`, `weight_alerts`, `motivational_templates` + translations | — |
| 7 | Nutrition + Meal Planning | ⬜ | `ref_food_items` + translations, `ref_nutrients` + translations, `pregnancy_food_preferences`, `recipes`, `meal_plans`, `meal_plan_days`, `meal_items`, `meal_item_nutrients`, `meal_plan_feedback`, `meal_item_feedback` | — |
| 8+ | Doctor Profiles + Chat + Consult + Call | ⬜ | `doctor_profiles`, `ref_specialties`, `doctor_specialties`, `doctor_availability_*`, `consult_requests`, `chat_conversations`, `chat_participants`, `chat_messages`, `chat_message_attachments`, `chat_read_receipts`, `call_sessions` | — |
| Future | Reminders | ⬜ | `reminder_rules`, `reminder_events` | — |

### Key Architecture Decisions

| Decision | Lý do |
|----------|-------|
| **Có `document_files` junction table** | MedicalDocument → DocumentFile(s) → StorageFile (hỗ trợ multi-file upload) |
| **Không có `extracted_medical_fields`** | Thay bằng `VitalsJson` + `ResultJson` + `StructuredJson` (flexible) |
| **`ai_request_logs` thay `nutrition_ai_requests`** | Generalized cho tất cả AI features (OCR, Nutrition, Chat) |
| **StubFileStorageService (W4) → SupabaseStorageService (W5)** | Tách metadata logic khỏi real storage |
| **Permissions in JWT claims** | Không query DB mỗi request (Approach 2) |

---

## 7. CHECKLIST — Trước khi hoàn thành Feature

- [ ] Entity có `builder.Ignore(p => p.IsDeleted)` trong EF Config
- [ ] Guid properties dùng `.HasColumnType("CHAR(36)")` — KHÔNG dùng BINARY(16)
- [ ] Enum dùng `.HasConversion<string>()` — KHÔNG dùng int
- [ ] DTOs là `record` — KHÔNG dùng `class`
- [ ] Service throw exceptions — Controller KHÔNG có try-catch
- [ ] IUnitOfWork + UnitOfWork đã thêm repository property (lazy `??=`)
- [ ] DependencyInjection.cs đã register service
- [ ] DbContext có `DbSet<>` cho entity mới
- [ ] Seed data dùng anonymous type + fixed DateTime
- [ ] Migration chạy thành công (`dotnet ef database update`)
- [ ] FluentValidation cho tất cả Create/Update DTOs
- [ ] Permission đã seed + gán cho roles phù hợp
- [ ] Test Swagger thành công (CRUD + error cases)
