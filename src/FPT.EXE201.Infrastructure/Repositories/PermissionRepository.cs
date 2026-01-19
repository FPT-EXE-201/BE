using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Permission?> GetByCodeAsync(string code, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await GetSingleAsync(
                p => p.Code == code,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<List<Permission>> GetByIdsAsync(List<Guid> ids, bool includeDeleted = false, CancellationToken ct = default)
        {
            var result = await GetAllAsync(
                p => ids.Contains(p.Id),
                includeDeleted: includeDeleted,
                cancellationToken: ct);
            
            return result.ToList();
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, bool includeDeleted = false, CancellationToken ct = default)
        {
            if (excludeId.HasValue)
            {
                return await ExistsAsync(
                    p => p.Code == code && p.Id != excludeId.Value,
                    includeDeleted: includeDeleted,
                    cancellationToken: ct);
            }

            return await ExistsAsync(
                p => p.Code == code,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<PagedResult<Permission>> GetPagedPermissionsAsync(QueryOptions options, CancellationToken ct = default)
        {
            return await GetPagedAsync(options, cancellationToken: ct);
        }

        public async Task<List<Permission>> GetByRoleIdAsync(Guid roleId, bool includeDeleted = false, CancellationToken ct = default)
        {
            var query = _context.Set<Permission>().AsQueryable();

            // Join with RolePermissions
            query = query
                .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId));

            if (!includeDeleted)
            {
                query = query.Where(p => p.DeletedAt == null);
            }

            return await query.ToListAsync(ct);
        }
    }
}
