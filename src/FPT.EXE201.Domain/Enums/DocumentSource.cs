namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Nguồn gốc của tài liệu y tế.
/// </summary>
public enum DocumentSource
{
    /// <summary>User tự chụp/upload từ thiết bị</summary>
    Upload,

    /// <summary>Được chia sẻ từ người khác (bác sĩ, người thân)</summary>
    Share,

    /// <summary>Import từ hệ thống khác</summary>
    Import
}
