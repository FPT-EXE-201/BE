# WEEK 3 PROMPTS GUIDE — Pregnancy Core Module (FINAL v2)

> ⚠️ **Database Convention**: Project sử dụng **CHAR(36)** để lưu Guid, KHÔNG dùng BINARY(16).  
> ⚠️ **Enum Convention**: Dùng `JsonStringEnumConverter` — enum serialize thành string.  
> ⚠️ **Naming Convention**: Property names phải self-documenting + có XML comment giải thích thuật ngữ y khoa.  
> ⚠️ **Exception Handling**: Services throw exceptions (`NotFoundException`, `BadRequestException`, `ConflictException`...), `GlobalExceptionFilter` xử lý thành `ApiResponse`.  
> ⚠️ **RBAC**: Dùng `[RequirePermission("permission.code")]` trong Controller.  
> ⚠️ **Soft Delete**: Dùng `deleted_at` timestamp + global query filter trong `AppDbContext.OnModelCreating`. Repository cũng filter manual (belt-and-suspenders). KHÔNG hard delete.  
> ⚠️ **Seed Data**: Dùng anonymous type + fixed DateTime, KHÔNG dùng entity instance.  
> ⚠️ **LangCode**: Lowercase match Week 1 (`"vi"`, `"en"`).  
> ⚠️ **Column Mapping**: C# property dùng tên rõ nghĩa (`LastMenstrualPeriodDate`), DB column dùng tên ngắn (`lmp_date`). Mapping qua `.HasColumnName()`.  
> ⚠️ **Controller**: Kế thừa `BaseApiController`, dùng `Success()`, `Created()`, `GetCurrentUserId()`. KHÔNG tự viết `ApiResponse`.  
> ⚠️ **Repository**: Kế thừa `GenericRepository<T>`, lazy init trong `UnitOfWork` qua `??=` pattern.  
> ⚠️ **DbContext**: Dùng `AppDbContext`, KHÔNG phải `ApplicationDbContext`. Configurations auto-apply qua `ApplyConfigurationsFromAssembly`.  

---

## 📋 THUẬT NGỮ Y KHOA — ĐỌC TRƯỚC

```
╔══════════════════════════════════════════════════════════════════════╗
║ THUẬT NGỮ         │ VIẾT TẮT │ GIẢI THÍCH                          ║
╠═══════════════════╪══════════╪══════════════════════════════════════╣
║ Last Menstrual    │ LMP      │ Ngày đầu tiên của kỳ kinh cuối     ║
║ Period            │          │ → Dùng để tính tuổi thai             ║
║                   │          │                                      ║
║ Expected Delivery │ EDD      │ Ngày dự sinh                        ║
║ Date              │          │ = LMP + 280 ngày (Naegele's rule)   ║
║                   │          │                                      ║
║ Gestational Age   │ GA       │ Tuổi thai (tuần + ngày)             ║
║                   │          │ = (Hôm nay - LMP) ÷ 7              ║
║                   │          │ Ví dụ: 28w3d = 28 tuần 3 ngày      ║
║                   │          │                                      ║
║ Trimester         │          │ Tam cá nguyệt                       ║
║                   │          │ 1st: tuần 0-13                      ║
║                   │          │ 2nd: tuần 14-27                     ║
║                   │          │ 3rd: tuần 28-42+                    ║
║                   │          │                                      ║
║ Prenatal          │          │ Trước sinh (prenatal visit = khám   ║
║                   │          │ thai, prenatal test = xét nghiệm    ║
║                   │          │ trong thai kỳ)                      ║
║                   │          │                                      ║
║ Preeclampsia      │          │ Tiền sản giật — huyết áp cao +     ║
║                   │          │ protein niệu sau tuần 20            ║
║                   │          │                                      ║
║ Gestational       │ GDM      │ Tiểu đường thai kỳ — đường huyết  ║
║ Diabetes          │          │ cao phát hiện trong thai kỳ         ║
║                   │          │                                      ║
║ OGTT              │          │ Oral Glucose Tolerance Test —       ║
║                   │          │ Nghiệm pháp dung nạp glucose       ║
║                   │          │                                      ║
║ CBC               │          │ Complete Blood Count —              ║
║                   │          │ Công thức máu toàn phần             ║
║                   │          │                                      ║
║ NT Scan           │          │ Nuchal Translucency — Đo độ mờ     ║
║                   │          │ da gáy (tầm soát dị tật)            ║
╚══════════════════════════════════════════════════════════════════════╝
```

---

## 📋 CONTEXT

### Week 3 Overview

**Mục tiêu**: Pregnancy Core — quản lý thai kỳ, bệnh lý, khám thai, xét nghiệm.

**Database Tables** (8 tables):
1. `pregnancies` — Hồ sơ thai kỳ
2. `ref_pregnancy_conditions` — Danh mục bệnh lý (master)
3. `ref_pregnancy_condition_translations` — Tên bệnh lý đa ngôn ngữ
4. `pregnancy_conditions` — Bệnh lý của từng thai kỳ
5. `prenatal_visits` — Lịch khám thai
6. `ref_test_types` — Danh mục xét nghiệm (master)
7. `ref_test_type_translations` — Tên xét nghiệm đa ngôn ngữ
8. `prenatal_tests` — Kết quả xét nghiệm

**Property Naming Rule**:

```
C# Property (rõ nghĩa)          │ DB Column (ngắn gọn)
─────────────────────────────────┼──────────────────────
LastMenstrualPeriodDate          │ lmp_date
ExpectedDeliveryDate             │ edd_date
EstimatedConceptionDate          │ conception_date
CurrentGestationalWeek           │ current_week
PregnancyNumber                  │ pregnancy_no
DiagnosedDate                    │ diagnosed_at
VisitDateTime                    │ visit_at
TestDateTime                     │ test_at
IsAbnormalResult                 │ abnormal_flag
LanguageCode                     │ lang_code
DisplayName                      │ name
```

**API Endpoints**:
```
# Pregnancy CRUD
POST   /api/pregnancies                          → Tạo thai kỳ mới
GET    /api/pregnancies                          → List tất cả pregnancies của user
GET    /api/pregnancies/active                   → Lấy thai kỳ đang active
GET    /api/pregnancies/{id}                     → Lấy chi tiết 1 thai kỳ
PUT    /api/pregnancies/{id}                     → Update thông tin thai kỳ
PATCH  /api/pregnancies/{id}/status              → Đổi trạng thái (Active → Delivered/Ended/Miscarriage)
DELETE /api/pregnancies/{id}                     → Soft delete thai kỳ

# Pregnancy Conditions
POST   /api/pregnancies/{id}/conditions          → Gán bệnh lý cho thai kỳ
GET    /api/pregnancies/{id}/conditions           → List bệnh lý của thai kỳ
DELETE /api/pregnancies/{pregnancyId}/conditions/{id} → Xóa bệnh lý

# Prenatal Visits
POST   /api/pregnancies/{id}/visits              → Tạo lịch khám
GET    /api/pregnancies/{id}/visits              → List lịch khám
PUT    /api/visits/{id}                          → Update lịch khám
DELETE /api/visits/{id}                          → Soft delete lịch khám

# Prenatal Tests
POST   /api/pregnancies/{id}/tests               → Tạo kết quả xét nghiệm
GET    /api/pregnancies/{id}/tests               → List xét nghiệm
PUT    /api/tests/{id}                           → Update kết quả
DELETE /api/tests/{id}                           → Soft delete xét nghiệm

# Reference Data (public, no auth required)
GET    /api/ref/pregnancy-conditions?lang=vi     → Danh mục bệnh lý
GET    /api/ref/test-types?lang=vi&category=LAB  → Danh mục xét nghiệm
```

