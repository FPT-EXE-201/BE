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
