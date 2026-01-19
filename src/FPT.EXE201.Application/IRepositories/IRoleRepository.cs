using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByCodeAsync(string code, bool includeDeleted = false, CancellationToken ct = default);
        Task<Role?> GetByIdWithPermissionsAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
        Task<List<Role>> GetAllWithPermissionsAsync(bool includeDeleted = false, CancellationToken ct = default);
        Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, bool includeDeleted = false, CancellationToken ct = default);
        Task<PagedResult<Role>> GetPagedRolesAsync(QueryOptions options, CancellationToken ct = default);
        Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken ct = default);
        Task RemoveRolePermissionsAsync(Guid roleId, CancellationToken ct = default);
    }
}
