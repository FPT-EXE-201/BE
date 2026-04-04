using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.IServices
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register new user account
        /// </summary>
        Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default);

        /// <summary>
        /// Login with email/phone and password
        /// </summary>
        Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        Task<RefreshTokenResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default);

        /// <summary>
        /// Logout and revoke refresh token for current device
        /// </summary>
        /// <param name="userId">User ID from access token claims</param>
        /// <param name="refreshTokenId">Refresh token ID from access token claims</param>
        /// <param name="ct">Cancellation token</param>
        Task LogoutAsync(Guid userId, Guid refreshTokenId, CancellationToken ct = default);

        Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Permanently close the current user's account (soft-delete + PII scrub). Any authenticated role may call for self only.
        /// </summary>
        Task DeleteAccountAsync(Guid userId, DeleteAccountRequestDto request, CancellationToken ct = default);
    }
}

