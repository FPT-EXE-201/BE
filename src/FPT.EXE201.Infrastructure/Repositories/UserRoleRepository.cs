using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ToListAsync(ct);
        }

        public async Task<List<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Include(ur => ur.User)
                .ToListAsync(ct);
        }

        public async Task<UserRole?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        }

        public async Task AddAsync(UserRole userRole, CancellationToken ct = default)
        {
            await _context.UserRoles.AddAsync(userRole, ct);
        }

        public Task RemoveAsync(UserRole userRole, CancellationToken ct = default)
        {
            _context.UserRoles.Remove(userRole);
            return Task.CompletedTask;
        }

        public Task RemoveRangeAsync(List<UserRole> userRoles, CancellationToken ct = default)
        {
            _context.UserRoles.RemoveRange(userRoles);
            return Task.CompletedTask;
        }

        public async Task<List<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
                .Distinct()
                .ToListAsync(ct);
        }

        public async Task<List<string>> GetUserRoleCodesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Code)
                .ToListAsync(ct);
        }
    }
}
