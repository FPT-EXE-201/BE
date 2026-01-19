using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// JWT token generation, validation and refresh token management service
    /// </summary>
    public interface IJwtTokenService
    {
        #region Access Token Methods

        /// <summary>
        /// Generate access token for authenticated user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="refreshTokenId">Optional refresh token ID to include in claims for logout</param>
        /// <returns>JWT access token string</returns>
        string GenerateAccessToken(User user, Guid? refreshTokenId = null);

        /// <summary>
        /// Generate access token with permissions and roles in claims (Approach 2 - Faster)
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="permissions">List of permission codes</param>
        /// <param name="roles">List of role codes</param>
        /// <param name="refreshTokenId">Optional refresh token ID</param>
        /// <returns>JWT access token string with permissions/roles</returns>
        string GenerateAccessTokenWithPermissions(User user, List<string> permissions, List<string> roles, Guid? refreshTokenId = null);

        /// <summary>
        /// Get token expiration time in seconds
        /// </summary>
        int GetTokenExpirationSeconds();

        /// <summary>
        /// Validate token and extract claims (optional, usually done by middleware)
        /// </summary>
        ClaimsPrincipal? ValidateToken(string token);

        #endregion

        #region Refresh Token Methods

        /// <summary>
        /// Issue a new refresh token for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="userAgent">Client user agent</param>
        /// <param name="deviceInfo">Optional device information (JSON)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Tuple of (refresh token string, refresh token ID, refresh token entity)</returns>
        Task<(string token, Guid tokenId, AuthRefreshToken entity)> IssueRefreshTokenAsync(
            Guid userId,
            string? ipAddress = null,
            string? userAgent = null,
            string? deviceInfo = null,
            CancellationToken ct = default);

        /// <summary>
        /// Rotate (replace) an existing refresh token with a new one
        /// Implements automatic revocation detection (if old token was already used)
        /// </summary>
        /// <param name="refreshToken">The current refresh token string</param>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="userAgent">Client user agent</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Tuple of (new token string, new token ID, user)</returns>
        /// <exception cref="UnauthorizedAccessException">If token is invalid, expired, revoked, or reused</exception>
        Task<(string newToken, Guid newTokenId, User user)> RotateRefreshTokenAsync(
            string refreshToken,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken ct = default);

        /// <summary>
        /// Revoke a refresh token (logout)
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke</param>
        /// <param name="ct">Cancellation token</param>
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

        /// <summary>
        /// Revoke a refresh token by ID (logout current device)
        /// </summary>
        /// <param name="tokenId">The refresh token ID</param>
        /// <param name="ct">Cancellation token</param>
        Task RevokeByIdAsync(Guid tokenId, CancellationToken ct = default);

        /// <summary>
        /// Revoke all refresh tokens for a user (logout all devices)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="ct">Cancellation token</param>
        Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Validate a refresh token without rotating it
        /// </summary>
        /// <param name="refreshToken">The refresh token string</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The refresh token entity if valid, null otherwise</returns>
        Task<AuthRefreshToken?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

        #endregion
    }
}
