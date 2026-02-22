# WEEK 6 PROMPTS GUIDE — Weight Tracking + Motivational

> ⚠️ **Database Convention**: Project sử dụng **CHAR(36)** để lưu Guid, KHÔNG dùng BINARY(16).  
> ⚠️ **Enum Convention**: Dùng `HasConversion<string>()` — enum serialize thành string.  
> ⚠️ **DTO Convention**: Dùng `record` — KHÔNG dùng `class`.  
> ⚠️ **Exception Handling**: Services throw exceptions (`NotFoundException`, `BadRequestException`, `ConflictException`...), `GlobalExceptionFilter` xử lý thành `ApiResponse`.  
> ⚠️ **RBAC**: Dùng `[RequirePermission("permission.code")]` trong Controller, KHÔNG try-catch.  
> ⚠️ **Soft Delete**: Dùng `deleted_at` timestamp + global query filter trong `AppDbContext.OnModelCreating`. KHÔNG hard delete.  
> ⚠️ **Seed Data**: Dùng anonymous type + fixed DateTime, KHÔNG dùng entity instance.  
> ⚠️ **LangCode**: Lowercase match Week 1 (`"vi"`, `"en"`).  
> ⚠️ **Column Mapping**: C# property dùng tên rõ nghĩa, DB column dùng tên ngắn. Mapping qua `.HasColumnName()`.  
> ⚠️ **Controller**: Kế thừa `BaseApiController`, dùng `Success()`, `Created()`, `GetCurrentUserId()`. KHÔNG tự viết `ApiResponse`.  
> ⚠️ **Repository**: Kế thừa `GenericRepository<T>`, lazy init trong `UnitOfWork` qua `??=` pattern.  
> ⚠️ **DbContext**: Dùng `AppDbContext`, KHÔNG phải `ApplicationDbContext`. Configurations auto-apply qua `ApplyConfigurationsFromAssembly`.  
> ⚠️ **Paging**: Follow `SEARCH_SORT_PAGING_GUIDE.md` — dùng `QueryOptions`, `PagedResult<T>`, `SearchHelper`, `SortHelper`, `QuerySpecRegistry`.

---

## 📋 CONTEXT

### Week 6 Overview

**Mục tiêu**: Implement Weight Tracking + Motivational Templates cho ứng dụng theo dõi thai kỳ (MomCare).

**Tính năng chính**:
- ✅ Daily weight logging (1 entry/day/pregnancy)
- ✅ Weight source: Manual (tự nhập) hoặc OCR (chụp ảnh cân → BE OCR trích xuất)
- ✅ **OCR Weight Extraction**: Chụp ảnh cân → upload → BE dùng `IOcrProvider` (Azure Document Intelligence) trích xuất text → regex parse số cân → trả về cho FE confirm → nếu confirm thì tạo WeightLog (Source=OCR), nếu không thì chụp lại
- ✅ Weight goal ranges based on IOM guidelines + BMI
- ✅ Auto-alert khi cân nặng ngoài khoảng khuyến nghị
- ✅ Motivational templates: baby size comparison, tips, milestones — theo tuần thai (public API, không cần login)
- ✅ Weight chart data cho FE (series: ngày → cân nặng)
- ✅ Search/Sort/Paging cho weight logs

**Prerequisite (đã implement)**:
- Week 3: `Pregnancy` entity (có `pre_pregnancy_weight_kg`, `height_cm`, `current_week`)
- Week 1-2: Auth + RBAC (permissions trong JWT claims)
- Week 1: `languages` table (seeded `"vi"`, `"en"`)
- Week 4-5: `IOcrProvider` interface + `AzureOcrProvider` implementation (Azure Document Intelligence) — đã registered trong DI via `AddHttpClient<IOcrProvider, AzureOcrProvider>()` (Infrastructure layer)
- Search/Sort/Paging infrastructure (GenericRepository.GetPagedAsync, SearchHelper, SortHelper)

> ⚠️ **IOcrProvider Injection Note**: `IOcrProvider` registered trong Infrastructure DI qua `AddHttpClient<>()`. Service ở Application layer KHÔNG inject trực tiếp `IOcrProvider` — phải tạo `IWeightOcrService` interface (Application) + `WeightOcrService` implementation (Infrastructure) để giữ Clean Architecture layer boundary.

**Database Tables** (4 tables + 1 translation):
1. `weight_logs` — Daily weight entries per pregnancy
2. `weight_goal_ranges` — BMI-based recommended weight gain (1 per pregnancy)
3. `weight_alerts` — Auto-generated alerts khi gain quá nhanh/chậm
4. `motivational_templates` — Baby size comparisons, tips, milestones per gestational week
5. `motivational_template_translations` — i18n cho motivational content

**Foreign Key Links**:
- `weight_logs.pregnancy_id` → `pregnancies.id` (CASCADE DELETE)
- `weight_goal_ranges.pregnancy_id` → `pregnancies.id` (CASCADE DELETE)
- `weight_alerts.pregnancy_id` → `pregnancies.id` (CASCADE DELETE)
- `motivational_template_translations.template_id` → `motivational_templates.id` (CASCADE DELETE)
- `motivational_template_translations.language_code` → `languages.code`

**Property Naming Rule**:
```
C# Property (rõ nghĩa)          │ DB Column (ngắn gọn)
─────────────────────────────────┼──────────────────────
LoggedOn                         │ logged_on
WeightKg                         │ weight_kg
PrePregnancyWeightKg             │ pre_pregnancy_weight_kg
HeightCm                         │ height_cm
Bmi                              │ bmi
RecommendedTotalGainMin          │ recommended_total_gain_min
RecommendedTotalGainMax          │ recommended_total_gain_max
AlertType                        │ alert_type
TriggeredAt                      │ triggered_at
DetailsJson                      │ details_json
ResolvedAt                       │ resolved_at
WeekStart                        │ week_start
WeekEnd                          │ week_end
VariablesJson                    │ variables_json
LanguageCode                     │ language_code
DisplayName (Title)              │ title
```

**API Endpoints**:
```
# Weight Logs (CRUD + Chart + OCR)
POST   /api/pregnancies/{id}/weight-logs               → Record weight (Manual or after OCR confirm)
POST   /api/pregnancies/{id}/weight-logs/extract-weight → OCR: upload ảnh cân → trích xuất số kg → trả về cho FE confirm
GET    /api/pregnancies/{id}/weight-logs                → List + paging/search/sort
GET    /api/pregnancies/{id}/weight-logs/chart          → Chart data (date → weight series)
PUT    /api/weight-logs/{id}                            → Update entry
DELETE /api/weight-logs/{id}                            → Soft delete

# Weight Goals (1 per pregnancy)
POST   /api/pregnancies/{id}/weight-goals          → Set goal range (auto-calculate BMI)
GET    /api/pregnancies/{id}/weight-goals          → Get current goals
PUT    /api/weight-goals/{id}                      → Update goals

# Weight Alerts
GET    /api/pregnancies/{id}/weight-alerts         → List alerts
PUT    /api/weight-alerts/{id}/resolve             → Resolve alert

# Motivational Templates (public API, không cần login — giống RefDataController)
GET    /api/motivational?week=28&lang=vi           → Get messages for current week
GET    /api/motivational?category=BabySize&lang=vi → Filter by category

# Reference Data
GET    /api/ref/enums                               → All enums (includes weightSource, weightAlertType, motivationalCategory)
GET    /api/ref/enums/{enumName}                     → Specific enum (e.g., /api/ref/enums/weightSource)
```

**Business Rules**:
- ⚠️ **Unique Constraint**: 1 weight log per day per pregnancy: `uk_weight_logs_pregnancy_date (pregnancy_id, logged_on)`
- ⚠️ **Weight Range**: `DECIMAL(5,2)`, CHECK > 0 AND < 500
- ⚠️ **1 Goal per Pregnancy**: `uk_weight_goals_pregnancy (pregnancy_id)`
- ⚠️ **BMI Auto-calculate**: `bmi = weight_kg / (height_cm / 100)²`
- ⚠️ **IOM Weight Gain Guidelines** (based on pre-pregnancy BMI):
  - Underweight (BMI < 18.5): gain 12.5 – 18.0 kg
  - Normal (BMI 18.5–24.9): gain 11.5 – 16.0 kg
  - Overweight (BMI 25.0–29.9): gain 7.0 – 11.5 kg
  - Obese (BMI ≥ 30.0): gain 5.0 – 9.0 kg
- ⚠️ **Alert Types**: `RapidGain` (>0.7 kg/week), `RapidLoss` (lost weight 2+ consecutive logs), `AboveRange`, `BelowRange` (EF HasConversion<string> → PascalCase)
- ⚠️ **LoggedOn date**: Dùng `DateOnly`, KHÔNG được là future date (phải <= today)
- ⚠️ **Ownership**: User chỉ CRUD own pregnancy's weight data (check `pregnancy.UserId`)
- ⚠️ **Motivational Templates**: Seed data (admin-managed), composite PK translations, filter by week range
- ⚠️ **WeightAlerts**: Không có `deleted_at` (intentional) — immutable audit log
- ⚠️ **Source**: `Manual` (user tự nhập thủ công), `OCR` (user chụp ảnh cân → BE OCR trích xuất cân nặng)
- ⚠️ **OCR Weight Extraction** (tái sử dụng `IOcrProvider` đã có từ Week 4-5):
  - Chỉ chấp nhận image: `.jpg`, `.jpeg`, `.png` (max 5 MB)
  - Gọi `IOcrProvider.ExtractTextAsync()` → lấy raw text → regex parse tìm số cân nặng
  - Regex 4 tầng: (1) `\d{2,3}\.?\d{0,2}\s*kg`, (2) `weight|cân nặng` label, (3) decimal number, (4) integer standalone
  - Validate range: 30–200 kg (ngoài range → trả về null)
  - Trả về `WeightOcrExtractResultDto` cho FE hiển thị → user confirm
  - Nếu confirm → FE gọi `POST /weight-logs` với `Source: WeightSource.OCR` + `WeightKg` đã extracted
  - Nếu không confirm → user chụp lại ảnh
- ⚠️ **Clean Architecture**: `IOcrProvider` registered trong Infrastructure → tạo `IWeightOcrService` (Application) + `WeightOcrService` (Infrastructure) wrapper

**Development Workflow**:
1. Prompt 1: Domain entities + Enums (WeightLog, WeightGoalRange, WeightAlert, MotivationalTemplate, MotivationalTemplateTranslation)
2. Prompt 2: EF Core Configurations (5 entities)
3. Prompt 3: Migration + Seed Data (motivational templates + permissions)
4. Prompt 4: DTOs (record) + FluentValidation
5. Prompt 5: Repository Interfaces + Implementations + UnitOfWork
6. Prompt 6: QuerySpec (Search/Sort/Paging cho WeightLog)
7. Prompt 7: Service Interfaces + Implementations (WeightLogService, MotivationalService)
8. Prompt 8: Controllers + DI Registration

---

## 🎯 PROMPT 1/8 — Domain Entities + Enums

**Nhiệm vụ**: Tạo 5 Domain entities + 3 Enums cho Weight Tracking + Motivational module.

**Reference SQL Schema** (trích từ `DATABASE_SCHEMA.sql` Section 7):

```sql
CREATE TABLE weight_logs (
    id              CHAR(36)       NOT NULL,
    pregnancy_id    CHAR(36)       NOT NULL,
    logged_on       DATE           NOT NULL,
    weight_kg       DECIMAL(5,2)   NOT NULL,
    note            VARCHAR(255)   NULL,
    source          VARCHAR(20)    NOT NULL DEFAULT 'Manual',  -- Manual | OCR

    created_at      DATETIME(6)    NOT NULL,
    updated_at      DATETIME(6)    NOT NULL,
    deleted_at      DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_weight_logs_pregnancy_date (pregnancy_id, logged_on),
    CONSTRAINT fk_weight_logs_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT chk_weight_kg CHECK (weight_kg > 0 AND weight_kg < 500)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE weight_goal_ranges (
    id                          CHAR(36)       NOT NULL,
    pregnancy_id                CHAR(36)       NOT NULL,
    height_cm                   DECIMAL(5,2)   NULL,
    pre_pregnancy_weight_kg     DECIMAL(5,2)   NULL,
    bmi                         DECIMAL(5,2)   NULL,
    recommended_total_gain_min  DECIMAL(5,2)   NULL,
    recommended_total_gain_max  DECIMAL(5,2)   NULL,
    notes                       VARCHAR(500)   NULL,

    created_at                  DATETIME(6)    NOT NULL,
    updated_at                  DATETIME(6)    NOT NULL,
    deleted_at                  DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_weight_goals_pregnancy (pregnancy_id),
    CONSTRAINT fk_weight_goals_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE weight_alerts (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    alert_type      VARCHAR(64)   NOT NULL,       -- RapidGain, RapidLoss, AboveRange, BelowRange (EF HasConversion<string>)
    triggered_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    details_json    JSON          NULL,            -- { "currentWeight": 70, "expectedRange": [65,68] }
    resolved_at     DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_weight_alerts_pregnancy (pregnancy_id, triggered_at),
    INDEX idx_weight_alerts_type (alert_type, triggered_at),
    CONSTRAINT fk_weight_alerts_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE motivational_templates (
    id               CHAR(36)      NOT NULL,
    category         VARCHAR(30)   NOT NULL DEFAULT 'BabySize',   -- BabySize | Milestone | Tip (EF HasConversion<string>)
    week_start       INT           NOT NULL,
    week_end         INT           NOT NULL,
    is_active        TINYINT(1)    NOT NULL DEFAULT 1,
    variables_json   JSON          NULL,

    created_at       DATETIME(6)   NOT NULL,
    updated_at       DATETIME(6)   NOT NULL,
    deleted_at       DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_motivational_week (week_start, week_end, is_active),
    CONSTRAINT chk_motivational_week CHECK (week_start >= 0 AND week_end >= week_start AND week_end <= 45)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE motivational_template_translations (
    template_id   CHAR(36)      NOT NULL,
    language_code VARCHAR(10)   NOT NULL,
    title         VARCHAR(120)  NULL,
    message       VARCHAR(500)  NOT NULL,

    PRIMARY KEY (template_id, language_code),
    CONSTRAINT fk_motivational_tr_template FOREIGN KEY (template_id) REFERENCES motivational_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_motivational_tr_lang     FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**Code — Enums**:

```csharp
// File: FPT.EXE201.Domain/Enums/WeightSource.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Nguồn ghi nhận cân nặng.
/// </summary>
public enum WeightSource
{
    /// <summary>User tự nhập thủ công.</summary>
    Manual,

