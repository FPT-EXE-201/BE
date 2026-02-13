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
