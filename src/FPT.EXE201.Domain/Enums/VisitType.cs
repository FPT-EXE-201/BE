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
