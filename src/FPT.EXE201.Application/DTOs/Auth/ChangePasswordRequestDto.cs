namespace FPT.EXE201.Application.DTOs.Auth;

/// <summary>
/// ⚠️ NGOẠI LỆ — dùng class (Auth DTOs convention, xem AUTH_FLOW_GUIDE §6)
/// </summary>
public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
