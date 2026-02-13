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
