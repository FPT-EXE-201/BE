namespace FPT.EXE201.Application.DTOs.Auth;

/// <summary>
/// ⚠️ NGOẠI LỆ — dùng class (Auth DTOs convention, xem AUTH_FLOW_GUIDE §6)
/// Flutter gửi idToken (eyJ...) từ googleUser.authentication.idToken
/// </summary>
public class GoogleSignInRequestDto
{
    /// <summary>Google ID Token (eyJ...) lấy từ Flutter google_sign_in</summary>
    public string IdToken { get; set; } = string.Empty;
}