**Business Rules**:
- ⚠️ **Unique Constraint**: 1 user chỉ có 1 ACTIVE pregnancy (`user_id + pregnancy_no` unique)
- ⚠️ **Auto-increment**: `pregnancy_no` tự động tăng per user (1, 2, 3...)
- ⚠️ **EDD Calculation**: EDD = LMP + 280 days (Naegele's rule). Có thể điều chỉnh qua UpdatePregnancyDto.
- ⚠️ **Gestational Week**: Calculated from LMP: `(Today - LMP).Days / 7`
- ⚠️ **BMI Computed**: `PrePregnancyBmi = weight / (height/100)^2` — auto-computed trong MapToDto, không lưu DB
- ⚠️ **Obstetric Formula**: `G{gravida}P{para}` — auto-computed, không lưu DB
- ⚠️ **Ownership**: User chỉ được access own pregnancies (`pregnancy.UserId == currentUserId`)
- ⚠️ **State Transition**: Chỉ cho phép `Active → {Delivered, Ended, Miscarriage}`. KHÔNG reverse.
- ⚠️ **Delivery Info**: Khi status = `Delivered` → BắT BUỘC `ActualDeliveryDate`, optional `DeliveryMethod`
- ⚠️ **Condition Unique**: Mỗi condition chỉ gán 1 lần per pregnancy (`pregnancy_id + condition_id` unique)
- ⚠️ **Visit-Test Consistency**: Khi tạo `PrenatalTest` với `VisitId`, phải verify visit thuộc cùng pregnancy

**Existing Codebase Patterns** (PHẢI follow):
- `BaseEntity` → `{ Id, CreatedAt, UpdatedAt, DeletedAt, IsDeleted }` (IsDeleted = computed, ignored by EF)
- `GenericRepository<T>` → methods: `GetByIdAsync`, `GetByIdTrackedAsync`, `AddAsync`, `Update`, `SoftDeleteAsync`...
- `AppDbContext` → global soft delete filter + auto timestamps in `SaveChangesAsync`
- `UnitOfWork` → lazy `??=` pattern
- `BaseApiController` → `Success()`, `Created()`, `GetCurrentUserId()`
- `GlobalExceptionFilter` → catches `NotFoundException`, `BadRequestException`, `ConflictException`, `ForbiddenException`

**Development Workflow**:
1. Prompt 1: Domain entities (Pregnancy, PregnancyStatus enum)
2. Prompt 2: Domain entities (Conditions — Ref + Translation + Assignment)
3. Prompt 3: Domain entities (Visits & Tests — Ref + Translation + Records)
4. Prompt 4: EF Core Configurations (ALL 8 entities)
5. Prompt 5: Migration + Seed Data (10 conditions + 10 test types)
6. Prompt 6: DTOs + FluentValidation
7. Prompt 7: Repository Interfaces + Service Interfaces
8. Prompt 8: Repository Implementations + UnitOfWork update
9. Prompt 9: Service Implementations (business logic)
10. Prompt 10: Controllers + AutoMapper + Permissions + Ref Data endpoint

---

## 🎯 PROMPT 1/10 — Domain Entities: Pregnancy + Enums

**Nhiệm vụ**: Tạo Pregnancy entity và 7 enums.

**Reference SQL**:
```sql
CREATE TABLE pregnancies (
    id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    pregnancy_no INT NOT NULL,
    status ENUM('ACTIVE','ENDED','MISCARRIAGE','DELIVERED') NOT NULL DEFAULT 'ACTIVE',
    lmp_date DATE NULL,
    edd_date DATE NULL,
    conception_date DATE NULL,
    current_week INT NULL,
    notes TEXT NULL,
    -- Nhóm 1: Thông tin bé
    baby_nickname VARCHAR(100) NULL,
    baby_gender ENUM('UNKNOWN','MALE','FEMALE') NOT NULL DEFAULT 'UNKNOWN',
    pregnancy_type ENUM('SINGLETON','TWINS','TRIPLETS','OTHER') NOT NULL DEFAULT 'SINGLETON',
    -- Nhóm 2: Y tế mẹ (baseline cho Weight/Nutrition modules)
    mother_blood_type VARCHAR(10) NULL,
    pre_pregnancy_weight_kg DECIMAL(5,2) NULL,
    height_cm DECIMAL(5,2) NULL,
    -- Nhóm 3: Thai sản chuyên sâu
    due_date_source ENUM('LMP','ULTRASOUND','IVF','MANUAL') NOT NULL DEFAULT 'LMP',
    gravida INT NULL,
    para INT NULL,
    actual_delivery_date DATE NULL,
    delivery_method ENUM('NATURAL','CESAREAN','ASSISTED') NULL,
    cover_image_url VARCHAR(500) NULL,
    -- Timestamps
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (user_id) REFERENCES users(id),
    UNIQUE KEY uk_pregnancies_user_no (user_id, pregnancy_no)
);
```

**Code**:

```csharp
// File: FPT.EXE201.Domain/Enums/PregnancyStatus.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái của thai kỳ.
/// </summary>
public enum PregnancyStatus
{
    /// <summary>Đang mang thai</summary>
    Active,

    /// <summary>Đã kết thúc (không rõ lý do cụ thể)</summary>
    Ended,

    /// <summary>Sảy thai</summary>
    Miscarriage,

    /// <summary>Đã sinh</summary>
    Delivered
}

// File: FPT.EXE201.Domain/Enums/VisitType.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Loại buổi khám thai.
/// </summary>
public enum VisitType
{
    /// <summary>Khám định kỳ theo lịch</summary>
    Routine,

    /// <summary>Khám cấp cứu</summary>
    Emergency,

    /// <summary>Tái khám / theo dõi</summary>
    FollowUp,

    /// <summary>Chỉ làm xét nghiệm (không khám)</summary>
    LabOnly,

    /// <summary>Loại khác</summary>
    Other
}

// File: FPT.EXE201.Domain/Enums/ConditionSeverity.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Mức độ nghiêm trọng của tình trạng bệnh lý thai kỳ.
/// </summary>
public enum ConditionSeverity
{
    /// <summary>Nhẹ — theo dõi, chưa cần can thiệp đặc biệt</summary>
    Mild,

    /// <summary>Trung bình — cần theo dõi sát và có thể cần điều trị</summary>
    Moderate,

    /// <summary>Nặng — cần can thiệp y tế ngay</summary>
    Severe
}

// File: FPT.EXE201.Domain/Enums/BabyGender.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Giới tính em bé. Thường xác định qua siêu âm tuần 16-20.
/// </summary>
public enum BabyGender
{
    /// <summary>Chưa biết / chưa xác định</summary>
    Unknown,

    /// <summary>Nam</summary>
    Male,

    /// <summary>Nữ</summary>
    Female
}

// File: FPT.EXE201.Domain/Enums/PregnancyType.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Loại thai. Ảnh hưởng đến dinh dưỡng, theo dõi cân nặng và mức độ rủi ro.
/// </summary>
public enum PregnancyType
{
    /// <summary>Đơn thai — 1 em bé</summary>
    Singleton,

    /// <summary>Song thai — 2 em bé</summary>
    Twins,

    /// <summary>Tam thai — 3 em bé</summary>
    Triplets,

    /// <summary>Khác (đa thai > 3)</summary>
    Other
}

// File: FPT.EXE201.Domain/Enums/DueDateSource.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Nguồn dùng để xác định ngày dự sinh.
/// Bác sĩ thường điều chỉnh EDD dựa trên siêu âm sớm.
/// </summary>
public enum DueDateSource
{
    /// <summary>Tính từ ngày kinh cuối (Naegele's rule)</summary>
    LMP,

    /// <summary>Điều chỉnh theo siêu âm</summary>
    Ultrasound,

    /// <summary>Thụ tinh trong ống nghiệm — ngày chuyển phôi chính xác</summary>
    IVF,

    /// <summary>Bác sĩ / user tự nhập thủ công</summary>
    Manual
}

// File: FPT.EXE201.Domain/Enums/DeliveryMethod.cs
namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Phương pháp sinh. Lưu khi thai kỳ kết thúc (status = Delivered).
/// </summary>
public enum DeliveryMethod
{
    /// <summary>Sinh thường</summary>
    Natural,

    /// <summary>Sinh mổ</summary>
    Cesarean,

    /// <summary>Sinh hỗ trợ (giác hút, forceps...)</summary>
    Assisted
}
```

```csharp
// File: FPT.EXE201.Domain/Entities/Pregnancy.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Hồ sơ thai kỳ — aggregate root trung tâm của ứng dụng.
/// Mỗi user có thể có nhiều pregnancies (lần mang thai),
/// nhưng chỉ 1 pregnancy ở trạng thái Active tại 1 thời điểm.
/// Mọi module khác (weight, nutrition, documents...) đều gắn vào pregnancy_id.
/// </summary>
public class Pregnancy : BaseEntity
{
    /// <summary>
    /// ID của user sở hữu thai kỳ này.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Số thứ tự lần mang thai của user (1, 2, 3...).
    /// Tự động tăng, unique per user.
    /// </summary>
    public int PregnancyNumber { get; set; }

    /// <summary>
    /// Trạng thái hiện tại: Active → Delivered / Ended / Miscarriage.
    /// Chỉ cho phép chuyển từ Active sang các trạng thái kết thúc.
    /// </summary>
    public PregnancyStatus Status { get; set; } = PregnancyStatus.Active;

    /// <summary>
    /// LMP — Last Menstrual Period Date (Ngày đầu tiên của kỳ kinh cuối cùng).
    /// Đây là mốc quan trọng nhất để tính tuổi thai và ngày dự sinh.
    /// Công thức: Tuổi thai = (Hôm nay - LMP) ÷ 7
    /// </summary>
    public DateTime? LastMenstrualPeriodDate { get; set; }

    /// <summary>
    /// EDD — Expected Delivery Date (Ngày dự sinh).
    /// Auto-calculated: EDD = LMP + 280 ngày (Naegele's rule).
    /// Có thể được bác sĩ điều chỉnh dựa trên siêu âm.
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Ngày thụ thai ước tính.
    /// Thường = LMP + 14 ngày (optional, user có thể không biết).
    /// </summary>
    public DateTime? EstimatedConceptionDate { get; set; }

    /// <summary>
    /// Tuần thai hiện tại (0-45).
    /// Auto-calculated từ LMP: CurrentWeek = (Today - LMP).Days / 7.
    /// Cached value — recalculate mỗi khi đọc.
    /// </summary>
    public int? CurrentGestationalWeek { get; set; }

    /// <summary>
    /// Ghi chú tự do của user về thai kỳ.
    /// </summary>
    public string? Notes { get; set; }

    // ══════════════════════════════════════
    // Nhóm 1: Thông tin bé (Personalization)
    // ══════════════════════════════════════

    /// <summary>
    /// Biệt danh bé. Ví dụ: "Bé Bông", "Cherry".
    /// FE hiển thị trên trang chủ: "Bé Bông tuần thứ 28".
    /// </summary>
    public string? BabyNickname { get; set; }

    /// <summary>
    /// Giới tính em bé: Unknown / Male / Female.
    /// Thường biết từ siêu âm tuần 16-20.
    /// </summary>
    public BabyGender BabyGender { get; set; } = BabyGender.Unknown;

    /// <summary>
    /// Loại thai: Singleton / Twins / Triplets / Other.
    /// Ảnh hưởng đến khuyến nghị dinh dưỡng và mức tăng cân.
    /// </summary>
    public PregnancyType PregnancyType { get; set; } = PregnancyType.Singleton;

    // ══════════════════════════════════════
    // Nhóm 2: Y tế mẹ (Baseline cho Weight/Nutrition)
    // ══════════════════════════════════════

    /// <summary>
    /// Nhóm máu mẹ. Ví dụ: "A+", "O-".
    /// Quan trọng cho phát hiện Rh incompatibility.
    /// </summary>
    public string? MotherBloodType { get; set; }

    /// <summary>
    /// Cân nặng trước mang thai (kg).
    /// Baseline cho module Weight Tracking: tính mức tăng cân phù hợp.
    /// </summary>
    public decimal? PrePregnancyWeightKg { get; set; }

    /// <summary>
    /// Chiều cao mẹ (cm). Dùng để tính BMI baseline trước mang thai.
    /// BMI = weight / (height/100)^2
    /// </summary>
    public decimal? HeightCm { get; set; }

    // ══════════════════════════════════════
    // Nhóm 3: Thai sản chuyên sâu
    // ══════════════════════════════════════

    /// <summary>
    /// Nguồn tính ngày dự sinh: LMP / Ultrasound / IVF / Manual.
    /// Bác sĩ thường điều chỉnh EDD theo siêu âm.
    /// </summary>
    public DueDateSource DueDateSource { get; set; } = DueDateSource.LMP;

    /// <summary>
    /// Gravida — tổng số lần mang thai tính cả lần hiện tại.
    /// Ký hiệu y khoa: G2 = mang thai lần 2.
    /// </summary>
    public int? Gravida { get; set; }

    /// <summary>
    /// Para — tổng số lần sinh (trước lần hiện tại).
    /// Ký hiệu y khoa: P1 = đã sinh 1 lần. → G2P1.
    /// </summary>
    public int? Para { get; set; }

    /// <summary>
    /// Ngày sinh thực tế. Chỉ lưu khi status chuyển sang Delivered.
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Phương pháp sinh: Natural / Cesarean / Assisted.
    /// Chỉ lưu khi status chuyển sang Delivered.
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; set; }

    /// <summary>
    /// Ảnh cover cho hồ sơ thai kỳ (URL ảnh siêu âm, bụng bầu...).
    /// Lưu trữ qua module File Storage (Week 4).
    /// </summary>
    public string? CoverImageUrl { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>User sở hữu thai kỳ.</summary>
    public User User { get; set; } = null!;

    /// <summary>Danh sách bệnh lý được chẩn đoán trong thai kỳ này.</summary>
    public ICollection<PregnancyCondition> Conditions { get; set; } = new List<PregnancyCondition>();

    /// <summary>Danh sách các lần khám thai.</summary>
    public ICollection<PrenatalVisit> Visits { get; set; } = new List<PrenatalVisit>();

    /// <summary>Danh sách kết quả xét nghiệm.</summary>
    public ICollection<PrenatalTest> Tests { get; set; } = new List<PrenatalTest>();
}
```

**✅ Checkpoint**: Build thành công (Visits/Tests/Conditions sẽ có warning vì entity chưa tạo — OK, sẽ tạo ở Prompt 2-3).

---

## 🎯 PROMPT 2/10 — Domain Entities: Conditions (Ref + Translation + Assignment)

**Nhiệm vụ**: Tạo 3 entities liên quan đến bệnh lý thai kỳ.

**Reference SQL**:
```sql
CREATE TABLE ref_pregnancy_conditions (
    id CHAR(36) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL
);

CREATE TABLE ref_pregnancy_condition_translations (
    condition_id CHAR(36) NOT NULL,
    lang_code VARCHAR(5) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT NULL,
    PRIMARY KEY (condition_id, lang_code),
    FOREIGN KEY (condition_id) REFERENCES ref_pregnancy_conditions(id) ON DELETE CASCADE,
    FOREIGN KEY (lang_code) REFERENCES languages(code)
);

CREATE TABLE pregnancy_conditions (
    id CHAR(36) PRIMARY KEY,
    pregnancy_id CHAR(36) NOT NULL,
    condition_id CHAR(36) NOT NULL,
    diagnosed_at DATETIME NULL,
    severity VARCHAR(20) NULL,
    notes TEXT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    FOREIGN KEY (condition_id) REFERENCES ref_pregnancy_conditions(id),
    UNIQUE KEY uk_pregnancy_condition (pregnancy_id, condition_id)
);
```

**Code**:

```csharp
// File: FPT.EXE201.Domain/Entities/RefPregnancyCondition.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục bệnh lý thai kỳ (reference/master data).
/// Đây là bảng lookup — seed sẵn bởi hệ thống, admin quản lý.
/// User KHÔNG tạo mới, chỉ CHỌN từ danh sách này để gán vào thai kỳ.
/// 
/// Ví dụ: GESTATIONAL_DIABETES, PREECLAMPSIA, ANEMIA...
/// </summary>
public class RefPregnancyCondition : BaseEntity
{
    /// <summary>
    /// Mã định danh duy nhất, dùng trong logic nghiệp vụ.
    /// Convention: UPPER_SNAKE_CASE. Ví dụ: "GESTATIONAL_DIABETES".
    /// Không được thay đổi sau khi đã có data reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Cho phép hiển thị trong dropdown hay không.
    /// false = đã ngưng sử dụng nhưng giữ lại cho data cũ.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    /// <summary>Tên hiển thị theo từng ngôn ngữ (VI, EN...).</summary>
    public ICollection<RefPregnancyConditionTranslation> Translations { get; set; }
        = new List<RefPregnancyConditionTranslation>();

    /// <summary>Các thai kỳ đã được chẩn đoán bệnh lý này.</summary>
    public ICollection<PregnancyCondition> PregnancyConditions { get; set; }
        = new List<PregnancyCondition>();
}

// File: FPT.EXE201.Domain/Entities/RefPregnancyConditionTranslation.cs
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho bệnh lý thai kỳ.
/// Composite key: (ConditionId + LanguageCode).
/// 
/// Ví dụ:
///   ConditionId=xxx, lang="vi" → "Tiểu đường thai kỳ"
///   ConditionId=xxx, lang="en" → "Gestational Diabetes"
///   
/// ⚠️ KHÔNG kế thừa BaseEntity — entity này dùng composite primary key.
/// </summary>
public class RefPregnancyConditionTranslation
{
    /// <summary>FK → RefPregnancyCondition.Id</summary>
    public Guid ConditionId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị cho user. Ví dụ: "Tiểu đường thai kỳ".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional). Hiển thị khi user tap xem thêm.</summary>
    public string? Description { get; set; }

    // Navigation
    public RefPregnancyCondition Condition { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/PregnancyCondition.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Ghi nhận một bệnh lý cụ thể cho một thai kỳ cụ thể.
/// Ví dụ: Thai kỳ #1 của Lan được chẩn đoán Tiểu đường thai kỳ vào ngày 15/06.
/// 
/// Business rules:
/// - Mỗi condition chỉ được gán 1 lần per pregnancy (unique: pregnancy_id + condition_id).
/// - Soft delete khi bác sĩ xác nhận chẩn đoán sai.
/// </summary>
public class PregnancyCondition : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ được chẩn đoán bệnh lý này.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>FK → RefPregnancyCondition. Loại bệnh lý (từ danh mục master).</summary>
    public Guid ConditionId { get; set; }

    /// <summary>
    /// Ngày được chẩn đoán bệnh lý này.
    /// Nullable vì user có thể chưa nhớ chính xác ngày.
    /// </summary>
    public DateTime? DiagnosedDate { get; set; }

    /// <summary>
    /// Mức độ nghiêm trọng: Mild / Moderate / Severe.
    /// Nullable vì lúc mới phát hiện có thể chưa đánh giá mức độ.
    /// </summary>
    public ConditionSeverity? Severity { get; set; }

    /// <summary>Ghi chú thêm của user hoặc bác sĩ.</summary>
    public string? Notes { get; set; }

    // Navigation
    /// <summary>Thai kỳ sở hữu condition này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;

    /// <summary>Thông tin bệnh lý từ danh mục master (code, translations).</summary>
    public RefPregnancyCondition Condition { get; set; } = null!;
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 3/10 — Domain Entities: Visits & Tests

**Nhiệm vụ**: Tạo 4 entities: `PrenatalVisit`, `RefTestType`, `RefTestTypeTranslation`, `PrenatalTest`.

**Reference SQL**:
```sql
CREATE TABLE prenatal_visits (
    id CHAR(36) PRIMARY KEY,
    pregnancy_id CHAR(36) NOT NULL,
    doctor_id CHAR(36) NULL,
    visit_at DATETIME NOT NULL,
    visit_type VARCHAR(20) NOT NULL,
    location VARCHAR(200) NULL,
    notes TEXT NULL,
    vitals_json TEXT NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
);

CREATE TABLE ref_test_types (
    id CHAR(36) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    category VARCHAR(50) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL
);

CREATE TABLE ref_test_type_translations (
    test_type_id CHAR(36) NOT NULL,
    lang_code VARCHAR(5) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT NULL,
    PRIMARY KEY (test_type_id, lang_code),
    FOREIGN KEY (test_type_id) REFERENCES ref_test_types(id) ON DELETE CASCADE,
    FOREIGN KEY (lang_code) REFERENCES languages(code)
);

CREATE TABLE prenatal_tests (
    id CHAR(36) PRIMARY KEY,
    pregnancy_id CHAR(36) NOT NULL,
    visit_id CHAR(36) NULL,
    test_type_id CHAR(36) NOT NULL,
    test_at DATETIME NOT NULL,
    result_text TEXT NULL,
    result_json TEXT NULL,
    abnormal_flag TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    deleted_at DATETIME(6) NULL,
    FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    FOREIGN KEY (visit_id) REFERENCES prenatal_visits(id) ON DELETE SET NULL,
    FOREIGN KEY (test_type_id) REFERENCES ref_test_types(id)
);
```

**Code**:

```csharp
// File: FPT.EXE201.Domain/Entities/PrenatalVisit.cs
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Ghi nhận một lần khám thai.
/// Mỗi thai kỳ có nhiều lần khám (định kỳ mỗi 2-4 tuần).
/// Một lần khám có thể kèm nhiều xét nghiệm (prenatal tests).
/// 
/// Vitals JSON lưu các chỉ số đo tại buổi khám:
/// {"bloodPressure": "120/80", "weightKg": 65.5, "pulseRate": 80, "temperature": 36.5}
/// </summary>
public class PrenatalVisit : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ mà buổi khám này thuộc về.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>
    /// FK → DoctorProfile (sẽ tạo FK constraint ở Week 7).
    /// Nullable vì Week 3 chưa có bảng doctor_profiles.
    /// Hiện tại chỉ lưu Guid raw, chưa validate existence.
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>Ngày giờ diễn ra buổi khám.</summary>
    public DateTime VisitDateTime { get; set; }

    /// <summary>
    /// Loại buổi khám: Routine (định kỳ), Emergency (cấp cứu),
    /// FollowUp (tái khám), LabOnly (chỉ xét nghiệm), Other.
    /// </summary>
    public VisitType VisitType { get; set; }

    /// <summary>Nơi khám. Ví dụ: "BV Từ Dũ", "Phòng khám Dr. Nguyễn".</summary>
    public string? Location { get; set; }

    /// <summary>Ghi chú của user hoặc bác sĩ về buổi khám.</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// JSON lưu chỉ số sinh tồn đo tại buổi khám.
    /// Schema linh hoạt vì mỗi buổi khám có thể đo các chỉ số khác nhau.
    /// Ví dụ: {"bloodPressure": "120/80", "weightKg": 65.5, "pulseRate": 80}
    /// </summary>
    public string? VitalsJson { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;

    /// <summary>Các xét nghiệm thực hiện trong buổi khám này.</summary>
    public ICollection<PrenatalTest> Tests { get; set; } = new List<PrenatalTest>();
}

// File: FPT.EXE201.Domain/Entities/RefTestType.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục loại xét nghiệm (reference/master data).
/// Seed sẵn bởi hệ thống. User chọn từ danh sách khi ghi kết quả.
/// 
/// Categories:
/// - LAB: Xét nghiệm máu, nước tiểu (CBC, OGTT, HIV...)
/// - IMAGING: Chẩn đoán hình ảnh (Siêu âm, NT Scan...)
/// - OTHER: Loại khác (NST, đo huyết áp liên tục...)
/// </summary>
public class RefTestType : BaseEntity
{
    /// <summary>
    /// Mã định danh. Convention: UPPER_SNAKE_CASE.
    /// Ví dụ: "COMPLETE_BLOOD_COUNT", "ULTRASOUND", "OGTT".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Phân nhóm xét nghiệm: "LAB", "IMAGING", "OTHER".
    /// Dùng để filter trong UI (tab Lab / Imaging / Other).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Còn sử dụng hay đã ngưng.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<RefTestTypeTranslation> Translations { get; set; }
        = new List<RefTestTypeTranslation>();
    public ICollection<PrenatalTest> Tests { get; set; }
        = new List<PrenatalTest>();
}

// File: FPT.EXE201.Domain/Entities/RefTestTypeTranslation.cs
namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho loại xét nghiệm.
/// Composite key: (TestTypeId + LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity.
/// </summary>
public class RefTestTypeTranslation
{
    /// <summary>FK → RefTestType.Id</summary>
    public Guid TestTypeId { get; set; }

    /// <summary>Mã ngôn ngữ ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị. Ví dụ: "Công thức máu toàn phần".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional).</summary>
    public string? Description { get; set; }

    // Navigation
    public RefTestType TestType { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

// File: FPT.EXE201.Domain/Entities/PrenatalTest.cs
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Kết quả một xét nghiệm trong thai kỳ.
/// 
/// Có thể gắn vào 1 buổi khám (VisitId) hoặc độc lập (VisitId = null).
/// Ví dụ: User tự đi xét nghiệm máu ở phòng lab không qua buổi khám.
/// 
/// ResultJson lưu kết quả có cấu trúc (optional):
/// {"glucose_fasting": 95, "glucose_1h": 180, "glucose_2h": 155, "unit": "mg/dL"}
/// </summary>
public class PrenatalTest : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ mà xét nghiệm này thuộc về.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>
    /// FK → PrenatalVisit. Buổi khám mà xét nghiệm này được thực hiện.
    /// Nullable: xét nghiệm có thể không gắn với buổi khám nào.
    /// </summary>
    public Guid? VisitId { get; set; }

    /// <summary>FK → RefTestType. Loại xét nghiệm (từ danh mục master).</summary>
    public Guid TestTypeId { get; set; }

    /// <summary>Ngày giờ thực hiện xét nghiệm.</summary>
    public DateTime TestDateTime { get; set; }

    /// <summary>
    /// Kết quả dạng text tự do.
    /// Ví dụ: "Hemoglobin: 11.5 g/dL, Hematocrit: 34%"
    /// </summary>
    public string? ResultText { get; set; }

    /// <summary>
    /// Kết quả dạng JSON có cấu trúc (optional).
    /// Dùng cho trường hợp cần query/so sánh giá trị cụ thể.
    /// Ví dụ: {"hemoglobin": 11.5, "hematocrit": 34, "unit": "g/dL"}
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// Có bất thường hay không.
    /// true = kết quả ngoài giới hạn bình thường, cần theo dõi.
    /// Có thể do user tự đánh dấu hoặc bác sĩ xác nhận.
    /// </summary>
    public bool IsAbnormalResult { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public PrenatalVisit? Visit { get; set; }
    public RefTestType TestType { get; set; } = null!;
}
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 4/10 — EF Core Configurations (ALL 8 Entities)

**Nhiệm vụ**: Map C# property names → DB column names (snake_case). Tạo 8 configuration files.

**⚠️ KEY PATTERN**: Property rõ nghĩa → Column ngắn gọn:

```
C#: LastMenstrualPeriodDate  →  DB: lmp_date
C#: ExpectedDeliveryDate     →  DB: edd_date  
C#: CurrentGestationalWeek   →  DB: current_week
C#: LanguageCode              →  DB: lang_code
C#: DisplayName               →  DB: name
C#: DiagnosedDate             →  DB: diagnosed_at
C#: VisitDateTime             →  DB: visit_at
C#: TestDateTime              →  DB: test_at
C#: IsAbnormalResult          →  DB: abnormal_flag
C#: PregnancyNumber           →  DB: pregnancy_no
```

**⚠️ IMPORTANT**: 
- `builder.Ignore(e => e.IsDeleted)` — computed property, KHÔNG map vào DB.
- Enum dùng `.HasConversion<string>()` cho consistency.
- Language FK dùng `.HasPrincipalKey(l => l.Code)` vì Language PK là `string Code`, không phải `Guid Id`.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Configurations/PregnancyConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyConfiguration : IEntityTypeConfiguration<Pregnancy>
{
    public void Configure(EntityTypeBuilder<Pregnancy> builder)
    {
        builder.ToTable("pregnancies");

        builder.Property(p => p.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(p => p.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");

        builder.Property(p => p.PregnancyNumber)
            .IsRequired().HasColumnName("pregnancy_no");

        builder.Property(p => p.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.LastMenstrualPeriodDate)
            .HasColumnName("lmp_date").HasColumnType("DATE");

        builder.Property(p => p.ExpectedDeliveryDate)
            .HasColumnName("edd_date").HasColumnType("DATE");

        builder.Property(p => p.EstimatedConceptionDate)
            .HasColumnName("conception_date").HasColumnType("DATE");

        builder.Property(p => p.CurrentGestationalWeek)
            .HasColumnName("current_week");

        builder.Property(p => p.Notes)
            .HasColumnName("notes").HasColumnType("TEXT");

        // ── Nhóm 1: Thông tin bé ──
        builder.Property(p => p.BabyNickname)
            .HasColumnName("baby_nickname").HasMaxLength(100);

        builder.Property(p => p.BabyGender)
            .IsRequired().HasColumnName("baby_gender")
            .HasConversion<string>().HasMaxLength(10)
            .HasDefaultValue(BabyGender.Unknown);

        builder.Property(p => p.PregnancyType)
            .IsRequired().HasColumnName("pregnancy_type")
            .HasConversion<string>().HasMaxLength(15)
            .HasDefaultValue(PregnancyType.Singleton);

        // ── Nhóm 2: Y tế mẹ ──
        builder.Property(p => p.MotherBloodType)
            .HasColumnName("mother_blood_type").HasMaxLength(10);

        builder.Property(p => p.PrePregnancyWeightKg)
            .HasColumnName("pre_pregnancy_weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(p => p.HeightCm)
            .HasColumnName("height_cm").HasColumnType("DECIMAL(5,2)");

        // ── Nhóm 3: Thai sản chuyên sâu ──
        builder.Property(p => p.DueDateSource)
            .IsRequired().HasColumnName("due_date_source")
            .HasConversion<string>().HasMaxLength(15)
            .HasDefaultValue(DueDateSource.LMP);

        builder.Property(p => p.Gravida)
            .HasColumnName("gravida");

        builder.Property(p => p.Para)
            .HasColumnName("para");

        builder.Property(p => p.ActualDeliveryDate)
            .HasColumnName("actual_delivery_date").HasColumnType("DATE");

        builder.Property(p => p.DeliveryMethod)
            .HasColumnName("delivery_method")
            .HasConversion<string>().HasMaxLength(15);

        builder.Property(p => p.CoverImageUrl)
            .HasColumnName("cover_image_url").HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        // Ignore computed property
        builder.Ignore(p => p.IsDeleted);

        // Unique: 1 user + pregnancy_no
        builder.HasIndex(p => new { p.UserId, p.PregnancyNumber })
            .IsUnique().HasDatabaseName("uk_pregnancies_user_no");

        builder.HasIndex(p => p.UserId).HasDatabaseName("idx_pregnancies_user");
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_pregnancies_status");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany().HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Conditions)
            .WithOne(c => c.Pregnancy).HasForeignKey(c => c.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Visits)
            .WithOne(v => v.Pregnancy).HasForeignKey(v => v.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tests)
            .WithOne(t => t.Pregnancy).HasForeignKey(t => t.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefPregnancyConditionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefPregnancyConditionConfiguration : IEntityTypeConfiguration<RefPregnancyCondition>
{
    public void Configure(EntityTypeBuilder<RefPregnancyCondition> builder)
    {
        builder.ToTable("ref_pregnancy_conditions");

        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Code).IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("uk_ref_conditions_code");
        builder.Property(r => r.IsActive).IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(r => r.IsDeleted);

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.Condition).HasForeignKey(t => t.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefPregnancyConditionTranslationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefPregnancyConditionTranslationConfiguration
    : IEntityTypeConfiguration<RefPregnancyConditionTranslation>
{
    public void Configure(EntityTypeBuilder<RefPregnancyConditionTranslation> builder)
    {
        builder.ToTable("ref_pregnancy_condition_translations");

        builder.HasKey(t => new { t.ConditionId, t.LanguageCode });

        builder.Property(t => t.ConditionId).HasColumnName("condition_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("lang_code").HasMaxLength(5);
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        builder.HasOne(t => t.Condition)
            .WithMany(c => c.Translations).HasForeignKey(t => t.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/PregnancyConditionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyConditionConfiguration : IEntityTypeConfiguration<PregnancyCondition>
{
    public void Configure(EntityTypeBuilder<PregnancyCondition> builder)
    {
        builder.ToTable("pregnancy_conditions");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.ConditionId).IsRequired().HasColumnName("condition_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.DiagnosedDate).HasColumnName("diagnosed_at").HasColumnType("DATETIME");
        builder.Property(p => p.Severity).HasColumnName("severity").HasConversion<string?>().HasMaxLength(20);
        builder.Property(p => p.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_pregnancy_conditions_pregnancy");
        builder.HasIndex(p => p.ConditionId).HasDatabaseName("idx_pregnancy_conditions_condition");
        builder.HasIndex(p => new { p.PregnancyId, p.ConditionId })
            .IsUnique().HasDatabaseName("uk_pregnancy_condition");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Conditions).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Condition)
            .WithMany(c => c.PregnancyConditions).HasForeignKey(p => p.ConditionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/PrenatalVisitConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PrenatalVisitConfiguration : IEntityTypeConfiguration<PrenatalVisit>
{
    public void Configure(EntityTypeBuilder<PrenatalVisit> builder)
    {
        builder.ToTable("prenatal_visits");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.DoctorId).HasColumnName("doctor_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.VisitDateTime).IsRequired().HasColumnName("visit_at").HasColumnType("DATETIME");
        builder.Property(p => p.VisitType).IsRequired().HasColumnName("visit_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(p => p.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(p => p.VitalsJson).HasColumnName("vitals_json").HasColumnType("TEXT");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_prenatal_visits_pregnancy");
        builder.HasIndex(p => p.VisitDateTime).HasDatabaseName("idx_prenatal_visits_date");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Visits).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Tests)
            .WithOne(t => t.Visit).HasForeignKey(t => t.VisitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefTestTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefTestTypeConfiguration : IEntityTypeConfiguration<RefTestType>
{
    public void Configure(EntityTypeBuilder<RefTestType> builder)
    {
        builder.ToTable("ref_test_types");

        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Code).IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("uk_ref_test_types_code");
        builder.Property(r => r.Category).HasColumnName("category").HasMaxLength(50);
        builder.HasIndex(r => r.Category).HasDatabaseName("idx_ref_test_types_category");
        builder.Property(r => r.IsActive).IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(r => r.IsDeleted);

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.TestType).HasForeignKey(t => t.TestTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/RefTestTypeTranslationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefTestTypeTranslationConfiguration : IEntityTypeConfiguration<RefTestTypeTranslation>
{
    public void Configure(EntityTypeBuilder<RefTestTypeTranslation> builder)
    {
        builder.ToTable("ref_test_type_translations");

        builder.HasKey(t => new { t.TestTypeId, t.LanguageCode });

        builder.Property(t => t.TestTypeId).HasColumnName("test_type_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("lang_code").HasMaxLength(5);
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        builder.HasOne(t => t.TestType)
            .WithMany(tt => tt.Translations).HasForeignKey(t => t.TestTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// File: FPT.EXE201.Infrastructure/Configurations/PrenatalTestConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PrenatalTestConfiguration : IEntityTypeConfiguration<PrenatalTest>
{
    public void Configure(EntityTypeBuilder<PrenatalTest> builder)
    {
        builder.ToTable("prenatal_tests");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.VisitId).HasColumnName("visit_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.TestTypeId).IsRequired().HasColumnName("test_type_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.TestDateTime).IsRequired().HasColumnName("test_at").HasColumnType("DATETIME");
        builder.Property(p => p.ResultText).HasColumnName("result_text").HasColumnType("TEXT");
        builder.Property(p => p.ResultJson).HasColumnName("result_json").HasColumnType("TEXT");
        builder.Property(p => p.IsAbnormalResult).IsRequired().HasColumnName("abnormal_flag")
            .HasColumnType("TINYINT(1)").HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_prenatal_tests_pregnancy");
        builder.HasIndex(p => p.VisitId).HasDatabaseName("idx_prenatal_tests_visit");
        builder.HasIndex(p => p.TestDateTime).HasDatabaseName("idx_prenatal_tests_date");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Tests).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Visit)
            .WithMany(v => v.Tests).HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.TestType)
            .WithMany(tt => tt.Tests).HasForeignKey(p => p.TestTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Update `AppDbContext` — thêm DbSets**:

```csharp
// Add to AppDbContext.cs (after existing DbSets)

// Week 3 — Pregnancy Core
public DbSet<Pregnancy> Pregnancies => Set<Pregnancy>();
public DbSet<RefPregnancyCondition> RefPregnancyConditions => Set<RefPregnancyCondition>();
public DbSet<RefPregnancyConditionTranslation> RefPregnancyConditionTranslations => Set<RefPregnancyConditionTranslation>();
public DbSet<PregnancyCondition> PregnancyConditions => Set<PregnancyCondition>();
public DbSet<PrenatalVisit> PrenatalVisits => Set<PrenatalVisit>();
public DbSet<RefTestType> RefTestTypes => Set<RefTestType>();
public DbSet<RefTestTypeTranslation> RefTestTypeTranslations => Set<RefTestTypeTranslation>();
public DbSet<PrenatalTest> PrenatalTests => Set<PrenatalTest>();
```

**⚠️ NOTE**: Configurations auto-applied via `ApplyConfigurationsFromAssembly` — KHÔNG cần manual apply. Chỉ cần đặt file trong đúng namespace.

**⚠️ NOTE**: Global soft-delete query filter đã có sẵn trong `AppDbContext.OnModelCreating` — tự động apply cho tất cả entities kế thừa `BaseEntity`. Translation entities (không kế thừa `BaseEntity`) KHÔNG có filter — đúng ý muốn.

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 5/10 — Migration + Seed Reference Data

**Nhiệm vụ**: Tạo migration + Seed 10 conditions + 10 test types (mỗi cái 2 translations = 40 records).

**⚠️ CRITICAL**: 
- Dùng **anonymous type** cho `HasData()`, KHÔNG dùng entity instance.
- Fixed `DateTime` — KHÔNG dùng `DateTime.UtcNow` (migration sẽ thay đổi mỗi lần chạy).
- Lang code lowercase: `"vi"`, `"en"` — match Week 1 seed.
- Nullable string cast `(string?)` cho optional fields trong anonymous type.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Persistence/Seeders/PregnancyConditionSeeder.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class PregnancyConditionSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid GestDiabetes   = Guid.Parse("a0000001-0000-0000-0000-000000000001");
    private static readonly Guid Preeclampsia   = Guid.Parse("a0000001-0000-0000-0000-000000000002");
    private static readonly Guid Anemia         = Guid.Parse("a0000001-0000-0000-0000-000000000003");
    private static readonly Guid Hyperemesis    = Guid.Parse("a0000001-0000-0000-0000-000000000004");
    private static readonly Guid PlacentaPrevia = Guid.Parse("a0000001-0000-0000-0000-000000000005");
    private static readonly Guid Hypertension   = Guid.Parse("a0000001-0000-0000-0000-000000000006");
    private static readonly Guid ThyroidDis     = Guid.Parse("a0000001-0000-0000-0000-000000000007");
    private static readonly Guid GroupBStrep    = Guid.Parse("a0000001-0000-0000-0000-000000000008");
    private static readonly Guid CervicalInsuf  = Guid.Parse("a0000001-0000-0000-0000-000000000009");
    private static readonly Guid EctopicPreg    = Guid.Parse("a0000001-0000-0000-0000-00000000000a");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefPregnancyCondition>().HasData(
            new { Id = GestDiabetes,   Code = "GESTATIONAL_DIABETES",   IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Preeclampsia,   Code = "PREECLAMPSIA",           IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Anemia,         Code = "ANEMIA",                 IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Hyperemesis,    Code = "HYPEREMESIS_GRAVIDARUM", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = PlacentaPrevia, Code = "PLACENTA_PREVIA",        IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Hypertension,   Code = "HYPERTENSION",           IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = ThyroidDis,     Code = "THYROID_DISORDER",       IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = GroupBStrep,    Code = "GROUP_B_STREP",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CervicalInsuf,  Code = "CERVICAL_INSUFFICIENCY", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = EctopicPreg,    Code = "ECTOPIC_PREGNANCY",      IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        // ⚠️ Property names trong anonymous type PHẢI match C# entity property names
        // (ConditionId, LanguageCode, DisplayName, Description) — EF sẽ map sang DB column qua config
        modelBuilder.Entity<RefPregnancyConditionTranslation>().HasData(
            // Vietnamese
            new { ConditionId = GestDiabetes,   LanguageCode = "vi", DisplayName = "Tiểu đường thai kỳ",         Description = (string?)"Tình trạng đường huyết cao phát triển trong thai kỳ" },
            new { ConditionId = Preeclampsia,   LanguageCode = "vi", DisplayName = "Tiền sản giật",              Description = (string?)"Huyết áp cao và protein niệu sau tuần 20" },
            new { ConditionId = Anemia,         LanguageCode = "vi", DisplayName = "Thiếu máu",                  Description = (string?)"Lượng hồng cầu hoặc hemoglobin thấp" },
            new { ConditionId = Hyperemesis,    LanguageCode = "vi", DisplayName = "Nghén nặng",                 Description = (string?)"Buồn nôn và nôn nghiêm trọng trong thai kỳ" },
            new { ConditionId = PlacentaPrevia, LanguageCode = "vi", DisplayName = "Nhau tiền đạo",              Description = (string?)"Nhau thai che phủ cổ tử cung" },
            new { ConditionId = Hypertension,   LanguageCode = "vi", DisplayName = "Tăng huyết áp thai kỳ",      Description = (string?)"Huyết áp cao phát hiện sau tuần 20" },
            new { ConditionId = ThyroidDis,     LanguageCode = "vi", DisplayName = "Rối loạn tuyến giáp",        Description = (string?)"Cường giáp hoặc suy giáp trong thai kỳ" },
            new { ConditionId = GroupBStrep,    LanguageCode = "vi", DisplayName = "Nhiễm liên cầu nhóm B",      Description = (string?)"Vi khuẩn GBS có thể lây sang con khi sinh" },
            new { ConditionId = CervicalInsuf,  LanguageCode = "vi", DisplayName = "Hở eo cổ tử cung",           Description = (string?)"Cổ tử cung mở sớm, nguy cơ sinh non" },
            new { ConditionId = EctopicPreg,    LanguageCode = "vi", DisplayName = "Thai ngoài tử cung",         Description = (string?)"Thai làm tổ ngoài buồng tử cung" },
            // English
            new { ConditionId = GestDiabetes,   LanguageCode = "en", DisplayName = "Gestational Diabetes",       Description = (string?)"High blood sugar that develops during pregnancy" },
            new { ConditionId = Preeclampsia,   LanguageCode = "en", DisplayName = "Preeclampsia",               Description = (string?)"High blood pressure and protein in urine after 20 weeks" },
            new { ConditionId = Anemia,         LanguageCode = "en", DisplayName = "Anemia",                     Description = (string?)"Low red blood cell count or hemoglobin" },
            new { ConditionId = Hyperemesis,    LanguageCode = "en", DisplayName = "Hyperemesis Gravidarum",     Description = (string?)"Severe nausea and vomiting during pregnancy" },
            new { ConditionId = PlacentaPrevia, LanguageCode = "en", DisplayName = "Placenta Previa",            Description = (string?)"Placenta covers the cervix" },
            new { ConditionId = Hypertension,   LanguageCode = "en", DisplayName = "Gestational Hypertension",   Description = (string?)"High blood pressure after week 20 without proteinuria" },
            new { ConditionId = ThyroidDis,     LanguageCode = "en", DisplayName = "Thyroid Disorder",           Description = (string?)"Hyperthyroidism or hypothyroidism during pregnancy" },
            new { ConditionId = GroupBStrep,    LanguageCode = "en", DisplayName = "Group B Streptococcus",      Description = (string?)"GBS bacteria that may pass to baby during delivery" },
            new { ConditionId = CervicalInsuf,  LanguageCode = "en", DisplayName = "Cervical Insufficiency",     Description = (string?)"Cervix opens prematurely, risk of preterm birth" },
            new { ConditionId = EctopicPreg,    LanguageCode = "en", DisplayName = "Ectopic Pregnancy",          Description = (string?)"Pregnancy implanted outside the uterus" }
        );
    }
}

// File: FPT.EXE201.Infrastructure/Persistence/Seeders/TestTypeSeeder.cs
using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class TestTypeSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid BloodGlucose = Guid.Parse("b0000001-0000-0000-0000-000000000001");
    private static readonly Guid Ultrasound   = Guid.Parse("b0000001-0000-0000-0000-000000000002");
    private static readonly Guid BloodPress   = Guid.Parse("b0000001-0000-0000-0000-000000000003");
    private static readonly Guid CBC          = Guid.Parse("b0000001-0000-0000-0000-000000000004");
    private static readonly Guid UrineTest    = Guid.Parse("b0000001-0000-0000-0000-000000000005");
    private static readonly Guid HepB         = Guid.Parse("b0000001-0000-0000-0000-000000000006");
    private static readonly Guid HIV          = Guid.Parse("b0000001-0000-0000-0000-000000000007");
    private static readonly Guid TSH          = Guid.Parse("b0000001-0000-0000-0000-000000000008");
    private static readonly Guid NTScan       = Guid.Parse("b0000001-0000-0000-0000-000000000009");
    private static readonly Guid OGTT         = Guid.Parse("b0000001-0000-0000-0000-00000000000a");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefTestType>().HasData(
            new { Id = BloodGlucose, Code = "BLOOD_GLUCOSE",        Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Ultrasound,   Code = "ULTRASOUND",           Category = (string?)"IMAGING", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = BloodPress,   Code = "BLOOD_PRESSURE",       Category = (string?)"OTHER",   IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CBC,          Code = "COMPLETE_BLOOD_COUNT",  Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = UrineTest,    Code = "URINE_TEST",           Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = HepB,         Code = "HEPATITIS_B",          Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = HIV,          Code = "HIV_SCREEN",           Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = TSH,          Code = "TSH",                  Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = NTScan,       Code = "NT_SCAN",              Category = (string?)"IMAGING", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = OGTT,         Code = "OGTT",                 Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        modelBuilder.Entity<RefTestTypeTranslation>().HasData(
            // Vietnamese
            new { TestTypeId = BloodGlucose, LanguageCode = "vi", DisplayName = "Xét nghiệm đường huyết",         Description = (string?)"Kiểm tra nồng độ glucose trong máu" },
            new { TestTypeId = Ultrasound,   LanguageCode = "vi", DisplayName = "Siêu âm",                         Description = (string?)"Chụp hình ảnh thai nhi bằng sóng siêu âm" },
            new { TestTypeId = BloodPress,   LanguageCode = "vi", DisplayName = "Đo huyết áp",                     Description = (string?)"Đo áp lực máu trong động mạch" },
            new { TestTypeId = CBC,          LanguageCode = "vi", DisplayName = "Công thức máu toàn phần",         Description = (string?)"Đếm số lượng và phân loại tế bào máu" },
            new { TestTypeId = UrineTest,    LanguageCode = "vi", DisplayName = "Xét nghiệm nước tiểu",           Description = (string?)"Phân tích thành phần nước tiểu" },
            new { TestTypeId = HepB,         LanguageCode = "vi", DisplayName = "Xét nghiệm viêm gan B",          Description = (string?)"Tầm soát virus viêm gan B" },
            new { TestTypeId = HIV,          LanguageCode = "vi", DisplayName = "Xét nghiệm HIV",                 Description = (string?)"Tầm soát virus HIV" },
            new { TestTypeId = TSH,          LanguageCode = "vi", DisplayName = "Xét nghiệm TSH",                 Description = (string?)"Kiểm tra chức năng tuyến giáp" },
            new { TestTypeId = NTScan,       LanguageCode = "vi", DisplayName = "Đo độ mờ da gáy",                Description = (string?)"Siêu âm tầm soát dị tật thai nhi" },
            new { TestTypeId = OGTT,         LanguageCode = "vi", DisplayName = "Nghiệm pháp dung nạp glucose",   Description = (string?)"Xét nghiệm chẩn đoán tiểu đường thai kỳ" },
            // English
            new { TestTypeId = BloodGlucose, LanguageCode = "en", DisplayName = "Blood Glucose Test",              Description = (string?)"Measures glucose level in blood" },
            new { TestTypeId = Ultrasound,   LanguageCode = "en", DisplayName = "Ultrasound",                      Description = (string?)"Imaging of fetus using sound waves" },
            new { TestTypeId = BloodPress,   LanguageCode = "en", DisplayName = "Blood Pressure",                  Description = (string?)"Measures blood pressure in arteries" },
            new { TestTypeId = CBC,          LanguageCode = "en", DisplayName = "Complete Blood Count",            Description = (string?)"Counts different blood cell types" },
            new { TestTypeId = UrineTest,    LanguageCode = "en", DisplayName = "Urine Test",                      Description = (string?)"Analyzes urine composition" },
            new { TestTypeId = HepB,         LanguageCode = "en", DisplayName = "Hepatitis B Screen",              Description = (string?)"Screens for hepatitis B virus" },
            new { TestTypeId = HIV,          LanguageCode = "en", DisplayName = "HIV Screen",                      Description = (string?)"Screens for HIV virus" },
            new { TestTypeId = TSH,          LanguageCode = "en", DisplayName = "TSH Test",                        Description = (string?)"Checks thyroid function" },
            new { TestTypeId = NTScan,       LanguageCode = "en", DisplayName = "Nuchal Translucency Scan",        Description = (string?)"Ultrasound screening for fetal abnormalities" },
            new { TestTypeId = OGTT,         LanguageCode = "en", DisplayName = "Oral Glucose Tolerance Test",     Description = (string?)"Diagnostic test for gestational diabetes" }
        );
    }
}
```

**Update `AppDbContext.OnModelCreating`** — add seeder calls:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing code ...
    
    // Week 3 Seeders
    PregnancyConditionSeeder.Seed(modelBuilder);
    TestTypeSeeder.Seed(modelBuilder);
}
```

**Commands to run** (từ thư mục `src/FPT.EXE201.Api`):
```bash
dotnet ef migrations add Week3_PregnancyCore --project ../FPT.EXE201.Infrastructure --startup-project .
dotnet ef database update --project ../FPT.EXE201.Infrastructure --startup-project .
```

**✅ Checkpoint**: 
- Migration tạo thành công
- Database update OK
- Verify: 8 tables mới, 10 conditions + 10 test types, 40 translation records

---

## 🎯 PROMPT 6/10 — DTOs + FluentValidation

**Nhiệm vụ**: Tạo DTOs cho tất cả modules + FluentValidation validators.

**⚠️ DTO Naming Rule**: Property names match C# entities (rõ nghĩa), KHÔNG dùng viết tắt. Enum type dùng trong Create/Update DTOs, `JsonStringEnumConverter` handle serialization.

**Code — Pregnancy DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/Pregnancies/CreatePregnancyDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Request body khi tạo thai kỳ mới.
/// Cần ít nhất 1 trong 2: LastMenstrualPeriodDate hoặc EstimatedConceptionDate.
/// </summary>
public record CreatePregnancyDto(
    /// <summary>LMP — Ngày đầu kỳ kinh cuối. Dùng để tính tuổi thai và ngày dự sinh.</summary>
    DateTime? LastMenstrualPeriodDate,

    /// <summary>Ngày thụ thai ước tính (optional).</summary>
    DateTime? EstimatedConceptionDate,

    /// <summary>Ghi chú tự do.</summary>
    string? Notes,

    // ── Nhóm 1: Thông tin bé ──
    /// <summary>Biệt danh bé. Ví dụ: "Bé Bông", "Cherry".</summary>
    string? BabyNickname = null,

    /// <summary>Giới tính em bé. Mặc định Unknown.</summary>
    BabyGender BabyGender = BabyGender.Unknown,

    /// <summary>Loại thai: Singleton / Twins / Triplets / Other.</summary>
    PregnancyType PregnancyType = PregnancyType.Singleton,

    // ── Nhóm 2: Y tế mẹ ──
    /// <summary>Nhóm máu mẹ. Ví dụ: "A+", "O-".</summary>
    string? MotherBloodType = null,

    /// <summary>Cân nặng trước mang thai (kg). Baseline cho Weight Tracking.</summary>
    decimal? PrePregnancyWeightKg = null,

    /// <summary>Chiều cao mẹ (cm). Dùng tính BMI.</summary>
    decimal? HeightCm = null,

    // ── Nhóm 3: Thai sản chuyên sâu ──
    /// <summary>Nguồn tính ngày dự sinh.</summary>
    DueDateSource DueDateSource = DueDateSource.LMP,

    /// <summary>Gravida — tổng số lần mang thai (tính cả lần này). Ví dụ: G2.</summary>
    int? Gravida = null,

    /// <summary>Para — tổng số lần sinh trước đó. Ví dụ: P1.</summary>
    int? Para = null,

    /// <summary>Ảnh cover cho hồ sơ thai kỳ (URL).</summary>
    string? CoverImageUrl = null
);

// File: FPT.EXE201.Application/DTOs/Pregnancies/UpdatePregnancyDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

public record UpdatePregnancyDto(
    DateTime? LastMenstrualPeriodDate,
    DateTime? EstimatedConceptionDate,
    string? Notes,

    // Nhóm 1
    string? BabyNickname,
    BabyGender? BabyGender,
    PregnancyType? PregnancyType,

    // Nhóm 2
    string? MotherBloodType,
    decimal? PrePregnancyWeightKg,
    decimal? HeightCm,

    // Nhóm 3
    DueDateSource? DueDateSource,
    /// <summary>Nếu FE truyền EDD mới (bác sĩ điều chỉnh theo ultrasound), cập nhật luôn.</summary>
    DateTime? ExpectedDeliveryDate,
    int? Gravida,
    int? Para,
    string? CoverImageUrl
);

// File: FPT.EXE201.Application/DTOs/Pregnancies/ChangePregnancyStatusDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Request body khi thay đổi trạng thái thai kỳ.
/// Chỉ cho phép: Active → Delivered / Ended / Miscarriage.
/// Nếu status = Delivered, có thể gửi kèm ActualDeliveryDate + DeliveryMethod.
/// </summary>
public record ChangePregnancyStatusDto(
    PregnancyStatus Status,

    /// <summary>Ngày sinh thực tế. Bắt buộc khi status = Delivered.</summary>
    DateTime? ActualDeliveryDate = null,

    /// <summary>Phương pháp sinh. Optional khi status = Delivered.</summary>
    DeliveryMethod? DeliveryMethod = null
);

// File: FPT.EXE201.Application/DTOs/Pregnancies/PregnancyDto.cs
namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Response trả về thông tin thai kỳ.
/// </summary>
public record PregnancyDto(
    Guid Id,
    Guid UserId,
    int PregnancyNumber,
    string Status,
    DateTime? LastMenstrualPeriodDate,
    DateTime? ExpectedDeliveryDate,
    DateTime? EstimatedConceptionDate,
    int? CurrentGestationalWeek,

    /// <summary>Tuổi thai dạng hiển thị. Ví dụ: "28w3d" (28 tuần 3 ngày).</summary>
    string? GestationalAgeDisplay,

    string? Notes,

    // Nhóm 1: Thông tin bé
    string? BabyNickname,
    string BabyGender,
    string PregnancyType,

    // Nhóm 2: Y tế mẹ
    string? MotherBloodType,
    decimal? PrePregnancyWeightKg,
    decimal? HeightCm,

    /// <summary>BMI trước mang thai. Auto-computed: weight / (height/100)^2. Null nếu thiếu dữ liệu.</summary>
    decimal? PrePregnancyBmi,

    // Nhóm 3: Thai sản chuyên sâu
    string DueDateSource,
    int? Gravida,
    int? Para,

    /// <summary>Hiển thị tiện kỷ hiệu y khoa. Ví dụ: "G2P1".</summary>
    string? ObstetricFormula,

    DateTime? ActualDeliveryDate,
    string? DeliveryMethod,
    string? CoverImageUrl,

    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

**Code — Condition DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/PregnancyConditions/CreatePregnancyConditionDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

public record CreatePregnancyConditionDto(
    /// <summary>ID của bệnh lý từ danh mục (ref_pregnancy_conditions).</summary>
    Guid ConditionId,

    /// <summary>Ngày được chẩn đoán.</summary>
    DateTime? DiagnosedDate,

    /// <summary>Mức độ: Mild / Moderate / Severe.</summary>
    ConditionSeverity? Severity,

    string? Notes
);

// File: FPT.EXE201.Application/DTOs/PregnancyConditions/PregnancyConditionDto.cs
namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

public record PregnancyConditionDto(
    Guid Id,
    Guid PregnancyId,
    Guid ConditionId,

    /// <summary>Mã bệnh lý. Ví dụ: "GESTATIONAL_DIABETES".</summary>
    string ConditionCode,

    /// <summary>Tên hiển thị theo ngôn ngữ. Ví dụ: "Tiểu đường thai kỳ".</summary>
    string ConditionDisplayName,

    /// <summary>Mô tả chi tiết theo ngôn ngữ.</summary>
    string? ConditionDescription,

    DateTime? DiagnosedDate,
    string? Severity,
    string? Notes,
    DateTime CreatedAt
);

// File: FPT.EXE201.Application/DTOs/PregnancyConditions/UpdatePregnancyConditionDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

/// <summary>Cập nhật mức độ và ghi chú của bệnh lý đã gán. ConditionId KHÔNG thay đổi.</summary>
public record UpdatePregnancyConditionDto(
    DateTime? DiagnosedDate,
    ConditionSeverity? Severity,
    string? Notes
);
```

**Code — Visit DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/PrenatalVisits/CreatePrenatalVisitDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record CreatePrenatalVisitDto(
    DateTime VisitDateTime,
    VisitType VisitType,
    Guid? DoctorId,
    string? Location,
    string? Notes,

    /// <summary>
    /// JSON chỉ số sinh tồn đo tại buổi khám.
    /// Ví dụ: {"bloodPressure": "120/80", "weightKg": 65.5}
    /// </summary>
    string? VitalsJson
);

// File: FPT.EXE201.Application/DTOs/PrenatalVisits/UpdatePrenatalVisitDto.cs
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record UpdatePrenatalVisitDto(
    DateTime VisitDateTime,
    VisitType VisitType,
    Guid? DoctorId,
    string? Location,
    string? Notes,
    string? VitalsJson
);

// File: FPT.EXE201.Application/DTOs/PrenatalVisits/PrenatalVisitDto.cs
namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

public record PrenatalVisitDto(
    Guid Id,
    Guid PregnancyId,
    Guid? DoctorId,
    DateTime VisitDateTime,
    string VisitType,
    string? Location,
    string? Notes,
    string? VitalsJson,

    /// <summary>Số xét nghiệm đã thực hiện trong buổi khám này.</summary>
    int TestCount,

    DateTime CreatedAt
);

// File: FPT.EXE201.Application/DTOs/PrenatalVisits/PrenatalVisitDetailDto.cs
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits;

/// <summary>
/// Chi tiết 1 buổi khám, bao gồm danh sách xét nghiệm.
/// Dùng cho GET /api/visits/{id} — FE hiển thị trang chi tiết buổi khám.
/// </summary>
public record PrenatalVisitDetailDto(
    Guid Id,
    Guid PregnancyId,
    Guid? DoctorId,
    DateTime VisitDateTime,
    string VisitType,
    string? Location,
    string? Notes,
    string? VitalsJson,

    /// <summary>Danh sách xét nghiệm trong buổi khám này, kèm tên loại xét nghiệm theo ngôn ngữ.</summary>
    List<PrenatalTestDto> Tests,

    DateTime CreatedAt
);
```

**Code — Test DTOs**:

```csharp
// File: FPT.EXE201.Application/DTOs/PrenatalTests/CreatePrenatalTestDto.cs
namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record CreatePrenatalTestDto(
    /// <summary>ID loại xét nghiệm từ danh mục (ref_test_types).</summary>
    Guid TestTypeId,

    /// <summary>Buổi khám liên kết (optional). Phải thuộc cùng pregnancy.</summary>
    Guid? VisitId,

    DateTime TestDateTime,
    string? ResultText,
    string? ResultJson,
    bool IsAbnormalResult = false
);

// File: FPT.EXE201.Application/DTOs/PrenatalTests/UpdatePrenatalTestDto.cs
namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record UpdatePrenatalTestDto(
    string? ResultText,
    string? ResultJson,
    bool IsAbnormalResult
);

// File: FPT.EXE201.Application/DTOs/PrenatalTests/PrenatalTestDto.cs
namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record PrenatalTestDto(
    Guid Id,
    Guid PregnancyId,
    Guid? VisitId,
    Guid TestTypeId,
    string TestTypeCode,

    /// <summary>Tên xét nghiệm theo ngôn ngữ. Ví dụ: "Công thức máu toàn phần".</summary>
    string TestTypeDisplayName,

    DateTime TestDateTime,
    string? ResultText,
    string? ResultJson,
    bool IsAbnormalResult,
    DateTime CreatedAt
);
```

**Code — Reference Data DTOs** (cho public endpoints):

```csharp
// File: FPT.EXE201.Application/DTOs/RefData/RefConditionDto.cs
namespace FPT.EXE201.Application.DTOs.RefData;

public record RefConditionDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description
);

// File: FPT.EXE201.Application/DTOs/RefData/RefTestTypeDto.cs
namespace FPT.EXE201.Application.DTOs.RefData;

public record RefTestTypeDto(
    Guid Id,
    string Code,
    string? Category,
    string DisplayName,
    string? Description
);
```

**Code — FluentValidation Validators**:

```csharp
// File: FPT.EXE201.Application/Validations/Pregnancies/CreatePregnancyDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.Validations.Pregnancies;

public class CreatePregnancyDtoValidator : AbstractValidator<CreatePregnancyDto>
{
    public CreatePregnancyDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.LastMenstrualPeriodDate.HasValue || x.EstimatedConceptionDate.HasValue)
            .WithMessage("Either Last Menstrual Period date or conception date must be provided");

        When(x => x.LastMenstrualPeriodDate.HasValue, () =>
        {
            RuleFor(x => x.LastMenstrualPeriodDate!.Value)
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("Last Menstrual Period date cannot be in the future")
                .GreaterThan(DateTime.Today.AddDays(-315))
                .WithMessage("Last Menstrual Period date cannot be more than 45 weeks ago");
        });

        When(x => x.EstimatedConceptionDate.HasValue, () =>
        {
            RuleFor(x => x.EstimatedConceptionDate!.Value)
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("Conception date cannot be in the future");
        });

        RuleFor(x => x.Notes).MaximumLength(2000);

        // Nhóm 1
        RuleFor(x => x.BabyNickname).MaximumLength(100);
        RuleFor(x => x.BabyGender).IsInEnum();
        RuleFor(x => x.PregnancyType).IsInEnum();

        // Nhóm 2
        RuleFor(x => x.MotherBloodType).MaximumLength(10);
        When(x => x.PrePregnancyWeightKg.HasValue, () =>
        {
            RuleFor(x => x.PrePregnancyWeightKg!.Value)
                .InclusiveBetween(30m, 300m)
                .WithMessage("Pre-pregnancy weight must be between 30 and 300 kg");
        });
        When(x => x.HeightCm.HasValue, () =>
        {
            RuleFor(x => x.HeightCm!.Value)
                .InclusiveBetween(100m, 250m)
                .WithMessage("Height must be between 100 and 250 cm");
        });

        // Nhóm 3
        RuleFor(x => x.DueDateSource).IsInEnum();
        When(x => x.Gravida.HasValue, () =>
        {
            RuleFor(x => x.Gravida!.Value)
                .InclusiveBetween(1, 20)
                .WithMessage("Gravida must be between 1 and 20");
        });
        When(x => x.Para.HasValue, () =>
        {
            RuleFor(x => x.Para!.Value)
                .InclusiveBetween(0, 20)
                .WithMessage("Para must be between 0 and 20");
        });
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);
    }
}

// File: FPT.EXE201.Application/Validations/Pregnancies/UpdatePregnancyDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.Validations.Pregnancies;

public class UpdatePregnancyDtoValidator : AbstractValidator<UpdatePregnancyDto>
{
    public UpdatePregnancyDtoValidator()
    {
        When(x => x.LastMenstrualPeriodDate.HasValue, () =>
        {
            RuleFor(x => x.LastMenstrualPeriodDate!.Value)
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("Last Menstrual Period date cannot be in the future");
        });
        RuleFor(x => x.Notes).MaximumLength(2000);

        // Nhóm 1
        RuleFor(x => x.BabyNickname).MaximumLength(100);
        When(x => x.BabyGender.HasValue, () => { RuleFor(x => x.BabyGender!.Value).IsInEnum(); });
        When(x => x.PregnancyType.HasValue, () => { RuleFor(x => x.PregnancyType!.Value).IsInEnum(); });

        // Nhóm 2
        RuleFor(x => x.MotherBloodType).MaximumLength(10);
        When(x => x.PrePregnancyWeightKg.HasValue, () =>
        {
            RuleFor(x => x.PrePregnancyWeightKg!.Value)
                .InclusiveBetween(30m, 300m)
                .WithMessage("Pre-pregnancy weight must be between 30 and 300 kg");
        });
        When(x => x.HeightCm.HasValue, () =>
        {
            RuleFor(x => x.HeightCm!.Value)
                .InclusiveBetween(100m, 250m)
                .WithMessage("Height must be between 100 and 250 cm");
        });

        // Nhóm 3
        When(x => x.DueDateSource.HasValue, () => { RuleFor(x => x.DueDateSource!.Value).IsInEnum(); });
        When(x => x.Gravida.HasValue, () =>
        {
            RuleFor(x => x.Gravida!.Value)
                .InclusiveBetween(1, 20)
                .WithMessage("Gravida must be between 1 and 20");
        });
        When(x => x.Para.HasValue, () =>
        {
            RuleFor(x => x.Para!.Value)
                .InclusiveBetween(0, 20)
                .WithMessage("Para must be between 0 and 20");
        });
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);
    }
}

// File: FPT.EXE201.Application/Validations/Pregnancies/ChangePregnancyStatusDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.Validations.Pregnancies;

public class ChangePregnancyStatusDtoValidator : AbstractValidator<ChangePregnancyStatusDto>
{
    public ChangePregnancyStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid pregnancy status");

        // Khi status = Delivered, bắt buộc có ActualDeliveryDate
        When(x => x.Status == Domain.Enums.PregnancyStatus.Delivered, () =>
        {
            RuleFor(x => x.ActualDeliveryDate)
                .NotNull().WithMessage("Actual delivery date is required when status is Delivered")
                .Must(d => d!.Value <= DateTime.Today)
                .WithMessage("Actual delivery date cannot be in the future");

            When(x => x.DeliveryMethod.HasValue, () =>
            {
                RuleFor(x => x.DeliveryMethod!.Value).IsInEnum()
                    .WithMessage("Invalid delivery method");
            });
        });
    }
}

// File: FPT.EXE201.Application/Validations/PregnancyConditions/CreatePregnancyConditionDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PregnancyConditions;

namespace FPT.EXE201.Application.Validations.PregnancyConditions;

public class CreatePregnancyConditionDtoValidator : AbstractValidator<CreatePregnancyConditionDto>
{
    public CreatePregnancyConditionDtoValidator()
    {
        RuleFor(x => x.ConditionId).NotEmpty();
        When(x => x.DiagnosedDate.HasValue, () =>
        {
            RuleFor(x => x.DiagnosedDate!.Value)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("Diagnosed date cannot be in the future");
        });
        When(x => x.Severity.HasValue, () =>
        {
            RuleFor(x => x.Severity!.Value).IsInEnum()
                .WithMessage("Severity must be Mild, Moderate, or Severe");
        });
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

// File: FPT.EXE201.Application/Validations/PregnancyConditions/UpdatePregnancyConditionDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PregnancyConditions;

namespace FPT.EXE201.Application.Validations.PregnancyConditions;

public class UpdatePregnancyConditionDtoValidator : AbstractValidator<UpdatePregnancyConditionDto>
{
    public UpdatePregnancyConditionDtoValidator()
    {
        When(x => x.DiagnosedDate.HasValue, () =>
        {
            RuleFor(x => x.DiagnosedDate!.Value)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("Diagnosed date cannot be in the future");
        });
        When(x => x.Severity.HasValue, () =>
        {
            RuleFor(x => x.Severity!.Value).IsInEnum()
                .WithMessage("Severity must be Mild, Moderate, or Severe");
        });
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

// File: FPT.EXE201.Application/Validations/PrenatalVisits/CreatePrenatalVisitDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.Validations.PrenatalVisits;

public class CreatePrenatalVisitDtoValidator : AbstractValidator<CreatePrenatalVisitDto>
{
    public CreatePrenatalVisitDtoValidator()
    {
        RuleFor(x => x.VisitDateTime).NotEmpty();
        RuleFor(x => x.VisitType).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

// File: FPT.EXE201.Application/Validations/PrenatalVisits/UpdatePrenatalVisitDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.Validations.PrenatalVisits;

public class UpdatePrenatalVisitDtoValidator : AbstractValidator<UpdatePrenatalVisitDto>
{
    public UpdatePrenatalVisitDtoValidator()
    {
        RuleFor(x => x.VisitDateTime).NotEmpty();
        RuleFor(x => x.VisitType).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

// File: FPT.EXE201.Application/Validations/PrenatalTests/CreatePrenatalTestDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.Validations.PrenatalTests;

public class CreatePrenatalTestDtoValidator : AbstractValidator<CreatePrenatalTestDto>
{
    public CreatePrenatalTestDtoValidator()
    {
        RuleFor(x => x.TestTypeId).NotEmpty();
        RuleFor(x => x.TestDateTime).NotEmpty();
        RuleFor(x => x.ResultText).MaximumLength(5000);
    }
}

// File: FPT.EXE201.Application/Validations/PrenatalTests/UpdatePrenatalTestDtoValidator.cs
using FluentValidation;
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.Validations.PrenatalTests;

public class UpdatePrenatalTestDtoValidator : AbstractValidator<UpdatePrenatalTestDto>
{
    public UpdatePrenatalTestDtoValidator()
    {
        RuleFor(x => x.ResultText).MaximumLength(5000);
    }
}
```

**⚠️ NOTE**: Validators auto-registered via `AddValidatorsFromAssembly` trong `DependencyInjection.cs` — KHÔNG cần register thủ công.

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 7/10 — Repository Interfaces + Service Interfaces

**Nhiệm vụ**: Tạo Repository interfaces, Service interfaces, và update `IUnitOfWork`.

**Code — Repository Interfaces**:

```csharp
// File: FPT.EXE201.Application/IRepositories/IPregnancyRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyRepository : IGenericRepository<Pregnancy>
{
    /// <summary>Lấy thai kỳ Active của user (chỉ có tối đa 1).</summary>
    Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả thai kỳ của user (bao gồm ended).</summary>
    Task<List<Pregnancy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy pregnancy_no tiếp theo cho user (max + 1).</summary>
    Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IPregnancyConditionRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyConditionRepository : IGenericRepository<PregnancyCondition>
{
    /// <summary>Lấy tất cả conditions của 1 pregnancy, include ref data + translations.</summary>
    Task<List<PregnancyCondition>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IPrenatalVisitRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPrenatalVisitRepository : IGenericRepository<PrenatalVisit>
{
    /// <summary>Lấy tất cả visits của 1 pregnancy, sắp xếp theo ngày khám desc.</summary>
    Task<List<PrenatalVisit>> GetByPregnancyIdAsync(Guid pregnancyId, CancellationToken cancellationToken = default);

    /// <summary>Lấy visit kèm danh sách tests.</summary>
    Task<PrenatalVisit?> GetByIdWithTestsAsync(Guid id, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IPrenatalTestRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPrenatalTestRepository : IGenericRepository<PrenatalTest>
{
    /// <summary>Lấy tất cả tests của 1 pregnancy, include test type translations.</summary>
    Task<List<PrenatalTest>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default);

    /// <summary>Lấy 1 test theo ID, include test type + translations theo lang.</summary>
    Task<PrenatalTest?> GetByIdWithTranslationsAsync(Guid id, string langCode, CancellationToken cancellationToken = default);

    /// <summary>Lấy tests theo visit.</summary>
    Task<List<PrenatalTest>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IRefPregnancyConditionRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefPregnancyConditionRepository : IGenericRepository<RefPregnancyCondition>
{
    /// <summary>Lấy tất cả conditions đang active, include translation theo lang.</summary>
    Task<List<RefPregnancyCondition>> GetActiveWithTranslationsAsync(string langCode, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IRepositories/IRefTestTypeRepository.cs
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefTestTypeRepository : IGenericRepository<RefTestType>
{
    /// <summary>Lấy tất cả test types đang active, include translation theo lang. Optional filter by category.</summary>
    Task<List<RefTestType>> GetActiveWithTranslationsAsync(string langCode, string? category = null, CancellationToken cancellationToken = default);
}
```

**Code — Service Interfaces**:

```csharp
// File: FPT.EXE201.Application/IServices/IPregnancyService.cs
using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.IServices;

public interface IPregnancyService
{
    Task<PregnancyDto> CreateAsync(Guid userId, CreatePregnancyDto dto, CancellationToken cancellationToken = default);
    Task<PregnancyDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<PregnancyDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PregnancyDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<PregnancyDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyDto dto, CancellationToken cancellationToken = default);
    Task<PregnancyDto> ChangeStatusAsync(Guid id, Guid userId, ChangePregnancyStatusDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/IPregnancyConditionService.cs
using FPT.EXE201.Application.DTOs.PregnancyConditions;

namespace FPT.EXE201.Application.IServices;

public interface IPregnancyConditionService
{
    Task<PregnancyConditionDto> AddAsync(Guid pregnancyId, Guid userId, CreatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default);
    Task<List<PregnancyConditionDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PregnancyConditionDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/IPrenatalVisitService.cs
using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.IServices;

public interface IPrenatalVisitService
{
    Task<PrenatalVisitDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalVisitDto dto, CancellationToken cancellationToken = default);
    Task<List<PrenatalVisitDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PrenatalVisitDetailDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PrenatalVisitDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalVisitDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/IPrenatalTestService.cs
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.IServices;

public interface IPrenatalTestService
{
    Task<PrenatalTestDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalTestDto dto, string langCode, CancellationToken cancellationToken = default);
    Task<List<PrenatalTestDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PrenatalTestDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PrenatalTestDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalTestDto dto, string langCode, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

// File: FPT.EXE201.Application/IServices/IRefDataService.cs
using FPT.EXE201.Application.DTOs.RefData;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service cho reference/lookup data — public endpoints, không cần auth.
/// User cần lấy danh mục bệnh lý và xét nghiệm trước khi tạo records.
/// </summary>
public interface IRefDataService
{
    Task<List<RefConditionDto>> GetActiveConditionsAsync(string langCode, CancellationToken cancellationToken = default);
    Task<List<RefTestTypeDto>> GetActiveTestTypesAsync(string langCode, string? category = null, CancellationToken cancellationToken = default);
}
```

**Update `IUnitOfWork`** — thêm Week 3 repositories:

```csharp
// Add to IUnitOfWork.cs (after existing repository properties)

// Week 3 — Pregnancy Core
IPregnancyRepository Pregnancies { get; }
IPregnancyConditionRepository PregnancyConditions { get; }
IPrenatalVisitRepository PrenatalVisits { get; }
IPrenatalTestRepository PrenatalTests { get; }
IRefPregnancyConditionRepository RefPregnancyConditions { get; }
IRefTestTypeRepository RefTestTypes { get; }
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 8/10 — Repository Implementations + UnitOfWork Update

**Nhiệm vụ**: Implement tất cả repositories + update `UnitOfWork`.

**⚠️ PATTERN**: Kế thừa `GenericRepository<T>`, constructor nhận `AppDbContext`.

**Code**:

```csharp
// File: FPT.EXE201.Infrastructure/Repositories/PregnancyRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyRepository : GenericRepository<Pregnancy>, IPregnancyRepository
{
    public PregnancyRepository(AppDbContext context) : base(context) { }

    public async Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.Status == PregnancyStatus.Active && p.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Pregnancy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Include deleted pregnancies to avoid pregnancy_no collisions
        var maxNo = await _dbSet
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .MaxAsync(p => (int?)p.PregnancyNumber, cancellationToken);

        return (maxNo ?? 0) + 1;
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/PregnancyConditionRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyConditionRepository : GenericRepository<PregnancyCondition>, IPregnancyConditionRepository
{
    public PregnancyConditionRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyCondition>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pc => pc.PregnancyId == pregnancyId && pc.DeletedAt == null)
            .Include(pc => pc.Condition)
                .ThenInclude(c => c.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(pc => pc.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/PrenatalVisitRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PrenatalVisitRepository : GenericRepository<PrenatalVisit>, IPrenatalVisitRepository
{
    public PrenatalVisitRepository(AppDbContext context) : base(context) { }

    public async Task<List<PrenatalVisit>> GetByPregnancyIdAsync(Guid pregnancyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.PregnancyId == pregnancyId && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
            .OrderByDescending(v => v.VisitDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PrenatalVisit?> GetByIdWithTestsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Id == id && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
                .ThenInclude(t => t.TestType)
                    .ThenInclude(tt => tt.Translations)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/PrenatalTestRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PrenatalTestRepository : GenericRepository<PrenatalTest>, IPrenatalTestRepository
{
    public PrenatalTestRepository(AppDbContext context) : base(context) { }

    public async Task<List<PrenatalTest>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.PregnancyId == pregnancyId && t.DeletedAt == null)
            .Include(t => t.TestType)
                .ThenInclude(tt => tt.Translations.Where(tr => tr.LanguageCode == langCode))
            .OrderByDescending(t => t.TestDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PrenatalTest?> GetByIdWithTranslationsAsync(Guid id, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Id == id && t.DeletedAt == null)
            .Include(t => t.TestType)
                .ThenInclude(tt => tt.Translations.Where(tr => tr.LanguageCode == langCode))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PrenatalTest>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.VisitId == visitId && t.DeletedAt == null)
            .Include(t => t.TestType)
            .OrderByDescending(t => t.TestDateTime)
            .ToListAsync(cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/RefPregnancyConditionRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefPregnancyConditionRepository : GenericRepository<RefPregnancyCondition>, IRefPregnancyConditionRepository
{
    public RefPregnancyConditionRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefPregnancyCondition>> GetActiveWithTranslationsAsync(string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive && r.DeletedAt == null)
            .Include(r => r.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }
}

// File: FPT.EXE201.Infrastructure/Repositories/RefTestTypeRepository.cs
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefTestTypeRepository : GenericRepository<RefTestType>, IRefTestTypeRepository
{
    public RefTestTypeRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefTestType>> GetActiveWithTranslationsAsync(string langCode, string? category = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(r => r.IsActive && r.DeletedAt == null);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(r => r.Category == category);

        return await query
            .Include(r => r.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }
}
```

**Update `UnitOfWork`** — thêm Week 3 repositories (sử dụng `??=` pattern):

```csharp
// Add to UnitOfWork.cs

// Fields (after existing fields)
private IPregnancyRepository? _pregnancies;
private IPregnancyConditionRepository? _pregnancyConditions;
private IPrenatalVisitRepository? _prenatalVisits;
private IPrenatalTestRepository? _prenatalTests;
private IRefPregnancyConditionRepository? _refPregnancyConditions;
private IRefTestTypeRepository? _refTestTypes;

// Properties (after existing properties)
public IPregnancyRepository Pregnancies => _pregnancies ??= new PregnancyRepository(_context);
public IPregnancyConditionRepository PregnancyConditions => _pregnancyConditions ??= new PregnancyConditionRepository(_context);
public IPrenatalVisitRepository PrenatalVisits => _prenatalVisits ??= new PrenatalVisitRepository(_context);
public IPrenatalTestRepository PrenatalTests => _prenatalTests ??= new PrenatalTestRepository(_context);
public IRefPregnancyConditionRepository RefPregnancyConditions => _refPregnancyConditions ??= new RefPregnancyConditionRepository(_context);
public IRefTestTypeRepository RefTestTypes => _refTestTypes ??= new RefTestTypeRepository(_context);
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 9/10 — Service Implementations

**Nhiệm vụ**: Implement tất cả services với business logic + ownership checks.

**⚠️ CRITICAL Business Logic**:
1. **State Transition**: Chỉ `Active → {Delivered, Ended, Miscarriage}`, KHÔNG reverse
2. **Auto EDD**: Khi set/update LMP → recalculate EDD = LMP + 280 days
3. **Auto Gestational Week**: Calculated on read, NOT stored permanently
4. **Ownership**: Mọi operation phải verify `pregnancy.UserId == currentUserId`
5. **Visit-Test Consistency**: Test.VisitId (nếu có) phải thuộc cùng pregnancy
6. **Soft Delete**: Dùng `SoftDeleteAsync`, KHÔNG dùng `Delete`

**Code**:

```csharp
// File: FPT.EXE201.Application/Services/PregnancyService.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class PregnancyService : IPregnancyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PregnancyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PregnancyDto> CreateAsync(Guid userId, CreatePregnancyDto dto, CancellationToken cancellationToken = default)
    {
        // Business rule: chỉ 1 active pregnancy per user
        var existing = await _unitOfWork.Pregnancies.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existing != null)
            throw new ConflictException("You already have an active pregnancy. Please end or deliver the current pregnancy before creating a new one.");

        var nextNo = await _unitOfWork.Pregnancies.GetNextPregnancyNumberAsync(userId, cancellationToken);

        var pregnancy = new Pregnancy
        {
            UserId = userId,
            PregnancyNumber = nextNo,
            Status = PregnancyStatus.Active,
            LastMenstrualPeriodDate = dto.LastMenstrualPeriodDate,
            EstimatedConceptionDate = dto.EstimatedConceptionDate,
            Notes = dto.Notes,
            // Nhóm 1
            BabyNickname = dto.BabyNickname,
            BabyGender = dto.BabyGender,
            PregnancyType = dto.PregnancyType,
            // Nhóm 2
            MotherBloodType = dto.MotherBloodType,
            PrePregnancyWeightKg = dto.PrePregnancyWeightKg,
            HeightCm = dto.HeightCm,
            // Nhóm 3
            DueDateSource = dto.DueDateSource,
            Gravida = dto.Gravida,
            Para = dto.Para,
            CoverImageUrl = dto.CoverImageUrl
        };

        // Auto-calculate EDD from LMP
        if (dto.LastMenstrualPeriodDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.LastMenstrualPeriodDate.Value.AddDays(280);
            pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(dto.LastMenstrualPeriodDate);
        }

        await _unitOfWork.Pregnancies.AddAsync(pregnancy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetActiveByUserIdAsync(userId, cancellationToken);
        if (pregnancy == null) return null;

        // Recalculate gestational week on read
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        return MapToDto(pregnancy);
    }

    public async Task<List<PregnancyDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancies = await _unitOfWork.Pregnancies.GetByUserIdAsync(userId, cancellationToken);
        return pregnancies.Select(p =>
        {
            p.CurrentGestationalWeek = CalculateCurrentGestationalWeek(p.LastMenstrualPeriodDate);
            return MapToDto(p);
        }).ToList();
    }

    public async Task<PregnancyDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnership(id, userId, cancellationToken);
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyDto dto, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);

        pregnancy.LastMenstrualPeriodDate = dto.LastMenstrualPeriodDate ?? pregnancy.LastMenstrualPeriodDate;
        pregnancy.EstimatedConceptionDate = dto.EstimatedConceptionDate ?? pregnancy.EstimatedConceptionDate;
        pregnancy.Notes = dto.Notes ?? pregnancy.Notes;

        // Nhóm 1
        pregnancy.BabyNickname = dto.BabyNickname ?? pregnancy.BabyNickname;
        if (dto.BabyGender.HasValue) pregnancy.BabyGender = dto.BabyGender.Value;
        if (dto.PregnancyType.HasValue) pregnancy.PregnancyType = dto.PregnancyType.Value;

        // Nhóm 2
        pregnancy.MotherBloodType = dto.MotherBloodType ?? pregnancy.MotherBloodType;
        pregnancy.PrePregnancyWeightKg = dto.PrePregnancyWeightKg ?? pregnancy.PrePregnancyWeightKg;
        pregnancy.HeightCm = dto.HeightCm ?? pregnancy.HeightCm;

        // Nhóm 3
        if (dto.DueDateSource.HasValue) pregnancy.DueDateSource = dto.DueDateSource.Value;
        pregnancy.Gravida = dto.Gravida ?? pregnancy.Gravida;
        pregnancy.Para = dto.Para ?? pregnancy.Para;
        pregnancy.CoverImageUrl = dto.CoverImageUrl ?? pregnancy.CoverImageUrl;

        // Recalculate EDD if LMP changed
        if (dto.LastMenstrualPeriodDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.LastMenstrualPeriodDate.Value.AddDays(280);
            pregnancy.DueDateSource = Domain.Enums.DueDateSource.LMP;
        }
        // Nếu FE truyền EDD mới (bác sĩ điều chỉnh), dùng EDD đó thay thế
        if (dto.ExpectedDeliveryDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        }
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);

        _unitOfWork.Pregnancies.Update(pregnancy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto> ChangeStatusAsync(Guid id, Guid userId, ChangePregnancyStatusDto dto, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);

        // Business rule: only Active → terminal states
        if (pregnancy.Status != PregnancyStatus.Active)
            throw new BadRequestException("Can only change status of an active pregnancy");
        if (dto.Status == PregnancyStatus.Active)
            throw new BadRequestException("Cannot revert to Active status");

        pregnancy.Status = dto.Status;

        // Lưu thông tin sinh khi status = Delivered
        if (dto.Status == PregnancyStatus.Delivered)
        {
            pregnancy.ActualDeliveryDate = dto.ActualDeliveryDate;
            pregnancy.DeliveryMethod = dto.DeliveryMethod;
        }

        _unitOfWork.Pregnancies.Update(pregnancy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);
        await _unitOfWork.Pregnancies.SoftDeleteAsync(pregnancy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ═══ Private Helpers ═══

    private async Task<Pregnancy> GetAndVerifyOwnership(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy with id '{id}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
        return pregnancy;
    }

    private async Task<Pregnancy> GetAndVerifyOwnershipTracked(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy with id '{id}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
        return pregnancy;
    }

    private static int? CalculateCurrentGestationalWeek(DateTime? lastMenstrualPeriodDate)
    {
        if (!lastMenstrualPeriodDate.HasValue) return null;
        var totalDays = (DateTime.UtcNow - lastMenstrualPeriodDate.Value).Days;
        if (totalDays < 0) return null;
        var weeks = totalDays / 7;
        return weeks <= 45 ? weeks : null;
    }

    private static string? FormatGestationalAge(DateTime? lastMenstrualPeriodDate)
    {
        if (!lastMenstrualPeriodDate.HasValue) return null;
        var totalDays = (DateTime.UtcNow - lastMenstrualPeriodDate.Value).Days;
        if (totalDays < 0) return null;
        var weeks = totalDays / 7;
        var remainingDays = totalDays % 7;
        return weeks <= 45 ? $"{weeks}w{remainingDays}d" : null;
    }

    private static decimal? CalculateBmi(decimal? weightKg, decimal? heightCm)
    {
        if (!weightKg.HasValue || !heightCm.HasValue || heightCm.Value <= 0) return null;
        var heightM = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (heightM * heightM), 1);
    }

    private static string? FormatObstetricFormula(int? gravida, int? para)
    {
        if (!gravida.HasValue) return null;
        return para.HasValue ? $"G{gravida}P{para}" : $"G{gravida}";
    }

    private PregnancyDto MapToDto(Pregnancy pregnancy)
    {
        return new PregnancyDto(
            Id: pregnancy.Id,
            UserId: pregnancy.UserId,
            PregnancyNumber: pregnancy.PregnancyNumber,
            Status: pregnancy.Status.ToString(),
            LastMenstrualPeriodDate: pregnancy.LastMenstrualPeriodDate,
            ExpectedDeliveryDate: pregnancy.ExpectedDeliveryDate,
            EstimatedConceptionDate: pregnancy.EstimatedConceptionDate,
            CurrentGestationalWeek: pregnancy.CurrentGestationalWeek,
            GestationalAgeDisplay: FormatGestationalAge(pregnancy.LastMenstrualPeriodDate),
            Notes: pregnancy.Notes,
            // Nhóm 1
            BabyNickname: pregnancy.BabyNickname,
            BabyGender: pregnancy.BabyGender.ToString(),
            PregnancyType: pregnancy.PregnancyType.ToString(),
            // Nhóm 2
            MotherBloodType: pregnancy.MotherBloodType,
            PrePregnancyWeightKg: pregnancy.PrePregnancyWeightKg,
            HeightCm: pregnancy.HeightCm,
            PrePregnancyBmi: CalculateBmi(pregnancy.PrePregnancyWeightKg, pregnancy.HeightCm),
            // Nhóm 3
            DueDateSource: pregnancy.DueDateSource.ToString(),
            Gravida: pregnancy.Gravida,
            Para: pregnancy.Para,
            ObstetricFormula: FormatObstetricFormula(pregnancy.Gravida, pregnancy.Para),
            ActualDeliveryDate: pregnancy.ActualDeliveryDate,
            DeliveryMethod: pregnancy.DeliveryMethod?.ToString(),
            CoverImageUrl: pregnancy.CoverImageUrl,
            CreatedAt: pregnancy.CreatedAt,
            UpdatedAt: pregnancy.UpdatedAt
        );
    }
}

// File: FPT.EXE201.Application/Services/PregnancyConditionService.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.PregnancyConditions;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PregnancyConditionService : IPregnancyConditionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PregnancyConditionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PregnancyConditionDto> AddAsync(Guid pregnancyId, Guid userId, CreatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        // Verify pregnancy ownership
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        // Verify condition exists in reference data
        var refCondition = await _unitOfWork.RefPregnancyConditions.GetByIdAsync(dto.ConditionId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Condition '{dto.ConditionId}' not found in reference data");

        // Check duplicate: same condition already assigned to this pregnancy
        var existingCondition = await _unitOfWork.PregnancyConditions
            .ExistsAsync(pc => pc.PregnancyId == pregnancyId && pc.ConditionId == dto.ConditionId && pc.DeletedAt == null, cancellationToken);
        if (existingCondition)
            throw new ConflictException($"Condition '{refCondition.Code}' is already assigned to this pregnancy");

        var entity = new PregnancyCondition
        {
            PregnancyId = pregnancyId,
            ConditionId = dto.ConditionId,
            DiagnosedDate = dto.DiagnosedDate,
            Severity = dto.Severity,
            Notes = dto.Notes
        };

        await _unitOfWork.PregnancyConditions.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        var saved = conditions.First(c => c.Id == entity.Id);
        return MapToDto(saved, langCode);
    }

    public async Task<List<PregnancyConditionDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        return conditions.Select(c => MapToDto(c, langCode)).ToList();
    }

    public async Task<PregnancyConditionDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        var condition = await _unitOfWork.PregnancyConditions.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy condition '{id}' not found");

        // Verify ownership through pregnancy
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(condition.PregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Pregnancy not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        condition.DiagnosedDate = dto.DiagnosedDate;
        condition.Severity = dto.Severity;
        condition.Notes = dto.Notes;

        _unitOfWork.PregnancyConditions.Update(condition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(condition.PregnancyId, langCode, cancellationToken);
        var updated = conditions.First(c => c.Id == id);
        return MapToDto(updated, langCode);
    }

    public async Task RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var condition = await _unitOfWork.PregnancyConditions.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy condition '{id}' not found");

        // Verify ownership through pregnancy
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(condition.PregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Pregnancy not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        await _unitOfWork.PregnancyConditions.SoftDeleteAsync(condition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PregnancyConditionDto MapToDto(PregnancyCondition entity, string langCode)
    {
        var translation = entity.Condition?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode);
        return new PregnancyConditionDto(
            Id: entity.Id,
            PregnancyId: entity.PregnancyId,
            ConditionId: entity.ConditionId,
            ConditionCode: entity.Condition?.Code ?? "",
            ConditionDisplayName: translation?.DisplayName ?? entity.Condition?.Code ?? "",
            ConditionDescription: translation?.Description,
            DiagnosedDate: entity.DiagnosedDate,
            Severity: entity.Severity?.ToString(),
            Notes: entity.Notes,
            CreatedAt: entity.CreatedAt
        );
    }
}

// File: FPT.EXE201.Application/Services/PrenatalVisitService.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PrenatalVisitService : IPrenatalVisitService
{
    private readonly IUnitOfWork _unitOfWork;

    public PrenatalVisitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PrenatalVisitDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalVisitDto dto, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var visit = new PrenatalVisit
        {
            PregnancyId = pregnancyId,
            DoctorId = dto.DoctorId,
            VisitDateTime = dto.VisitDateTime,
            VisitType = dto.VisitType,
            Location = dto.Location,
            Notes = dto.Notes,
            VitalsJson = dto.VitalsJson
        };

        await _unitOfWork.PrenatalVisits.AddAsync(visit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(visit);
    }

    public async Task<List<PrenatalVisitDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var visits = await _unitOfWork.PrenatalVisits.GetByPregnancyIdAsync(pregnancyId, cancellationToken);
        return visits.Select(MapToDto).ToList();
    }

    public async Task<PrenatalVisitDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalVisitDto dto, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        visit.DoctorId = dto.DoctorId;
        visit.VisitDateTime = dto.VisitDateTime;
        visit.VisitType = dto.VisitType;
        visit.Location = dto.Location;
        visit.Notes = dto.Notes;
        visit.VitalsJson = dto.VitalsJson;

        _unitOfWork.PrenatalVisits.Update(visit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(visit);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        await _unitOfWork.PrenatalVisits.SoftDeleteAsync(visit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrenatalVisitDetailDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdWithTestsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        return MapToDetailDto(visit, langCode);
    }

    private async Task VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
    }

    private static PrenatalVisitDto MapToDto(PrenatalVisit visit)
    {
        return new PrenatalVisitDto(
            Id: visit.Id,
            PregnancyId: visit.PregnancyId,
            DoctorId: visit.DoctorId,
            VisitDateTime: visit.VisitDateTime,
            VisitType: visit.VisitType.ToString(),
            Location: visit.Location,
            Notes: visit.Notes,
            VitalsJson: visit.VitalsJson,
            TestCount: visit.Tests?.Count(t => t.DeletedAt == null) ?? 0,
            CreatedAt: visit.CreatedAt
        );
    }

    private static PrenatalVisitDetailDto MapToDetailDto(PrenatalVisit visit, string langCode)
    {
        var tests = visit.Tests?
            .Where(t => t.DeletedAt == null)
            .Select(t =>
            {
                var translation = t.TestType?.Translations?.FirstOrDefault(tr => tr.LanguageCode == langCode);
                return new PrenatalTestDto(
                    Id: t.Id,
                    PregnancyId: t.PregnancyId,
                    VisitId: t.VisitId,
                    TestTypeId: t.TestTypeId,
                    TestTypeCode: t.TestType?.Code ?? "",
                    TestTypeDisplayName: translation?.DisplayName ?? t.TestType?.Code ?? "",
                    TestDateTime: t.TestDateTime,
                    ResultText: t.ResultText,
                    ResultJson: t.ResultJson,
                    IsAbnormalResult: t.IsAbnormalResult,
                    CreatedAt: t.CreatedAt
                );
            })
            .OrderByDescending(t => t.TestDateTime)
            .ToList() ?? new List<PrenatalTestDto>();

        return new PrenatalVisitDetailDto(
            Id: visit.Id,
            PregnancyId: visit.PregnancyId,
            DoctorId: visit.DoctorId,
            VisitDateTime: visit.VisitDateTime,
            VisitType: visit.VisitType.ToString(),
            Location: visit.Location,
            Notes: visit.Notes,
            VitalsJson: visit.VitalsJson,
            Tests: tests,
            CreatedAt: visit.CreatedAt
        );
    }
}

// File: FPT.EXE201.Application/Services/PrenatalTestService.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PrenatalTestService : IPrenatalTestService
{
    private readonly IUnitOfWork _unitOfWork;

    public PrenatalTestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PrenatalTestDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalTestDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        // Verify test type exists
        var testType = await _unitOfWork.RefTestTypes.GetByIdAsync(dto.TestTypeId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test type '{dto.TestTypeId}' not found");

        // If VisitId provided, verify it belongs to the SAME pregnancy
        if (dto.VisitId.HasValue)
        {
            var visit = await _unitOfWork.PrenatalVisits.GetByIdAsync(dto.VisitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException($"Visit '{dto.VisitId}' not found");
            if (visit.PregnancyId != pregnancyId)
                throw new BadRequestException("The specified visit does not belong to this pregnancy");
        }

        var test = new PrenatalTest
        {
            PregnancyId = pregnancyId,
            VisitId = dto.VisitId,
            TestTypeId = dto.TestTypeId,
            TestDateTime = dto.TestDateTime,
            ResultText = dto.ResultText,
            ResultJson = dto.ResultJson,
            IsAbnormalResult = dto.IsAbnormalResult
        };

        await _unitOfWork.PrenatalTests.AddAsync(test, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        var saved = tests.First(t => t.Id == test.Id);
        return MapToDto(saved, langCode);
    }

    public async Task<List<PrenatalTestDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        return tests.Select(t => MapToDto(t, langCode)).ToList();
    }

    public async Task<PrenatalTestDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalTestDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        test.ResultText = dto.ResultText;
        test.ResultJson = dto.ResultJson;
        test.IsAbnormalResult = dto.IsAbnormalResult;

        _unitOfWork.PrenatalTests.Update(test);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations
        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(test.PregnancyId, langCode, cancellationToken);
        var updated = tests.First(t => t.Id == id);
        return MapToDto(updated, langCode);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        await _unitOfWork.PrenatalTests.SoftDeleteAsync(test, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrenatalTestDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdWithTranslationsAsync(id, langCode, cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        return MapToDto(test, langCode);
    }

    private async Task VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
    }

    private static PrenatalTestDto MapToDto(PrenatalTest test, string langCode)
    {
        var translation = test.TestType?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode);
        return new PrenatalTestDto(
            Id: test.Id,
            PregnancyId: test.PregnancyId,
            VisitId: test.VisitId,
            TestTypeId: test.TestTypeId,
            TestTypeCode: test.TestType?.Code ?? "",
            TestTypeDisplayName: translation?.DisplayName ?? test.TestType?.Code ?? "",
            TestDateTime: test.TestDateTime,
            ResultText: test.ResultText,
            ResultJson: test.ResultJson,
            IsAbnormalResult: test.IsAbnormalResult,
            CreatedAt: test.CreatedAt
        );
    }
}

// File: FPT.EXE201.Application/Services/RefDataService.cs
using FPT.EXE201.Application.DTOs.RefData;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class RefDataService : IRefDataService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefDataService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RefConditionDto>> GetActiveConditionsAsync(string langCode, CancellationToken cancellationToken = default)
    {
        var conditions = await _unitOfWork.RefPregnancyConditions.GetActiveWithTranslationsAsync(langCode, cancellationToken);
        return conditions.Select(c =>
        {
            var t = c.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefConditionDto(c.Id, c.Code, t?.DisplayName ?? c.Code, t?.Description);
        }).ToList();
    }

    public async Task<List<RefTestTypeDto>> GetActiveTestTypesAsync(string langCode, string? category = null, CancellationToken cancellationToken = default)
    {
        var testTypes = await _unitOfWork.RefTestTypes.GetActiveWithTranslationsAsync(langCode, category, cancellationToken);
        return testTypes.Select(tt =>
        {
            var t = tt.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefTestTypeDto(tt.Id, tt.Code, tt.Category, t?.DisplayName ?? tt.Code, t?.Description);
        }).ToList();
    }
}
```

**Register Services** — add to `FPT.EXE201.Application/DependencyInjection.cs`:

```csharp
// Week 3 — Pregnancy Core
services.AddScoped<IPregnancyService, PregnancyService>();
services.AddScoped<IPregnancyConditionService, PregnancyConditionService>();
services.AddScoped<IPrenatalVisitService, PrenatalVisitService>();
services.AddScoped<IPrenatalTestService, PrenatalTestService>();
services.AddScoped<IRefDataService, RefDataService>();
```

**✅ Checkpoint**: Build thành công.

---

## 🎯 PROMPT 10/10 — Controllers + AutoMapper + Permissions

**Nhiệm vụ**: Tạo Controllers, AutoMapper profile, Permissions, và Ref Data endpoint.

**⚠️ PATTERN**: Kế thừa `BaseApiController`, dùng `Success()` / `Created()` / `GetCurrentUserId()`.

**Code — AutoMapper Profile**:

```csharp
// File: FPT.EXE201.Application/MapperProfiles/PregnancyMappingProfile.cs
using AutoMapper;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.MapperProfiles;

public class PregnancyMappingProfile : Profile
{
    public PregnancyMappingProfile()
    {
        // Pregnancy → PregnancyDto mapping (used where AutoMapper is preferred)
        // Note: Most mapping is done manually in services for computed fields.
        // This profile covers basic mappings and CreateDto → Entity.

        CreateMap<CreatePregnancyDto, Pregnancy>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.PregnancyNumber, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.ExpectedDeliveryDate, o => o.Ignore())
            .ForMember(d => d.CurrentGestationalWeek, o => o.Ignore())
            .ForMember(d => d.ActualDeliveryDate, o => o.Ignore())
            .ForMember(d => d.DeliveryMethod, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Conditions, o => o.Ignore())
            .ForMember(d => d.Visits, o => o.Ignore())
            .ForMember(d => d.Tests, o => o.Ignore());

        CreateMap<CreatePrenatalVisitDto, PrenatalVisit>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.PregnancyId, o => o.Ignore())
            .ForMember(d => d.Pregnancy, o => o.Ignore())
            .ForMember(d => d.Tests, o => o.Ignore());
    }
}
```

**Code — Permissions** (add to existing Permission seed data):

```csharp
// ⚠️ Add these permission records to the existing PermissionConfiguration seed data
// or create a separate PermissionSeeder for Week 3.
// Permission codes follow convention: "module.resource.action"

// Week 3 Permissions:
"pregnancy.read"
"pregnancy.write"
"pregnancy.delete"
"pregnancy.condition.read"
"pregnancy.condition.write"
"pregnancy.condition.delete"
"pregnancy.visit.read"
"pregnancy.visit.write"
"pregnancy.visit.delete"
"pregnancy.test.read"
"pregnancy.test.write"
"pregnancy.test.delete"

// Assign to USER role: all of above
// Assign to ADMIN role: all of above
```

**Code — Controllers**:

```csharp
// File: FPT.EXE201.Api/Controllers/PregnanciesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

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

    [HttpGet("active")]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _pregnancyService.GetActiveAsync(GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _pregnancyService.GetAllByUserAsync(GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _pregnancyService.GetByIdAsync(id, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePregnancyDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Pregnancy updated successfully");
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangePregnancyStatusDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.ChangeStatusAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Pregnancy status changed successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("pregnancy.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _pregnancyService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Pregnancy deleted successfully");
    }
}

// File: FPT.EXE201.Api/Controllers/PregnancyConditionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PregnancyConditions;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Route("api/pregnancies/{pregnancyId:guid}/conditions")]
[Authorize]
public class PregnancyConditionsController : BaseApiController
{
    private readonly IPregnancyConditionService _conditionService;

    public PregnancyConditionsController(IPregnancyConditionService conditionService)
    {
        _conditionService = conditionService;
    }

    [HttpPost]
    [RequirePermission("pregnancy.condition.write")]
    public async Task<IActionResult> Add(Guid pregnancyId, [FromBody] CreatePregnancyConditionDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.AddAsync(pregnancyId, GetCurrentUserId(), dto, lang, ct);
        return Created(result, "Condition added successfully");
    }

    [HttpGet]
    [RequirePermission("pregnancy.condition.read")]
    public async Task<IActionResult> GetAll(Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("pregnancy.condition.write")]
    public async Task<IActionResult> Update(Guid pregnancyId, Guid id, [FromBody] UpdatePregnancyConditionDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.UpdateAsync(id, GetCurrentUserId(), dto, lang, ct);
        return Success(result, "Condition updated successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("pregnancy.condition.delete")]
    public async Task<IActionResult> Remove(Guid pregnancyId, Guid id, CancellationToken ct)
    {
        await _conditionService.RemoveAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Condition removed successfully");
    }
}

// File: FPT.EXE201.Api/Controllers/PrenatalVisitsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Authorize]
public class PrenatalVisitsController : BaseApiController
{
    private readonly IPrenatalVisitService _visitService;

    public PrenatalVisitsController(IPrenatalVisitService visitService)
    {
        _visitService = visitService;
    }

    [HttpPost("api/pregnancies/{pregnancyId:guid}/visits")]
    [RequirePermission("pregnancy.visit.write")]
    public async Task<IActionResult> Create(Guid pregnancyId, [FromBody] CreatePrenatalVisitDto dto, CancellationToken ct)
    {
        var result = await _visitService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Visit created successfully");
    }

    [HttpGet("api/pregnancies/{pregnancyId:guid}/visits")]
    [RequirePermission("pregnancy.visit.read")]
    public async Task<IActionResult> GetByPregnancy(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _visitService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.read")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _visitService.GetByIdAsync(id, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPut("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrenatalVisitDto dto, CancellationToken ct)
    {
        var result = await _visitService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Visit updated successfully");
    }

    [HttpDelete("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _visitService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Visit deleted successfully");
    }
}

// File: FPT.EXE201.Api/Controllers/PrenatalTestsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Authorize]
public class PrenatalTestsController : BaseApiController
{
    private readonly IPrenatalTestService _testService;

    public PrenatalTestsController(IPrenatalTestService testService)
    {
        _testService = testService;
    }

    [HttpPost("api/pregnancies/{pregnancyId:guid}/tests")]
    [RequirePermission("pregnancy.test.write")]
    public async Task<IActionResult> Create(Guid pregnancyId, [FromBody] CreatePrenatalTestDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, lang, ct);
        return Created(result, "Test created successfully");
    }

    [HttpGet("api/pregnancies/{pregnancyId:guid}/tests")]
    [RequirePermission("pregnancy.test.read")]
    public async Task<IActionResult> GetByPregnancy(Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpGet("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.read")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.GetByIdAsync(id, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPut("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrenatalTestDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.UpdateAsync(id, GetCurrentUserId(), dto, lang, ct);
        return Success(result, "Test updated successfully");
    }

    [HttpDelete("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _testService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Test deleted successfully");
    }
}

// File: FPT.EXE201.Api/Controllers/RefDataController.cs
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

/// <summary>
/// Public endpoints cho reference/lookup data.
/// Không cần authentication — user cần xem danh mục trước khi đăng ký.
/// </summary>
[Route("api/ref")]
public class RefDataController : BaseApiController
{
    private readonly IRefDataService _refDataService;

    public RefDataController(IRefDataService refDataService)
    {
        _refDataService = refDataService;
    }

    /// <summary>
    /// Lấy danh mục bệnh lý thai kỳ.
    /// Frontend gọi khi hiển thị dropdown "Chọn bệnh lý".
    /// </summary>
    [HttpGet("pregnancy-conditions")]
    public async Task<IActionResult> GetConditions([FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveConditionsAsync(lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Lấy danh mục loại xét nghiệm.
    /// Optional filter by category: LAB, IMAGING, OTHER.
    /// </summary>
    [HttpGet("test-types")]
    public async Task<IActionResult> GetTestTypes([FromQuery] string lang = "vi", [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveTestTypesAsync(lang, category, ct);
        return Success(result);
    }
}
```

**✅ Checkpoint**: Build thành công.

---

## 📱 FE INTEGRATION FLOW — API Usage Guide

> **Lưu ý cho Frontend**: Tất cả endpoints `pregnancy.*` đều dùng JWT token để xác định user.
> FE KHÔNG cần truyền `userId` — server tự lấy từ token via `GetCurrentUserId()`.

### Bước 1: Lấy `pregnancyId` (bắt buộc trước khi truy cập sub-resources)
```
GET /api/pregnancies/active
Authorization: Bearer {jwt_token}

→ Response: { data: { id: "pregnancy-guid-here", ... } }
→ Lưu pregnancyId này vào state/context của app
```

### Bước 2: Truy cập sub-resources bằng `pregnancyId`
```
GET /api/pregnancies/{pregnancyId}/conditions?lang=vi     → Danh sách bệnh lý
GET /api/pregnancies/{pregnancyId}/visits                  → Danh sách buổi khám
GET /api/pregnancies/{pregnancyId}/tests?lang=vi           → Danh sách xét nghiệm
```

### Bước 3: Xem chi tiết (FE đã có item ID từ danh sách ở Bước 2)
```
GET /api/visits/{visitId}?lang=vi     → Chi tiết buổi khám + danh sách tests
GET /api/tests/{testId}?lang=vi       → Chi tiết xét nghiệm
```

### Bước 4: Tạo/Cập nhật sub-resources
```
POST   /api/pregnancies/{pregnancyId}/visits              → Tạo buổi khám
PUT    /api/visits/{visitId}                               → Cập nhật buổi khám
DELETE /api/visits/{visitId}                               → Xoá buổi khám

POST   /api/pregnancies/{pregnancyId}/tests               → Tạo xét nghiệm
PUT    /api/tests/{testId}                                 → Cập nhật xét nghiệm
DELETE /api/tests/{testId}                                 → Xoá xét nghiệm

POST   /api/pregnancies/{pregnancyId}/conditions          → Gán bệnh lý
PUT    /api/pregnancies/{pregnancyId}/conditions/{condId}  → Cập nhật mức độ/ghi chú
DELETE /api/pregnancies/{pregnancyId}/conditions/{condId}  → Xoá bệnh lý
```

### API Endpoints Summary

```
╔══════════════════════════════════════════════════════════════════════════╗
║ Method │ Endpoint                                     │ Description    ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ PUBLIC ENDPOINTS (không cần auth)                                      ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ GET    │ /api/ref/pregnancy-conditions?lang=vi        │ Danh mục bệnh ║
║ GET    │ /api/ref/test-types?lang=vi&category=LAB     │ Danh mục XN   ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ PREGNANCY (auth required)                                              ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ POST   │ /api/pregnancies                             │ Tạo thai kỳ   ║
║ GET    │ /api/pregnancies/active                      │ Thai kỳ active ║
║ GET    │ /api/pregnancies                             │ Tất cả thai kỳ ║
║ GET    │ /api/pregnancies/{id}                        │ Chi tiết 1     ║
║ PUT    │ /api/pregnancies/{id}                        │ Cập nhật       ║
║ PATCH  │ /api/pregnancies/{id}/status                 │ Đổi trạng thái║
║ DELETE │ /api/pregnancies/{id}                        │ Xoá (soft)     ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ VISITS (auth required)                                                 ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ POST   │ /api/pregnancies/{pid}/visits                │ Tạo buổi khám ║
║ GET    │ /api/pregnancies/{pid}/visits                │ DS buổi khám  ║
║ GET    │ /api/visits/{id}?lang=vi                     │ Chi tiết + XN ║
║ PUT    │ /api/visits/{id}                             │ Cập nhật      ║
║ DELETE │ /api/visits/{id}                             │ Xoá (soft)    ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ TESTS (auth required)                                                  ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ POST   │ /api/pregnancies/{pid}/tests                 │ Tạo xét nghiệm║
║ GET    │ /api/pregnancies/{pid}/tests?lang=vi         │ DS xét nghiệm ║
║ GET    │ /api/tests/{id}?lang=vi                      │ Chi tiết XN   ║
║ PUT    │ /api/tests/{id}                              │ Cập nhật      ║
║ DELETE │ /api/tests/{id}                              │ Xoá (soft)    ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ CONDITIONS (auth required)                                             ║
╠════════╪══════════════════════════════════════════╪════════════════╣
║ POST   │ /api/pregnancies/{pid}/conditions            │ Gán bệnh lý  ║
║ GET    │ /api/pregnancies/{pid}/conditions?lang=vi    │ DS bệnh lý   ║
║ PUT    │ /api/pregnancies/{pid}/conditions/{id}       │ Cập nhật      ║
║ DELETE │ /api/pregnancies/{pid}/conditions/{id}       │ Xoá           ║
╚══════════════════════════════════════════════════════════════════════════╝
```

---

## ✅ WEEK 3 COMPLETE — FINAL CHECKLIST

### 1. Database Layer
- [ ] 8 tables created with proper schema
- [ ] Unique constraints: `uk_pregnancies_user_no`, `uk_pregnancy_condition`
- [ ] FK links: pregnancies→users, conditions→pregnancies, visits→pregnancies, tests→pregnancies+visits+testTypes
- [ ] Seed data: 10 conditions + 10 test types (40 translations: 20 VI + 20 EN)
- [ ] Soft delete: `deleted_at` column on all `BaseEntity` tables

### 2. Domain Layer
- [ ] 8 entities: `Pregnancy`, `RefPregnancyCondition`, `RefPregnancyConditionTranslation`, `PregnancyCondition`, `PrenatalVisit`, `RefTestType`, `RefTestTypeTranslation`, `PrenatalTest`
- [ ] 7 enums: `PregnancyStatus`, `VisitType`, `ConditionSeverity`, `BabyGender`, `PregnancyType`, `DueDateSource`, `DeliveryMethod`
- [ ] Self-documenting property names with XML comments
- [ ] Navigation properties configured

### 3. Infrastructure Layer
- [ ] 8 EF Configurations with CHAR(36) for Guid, `Ignore(IsDeleted)`, enum as string
- [ ] 6 Repository implementations extending `GenericRepository<T>`
- [ ] `UnitOfWork` updated with `??=` lazy pattern

### 4. Application Layer
- [ ] 16 DTOs: Create/Update/Response for all modules + PrenatalVisitDetailDto + RefData DTOs
- [ ] `ChangePregnancyStatusDto` — separate từ `UpdatePregnancyDto`
- [ ] 9 FluentValidation validators (auto-registered)
- [ ] 5 Service implementations with ownership checks
- [ ] `IRefDataService` for public reference data
- [ ] AutoMapper profile

### 5. API Layer
- [ ] 5 Controllers: Pregnancies, PregnancyConditions, PrenatalVisits, PrenatalTests, RefData
- [ ] `RefDataController` — public (no `[Authorize]`)
- [ ] 12 Permission codes registered
- [ ] `BaseApiController` pattern: `Success()`, `Created()`, `GetCurrentUserId()`

### 6. Business Logic
- [ ] Auto-increment `PregnancyNumber` per user (includes deleted to avoid collision)
- [ ] Auto-calculate EDD = LMP + 280 days
- [ ] Auto-calculate `CurrentGestationalWeek` on read
- [ ] `GestationalAgeDisplay` formatted as "28w3d"
- [ ] State transition: only `Active → {Delivered, Ended, Miscarriage}`, NO reverse
- [ ] Ownership checks: `pregnancy.UserId == currentUserId`
- [ ] Condition uniqueness: same condition only once per pregnancy
- [ ] Visit-Test consistency: `Test.VisitId` must belong to same pregnancy
- [ ] Soft delete via `SoftDeleteAsync`

### 7. Testing with Swagger 🧪
```bash
# Run API
dotnet run --project src/FPT.EXE201.Api

# Test workflow:
1. GET /api/ref/pregnancy-conditions?lang=vi → Lấy danh mục bệnh lý
2. GET /api/ref/test-types?lang=vi&category=LAB → Lấy danh mục xét nghiệm
3. Login as USER → get JWT token
4. POST /api/pregnancies → Tạo thai kỳ (LMP + BabyNickname + PrePregnancyWeightKg + HeightCm)
   → Verify: EDD auto-calculated, gestational week computed
   → Verify: PrePregnancyBmi computed, BabyGender = "Unknown"
5. GET /api/pregnancies/active → Lấy thai kỳ active → LƯU pregnancyId
   → Verify: ObstetricFormula = "G1P0" (nếu truyền Gravida/Para)
6. PUT /api/pregnancies/{id} → Update BabyNickname = "Bé Bông", BabyGender = "Female"
   → Verify: response cập nhật đúng
7. POST /api/pregnancies/{id}/conditions → Gán bệnh lý
   → Verify: response có ConditionDisplayName theo lang
8. PUT /api/pregnancies/{id}/conditions/{condId} → Cập nhật severity/notes
9. POST /api/pregnancies/{id}/visits → Tạo lịch khám → LƯU visitId
10. GET /api/visits/{visitId}?lang=vi → Xem chi tiết buổi khám (kèm danh sách tests)
11. POST /api/pregnancies/{id}/tests → Tạo xét nghiệm
    → Verify: nếu có visitId, phải thuộc cùng pregnancy
12. GET /api/tests/{testId}?lang=vi → Xem chi tiết xét nghiệm
13. PATCH /api/pregnancies/{id}/status → Đổi sang Delivered:
    {"status": "Delivered", "actualDeliveryDate": "2026-02-10", "deliveryMethod": "Natural"}
    → Verify: ActualDeliveryDate + DeliveryMethod lưu đúng
    → Verify: thử đổi lại Active → phải lỗi 400
14. Login as DIFFERENT USER → GET pregnancy → phải lỗi 403 Forbidden
15. POST /api/pregnancies → Tạo pregnancy #2 khi #1 đã Delivered → OK
    → Verify: PregnancyNumber = 2
```

---

### 📊 NAMING CONSISTENCY CHECKLIST

```
╔══════════════════════════════════════════════════════════════════════════════╗
║ Layer      │ Property               │ DB Column              │ DTO      ║
╠════════════╪════════════════════════╪════════════════════════╪═════════╣
║ Entity     │ LastMenstrualPeriodDate│ lmp_date               │ same     ║
║ Entity     │ ExpectedDeliveryDate   │ edd_date               │ same     ║
║ Entity     │ EstimatedConceptionDate│ conception_date         │ same     ║
║ Entity     │ CurrentGestationalWeek │ current_week            │ same     ║
║ Entity     │ PregnancyNumber        │ pregnancy_no            │ same     ║
║ Entity     │ BabyNickname           │ baby_nickname           │ same     ║
║ Entity     │ BabyGender             │ baby_gender             │ string   ║
║ Entity     │ PregnancyType          │ pregnancy_type          │ string   ║
║ Entity     │ MotherBloodType        │ mother_blood_type       │ same     ║
║ Entity     │ PrePregnancyWeightKg   │ pre_pregnancy_weight_kg │ same     ║
║ Entity     │ HeightCm               │ height_cm               │ same     ║
║ Entity     │ DueDateSource          │ due_date_source         │ string   ║
║ Entity     │ Gravida                │ gravida                 │ same     ║
║ Entity     │ Para                   │ para                    │ same     ║
║ Entity     │ ActualDeliveryDate     │ actual_delivery_date    │ same     ║
║ Entity     │ DeliveryMethod         │ delivery_method         │ string?  ║
║ Entity     │ CoverImageUrl          │ cover_image_url         │ same     ║
║ Entity     │ DiagnosedDate          │ diagnosed_at            │ same     ║
║ Entity     │ VisitDateTime          │ visit_at                │ same     ║
║ Entity     │ TestDateTime           │ test_at                 │ same     ║
║ Entity     │ IsAbnormalResult       │ abnormal_flag           │ same     ║
║ Entity     │ LanguageCode           │ lang_code               │ same     ║
║ Entity     │ DisplayName            │ name                    │ same     ║
║ Response   │ GestationalAgeDisplay  │ (computed)              │ "28w3d"  ║
║ Response   │ PrePregnancyBmi        │ (computed)              │ decimal? ║
║ Response   │ ObstetricFormula       │ (computed)              │ "G2P1"  ║
║ Response   │ ConditionDisplayName   │ (from join)             │ string   ║
║ Response   │ ConditionDescription   │ (from join)             │ string?  ║
║ Response   │ TestTypeDisplayName    │ (from join)             │ string   ║
║ Response   │ TestCount              │ (computed)              │ int      ║
╚══════════════════════════════════════════════════════════════════════════════╝
```

---

**🎉 Week 3 hoàn thành! Ready for Week 4 — Storage + Medical Documents.**