    /// <summary>User chụp ảnh cân nặng → BE OCR trích xuất giá trị cân.</summary>
    OCR
}

// File: FPT.EXE201.Domain/Enums/WeightAlertType.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Loại cảnh báo cân nặng.
/// </summary>
public enum WeightAlertType
{
    /// <summary>Tăng cân quá nhanh (>0.7 kg/week).</summary>
    RapidGain,

    /// <summary>Giảm cân liên tiếp (2+ lần ghi nhận giảm).</summary>
    RapidLoss,

    /// <summary>Tổng tăng cân vượt mức khuyến nghị tối đa.</summary>
    AboveRange,

    /// <summary>Tổng tăng cân dưới mức khuyến nghị tối thiểu.</summary>
    BelowRange
}

// File: FPT.EXE201.Domain/Enums/MotivationalCategory.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Danh mục nội dung động viên.
/// </summary>
public enum MotivationalCategory
{
    /// <summary>So sánh kích thước bé với trái cây/vật thể quen thuộc.</summary>
    BabySize,

    /// <summary>Cột mốc phát triển (bé biết đạp, mở mắt...).</summary>
    Milestone,

    /// <summary>Mẹo sức khỏe, dinh dưỡng, tâm lý cho mẹ.</summary>
    Tip
}
```

**Code — Entities**:

```csharp
// File: FPT.EXE201.Domain/Entities/WeightLog.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Bản ghi cân nặng hàng ngày của thai phụ.
/// Mỗi thai kỳ chỉ cho phép 1 entry/ngày (uk_weight_logs_pregnancy_date).
/// Dùng để vẽ biểu đồ tăng cân, phát hiện bất thường, đưa khuyến nghị.
/// </summary>
public class WeightLog : BaseEntity
{
    /// <summary>FK → Pregnancy. CASCADE DELETE khi xóa thai kỳ.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Ngày ghi nhận cân nặng. Dùng DateOnly — 1 entry/day.</summary>
    public DateOnly LoggedOn { get; set; }

    /// <summary>Cân nặng (kg). DECIMAL(5,2), range: 0.01–499.99.</summary>
    public decimal WeightKg { get; set; }

    /// <summary>Ghi chú tùy chọn, max 255 chars.</summary>
    public string? Note { get; set; }

    /// <summary>Nguồn dữ liệu: Manual (user tự nhập) hoặc OCR (chụp ảnh cân → BE OCR).</summary>
    public WeightSource Source { get; set; } = WeightSource.Manual;

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Thai kỳ sở hữu bản ghi này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/WeightGoalRange.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Mục tiêu tăng cân cho thai kỳ — dựa trên IOM guidelines.
/// 1 record per pregnancy (unique key uk_weight_goals_pregnancy).
/// Auto-calculate BMI từ height + pre-pregnancy weight.
/// </summary>
public class WeightGoalRange : BaseEntity
{
    /// <summary>FK → Pregnancy (unique).</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Chiều cao mẹ (cm). Copy từ Pregnancy.HeightCm hoặc user nhập lại.</summary>
    public decimal? HeightCm { get; set; }

    /// <summary>Cân nặng trước mang thai (kg). Copy từ Pregnancy.PrePregnancyWeightKg.</summary>
    public decimal? PrePregnancyWeightKg { get; set; }

    /// <summary>BMI trước mang thai. Auto-calculated: weight / (height/100)².</summary>
    public decimal? Bmi { get; set; }

    /// <summary>Mức tăng cân tối thiểu khuyến nghị (kg) — theo IOM guidelines.</summary>
    public decimal? RecommendedTotalGainMin { get; set; }

    /// <summary>Mức tăng cân tối đa khuyến nghị (kg) — theo IOM guidelines.</summary>
    public decimal? RecommendedTotalGainMax { get; set; }

