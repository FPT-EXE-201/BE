using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// Repository for refresh token operations
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Add a new refresh token
        /// </summary>
        Task AddAsync(AuthRefreshToken entity, CancellationToken ct = default);

        /// <summary>
        /// Get refresh token by ID
        /// </summary>
        Task<AuthRefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Find refresh token by token hash
        /// </summary>
        Task<AuthRefreshToken?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken ct = default);

        /// <summary>
        /// Get all active (non-revoked, non-expired) tokens for a user
        /// </summary>
        Task<IEnumerable<AuthRefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Revoke all tokens in a rotation chain starting from a specific token
        /// </summary>
        Task RevokeTokenChainAsync(Guid tokenId, CancellationToken ct = default);

        /// <summary>
        /// Revoke all active tokens for a user (logout all devices)
        /// </summary>
        Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Clean up expired tokens (for maintenance job)
        /// </summary>
        Task<int> DeleteExpiredTokensAsync(DateTime before, CancellationToken ct = default);
    }
}
