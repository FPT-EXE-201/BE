namespace FPT.EXE201.Application.DTOs.Auth
{
    /// <summary>
    /// Request to refresh access token
    /// </summary>
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing new access and refresh tokens
    /// </summary>
    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // seconds
    }
}