    /// <summary>Ghi chú (bác sĩ tư vấn, ghi nhận đặc biệt).</summary>
    public string? Notes { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public Pregnancy Pregnancy { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/WeightAlert.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Cảnh báo cân nặng — tự động phát sinh khi phát hiện bất thường.
/// ⚠️ KHÔNG kế thừa BaseEntity — không soft delete.
/// WeightAlert là audit log, immutable (chỉ thêm ResolvedAt).
/// </summary>
public class WeightAlert
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → Pregnancy. CASCADE DELETE.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Loại cảnh báo. EF HasConversion<string>() → lưu dạng string trong DB.</summary>
    public WeightAlertType AlertType { get; set; }

    /// <summary>Thời điểm cảnh báo được tạo.</summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Chi tiết JSON: { currentWeight, expectedRange, weeklyGain... }.</summary>
    public string? DetailsJson { get; set; }

    /// <summary>Thời điểm cảnh báo được xử lý/giải quyết. NULL = chưa resolve.</summary>
    public DateTime? ResolvedAt { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public Pregnancy Pregnancy { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/MotivationalTemplate.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Template nội dung động viên cho mẹ bầu — theo tuần thai.
/// 3 categories: BabySize (so sánh kích thước bé), Milestone (cột mốc), Tip (mẹo hay).
/// Admin quản lý nội dung. User nhận nội dung phù hợp với tuần thai hiện tại.
/// </summary>
public class MotivationalTemplate : BaseEntity
{
    /// <summary>Danh mục: BabySize, Milestone, Tip — lưu dạng string.</summary>
    public MotivationalCategory Category { get; set; } = MotivationalCategory.BabySize;

    /// <summary>Tuần thai bắt đầu áp dụng (inclusive, 0-45).</summary>
    public int WeekStart { get; set; }

    /// <summary>Tuần thai kết thúc áp dụng (inclusive, 0-45, >= WeekStart).</summary>
    public int WeekEnd { get; set; }

    /// <summary>Template có đang active không.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Variables JSON cho template (keys mà FE có thể thay thế).</summary>
    public string? VariablesJson { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Nội dung đa ngôn ngữ.</summary>
    public ICollection<MotivationalTemplateTranslation> Translations { get; set; }
        = new List<MotivationalTemplateTranslation>();
}

// File: FPT.EXE201.Domain/Entities/MotivationalTemplateTranslation.cs
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Nội dung đa ngôn ngữ cho MotivationalTemplate.
/// Composite PK: (TemplateId, LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity (composite key entity).
/// </summary>
public class MotivationalTemplateTranslation
{
    /// <summary>FK → MotivationalTemplate.Id.</summary>
    public Guid TemplateId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tiêu đề ngắn (optional). Ví dụ: "Bé to bằng quả xoài!".</summary>
    public string? Title { get; set; }

    /// <summary>Nội dung chi tiết. Ví dụ: "Tuần thứ 28, bé nặng khoảng 1kg...".</summary>
    public string Message { get; set; } = string.Empty;

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public MotivationalTemplate Template { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
```

**✅ Checkpoint**:
- Build thành công
- 5 entities: WeightLog, WeightGoalRange, WeightAlert (NO BaseEntity), MotivationalTemplate, MotivationalTemplateTranslation (NO BaseEntity)
- 3 enums: WeightSource, WeightAlertType, MotivationalCategory
- WeightAlert: NO `deleted_at` — immutable audit log
- MotivationalTemplateTranslation: composite PK — NO BaseEntity
- WeightLog dùng `DateOnly LoggedOn` (giống PrenatalVisit pattern)
- Pregnancy navigation từ WeightLog/WeightGoalRange/WeightAlert

---

## 🎯 PROMPT 2/8 — EF Core Configurations

**Nhiệm vụ**: Tạo EF Configurations cho 5 entities. Thêm `DbSet<>` vào `AppDbContext`.

**⚠️ Conventions**:
- Guid → `CHAR(36)` (KHÔNG dùng BINARY(16))
- DateTime → `DATETIME(6)`
- DateOnly → `DATE`
- Enum → `.HasConversion<string>()` + `.HasMaxLength()`
- Decimal → `DECIMAL(5,2)`
- `builder.Ignore(x => x.IsDeleted)` cho BaseEntity entities
- WeightAlert: NO global query filter (không có `deleted_at`)
- MotivationalTemplateTranslation: composite PK
- Index/Unique naming convention: `idx_{table}_{columns}`, `uk_{table}_{columns}`

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Configurations/WeightLogConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightLogConfiguration : IEntityTypeConfiguration<WeightLog>
{
    public void Configure(EntityTypeBuilder<WeightLog> builder)
    {
        builder.ToTable("weight_logs");

        // Primary Key
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        // Properties
        builder.Property(w => w.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(w => w.LoggedOn)
            .IsRequired().HasColumnName("logged_on").HasColumnType("DATE");

        builder.Property(w => w.WeightKg)
            .IsRequired().HasColumnName("weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(w => w.Note)
            .HasColumnName("note").HasMaxLength(255);

        builder.Property(w => w.Source)
            .IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);

        // BaseEntity timestamps
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(w => w.IsDeleted);

        // Unique: 1 log per day per pregnancy
        builder.HasIndex(w => new { w.PregnancyId, w.LoggedOn })
            .IsUnique().HasDatabaseName("uk_weight_logs_pregnancy_date");

        // Relationships
        builder.HasOne(w => w.Pregnancy)
            .WithMany().HasForeignKey(w => w.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/WeightGoalRangeConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightGoalRangeConfiguration : IEntityTypeConfiguration<WeightGoalRange>
{
    public void Configure(EntityTypeBuilder<WeightGoalRange> builder)
    {
        builder.ToTable("weight_goal_ranges");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(g => g.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(g => g.HeightCm)
            .HasColumnName("height_cm").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.PrePregnancyWeightKg)
            .HasColumnName("pre_pregnancy_weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.Bmi)
            .HasColumnName("bmi").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.RecommendedTotalGainMin)
            .HasColumnName("recommended_total_gain_min").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.RecommendedTotalGainMax)
            .HasColumnName("recommended_total_gain_max").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.Notes)
            .HasColumnName("notes").HasMaxLength(500);

        // BaseEntity
        builder.Property(g => g.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(g => g.IsDeleted);

        // Unique: 1 goal per pregnancy
        builder.HasIndex(g => g.PregnancyId)
            .IsUnique().HasDatabaseName("uk_weight_goals_pregnancy");

        // Relationship
        builder.HasOne(g => g.Pregnancy)
            .WithMany().HasForeignKey(g => g.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/WeightAlertConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightAlertConfiguration : IEntityTypeConfiguration<WeightAlert>
{
    public void Configure(EntityTypeBuilder<WeightAlert> builder)
    {
        builder.ToTable("weight_alerts");

        // ⚠️ WeightAlert KHÔNG kế thừa BaseEntity
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(a => a.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(a => a.AlertType)
            .IsRequired().HasColumnName("alert_type")
            .HasConversion<string>().HasMaxLength(64);

        builder.Property(a => a.TriggeredAt)
            .IsRequired().HasColumnName("triggered_at").HasColumnType("DATETIME(6)");

        builder.Property(a => a.DetailsJson)
            .HasColumnName("details_json").HasColumnType("JSON");

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("resolved_at").HasColumnType("DATETIME(6)");

        // Indexes
        builder.HasIndex(a => new { a.PregnancyId, a.TriggeredAt })
            .HasDatabaseName("idx_weight_alerts_pregnancy");

        builder.HasIndex(a => new { a.AlertType, a.TriggeredAt })
            .HasDatabaseName("idx_weight_alerts_type");

        // Relationship
        builder.HasOne(a => a.Pregnancy)
            .WithMany().HasForeignKey(a => a.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ⚠️ NO query filter — WeightAlert không có soft delete
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/MotivationalTemplateConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MotivationalTemplateConfiguration : IEntityTypeConfiguration<MotivationalTemplate>
{
    public void Configure(EntityTypeBuilder<MotivationalTemplate> builder)
    {
        builder.ToTable("motivational_templates");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(m => m.Category)
            .IsRequired().HasColumnName("category")
            .HasConversion<string>().HasMaxLength(30);

        builder.Property(m => m.WeekStart)
            .IsRequired().HasColumnName("week_start");

        builder.Property(m => m.WeekEnd)
            .IsRequired().HasColumnName("week_end");

        builder.Property(m => m.IsActive)
            .IsRequired().HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(m => m.VariablesJson)
            .HasColumnName("variables_json").HasColumnType("JSON");

        // BaseEntity
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(m => m.IsDeleted);

        // Index
        builder.HasIndex(m => new { m.WeekStart, m.WeekEnd, m.IsActive })
            .HasDatabaseName("idx_motivational_week");

        // Relationships
        builder.HasMany(m => m.Translations)
            .WithOne(t => t.Template).HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/MotivationalTemplateTranslationConfiguration.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MotivationalTemplateTranslationConfiguration : IEntityTypeConfiguration<MotivationalTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<MotivationalTemplateTranslation> builder)
    {
        builder.ToTable("motivational_template_translations");

        // Composite PK
        builder.HasKey(t => new { t.TemplateId, t.LanguageCode });

        builder.Property(t => t.TemplateId)
            .HasColumnName("template_id").HasColumnType("CHAR(36)");

        builder.Property(t => t.LanguageCode)
            .HasColumnName("language_code").HasMaxLength(10);

        builder.Property(t => t.Title)
            .HasColumnName("title").HasMaxLength(120);

        builder.Property(t => t.Message)
            .IsRequired().HasColumnName("message").HasMaxLength(500);

        // Relationships
        builder.HasOne(t => t.Template)
            .WithMany(m => m.Translations)
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Thêm vào `AppDbContext.cs`** — DbSet:

```csharp
public DbSet<WeightLog> WeightLogs { get; set; }
public DbSet<WeightGoalRange> WeightGoalRanges { get; set; }
public DbSet<WeightAlert> WeightAlerts { get; set; }
public DbSet<MotivationalTemplate> MotivationalTemplates { get; set; }
public DbSet<MotivationalTemplateTranslation> MotivationalTemplateTranslations { get; set; }
```

**✅ Checkpoint**: Build thành công. Configurations match SQL schema chính xác.

---

## 🎯 PROMPT 3/8 — Migration + Seed Data

**Nhiệm vụ**: Tạo migration + seed motivational templates (baby size comparisons + milestones + tips).

**Step 1: Tạo Migration**:
```bash
cd src/FPT.EXE201.Api
dotnet ef migrations add AddWeightTrackingAndMotivational --project ../FPT.EXE201.Infrastructure --startup-project .
```

**Step 2: Seed Data — Motivational Templates**

```csharp
// File: FPT.EXE201.Infrastructure/Persistence/Seeders/MotivationalTemplateSeeder.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class MotivationalTemplateSeeder
{
    private static readonly DateTime SeedDate = new(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        // ═══════════════════════════════════════════════════
        // BABY_SIZE — So sánh kích thước bé theo tuần thai
        // ═══════════════════════════════════════════════════

        // GUIDs prefix: c6000001-xxxx for motivational templates
        var babySizeTemplates = new (string id, int weekStart, int weekEnd, string variablesJson)[]
        {
            ("c6000001-0000-0000-0000-000000000001", 4, 5,   """{"fruitVi":"hạt mè","fruitEn":"poppy seed","sizeCm":"0.1"}"""),
            ("c6000001-0000-0000-0000-000000000002", 6, 7,   """{"fruitVi":"hạt đậu lăng","fruitEn":"lentil","sizeCm":"0.6"}"""),
            ("c6000001-0000-0000-0000-000000000003", 8, 9,   """{"fruitVi":"quả mâm xôi","fruitEn":"raspberry","sizeCm":"1.6"}"""),
            ("c6000001-0000-0000-0000-000000000004", 10, 11, """{"fruitVi":"quả mận","fruitEn":"prune","sizeCm":"3.1"}"""),
            ("c6000001-0000-0000-0000-000000000005", 12, 13, """{"fruitVi":"quả chanh","fruitEn":"lime","sizeCm":"5.4"}"""),
            ("c6000001-0000-0000-0000-000000000006", 14, 15, """{"fruitVi":"quả cam","fruitEn":"orange","sizeCm":"8.7"}"""),
            ("c6000001-0000-0000-0000-000000000007", 16, 17, """{"fruitVi":"quả bơ","fruitEn":"avocado","sizeCm":"11.6"}"""),
            ("c6000001-0000-0000-0000-000000000008", 18, 19, """{"fruitVi":"quả xoài","fruitEn":"mango","sizeCm":"15.3"}"""),
            ("c6000001-0000-0000-0000-000000000009", 20, 21, """{"fruitVi":"quả chuối","fruitEn":"banana","sizeCm":"25.6"}"""),
            ("c6000001-0000-0000-0000-00000000000a", 22, 23, """{"fruitVi":"quả bắp","fruitEn":"corn","sizeCm":"28.9"}"""),
            ("c6000001-0000-0000-0000-00000000000b", 24, 25, """{"fruitVi":"quả dưa lưới","fruitEn":"cantaloupe","sizeCm":"30.0"}"""),
            ("c6000001-0000-0000-0000-00000000000c", 26, 27, """{"fruitVi":"bông cải xanh","fruitEn":"broccoli","sizeCm":"36.6"}"""),
            ("c6000001-0000-0000-0000-00000000000d", 28, 29, """{"fruitVi":"quả bí ngô","fruitEn":"butternut squash","sizeCm":"38.6"}"""),
            ("c6000001-0000-0000-0000-00000000000e", 30, 31, """{"fruitVi":"quả dừa","fruitEn":"coconut","sizeCm":"40.0"}"""),
            ("c6000001-0000-0000-0000-00000000000f", 32, 33, """{"fruitVi":"quả dứa","fruitEn":"pineapple","sizeCm":"42.4"}"""),
            ("c6000001-0000-0000-0000-000000000010", 34, 35, """{"fruitVi":"quả dưa hấu","fruitEn":"honeydew melon","sizeCm":"45.0"}"""),
            ("c6000001-0000-0000-0000-000000000011", 36, 37, """{"fruitVi":"quả bưởi","fruitEn":"papaya","sizeCm":"47.4"}"""),
            ("c6000001-0000-0000-0000-000000000012", 38, 40, """{"fruitVi":"quả dưa hấu","fruitEn":"watermelon","sizeCm":"50.0"}"""),
        };

        foreach (var (id, weekStart, weekEnd, variablesJson) in babySizeTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = "BabySize",
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                VariablesJson = variablesJson,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // MILESTONE — Cột mốc phát triển
        // ═══════════════════════════════════════════════════

        var milestoneTemplates = new (string id, int weekStart, int weekEnd)[]
        {
            ("c6000002-0000-0000-0000-000000000001", 8, 9),    // Tim bé đập
            ("c6000002-0000-0000-0000-000000000002", 12, 13),   // Bé biết nuốt
            ("c6000002-0000-0000-0000-000000000003", 16, 17),   // Bé biết đạp
            ("c6000002-0000-0000-0000-000000000004", 20, 21),   // Bé nghe được
            ("c6000002-0000-0000-0000-000000000005", 24, 25),   // Phổi phát triển
            ("c6000002-0000-0000-0000-000000000006", 28, 29),   // Bé mở mắt
            ("c6000002-0000-0000-0000-000000000007", 32, 33),   // Bé quay đầu
            ("c6000002-0000-0000-0000-000000000008", 36, 37),   // Bé sẵn sàng
            ("c6000002-0000-0000-0000-000000000009", 38, 40),   // Bé đủ tháng
        };

        foreach (var (id, weekStart, weekEnd) in milestoneTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = "Milestone",
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // TIP — Mẹo sức khỏe
        // ═══════════════════════════════════════════════════

        var tipTemplates = new (string id, int weekStart, int weekEnd)[]
        {
            ("c6000003-0000-0000-0000-000000000001", 0, 12),    // Tam cá nguyệt 1
            ("c6000003-0000-0000-0000-000000000002", 13, 27),   // Tam cá nguyệt 2
            ("c6000003-0000-0000-0000-000000000003", 28, 40),   // Tam cá nguyệt 3
        };

        foreach (var (id, weekStart, weekEnd) in tipTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = "Tip",
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // TRANSLATIONS — Vietnamese
        // ═══════════════════════════════════════════════════

        // Baby Size — VI
        var babySizeTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000001-0000-0000-0000-000000000001", "Bé to bằng hạt mè!", "Tuần 4-5: Bé mới chỉ nhỏ bằng hạt mè (0.1 cm), nhưng các cơ quan đã bắt đầu hình thành. Hãy bổ sung acid folic nhé mẹ!"),
            ("c6000001-0000-0000-0000-000000000002", "Bé to bằng hạt đậu lăng!", "Tuần 6-7: Bé dài khoảng 0.6 cm, tim bé đã bắt đầu đập. Mẹ có thể thấy nhịp tim bé qua siêu âm!"),
            ("c6000001-0000-0000-0000-000000000003", "Bé to bằng quả mâm xôi!", "Tuần 8-9: Bé dài 1.6 cm, các ngón tay bé đang hình thành. Mẹ nhớ uống đủ nước nhé!"),
            ("c6000001-0000-0000-0000-000000000004", "Bé to bằng quả mận!", "Tuần 10-11: Bé dài 3.1 cm, đã có thể cử động nhẹ. Giai đoạn này mẹ có thể bị ốm nghén nhiều."),
            ("c6000001-0000-0000-0000-000000000005", "Bé to bằng quả chanh!", "Tuần 12-13: Bé dài 5.4 cm, khuôn mặt bé đã rõ nét hơn. Mẹ sắp qua giai đoạn ốm nghén rồi!"),
            ("c6000001-0000-0000-0000-000000000006", "Bé to bằng quả cam!", "Tuần 14-15: Bé dài 8.7 cm, bé đã biết nhăn mặt và mút tay. Mẹ bắt đầu cảm thấy khỏe hơn!"),
            ("c6000001-0000-0000-0000-000000000007", "Bé to bằng quả bơ!", "Tuần 16-17: Bé dài 11.6 cm, xương bé đang cứng dần. Mẹ có thể bắt đầu cảm nhận bé đạp nhẹ!"),
            ("c6000001-0000-0000-0000-000000000008", "Bé to bằng quả xoài!", "Tuần 18-19: Bé dài 15.3 cm, bé đã biết nghe âm thanh. Hãy nói chuyện với bé mỗi ngày nhé!"),
            ("c6000001-0000-0000-0000-000000000009", "Bé to bằng quả chuối!", "Tuần 20-21: Bé dài 25.6 cm — nửa chặng đường rồi mẹ ơi! Bé đã có lông mày và mi mắt."),
            ("c6000001-0000-0000-0000-00000000000a", "Bé to bằng bắp ngô!", "Tuần 22-23: Bé dài 28.9 cm, da bé đang dần hồng hào hơn. Mẹ nhớ bổ sung sắt nhé!"),
            ("c6000001-0000-0000-0000-00000000000b", "Bé to bằng quả dưa lưới!", "Tuần 24-25: Bé dài khoảng 30 cm, phổi đang phát triển mạnh. Bé phản ứng với ánh sáng rồi mẹ ạ!"),
            ("c6000001-0000-0000-0000-00000000000c", "Bé to bằng bông cải xanh!", "Tuần 26-27: Bé dài 36.6 cm, mắt bé đã mở được. Bé đang tập thở trong bụng mẹ!"),
            ("c6000001-0000-0000-0000-00000000000d", "Bé to bằng quả bí ngô!", "Tuần 28-29: Bé dài 38.6 cm, nặng khoảng 1 kg. Não bé phát triển rất nhanh giai đoạn này!"),
            ("c6000001-0000-0000-0000-00000000000e", "Bé to bằng quả dừa!", "Tuần 30-31: Bé dài 40 cm, bé tích mỡ để giữ ấm sau khi sinh. Mẹ nên nghỉ ngơi nhiều hơn!"),
            ("c6000001-0000-0000-0000-00000000000f", "Bé to bằng quả dứa!", "Tuần 32-33: Bé dài 42.4 cm, xương bé gần như hoàn thiện. Mẹ bắt đầu chuẩn bị đồ sơ sinh nhé!"),
            ("c6000001-0000-0000-0000-000000000010", "Bé to bằng quả dưa!", "Tuần 34-35: Bé dài 45 cm, phổi gần trưởng thành. Mẹ nhớ đếm cử động bé hàng ngày!"),
            ("c6000001-0000-0000-0000-000000000011", "Bé to bằng quả bưởi!", "Tuần 36-37: Bé dài 47.4 cm, đầu bé đã quay xuống. Sắp được gặp con rồi mẹ ơi!"),
            ("c6000001-0000-0000-0000-000000000012", "Bé to bằng quả dưa hấu!", "Tuần 38-40: Bé dài khoảng 50 cm, nặng 3-3.5 kg. Bé đủ tháng và sẵn sàng chào đời!"),
        };

        foreach (var (templateId, title, message) in babySizeTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // Milestone — VI
        var milestoneTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000002-0000-0000-0000-000000000001", "Tim bé đập rồi! 💓", "Tuần 8: Tim bé đang đập 120-160 nhịp/phút, nhanh gấp đôi mẹ! Mẹ có thể nghe thấy qua siêu âm."),
            ("c6000002-0000-0000-0000-000000000002", "Bé biết nuốt! 🍼", "Tuần 12: Bé bắt đầu tập nuốt nước ối — đây là cách bé tập ăn trước khi ra đời!"),
            ("c6000002-0000-0000-0000-000000000003", "Bé biết đạp! 🦶", "Tuần 16: Mẹ bắt đầu cảm nhận bé cử động — những cú đạp đầu tiên thật tuyệt vời!"),
            ("c6000002-0000-0000-0000-000000000004", "Bé nghe được rồi! 👂", "Tuần 20: Bé đã nghe được giọng mẹ! Hãy hát và nói chuyện với bé nhiều nhé."),
            ("c6000002-0000-0000-0000-000000000005", "Phổi bé phát triển! 🫁", "Tuần 24: Phổi bé đang hình thành túi khí. Bé có thể sống ngoài tử cung nếu sinh non (với hỗ trợ y tế)."),
            ("c6000002-0000-0000-0000-000000000006", "Bé mở mắt! 👀", "Tuần 28: Bé đã mở mắt và nhìn thấy ánh sáng từ bên ngoài bụng mẹ!"),
            ("c6000002-0000-0000-0000-000000000007", "Bé quay đầu! 🔄", "Tuần 32: Hầu hết bé đã quay đầu xuống dưới, sẵn sàng cho ngày sinh."),
            ("c6000002-0000-0000-0000-000000000008", "Bé sẵn sàng! ✨", "Tuần 36: Bé đã phát triển gần hoàn thiện. Mẹ nên chuẩn bị túi đồ đi sinh nhé!"),
            ("c6000002-0000-0000-0000-000000000009", "Bé đủ tháng! 🎉", "Tuần 38-40: Bé đã sẵn sàng chào đời! Mẹ bình tĩnh và tin tưởng vào bản thân nhé."),
        };

        foreach (var (templateId, title, message) in milestoneTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // Tip — VI
        var tipTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000003-0000-0000-0000-000000000001", "Mẹo tam cá nguyệt 1 💊", "3 tháng đầu: Bổ sung acid folic 400mcg/ngày, ăn ít nhưng nhiều bữa để giảm ốm nghén, uống đủ 2L nước/ngày."),
            ("c6000003-0000-0000-0000-000000000002", "Mẹo tam cá nguyệt 2 🏃‍♀️", "3 tháng giữa: Giai đoạn mẹ khỏe nhất! Tập thể dục nhẹ (yoga, đi bộ), bổ sung sắt + canxi, theo dõi cân nặng đều đặn."),
            ("c6000003-0000-0000-0000-000000000003", "Mẹo tam cá nguyệt 3 🧸", "3 tháng cuối: Đếm cử động bé (>10 lần/ngày), chuẩn bị đồ sơ sinh, nghỉ ngơi nhiều, nằm nghiêng trái để tăng tuần hoàn."),
        };

        foreach (var (templateId, title, message) in tipTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // ═══════════════════════════════════════════════════
        // TRANSLATIONS — English
        // ═══════════════════════════════════════════════════

        // Baby Size — EN (representative subset — add all in real implementation)
        var babySizeTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000001-0000-0000-0000-000000000001", "Baby is the size of a poppy seed!", "Week 4-5: Baby is just 0.1 cm, but organs are starting to form. Remember to take your folic acid!"),
            ("c6000001-0000-0000-0000-000000000002", "Baby is the size of a lentil!", "Week 6-7: Baby is about 0.6 cm and the heart has started beating. You may see the heartbeat on ultrasound!"),
            ("c6000001-0000-0000-0000-000000000003", "Baby is the size of a raspberry!", "Week 8-9: Baby is 1.6 cm, fingers are forming. Stay hydrated!"),
            ("c6000001-0000-0000-0000-000000000004", "Baby is the size of a prune!", "Week 10-11: Baby is 3.1 cm and can make small movements. Morning sickness may peak around now."),
            ("c6000001-0000-0000-0000-000000000005", "Baby is the size of a lime!", "Week 12-13: Baby is 5.4 cm with more defined facial features. Morning sickness should ease soon!"),
            ("c6000001-0000-0000-0000-000000000006", "Baby is the size of an orange!", "Week 14-15: Baby is 8.7 cm — can squint, frown, and suck thumb. You should feel more energetic!"),
            ("c6000001-0000-0000-0000-000000000007", "Baby is the size of an avocado!", "Week 16-17: Baby is 11.6 cm, bones are hardening. You may start feeling first kicks!"),
            ("c6000001-0000-0000-0000-000000000008", "Baby is the size of a mango!", "Week 18-19: Baby is 15.3 cm and can hear sounds. Talk to your baby every day!"),
            ("c6000001-0000-0000-0000-000000000009", "Baby is the size of a banana!", "Week 20-21: Baby is 25.6 cm — halfway there! Baby now has eyebrows and eyelids."),
            ("c6000001-0000-0000-0000-00000000000a", "Baby is the size of an ear of corn!", "Week 22-23: Baby is 28.9 cm, skin is becoming more opaque. Remember to take your iron supplements!"),
            ("c6000001-0000-0000-0000-00000000000b", "Baby is the size of a cantaloupe!", "Week 24-25: Baby is about 30 cm, lungs are developing rapidly. Baby responds to light now!"),
            ("c6000001-0000-0000-0000-00000000000c", "Baby is the size of a broccoli!", "Week 26-27: Baby is 36.6 cm, eyes can open now. Baby is practicing breathing in the womb!"),
            ("c6000001-0000-0000-0000-00000000000d", "Baby is the size of a butternut squash!", "Week 28-29: Baby is 38.6 cm, weighing about 1 kg. Brain is developing very rapidly now!"),
            ("c6000001-0000-0000-0000-00000000000e", "Baby is the size of a coconut!", "Week 30-31: Baby is 40 cm, building up fat to stay warm after birth. Get more rest!"),
            ("c6000001-0000-0000-0000-00000000000f", "Baby is the size of a pineapple!", "Week 32-33: Baby is 42.4 cm, bones are nearly complete. Start preparing the nursery!"),
            ("c6000001-0000-0000-0000-000000000010", "Baby is the size of a honeydew melon!", "Week 34-35: Baby is 45 cm, lungs are nearly mature. Count baby movements daily!"),
            ("c6000001-0000-0000-0000-000000000011", "Baby is the size of a papaya!", "Week 36-37: Baby is 47.4 cm, head has turned down. Almost time to meet your baby!"),
            ("c6000001-0000-0000-0000-000000000012", "Baby is the size of a watermelon!", "Week 38-40: Baby is about 50 cm, weighing 3-3.5 kg. Baby is full-term and ready to be born!"),
        };

        foreach (var (templateId, title, message) in babySizeTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }

        // Milestone — EN
        var milestoneTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000002-0000-0000-0000-000000000001", "Baby's heart is beating! 💓", "Week 8: Baby's heart beats 120-160 bpm — twice as fast as yours! You can hear it via ultrasound."),
            ("c6000002-0000-0000-0000-000000000002", "Baby can swallow! 🍼", "Week 12: Baby begins swallowing amniotic fluid — it's how they practice eating before birth!"),
            ("c6000002-0000-0000-0000-000000000003", "Baby can kick! 🦶", "Week 16: You may start feeling baby's movements — those first kicks are magical!"),
            ("c6000002-0000-0000-0000-000000000004", "Baby can hear you! 👂", "Week 20: Baby can hear your voice! Sing and talk to your little one regularly."),
            ("c6000002-0000-0000-0000-000000000005", "Baby's lungs are developing! 🫁", "Week 24: Air sacs are forming in baby's lungs. Baby could survive outside the womb with medical support."),
            ("c6000002-0000-0000-0000-000000000006", "Baby opens eyes! 👀", "Week 28: Baby's eyes are open and can see light filtering through from outside!"),
            ("c6000002-0000-0000-0000-000000000007", "Baby turns head down! 🔄", "Week 32: Most babies have turned head-down, getting ready for delivery day."),
            ("c6000002-0000-0000-0000-000000000008", "Baby is almost ready! ✨", "Week 36: Baby is nearly fully developed. Start packing your hospital bag!"),
            ("c6000002-0000-0000-0000-000000000009", "Baby is full-term! 🎉", "Week 38-40: Baby is ready to be born! Stay calm and trust yourself."),
        };

        foreach (var (templateId, title, message) in milestoneTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }

        // Tip — EN
        var tipTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000003-0000-0000-0000-000000000001", "First trimester tips 💊", "Months 1-3: Take 400mcg folic acid daily, eat small frequent meals to reduce nausea, drink 2L water daily."),
            ("c6000003-0000-0000-0000-000000000002", "Second trimester tips 🏃‍♀️", "Months 4-6: Your most energetic period! Light exercise (yoga, walking), take iron & calcium, monitor weight regularly."),
            ("c6000003-0000-0000-0000-000000000003", "Third trimester tips 🧸", "Months 7-9: Count baby movements (10+/day), prepare baby essentials, rest well, sleep on your left side for better circulation."),
        };

        foreach (var (templateId, title, message) in tipTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }
    }
}
```

**Step 3: Gọi Seeder trong `DatabaseSeeder.cs`** (nếu cần runtime seeding) hoặc trong migration.

**Step 4: Thêm permission mới vào `DatabaseSeeder.cs`**:

**⚠️ Permission Naming Convention** (theo codebase pattern: `lowercase.dot.notation`):

Permissions đã seed sẵn trong DatabaseSeeder.cs (Week trước):
- `weight_logs.write.own` — User can log their weight
- `weight_logs.read.any` — Doctor can view patient weight logs
- `weight_alerts.manage` — Admin can configure alert rules
- `motivational_templates.write` — Admin can create/update motivational messages

→ **Cần xóa 4 permissions cũ trên** và thay bằng 7 permissions mới (consistent naming, granular hơn):

```csharp
// Thêm vào DatabaseSeeder.SeedPermissionsAsync():
// ═══ Week 6: Weight Tracking ═══
await SeedPermissionIfNotExists(context, "weight_log.read", "Read Weight Logs", "User can view their own weight logs");
await SeedPermissionIfNotExists(context, "weight_log.write", "Write Weight Logs", "User can create/update weight logs + OCR extract");
await SeedPermissionIfNotExists(context, "weight_log.delete", "Delete Weight Logs", "User can delete their own weight logs");
await SeedPermissionIfNotExists(context, "weight_goal.read", "Read Weight Goals", "User can view their weight goals");
await SeedPermissionIfNotExists(context, "weight_goal.write", "Write Weight Goals", "User can set/update weight goals");
await SeedPermissionIfNotExists(context, "weight_alert.read", "Read Weight Alerts", "User can view their weight alerts");
await SeedPermissionIfNotExists(context, "weight_alert.resolve", "Resolve Weight Alerts", "User can resolve weight alerts");
// ❌ motivational.read KHÔNG CẦN — MotivationalController là public API (giống RefDataController)

// ═══ Xóa permissions cũ (conflicting naming) ═══
// DELETE FROM permissions WHERE code IN ('weight_logs.write.own','weight_logs.read.any','weight_alerts.manage','motivational_templates.write');

// Assign to USER role:
// weight_log.read, weight_log.write, weight_log.delete, weight_goal.read, weight_goal.write,
// weight_alert.read, weight_alert.resolve
```

**✅ Checkpoint**: Migration thành công. Database có 5 tables mới. 30 motivational templates seeded (18 BabySize + 9 Milestone + 3 Tip) × 2 languages = 60 translations.

---

## 🎯 PROMPT 4/8 — DTOs (record) + FluentValidation

**Nhiệm vụ**: Tạo DTOs và validators cho Weight Tracking APIs.

**⚠️ Convention**: Dùng `record` — KHÔNG dùng `class`.

**Code**:

```csharp
// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/CreateWeightLogDto.cs
// ═══════════════════════════════════════════════════
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record CreateWeightLogDto(
    DateOnly LoggedOn,
    decimal WeightKg,
    string? Note = null,
    WeightSource Source = WeightSource.Manual
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/UpdateWeightLogDto.cs
// ═══════════════════════════════════════════════════
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record UpdateWeightLogDto(
    decimal? WeightKg = null,
    string? Note = null,
    WeightSource? Source = null
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/WeightLogDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightLogDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly LoggedOn,
    decimal WeightKg,
    string? Note,
    string Source,
    decimal? WeightGainFromBaseline,  // weightKg - prePregnancyWeight (computed)
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/WeightChartDataDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightChartDataDto(
    decimal? PrePregnancyWeightKg,
    decimal? RecommendedGainMin,
    decimal? RecommendedGainMax,
    decimal? CurrentWeightKg,
    decimal? TotalGainKg,
    int TotalEntries,
    List<WeightChartPointDto> DataPoints
);

public record WeightChartPointDto(
    DateOnly Date,
    decimal WeightKg,
    int? GestationalWeek  // computed from LMP
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/CreateWeightGoalDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record CreateWeightGoalDto(
    decimal? HeightCm = null,
    decimal? PrePregnancyWeightKg = null,
    decimal? RecommendedTotalGainMin = null,
    decimal? RecommendedTotalGainMax = null,
    string? Notes = null
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/WeightGoalDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightGoalDto(
    Guid Id,
    Guid PregnancyId,
    decimal? HeightCm,
    decimal? PrePregnancyWeightKg,
    decimal? Bmi,
    string? BmiCategory,  // Underweight | Normal | Overweight | Obese
    decimal? RecommendedTotalGainMin,
    decimal? RecommendedTotalGainMax,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/WeightAlertDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightAlertDto(
    Guid Id,
    Guid PregnancyId,
    string AlertType,
    DateTime TriggeredAt,
    string? DetailsJson,
    DateTime? ResolvedAt,
    bool IsResolved
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/MotivationalTemplateDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record MotivationalTemplateDto(
    Guid Id,
    string Category,
    int WeekStart,
    int WeekEnd,
    string? VariablesJson,
    string? Title,      // from translation
    string Message      // from translation
);

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/DTOs/WeightTracking/WeightOcrExtractResultDto.cs
// ═══════════════════════════════════════════════════
namespace FPT.EXE201.Application.DTOs.WeightTracking;

/// <summary>
/// Kết quả trích xuất cân nặng từ ảnh — trả về cho FE để user confirm.
/// Nếu Success = true → FE hiển thị ExtractedWeightKg cho user xác nhận.
/// Nếu Success = false → FE thông báo không nhận diện được, mời chụp lại.
/// </summary>
public record WeightOcrExtractResultDto(
    bool Success,
    decimal? ExtractedWeightKg,   // Giá trị cân nặng trích xuất được (null nếu fail)
    string? RawOcrText,           // Raw text từ OCR engine (debug/logging)
    decimal? ConfidenceScore,     // 0.00 – 100.00 từ OCR engine
    string Message                // "Weight extracted successfully" hoặc lý do fail
);
```

**Validators**:

```csharp
// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/Validations/WeightTracking/CreateWeightLogDtoValidator.cs
// ═══════════════════════════════════════════════════
using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class CreateWeightLogDtoValidator : AbstractValidator<CreateWeightLogDto>
{
    public CreateWeightLogDtoValidator()
    {
        RuleFor(x => x.LoggedOn)
            .NotEmpty().WithMessage("Logged date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Logged date cannot be in the future.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThan(500).WithMessage("Weight must be less than 500 kg.");

        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("Note cannot exceed 255 characters.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Source must be a valid WeightSource value (Manual, OCR).");
    }
}

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/Validations/WeightTracking/UpdateWeightLogDtoValidator.cs
// ═══════════════════════════════════════════════════
using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class UpdateWeightLogDtoValidator : AbstractValidator<UpdateWeightLogDto>
{
    public UpdateWeightLogDtoValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be greater than 0.")
            .LessThan(500).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be less than 500 kg.");

        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("Note cannot exceed 255 characters.");

        RuleFor(x => x.Source)
            .IsInEnum().When(x => x.Source.HasValue)
            .WithMessage("Source must be a valid WeightSource value (Manual, OCR).");
    }
}

// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/Validations/WeightTracking/CreateWeightGoalDtoValidator.cs
// ═══════════════════════════════════════════════════
using FluentValidation;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.Validations.WeightTracking;

public class CreateWeightGoalDtoValidator : AbstractValidator<CreateWeightGoalDto>
{
    public CreateWeightGoalDtoValidator()
    {
        RuleFor(x => x.HeightCm)
            .GreaterThan(50).When(x => x.HeightCm.HasValue)
            .WithMessage("Height must be greater than 50 cm.")
            .LessThan(250).When(x => x.HeightCm.HasValue)
            .WithMessage("Height must be less than 250 cm.");

        RuleFor(x => x.PrePregnancyWeightKg)
            .GreaterThan(0).When(x => x.PrePregnancyWeightKg.HasValue)
            .WithMessage("Pre-pregnancy weight must be greater than 0.")
            .LessThan(500).When(x => x.PrePregnancyWeightKg.HasValue)
            .WithMessage("Pre-pregnancy weight must be less than 500 kg.");

        RuleFor(x => x.RecommendedTotalGainMin)
            .GreaterThanOrEqualTo(0).When(x => x.RecommendedTotalGainMin.HasValue)
            .WithMessage("Minimum gain must be >= 0.");

        RuleFor(x => x.RecommendedTotalGainMax)
            .GreaterThanOrEqualTo(x => x.RecommendedTotalGainMin ?? 0)
            .When(x => x.RecommendedTotalGainMax.HasValue && x.RecommendedTotalGainMin.HasValue)
            .WithMessage("Maximum gain must be >= minimum gain.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
```

**✅ Checkpoint**: Build thành công. DTOs dùng `record`. Validators cover range checks, future date, max length.

---

## 🎯 PROMPT 5/8 — Repository Interfaces + Implementations + UnitOfWork

**Nhiệm vụ**: Tạo repository interfaces (Application) + implementations (Infrastructure) + update UnitOfWork.

**Code — Interfaces** (Application Layer):

```csharp
// File: FPT.EXE201.Application/IRepositories/IWeightLogRepository.cs
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightLogRepository : IGenericRepository<WeightLog>
{
    Task<PagedResult<WeightLog>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default);

    Task<List<WeightLog>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<WeightLog?> GetByPregnancyAndDateAsync(
        Guid pregnancyId, DateOnly loggedOn, CancellationToken ct = default);

    Task<WeightLog?> GetLatestByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<List<WeightLog>> GetRecentByPregnancyIdAsync(
        Guid pregnancyId, int count = 5, CancellationToken ct = default);
}

// File: FPT.EXE201.Application/IRepositories/IWeightGoalRangeRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightGoalRangeRepository : IGenericRepository<WeightGoalRange>
{
    Task<WeightGoalRange?> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);
}

// File: FPT.EXE201.Application/IRepositories/IWeightAlertRepository.cs
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightAlertRepository
{
    Task<List<WeightAlert>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<WeightAlert?> GetByIdAsync(
        Guid id, CancellationToken ct = default);

    Task AddAsync(WeightAlert alert, CancellationToken ct = default);

    void Update(WeightAlert alert);

    /// <summary>
    /// Check if an alert of the same type was created within the last <paramref name="days"/> days.
    /// Used for cooldown to prevent duplicate alerts.
    /// </summary>
    Task<bool> HasRecentAlertAsync(
        Guid pregnancyId, WeightAlertType alertType, int days = 7, CancellationToken ct = default);
}

// File: FPT.EXE201.Application/IRepositories/IMotivationalTemplateRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMotivationalTemplateRepository : IGenericRepository<MotivationalTemplate>
{
    Task<List<MotivationalTemplate>> GetByWeekAsync(
        int gestationalWeek, string? category = null, string langCode = "vi",
        CancellationToken ct = default);
}
```

**Code — Implementations** (Infrastructure Layer):

```csharp
// File: FPT.EXE201.Infrastructure/Repositories/WeightLogRepository.cs
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.WeightLogs;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class WeightLogRepository : GenericRepository<WeightLog>, IWeightLogRepository
{
    public WeightLogRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<WeightLog>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default)
    {
        return await GetPagedAsync(
            options,
            predicate: w => w.PregnancyId == pregnancyId,
            include: null,
            searchBuilder: SearchHelper.CreateSearchBuilder(
                WeightLogListQuerySpec.SearchMap,
                WeightLogListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: WeightLogListQuerySpec.SortMap,
            defaultSort: WeightLogListQuerySpec.DefaultSort,
            cancellationToken: ct);
    }

    public async Task<List<WeightLog>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderBy(w => w.LoggedOn)
            .ToListAsync(ct);
    }

    public async Task<WeightLog?> GetByPregnancyAndDateAsync(
        Guid pregnancyId, DateOnly loggedOn, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.PregnancyId == pregnancyId && w.LoggedOn == loggedOn, ct);
    }

    public async Task<WeightLog?> GetLatestByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderByDescending(w => w.LoggedOn)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<WeightLog>> GetRecentByPregnancyIdAsync(
        Guid pregnancyId, int count = 5, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderByDescending(w => w.LoggedOn)
            .Take(count)
            .ToListAsync(ct);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/WeightGoalRangeRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class WeightGoalRangeRepository : GenericRepository<WeightGoalRange>, IWeightGoalRangeRepository
{
    public WeightGoalRangeRepository(AppDbContext context) : base(context) { }

    public async Task<WeightGoalRange?> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.PregnancyId == pregnancyId, ct);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/WeightAlertRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

/// <summary>
/// ⚠️ WeightAlert KHÔNG kế thừa BaseEntity → KHÔNG dùng GenericRepository.
/// Implement trực tiếp IWeightAlertRepository.
/// </summary>
public class WeightAlertRepository : IWeightAlertRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<WeightAlert> _dbSet;

    public WeightAlertRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<WeightAlert>();
    }

    public async Task<List<WeightAlert>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(a => a.PregnancyId == pregnancyId)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(ct);
    }

    public async Task<WeightAlert?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(WeightAlert alert, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(alert, ct);
    }

    public void Update(WeightAlert alert)
    {
        _dbSet.Update(alert);
    }

    public async Task<bool> HasRecentAlertAsync(
        Guid pregnancyId, WeightAlertType alertType, int days = 7, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _dbSet.AnyAsync(
            a => a.PregnancyId == pregnancyId
                 && a.AlertType == alertType
                 && a.TriggeredAt >= cutoff,
            ct);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/MotivationalTemplateRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MotivationalTemplateRepository : GenericRepository<MotivationalTemplate>, IMotivationalTemplateRepository
{
    public MotivationalTemplateRepository(AppDbContext context) : base(context) { }

    public async Task<List<MotivationalTemplate>> GetByWeekAsync(
        int gestationalWeek, string? category = null, string langCode = "vi",
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Where(m => m.IsActive && m.WeekStart <= gestationalWeek && m.WeekEnd >= gestationalWeek)
            .Include(m => m.Translations.Where(t => t.LanguageCode == langCode));

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<MotivationalCategory>(category, ignoreCase: true, out var parsedCategory))
        {
            query = query.Where(m => m.Category == parsedCategory);
        }

        return await query
            .OrderBy(m => m.Category)
            .ThenBy(m => m.WeekStart)
            .ToListAsync(ct);
    }
}
```

**Update UnitOfWork** (lazy `??=` pattern):

```csharp
// File: FPT.EXE201.Application/IUnitOfWork.cs — thêm:
IWeightLogRepository WeightLogs { get; }
IWeightGoalRangeRepository WeightGoalRanges { get; }
IWeightAlertRepository WeightAlerts { get; }
IMotivationalTemplateRepository MotivationalTemplates { get; }

// File: FPT.EXE201.Infrastructure/Repositories/UnitOfWork.cs — thêm:
private IWeightLogRepository? _weightLogs;
private IWeightGoalRangeRepository? _weightGoalRanges;
private IWeightAlertRepository? _weightAlerts;
private IMotivationalTemplateRepository? _motivationalTemplates;

public IWeightLogRepository WeightLogs => _weightLogs ??= new WeightLogRepository(_context);
public IWeightGoalRangeRepository WeightGoalRanges => _weightGoalRanges ??= new WeightGoalRangeRepository(_context);
public IWeightAlertRepository WeightAlerts => _weightAlerts ??= new WeightAlertRepository(_context);
public IMotivationalTemplateRepository MotivationalTemplates => _motivationalTemplates ??= new MotivationalTemplateRepository(_context);
```

**✅ Checkpoint**: Build thành công. 4 repositories registered via UnitOfWork lazy pattern.

---

## 🎯 PROMPT 6/8 — QuerySpec (Search/Sort/Paging)

**Nhiệm vụ**: Tạo `WeightLogListQuerySpec` + đăng ký vào `QuerySpecRegistry`.

**Code**:

```csharp
// File: FPT.EXE201.Application/Features/WeightLogs/WeightLogListQuerySpec.cs
using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.WeightLogs;

/// <summary>
/// Query specification for WeightLog entity listing.
/// Searchable: note | Sortable: loggedon, weightkg, createdat
/// </summary>
public static class WeightLogListQuerySpec
{
    // ─── Search whitelist ──────────────────────────────────────
    public static readonly Dictionary<string, Expression<Func<WeightLog, string?>>> SearchMap = new()
    {
        ["note"] = w => w.Note
    };

    public static readonly string[] DefaultSearchKeys = ["note"];

    // ─── Sort whitelist ────────────────────────────────────────
    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["loggedon"]  = (Expression<Func<WeightLog, DateOnly>>)(w => w.LoggedOn),
        ["weightkg"]  = (Expression<Func<WeightLog, decimal>>)(w => w.WeightKg),
        ["createdat"] = (Expression<Func<WeightLog, DateTime>>)(w => w.CreatedAt)
    };

    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<WeightLog, DateOnly>>)(w => w.LoggedOn);

    // ─── Metadata cho FE ───────────────────────────────────────
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "loggedon",
        DefaultSortDir = "desc"
    };
}
```

**Đăng ký vào `QuerySpecRegistry.cs`**:

```csharp
// File: FPT.EXE201.Application/Common/Querying/QuerySpecRegistry.cs — thêm entry:
["weightLogs"] = WeightLogListQuerySpec.Metadata,
```

**✅ Checkpoint**: Build thành công. `GET /api/ref/query-specs` sẽ trả metadata cho `weightLogs`.

---

## 🎯 PROMPT 7/8 — Service Interfaces + Implementations

**Nhiệm vụ**: Tạo `IWeightLogService` + `WeightLogService` + `IMotivationalService` + `MotivationalService`.

**Code — Interfaces** (Application Layer):

```csharp
// File: FPT.EXE201.Application/IServices/IWeightLogService.cs
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

public interface IWeightLogService
{
    Task<WeightLogDto> CreateAsync(Guid pregnancyId, Guid userId, CreateWeightLogDto dto, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByPregnancyIdPagedAsync(Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken ct = default);
    Task<WeightChartDataDto> GetChartDataAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightLogDto> UpdateAsync(Guid id, Guid userId, UpdateWeightLogDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);

    // OCR Weight Extraction (delegates to IWeightOcrService — Infrastructure layer)
    Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(Guid pregnancyId, Guid userId, Stream imageStream, string fileName, CancellationToken ct = default);

    // Weight Goals
    Task<WeightGoalDto> CreateGoalAsync(Guid pregnancyId, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default);
    Task<WeightGoalDto?> GetGoalAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightGoalDto> UpdateGoalAsync(Guid id, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default);

    // Weight Alerts
    Task<List<WeightAlertDto>> GetAlertsAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightAlertDto> ResolveAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
}

// File: FPT.EXE201.Application/IServices/IMotivationalService.cs
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

public interface IMotivationalService
{
    Task<List<MotivationalTemplateDto>> GetByWeekAsync(int week, string? category = null, string langCode = "vi", CancellationToken ct = default);
}
```

**Code — WeightLogService** (Application Layer):

```csharp
// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Application/IServices/IWeightOcrService.cs
// ═══════════════════════════════════════════════════
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Weight OCR extraction — interface ở Application layer.
/// Implementation ở Infrastructure layer (dùng IOcrProvider).
/// Tách riêng để giữ Clean Architecture: Application KHÔNG phụ thuộc IOcrProvider.
/// </summary>
public interface IWeightOcrService
{
    /// <summary>
    /// Trích xuất cân nặng từ ảnh chụp cân.
    /// Returns WeightOcrExtractResultDto cho FE confirm.
    /// </summary>
    Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Stream imageStream, string fileName, CancellationToken ct = default);
}
```

```csharp
// File: FPT.EXE201.Application/Services/WeightLogService.cs
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class WeightLogService : IWeightLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWeightOcrService _weightOcrService;

    public WeightLogService(IUnitOfWork unitOfWork, IWeightOcrService weightOcrService)
    {
        _unitOfWork = unitOfWork;
        _weightOcrService = weightOcrService;
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT LOGS
    // ═══════════════════════════════════════════════════

    public async Task<WeightLogDto> CreateAsync(Guid pregnancyId, Guid userId, CreateWeightLogDto dto, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Check duplicate date
        var existing = await _unitOfWork.WeightLogs.GetByPregnancyAndDateAsync(pregnancyId, dto.LoggedOn, ct);
        if (existing != null)
            throw new ConflictException($"A weight log already exists for {dto.LoggedOn:yyyy-MM-dd}.");

        var weightLog = new WeightLog
        {
            PregnancyId = pregnancyId,
            LoggedOn = dto.LoggedOn,
            WeightKg = dto.WeightKg,
            Note = dto.Note,
            Source = dto.Source
        };

        await _unitOfWork.WeightLogs.AddAsync(weightLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Check for alerts after logging
        await CheckAndCreateAlerts(pregnancyId, weightLog, ct);

        return MapToDto(weightLog, pregnancy.PrePregnancyWeightKg);
    }

    // ═══════════════════════════════════════════════════
    // OCR WEIGHT EXTRACTION (delegates to IWeightOcrService)
    // ═══════════════════════════════════════════════════

    public async Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Guid pregnancyId, Guid userId, Stream imageStream, string fileName, CancellationToken ct = default)
    {
        // Verify pregnancy ownership trước khi gọi OCR
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Delegate to IWeightOcrService (Infrastructure layer — has access to IOcrProvider)
        return await _weightOcrService.ExtractWeightFromImageAsync(imageStream, fileName, ct);
    }

    public async Task<PagedResult<WeightLogDto>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var paged = await _unitOfWork.WeightLogs.GetByPregnancyIdPagedAsync(pregnancyId, options, ct);
        var dtos = paged.Items.Select(w => MapToDto(w, pregnancy.PrePregnancyWeightKg)).ToList();

        return new PagedResult<WeightLogDto>(dtos, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<WeightChartDataDto> GetChartDataAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var logs = await _unitOfWork.WeightLogs.GetByPregnancyIdAsync(pregnancyId, ct);
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);

        var latestLog = logs.LastOrDefault();
        var totalGain = latestLog != null && pregnancy.PrePregnancyWeightKg.HasValue
            ? latestLog.WeightKg - pregnancy.PrePregnancyWeightKg.Value
            : (decimal?)null;

        var dataPoints = logs.Select(w => new WeightChartPointDto(
            w.LoggedOn,
            w.WeightKg,
            pregnancy.LastMenstrualPeriodDate.HasValue
                ? (int)((w.LoggedOn.ToDateTime(TimeOnly.MinValue) - pregnancy.LastMenstrualPeriodDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 7)
                : null
        )).ToList();

        return new WeightChartDataDto(
            PrePregnancyWeightKg: pregnancy.PrePregnancyWeightKg,
            RecommendedGainMin: goal?.RecommendedTotalGainMin,
            RecommendedGainMax: goal?.RecommendedTotalGainMax,
            CurrentWeightKg: latestLog?.WeightKg,
            TotalGainKg: totalGain,
            TotalEntries: logs.Count,
            DataPoints: dataPoints
        );
    }

    public async Task<WeightLogDto> UpdateAsync(Guid id, Guid userId, UpdateWeightLogDto dto, CancellationToken ct = default)
    {
        var weightLog = await _unitOfWork.WeightLogs.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight log not found.");

        var pregnancy = await VerifyPregnancyOwnership(weightLog.PregnancyId, userId, ct);

        if (dto.WeightKg.HasValue) weightLog.WeightKg = dto.WeightKg.Value;
        if (dto.Note != null) weightLog.Note = dto.Note;
        if (dto.Source.HasValue) weightLog.Source = dto.Source.Value;

        _unitOfWork.WeightLogs.Update(weightLog);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(weightLog, pregnancy.PrePregnancyWeightKg);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var weightLog = await _unitOfWork.WeightLogs.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight log not found.");

        await VerifyPregnancyOwnership(weightLog.PregnancyId, userId, ct);

        await _unitOfWork.WeightLogs.SoftDeleteAsync(weightLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT GOALS
    // ═══════════════════════════════════════════════════

    public async Task<WeightGoalDto> CreateGoalAsync(Guid pregnancyId, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var existing = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        if (existing != null)
            throw new ConflictException("Weight goal already exists for this pregnancy. Use PUT to update.");

        var heightCm = dto.HeightCm ?? pregnancy.HeightCm;
        var preWeight = dto.PrePregnancyWeightKg ?? pregnancy.PrePregnancyWeightKg;
        var bmi = CalculateBmi(preWeight, heightCm);

        // Auto IOM guidelines if user did not provide custom range
        var (gainMin, gainMax) = dto.RecommendedTotalGainMin.HasValue && dto.RecommendedTotalGainMax.HasValue
            ? (dto.RecommendedTotalGainMin.Value, dto.RecommendedTotalGainMax.Value)
            : GetIomRecommendation(bmi);

        var goal = new WeightGoalRange
        {
            PregnancyId = pregnancyId,
            HeightCm = heightCm,
            PrePregnancyWeightKg = preWeight,
            Bmi = bmi,
            RecommendedTotalGainMin = gainMin,
            RecommendedTotalGainMax = gainMax,
            Notes = dto.Notes
        };

        await _unitOfWork.WeightGoalRanges.AddAsync(goal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToGoalDto(goal);
    }

    public async Task<WeightGoalDto?> GetGoalAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        return goal == null ? null : MapToGoalDto(goal);
    }

    public async Task<WeightGoalDto> UpdateGoalAsync(Guid id, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default)
    {
        var goal = await _unitOfWork.WeightGoalRanges.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight goal not found.");

        await VerifyPregnancyOwnership(goal.PregnancyId, userId, ct);

        if (dto.HeightCm.HasValue) goal.HeightCm = dto.HeightCm;
        if (dto.PrePregnancyWeightKg.HasValue) goal.PrePregnancyWeightKg = dto.PrePregnancyWeightKg;
        goal.Bmi = CalculateBmi(goal.PrePregnancyWeightKg, goal.HeightCm);

        if (dto.RecommendedTotalGainMin.HasValue) goal.RecommendedTotalGainMin = dto.RecommendedTotalGainMin;
        if (dto.RecommendedTotalGainMax.HasValue) goal.RecommendedTotalGainMax = dto.RecommendedTotalGainMax;
        if (dto.Notes != null) goal.Notes = dto.Notes;

        _unitOfWork.WeightGoalRanges.Update(goal);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToGoalDto(goal);
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT ALERTS
    // ═══════════════════════════════════════════════════

    public async Task<List<WeightAlertDto>> GetAlertsAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var alerts = await _unitOfWork.WeightAlerts.GetByPregnancyIdAsync(pregnancyId, ct);
        return alerts.Select(MapToAlertDto).ToList();
    }

    public async Task<WeightAlertDto> ResolveAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default)
    {
        var alert = await _unitOfWork.WeightAlerts.GetByIdAsync(alertId, ct)
            ?? throw new NotFoundException("Weight alert not found.");

        await VerifyPregnancyOwnership(alert.PregnancyId, userId, ct);

        if (alert.ResolvedAt.HasValue)
            throw new BadRequestException("Alert is already resolved.");

        alert.ResolvedAt = DateTime.UtcNow;
        _unitOfWork.WeightAlerts.Update(alert);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToAlertDto(alert);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════

    private async Task<Pregnancy> VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
        return pregnancy;
    }

    private async Task CheckAndCreateAlerts(Guid pregnancyId, WeightLog newLog, CancellationToken ct)
    {
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        if (goal == null) return;

        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: ct);
        if (pregnancy == null) return;

        // Check total gain vs recommended range
        if (goal.PrePregnancyWeightKg.HasValue)
        {
            var totalGain = newLog.WeightKg - goal.PrePregnancyWeightKg.Value;

            if (goal.RecommendedTotalGainMax.HasValue && totalGain > goal.RecommendedTotalGainMax.Value)
            {
                await CreateAlert(pregnancyId, WeightAlertType.AboveRange,
                    $"{{\"currentWeight\":{newLog.WeightKg},\"totalGain\":{totalGain},\"maxRecommended\":{goal.RecommendedTotalGainMax}}}", ct);
            }
            else if (goal.RecommendedTotalGainMin.HasValue && totalGain < goal.RecommendedTotalGainMin.Value
                     && pregnancy.CurrentGestationalWeek >= 37) // Only alert below range near term
            {
                await CreateAlert(pregnancyId, WeightAlertType.BelowRange,
                    $"{{\"currentWeight\":{newLog.WeightKg},\"totalGain\":{totalGain},\"minRecommended\":{goal.RecommendedTotalGainMin}}}", ct);
            }
        }

        // Check rapid gain/loss — compare with log from ~1 week ago (7–14 day window)
        // Fetch recent logs, skip the current one (index 0), find the first one >= 7 days apart
        var recentLogs = await _unitOfWork.WeightLogs.GetRecentByPregnancyIdAsync(pregnancyId, 15, ct);
        var compareLog = recentLogs
            .Skip(1) // skip current log (newest)
            .FirstOrDefault(log =>
            {
                var diff = (newLog.LoggedOn.ToDateTime(TimeOnly.MinValue) - log.LoggedOn.ToDateTime(TimeOnly.MinValue)).TotalDays;
                return diff >= 7 && diff <= 14;
            });

        if (compareLog != null)
        {
            var daysDiff = (newLog.LoggedOn.ToDateTime(TimeOnly.MinValue) - compareLog.LoggedOn.ToDateTime(TimeOnly.MinValue)).TotalDays;
            var weeklyGain = (newLog.WeightKg - compareLog.WeightKg) / (decimal)(daysDiff / 7.0);
            if (weeklyGain > 0.7m)
            {
                // Cooldown: only create alert if no RapidGain alert in the last 7 days
                if (!await _unitOfWork.WeightAlerts.HasRecentAlertAsync(pregnancyId, WeightAlertType.RapidGain, 7, ct))
                {
                    await CreateAlert(pregnancyId, WeightAlertType.RapidGain,
                        $"{{\"weeklyGain\":{weeklyGain:F2},\"currentWeight\":{newLog.WeightKg},\"previousWeight\":{compareLog.WeightKg},\"daysBetween\":{daysDiff}}}", ct);
                }
            }
            else if (weeklyGain < -0.3m)
            {
                if (!await _unitOfWork.WeightAlerts.HasRecentAlertAsync(pregnancyId, WeightAlertType.RapidLoss, 7, ct))
                {
                    await CreateAlert(pregnancyId, WeightAlertType.RapidLoss,
                        $"{{\"weeklyChange\":{weeklyGain:F2},\"currentWeight\":{newLog.WeightKg},\"previousWeight\":{compareLog.WeightKg},\"daysBetween\":{daysDiff}}}", ct);
                }
            }
        }
    }

    private async Task CreateAlert(Guid pregnancyId, WeightAlertType type, string detailsJson, CancellationToken ct)
    {
        var alert = new WeightAlert
        {
            PregnancyId = pregnancyId,
            AlertType = type,
            TriggeredAt = DateTime.UtcNow,
            DetailsJson = detailsJson
        };
        await _unitOfWork.WeightAlerts.AddAsync(alert, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static decimal? CalculateBmi(decimal? weightKg, decimal? heightCm)
    {
        if (!weightKg.HasValue || !heightCm.HasValue || heightCm.Value == 0) return null;
        var heightM = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (heightM * heightM), 2);
    }

    private static (decimal min, decimal max) GetIomRecommendation(decimal? bmi)
    {
        if (!bmi.HasValue) return (11.5m, 16.0m); // default Normal
        return bmi.Value switch
        {
            < 18.5m => (12.5m, 18.0m),   // Underweight
            < 25.0m => (11.5m, 16.0m),   // Normal
            < 30.0m => (7.0m, 11.5m),    // Overweight
            _       => (5.0m, 9.0m)      // Obese
        };
    }

    private static string GetBmiCategory(decimal? bmi)
    {
        if (!bmi.HasValue) return "Unknown";
        return bmi.Value switch
        {
            < 18.5m => "Underweight",
            < 25.0m => "Normal",
            < 30.0m => "Overweight",
            _       => "Obese"
        };
    }

    private static WeightLogDto MapToDto(WeightLog w, decimal? prePregnancyWeight) => new(
        w.Id, w.PregnancyId, w.LoggedOn, w.WeightKg, w.Note, w.Source.ToString(),
        prePregnancyWeight.HasValue ? w.WeightKg - prePregnancyWeight.Value : null,
        w.CreatedAt, w.UpdatedAt);

    private static WeightGoalDto MapToGoalDto(WeightGoalRange g) => new(
        g.Id, g.PregnancyId, g.HeightCm, g.PrePregnancyWeightKg, g.Bmi,
        GetBmiCategory(g.Bmi),
        g.RecommendedTotalGainMin, g.RecommendedTotalGainMax,
        g.Notes, g.CreatedAt, g.UpdatedAt);

    private static WeightAlertDto MapToAlertDto(WeightAlert a) => new(
        a.Id, a.PregnancyId, a.AlertType.ToString(), a.TriggeredAt,
        a.DetailsJson, a.ResolvedAt, a.ResolvedAt.HasValue);
}
```

**Code — MotivationalService** (Application Layer):

```csharp
// File: FPT.EXE201.Application/Services/MotivationalService.cs
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class MotivationalService : IMotivationalService
{
    private readonly IUnitOfWork _unitOfWork;

    public MotivationalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MotivationalTemplateDto>> GetByWeekAsync(
        int week, string? category = null, string langCode = "vi", CancellationToken ct = default)
    {
        var templates = await _unitOfWork.MotivationalTemplates
            .GetByWeekAsync(week, category, langCode, ct);

        return templates.Select(t =>
        {
            var translation = t.Translations.FirstOrDefault();
            return new MotivationalTemplateDto(
                t.Id,
                t.Category.ToString(),
                t.WeekStart,
                t.WeekEnd,
                t.VariablesJson,
                translation?.Title,
                translation?.Message ?? string.Empty
            );
        }).ToList();
    }
}
```

**✅ Checkpoint**: Build thành công. Services handle: ownership check, duplicate date, BMI calculation, IOM guidelines, alert auto-generation, chart data, resolve alert. OCR extraction delegates to `IWeightOcrService` (Infrastructure layer).

**Code — WeightOcrService** (Infrastructure Layer):

```csharp
// ═══════════════════════════════════════════════════
// File: FPT.EXE201.Infrastructure/Services/WeightOcrService.cs
// ═══════════════════════════════════════════════════
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using System.Text.RegularExpressions;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// OCR weight extraction — Infrastructure implementation.
/// Wraps IOcrProvider (AzureOcrProvider) + regex parsing logic.
/// Keeps Application layer clean — WeightLogService chỉ gọi IWeightOcrService.
/// </summary>
public class WeightOcrService : IWeightOcrService
{
    private readonly IOcrProvider _ocrProvider;

    public WeightOcrService(IOcrProvider ocrProvider)
    {
        _ocrProvider = ocrProvider;
    }

    public async Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Stream imageStream, string fileName, CancellationToken ct = default)
    {
        // Determine content type from file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new BadRequestException("Only JPEG and PNG images are supported.")
        };

        try
        {
            // Reuse IOcrProvider (AzureOcrProvider) — same provider used for medical record OCR
            var ocrRequest = new OcrRequest(imageStream, fileName, contentType, "vi");
            var ocrResponse = await _ocrProvider.ExtractTextAsync(ocrRequest, ct);

            if (string.IsNullOrWhiteSpace(ocrResponse.RawText))
            {
                return new WeightOcrExtractResultDto(
                    Success: false,
                    ExtractedWeightKg: null,
                    RawOcrText: null,
                    ConfidenceScore: ocrResponse.ConfidenceScore,
                    Message: "Không nhận diện được text từ ảnh. Vui lòng chụp rõ hơn."
                );
            }

            // Parse weight from OCR text using regex
            var extractedWeight = ParseWeightFromText(ocrResponse.RawText);

            if (!extractedWeight.HasValue)
            {
                return new WeightOcrExtractResultDto(
                    Success: false,
                    ExtractedWeightKg: null,
                    RawOcrText: ocrResponse.RawText,
                    ConfidenceScore: ocrResponse.ConfidenceScore,
                    Message: "Nhận diện được text nhưng không tìm thấy giá trị cân nặng hợp lệ (30–200 kg). Vui lòng chụp lại."
                );
            }

            return new WeightOcrExtractResultDto(
                Success: true,
                ExtractedWeightKg: extractedWeight.Value,
                RawOcrText: ocrResponse.RawText,
                ConfidenceScore: ocrResponse.ConfidenceScore,
                Message: $"Trích xuất thành công: {extractedWeight.Value} kg. Vui lòng xác nhận."
            );
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            return new WeightOcrExtractResultDto(
                Success: false,
                ExtractedWeightKg: null,
                RawOcrText: null,
                ConfidenceScore: null,
                Message: $"Lỗi khi xử lý ảnh: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Parse weight value from OCR raw text using 4-tier regex.
    /// </summary>
    private static decimal? ParseWeightFromText(string text)
    {
        // Pattern 1: "65.5 kg" or "65.5kg" — most common on digital scales
        var match1 = Regex.Match(text, @"(\d{2,3}\.?\d{0,2})\s*kg", RegexOptions.IgnoreCase);
        if (match1.Success && decimal.TryParse(match1.Groups[1].Value, out var w1) && w1 >= 30 && w1 <= 200)
            return w1;

        // Pattern 2: "Weight: 65.5" or "Cân nặng: 65.5" — labeled format
        var match2 = Regex.Match(text, @"(?:weight|cân\s*nặng|wt)[:\s]+(\d{2,3}\.?\d{0,2})", RegexOptions.IgnoreCase);
        if (match2.Success && decimal.TryParse(match2.Groups[1].Value, out var w2) && w2 >= 30 && w2 <= 200)
            return w2;

        // Pattern 3: Standalone decimal number in valid range (30–200)
        var matches = Regex.Matches(text, @"\b(\d{2,3}\.\d{1,2})\b");
        foreach (Match m in matches)
        {
            if (decimal.TryParse(m.Groups[1].Value, out var w3) && w3 >= 30 && w3 <= 200)
                return w3;
        }

        // Pattern 4: Integer-only fallback (e.g. "65")
        var matches4 = Regex.Matches(text, @"\b(\d{2,3})\b");
        foreach (Match m in matches4)
        {
            if (decimal.TryParse(m.Groups[1].Value, out var w4) && w4 >= 30 && w4 <= 200)
                return w4;
        }

        return null;
    }
}
```

---

## 🎯 PROMPT 8/8 — Controllers + DI Registration

**Nhiệm vụ**: Tạo controllers + register services trong DI.

**Code — Controllers**:

```csharp
// File: FPT.EXE201.Api/Controllers/WeightLogsController.cs
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
[Authorize]
public class WeightLogsController : BaseApiController
{
    private readonly IWeightLogService _weightLogService;

    public WeightLogsController(IWeightLogService weightLogService)
    {
        _weightLogService = weightLogService;
    }

    // ═══ OCR Weight Extraction ═══

    /// <summary>
    /// Upload ảnh chụp cân → OCR trích xuất cân nặng → trả về cho FE confirm.
    /// Flow: FE upload ảnh → BE OCR → trả extractedWeightKg → FE hiển thị cho user xác nhận
    ///       → Nếu confirm → FE gọi POST /weight-logs với Source=WeightSource.OCR + WeightKg
    ///       → Nếu không → FE cho user chụp lại ảnh
    /// </summary>
    [HttpPost("pregnancies/{pregnancyId:guid}/weight-logs/extract-weight")]
    [RequirePermission("weight_log.write")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExtractWeight(
        Guid pregnancyId, IFormFile image, CancellationToken ct)
    {
        // Validate image file — throw exceptions (NOT return BadRequest)
        if (image == null || image.Length == 0)
            throw new BadRequestException("Image file is required.");

        // Validate format
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new BadRequestException("Only JPEG and PNG images are allowed.");

        // Validate size (max 5 MB)
        if (image.Length > 5 * 1024 * 1024)
            throw new BadRequestException("Image size must not exceed 5 MB.");

        using var stream = image.OpenReadStream();
        var result = await _weightLogService.ExtractWeightFromImageAsync(
            pregnancyId, GetCurrentUserId(), stream, image.FileName, ct);

        return Success(result, result.Message);
    }

    // ═══ Weight Logs ═══

    [HttpPost("pregnancies/{pregnancyId:guid}/weight-logs")]
    [RequirePermission("weight_log.write")]
    public async Task<IActionResult> Create(
        Guid pregnancyId, [FromBody] CreateWeightLogDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Weight log recorded successfully");
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-logs")]
    [RequirePermission("weight_log.read")]
    public async Task<IActionResult> GetByPregnancy(
        Guid pregnancyId, [FromQuery] QueryOptions options, CancellationToken ct)
    {
        var result = await _weightLogService.GetByPregnancyIdPagedAsync(
            pregnancyId, GetCurrentUserId(), options, ct);
        return Success(result);
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-logs/chart")]
    [RequirePermission("weight_log.read")]
    public async Task<IActionResult> GetChartData(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetChartDataAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-logs/{id:guid}")]
    [RequirePermission("weight_log.write")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWeightLogDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Weight log updated successfully");
    }

    [HttpDelete("weight-logs/{id:guid}")]
    [RequirePermission("weight_log.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _weightLogService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Weight log deleted successfully");
    }

    // ═══ Weight Goals ═══

    [HttpPost("pregnancies/{pregnancyId:guid}/weight-goals")]
    [RequirePermission("weight_goal.write")]
    public async Task<IActionResult> CreateGoal(
        Guid pregnancyId, [FromBody] CreateWeightGoalDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.CreateGoalAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Weight goal set successfully");
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-goals")]
    [RequirePermission("weight_goal.read")]
    public async Task<IActionResult> GetGoal(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetGoalAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-goals/{id:guid}")]
    [RequirePermission("weight_goal.write")]
    public async Task<IActionResult> UpdateGoal(
        Guid id, [FromBody] CreateWeightGoalDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.UpdateGoalAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Weight goal updated successfully");
    }

    // ═══ Weight Alerts ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-alerts")]
    [RequirePermission("weight_alert.read")]
    public async Task<IActionResult> GetAlerts(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetAlertsAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-alerts/{id:guid}/resolve")]
    [RequirePermission("weight_alert.resolve")]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken ct)
    {
        var result = await _weightLogService.ResolveAlertAsync(id, GetCurrentUserId(), ct);
        return Success(result, "Alert resolved successfully");
    }
}

// File: FPT.EXE201.Api/Controllers/MotivationalController.cs
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

/// <summary>
/// Public API — giống RefDataController.
/// Motivational templates là public content, không cần login.
/// Mọi user (kể cả chưa đăng nhập) đều xem được nội dung động viên.
/// </summary>
[Route("api/motivational")]
public class MotivationalController : BaseApiController
{
    private readonly IMotivationalService _motivationalService;

    public MotivationalController(IMotivationalService motivationalService)
    {
        _motivationalService = motivationalService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int week,
        [FromQuery] string? category = null,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var result = await _motivationalService.GetByWeekAsync(week, category, lang, ct);
        return Success(result);
    }
}
```

**DI Registration**:

```csharp
// File: FPT.EXE201.Application/DependencyInjection.cs — thêm:
services.AddScoped<IWeightLogService, WeightLogService>();
services.AddScoped<IMotivationalService, MotivationalService>();

// File: FPT.EXE201.Infrastructure/DependencyInjection.cs — thêm:
// ⚠️ WeightOcrService ở Infrastructure vì cần IOcrProvider (registered via AddHttpClient<>)
services.AddScoped<IWeightOcrService, WeightOcrService>();
```

**✅ Checkpoint**: Build thành công. 0 errors, 0 warnings.

---

## ✅ WEEK 6 COMPLETION CHECKLIST

- [ ] **Prompt 1**: Domain entities + Enums (3 enums, 5 entities)
- [ ] **Prompt 2**: EF Configurations (5 configs) + DbSet in AppDbContext
- [ ] **Prompt 3**: Migration + Seed Data (30 motivational templates × 2 lang = 60 translations)
- [ ] **Prompt 4**: DTOs (record) + FluentValidation (3 validators)
- [ ] **Prompt 5**: Repositories (4 interfaces + 4 implementations) + UnitOfWork
- [ ] **Prompt 6**: QuerySpec (WeightLogListQuerySpec) + QuerySpecRegistry
- [ ] **Prompt 7**: Services (IWeightLogService + IMotivationalService + implementations) — bao gồm `ExtractWeightFromImageAsync` (tái sử dụng `IOcrProvider`)
- [ ] **Prompt 8**: Controllers (WeightLogsController + MotivationalController) + DI — bao gồm `POST /extract-weight` endpoint

**📊 Final Statistics**:

| Category | Count |
|----------|-------|
| **Enums** | 3 (WeightSource, WeightAlertType, MotivationalCategory) |
| **Entities** | 5 (WeightLog, WeightGoalRange, WeightAlert, MotivationalTemplate, MotivationalTemplateTranslation) |
| **Tables** | 5 (weight_logs, weight_goal_ranges, weight_alerts, motivational_templates, motivational_template_translations) |
| **EF Configurations** | 5 |
| **DTOs** | 9 records (+WeightOcrExtractResultDto) |
| **Validators** | 3 |
| **Repository Interfaces** | 4 |
| **Repository Implementations** | 4 |
| **Service Interfaces** | 3 (IWeightLogService, IMotivationalService, IWeightOcrService) |
| **Service Implementations** | 3 (WeightLogService, MotivationalService, WeightOcrService) |
| **Controllers** | 2 |
| **API Endpoints** | 11 (+POST extract-weight) |
| **Permissions** | 7 (weight_log.read/write/delete, weight_goal.read/write, weight_alert.read/resolve) — motivational = public API |
| **Seed Data** | 30 motivational templates × 2 languages = 60 translations |
| **Reused from Week 4-5** | IOcrProvider (AzureOcrProvider) — Azure Document Intelligence |

**🔗 Key Integration Points**:

| From | To | Relationship |
|------|-----|-------------|
| WeightLog | Pregnancy | N:1 via `pregnancy_id` |
| WeightGoalRange | Pregnancy | 1:1 via `pregnancy_id` (unique) |
| WeightAlert | Pregnancy | N:1 via `pregnancy_id` |
| WeightGoalRange | Pregnancy.PrePregnancyWeightKg + HeightCm | Read baseline for BMI calculation |
| WeightLog.Create | WeightAlert auto-check | After logging, check rapid gain/loss/range |
| MotivationalTemplate | Pregnancy.CurrentGestationalWeek | Filter templates by current week |

---

## 📝 BUSINESS LOGIC SUMMARY

### IOM Weight Gain Guidelines (auto-applied)

| Pre-pregnancy BMI | BMI Range | Recommended Total Gain (kg) |
|---|---|---|
| Underweight | < 18.5 | 12.5 – 18.0 |
| Normal | 18.5 – 24.9 | 11.5 – 16.0 |
| Overweight | 25.0 – 29.9 | 7.0 – 11.5 |
| Obese | ≥ 30.0 | 5.0 – 9.0 |

### Alert Logic (runs after each weight log)

| Alert Type | Trigger Condition | Cooldown |
|---|---|---|
| `RapidGain` | Weekly gain > 0.7 kg (7–14 day comparison window) | 7 ngày |
| `RapidLoss` | Weekly loss > 0.3 kg (7–14 day comparison window) | 7 ngày |
| `AboveRange` | Total gain > recommended max | Không |
| `BelowRange` | Total gain < recommended min AND gestational week ≥ 37 | Không |

**Comparison logic**: Fetch 15 recent logs → skip newest → find first log in 7–14 day window → calculate `weeklyGain = (current - previous) / (daysDiff / 7.0)` → check cooldown via `HasRecentAlertAsync` before creating alert.

**detailsJson** includes `daysBetween` field for RapidGain/RapidLoss alerts.

### OCR Weight Extraction Flow

```
┌─────────┐     POST /extract-weight     ┌──────────────────┐
│   FE    │ ────── IFormFile (ảnh) ─────→ │ WeightLogsCtrl   │
│ (chụp   │                               │   ExtractWeight  │
│  ảnh    │                               └────────┬─────────┘
│  cân)   │                                        │
│         │                                        ▼
│         │                             ┌──────────────────────┐
│         │                             │ WeightLogService     │
│         │                             │ ExtractWeightFrom    │
│         │                             │ ImageAsync()         │
│         │                             └────────┬─────────────┘
│         │                                      │
│         │                                      ▼
│         │                             ┌──────────────────────┐
│         │                             │ IOcrProvider         │
│         │                             │ (AzureOcrProvider)   │
│         │                             │ ExtractTextAsync()   │
│         │                             │ → Azure Doc Intel    │
│         │                             └────────┬─────────────┘
│         │                                      │
│         │                                      ▼
│         │                             ┌──────────────────────┐
│         │                             │ ParseWeightFromText  │
│         │                             │ (regex 4 tầng)       │
│         │                             │ → decimal? weight    │
│         │                             └────────┬─────────────┘
│         │     WeightOcrExtractResultDto         │
│         │ ◄──── { success, extractedWeightKg,   │
│         │        confidenceScore, message }  ◄──┘
│         │
│  User   │  ┌── confirm? ──┐
│  thấy   │  │              │
│  65.5kg │  │  YES         │  NO
│         │  ▼              ▼
│  POST /weight-logs    Chụp lại ảnh
│  { Source: "OCR",     (gọi lại /extract-weight)
│    WeightKg: 65.5 }
└─────────┘
```

| Regex Tier | Pattern | Example Match |
|---|---|---|
| 1 (có đơn vị) | `\d{2,3}\.?\d{0,2}\s*kg` | "65.5 kg", "65.5kg" |
| 2 (có label) | `weight\|cân nặng` + số | "Cân nặng: 65.5" |
| 3 (số thập phân) | `\b\d{2,3}\.\d{1,2}\b` trong range 30–200 | "65.50" |
| 4 (số nguyên) | `\b\d{2,3}\b` trong range 30–200 | "65" |

### Weight Chart Data Structure

```json
{
  "prePregnancyWeightKg": 55.0,
  "recommendedGainMin": 11.5,
  "recommendedGainMax": 16.0,
  "currentWeightKg": 65.5,
  "totalGainKg": 10.5,
  "totalEntries": 45,
  "dataPoints": [
    { "date": "2025-12-15", "weightKg": 55.2, "gestationalWeek": 6 },
    { "date": "2025-12-22", "weightKg": 55.5, "gestationalWeek": 7 },
    ...
  ]
}
```

---

## 🔗 FILES REFERENCE

```
src/
├── FPT.EXE201.Domain/
│   ├── Entities/
│   │   ├── WeightLog.cs                          ← BaseEntity
│   │   ├── WeightGoalRange.cs                    ← BaseEntity
│   │   ├── WeightAlert.cs                        ← NO BaseEntity (immutable)
│   │   ├── MotivationalTemplate.cs               ← BaseEntity
│   │   └── MotivationalTemplateTranslation.cs    ← NO BaseEntity (composite PK)
│   └── Enums/
│       ├── WeightSource.cs
│       ├── WeightAlertType.cs
│       └── MotivationalCategory.cs
│
├── FPT.EXE201.Application/
│   ├── DTOs/WeightTracking/
│   │   ├── CreateWeightLogDto.cs                 ← record
│   │   ├── UpdateWeightLogDto.cs                 ← record
│   │   ├── WeightLogDto.cs                       ← record (+ computed WeightGainFromBaseline)
│   │   ├── WeightChartDataDto.cs                 ← record (+ WeightChartPointDto)
│   │   ├── CreateWeightGoalDto.cs                ← record
│   │   ├── WeightGoalDto.cs                      ← record (+ computed BmiCategory)
│   │   ├── WeightAlertDto.cs                     ← record (+ computed IsResolved)
│   │   ├── MotivationalTemplateDto.cs            ← record
│   │   └── WeightOcrExtractResultDto.cs          ← record (OCR extraction result for FE confirm)
│   ├── Validations/WeightTracking/
│   │   ├── CreateWeightLogDtoValidator.cs
│   │   ├── UpdateWeightLogDtoValidator.cs
│   │   └── CreateWeightGoalDtoValidator.cs
│   ├── Features/WeightLogs/
│   │   └── WeightLogListQuerySpec.cs
│   ├── IRepositories/
│   │   ├── IWeightLogRepository.cs
│   │   ├── IWeightGoalRangeRepository.cs
│   │   ├── IWeightAlertRepository.cs             ← standalone (NOT IGenericRepository)
│   │   └── IMotivationalTemplateRepository.cs
│   ├── IServices/
│   │   ├── IWeightLogService.cs                  ← includes ExtractWeightFromImageAsync
│   │   ├── IWeightOcrService.cs                  ← OCR interface (impl in Infrastructure)
│   │   └── IMotivationalService.cs
│   └── Services/
│       ├── WeightLogService.cs                   ← BMI calc + IOM + alert logic (delegates OCR to IWeightOcrService)
│       └── MotivationalService.cs
│
├── FPT.EXE201.Infrastructure/
│   ├── Services/
│   │   └── WeightOcrService.cs                   ← IOcrProvider wrapper + regex parse weight
│   ├── Configurations/
│   │   ├── WeightLogConfiguration.cs
│   │   ├── WeightGoalRangeConfiguration.cs
│   │   ├── WeightAlertConfiguration.cs           ← NO query filter
│   │   ├── MotivationalTemplateConfiguration.cs
│   │   └── MotivationalTemplateTranslationConfiguration.cs
│   ├── Persistence/Seeders/
│   │   └── MotivationalTemplateSeeder.cs         ← 30 templates × 2 lang
│   └── Repositories/
│       ├── WeightLogRepository.cs
│       ├── WeightGoalRangeRepository.cs
│       ├── WeightAlertRepository.cs              ← standalone (NOT GenericRepository)
│       └── MotivationalTemplateRepository.cs
│
└── FPT.EXE201.Api/Controllers/
    ├── WeightLogsController.cs                   ← Logs + Goals + Alerts + OCR extract (11 endpoints)
    └── MotivationalController.cs                 ← GET by week (1 endpoint)
```

---

## 🎯 END OF WEEK 6 PROMPTS GUIDE
