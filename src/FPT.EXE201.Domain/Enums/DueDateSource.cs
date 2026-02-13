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
