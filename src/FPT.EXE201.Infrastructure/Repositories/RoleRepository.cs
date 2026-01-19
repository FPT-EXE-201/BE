using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByCodeAsync(string code, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await GetSingleAsync(
                r => r.Code == code,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<Role?> GetByIdWithPermissionsAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await GetByIdAsync(
                id,
                include: query => query
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission),
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<List<Role>> GetAllWithPermissionsAsync(bool includeDeleted = false, CancellationToken ct = default)
        {
            var result = await GetAllAsync(
                include: query => query
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission),
                includeDeleted: includeDeleted,
                cancellationToken: ct);
            
            return result.ToList();
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, bool includeDeleted = false, CancellationToken ct = default)
        {
            if (excludeId.HasValue)
            {
                return await ExistsAsync(
                    r => r.Code == code && r.Id != excludeId.Value,
                    includeDeleted: includeDeleted,
                    cancellationToken: ct);
            }

            return await ExistsAsync(
                r => r.Code == code,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<PagedResult<Role>> GetPagedRolesAsync(QueryOptions options, CancellationToken ct = default)
        {
            return await GetPagedAsync(options, cancellationToken: ct);
        }

        public async Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken ct = default)
        {
            await _context.Set<RolePermission>().AddAsync(rolePermission, ct);
        }

        public Task RemoveRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
        {
            var rolePermissions = _context.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ToList();
            
            _context.Set<RolePermission>().RemoveRange(rolePermissions);
            return Task.CompletedTask;
        }
    }
}
