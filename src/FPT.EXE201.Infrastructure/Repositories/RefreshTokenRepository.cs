using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<AuthRefreshToken> _dbSet;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<AuthRefreshToken>();
        }

        public async Task AddAsync(AuthRefreshToken entity, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(entity, ct);
        }

        public async Task<AuthRefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Id == id, ct);
        }

        public async Task<AuthRefreshToken?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
        }

        public async Task<IEnumerable<AuthRefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(rt => rt.UserId == userId 
                    && rt.RevokedAt == null 
                    && rt.ExpiresAt > now)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task RevokeTokenChainAsync(Guid tokenId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            
            // Find the token and all tokens that were rotated from it
            var tokensToRevoke = await _dbSet
                .Where(rt => rt.Id == tokenId || rt.RotatedFromId == tokenId)
                .Where(rt => rt.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in tokensToRevoke)
            {
                token.RevokedAt = now;
            }
        }

        public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            
            var activeTokens = await _dbSet
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
            }
        }

        public async Task<int> DeleteExpiredTokensAsync(DateTime before, CancellationToken ct = default)
        {
            var expiredTokens = await _dbSet
                .Where(rt => rt.ExpiresAt < before)
                .ToListAsync(ct);

            _dbSet.RemoveRange(expiredTokens);
            return expiredTokens.Count;
        }
    }
}
