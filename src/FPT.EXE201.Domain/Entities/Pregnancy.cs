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
    public DateOnly? LastMenstrualPeriodDate { get; set; }

    /// <summary>
    /// EDD — Expected Delivery Date (Ngày dự sinh).
    /// Auto-calculated: EDD = LMP + 280 ngày (Naegele's rule).
    /// Có thể được bác sĩ điều chỉnh dựa trên siêu âm.
    /// </summary>
    public DateOnly? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Ngày thụ thai ước tính.
    /// Thường = LMP + 14 ngày (optional, user có thể không biết).
    /// </summary>
    public DateOnly? EstimatedConceptionDate { get; set; }

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
    public DateOnly? ActualDeliveryDate { get; set; }

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

    // ══════════════════════════════════════
    // Week 7 — Nutrition + Meal Planning
    // ══════════════════════════════════════

    /// <summary>Danh sách dị ứng / không thích theo thai kỳ.</summary>
    public ICollection<PregnancyFoodPreference> FoodPreferences { get; set; } = new List<PregnancyFoodPreference>();

    /// <summary>Ghi chú dinh dưỡng dạng text tự do.</summary>
    public ICollection<PregnancyNutritionNote> NutritionNotes { get; set; } = new List<PregnancyNutritionNote>();

    /// <summary>Công thức nấu ăn (AI-generated).</summary>
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    /// <summary>Kế hoạch bữa ăn hàng tuần.</summary>
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();

    /// <summary>Log các request AI (Gemini/OCR).</summary>
    public ICollection<AiRequestLog> AiRequestLogs { get; set; } = new List<AiRequestLog>();
}
