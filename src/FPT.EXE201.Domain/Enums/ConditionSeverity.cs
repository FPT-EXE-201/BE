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
