using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FPT.EXE201.Infrastructure.Services
{
    /// <summary>
    /// JWT token generation, validation and refresh token management service
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public JwtTokenService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            _issuer = configuration["Jwt:Issuer"] ?? "FPT.EXE201.Api";
            _audience = configuration["Jwt:Audience"] ?? "FPT.EXE201.Client";
            _expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 60;
            _refreshTokenExpirationDays = int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"], out var days) ? days : 30;
        }

        #region Access Token Methods

        public string GenerateAccessToken(User user, Guid? refreshTokenId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("status", user.Status.ToString())
            };

            if (!string.IsNullOrEmpty(user.Phone))
            {
                claims.Add(new Claim("phone", user.Phone));
            }

            // Add refresh token ID for logout (Option 3 - Hybrid approach)
            if (refreshTokenId.HasValue)
            {
                claims.Add(new Claim("rtid", refreshTokenId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateAccessTokenWithPermissions(User user, List<string> permissions, List<string> roles, Guid? refreshTokenId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("status", user.Status.ToString())
            };

            if (!string.IsNullOrEmpty(user.Phone))
            {
                claims.Add(new Claim("phone", user.Phone));
            }

            // Add refresh token ID
            if (refreshTokenId.HasValue)
            {
                claims.Add(new Claim("rtid", refreshTokenId.Value.ToString()));
            }

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add permissions to claims (Approach 2 - No DB query on each request)
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permissions", permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int GetTokenExpirationSeconds()
        {
            return _expirationMinutes * 60;
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Refresh Token Methods

        public async Task<(string token, Guid tokenId, AuthRefreshToken entity)> IssueRefreshTokenAsync(
            Guid userId,
            string? ipAddress = null,
            string? userAgent = null,
            string? deviceInfo = null,
            CancellationToken ct = default)
        {
            // Generate random token string (32 bytes = 64 hex chars)
            var tokenString = GenerateRandomToken();
            var tokenHash = HashToken(tokenString);
            var jti = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var refreshToken = new AuthRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Jti = jti,
                TokenHash = tokenHash,
                IssuedAt = now,
                ExpiresAt = now.AddDays(_refreshTokenExpirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceInfo = deviceInfo,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken, ct);

            // Return token string (not hash) for client to store + token ID for claims
            return (tokenString, refreshToken.Id, refreshToken);
        }

        public async Task<(string newToken, Guid newTokenId, User user)> RotateRefreshTokenAsync(
            string refreshToken,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken ct = default)
        {
            var tokenHash = HashToken(refreshToken);
            var now = DateTime.UtcNow;

            // 1. Find the token by hash
            var existingToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);
            if (existingToken == null)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // 2. Check if token is expired
            if (existingToken.ExpiresAt <= now)
            {
                throw new UnauthorizedException("Refresh token has expired");
            }

            // 3. CRITICAL: Check if token was already revoked (reuse detection)
            if (existingToken.RevokedAt != null)
            {
                // TOKEN REUSE DETECTED! This is a security breach.
                // Revoke the entire token chain to prevent further attacks
                await _unitOfWork.RefreshTokens.RevokeTokenChainAsync(existingToken.Id, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                throw new UnauthorizedException("Refresh token has been revoked due to suspected reuse");
            }

            // 4. Load user
            var user = existingToken.User ?? await _unitOfWork.Users.GetByIdAsync(existingToken.UserId, cancellationToken: ct);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // 5. Revoke the old token
            existingToken.RevokedAt = now;
            existingToken.UpdatedAt = now;

            // 6. Issue new token with rotation tracking
            var (newTokenString, newTokenId, newTokenEntity) = await IssueRefreshTokenAsync(
                user.Id,
                ipAddress,
                userAgent,
                existingToken.DeviceInfo, // Carry over device info
                ct: ct);

            // Link new token to old one for rotation chain tracking
            newTokenEntity.RotatedFromId = existingToken.Id;

            await _unitOfWork.SaveChangesAsync(ct);

            return (newTokenString, newTokenId, user);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = HashToken(refreshToken);
            var token = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

            if (token == null)
            {
                throw new NotFoundException("Refresh token not found");
            }

            if (token.RevokedAt != null)
            {
                // Already revoked, ignore (idempotent)
                return;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task RevokeByIdAsync(Guid tokenId, CancellationToken ct = default)
        {
            var token = await _unitOfWork.RefreshTokens.GetByIdAsync(tokenId, ct);

            if (token == null)
            {
                throw new NotFoundException("Refresh token not found");
            }

            if (token.RevokedAt != null)
            {
                // Already revoked, ignore (idempotent)
                return;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default)
        {
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<AuthRefreshToken?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = HashToken(refreshToken);
            var token = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

            if (token == null) return null;
            if (token.RevokedAt != null) return null;
            if (token.ExpiresAt <= DateTime.UtcNow) return null;

            return token;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generate a cryptographically secure random token string
        /// </summary>
        private static string GenerateRandomToken()
        {
            var randomBytes = new byte[32]; // 256 bits
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Hash token using SHA-256 for storage
        /// </summary>
        private static byte[] HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        }

        #endregion
    }
}
