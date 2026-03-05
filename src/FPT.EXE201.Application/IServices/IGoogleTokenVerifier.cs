namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Verify Google ID Token bằng cách gọi Google tokeninfo endpoint.
/// Implementation ở Infrastructure — Application chỉ biết interface.
/// </summary>
public interface IGoogleTokenVerifier
{
    /// <summary>
    /// Verify Google ID Token (JWT) và trả về user info nếu hợp lệ.
    /// Gọi https://oauth2.googleapis.com/tokeninfo?id_token={idToken}
    /// </summary>
    /// <returns>GoogleUserInfo nếu token hợp lệ, null nếu không hợp lệ</returns>
    Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}

/// <summary>Thông tin user trả về sau khi verify Google ID Token</summary>
public record GoogleUserInfo(
    string GoogleId,        // "sub" field — unique Google user ID
    string Email,
    bool EmailVerified,
    string? Name,
    string? PictureUrl
);
