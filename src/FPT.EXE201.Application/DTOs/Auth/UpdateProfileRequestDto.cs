namespace FPT.EXE201.Application.DTOs.Auth;

/// <summary>
/// ⚠️ NGOẠI LỆ — dùng class (Auth DTOs convention, xem AUTH_FLOW_GUIDE §6)
/// Dùng [FromForm] ở Controller. File avatar được truyền riêng vào service, không nằm trong DTO này.
/// </summary>
public class UpdateProfileRequestDto
{
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? Phone { get; set; }
}

/// <summary>Thông tin avatar upload — Controller tạo và truyền xuống Service riêng biệt.</summary>
public record AvatarUploadInfo(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes
);
