using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    public interface IPermissionRepository : IGenericRepository<Permission>
    {
        Task<Permission?> GetByCodeAsync(string code, bool includeDeleted = false, CancellationToken ct = default);
        Task<List<Permission>> GetByIdsAsync(List<Guid> ids, bool includeDeleted = false, CancellationToken ct = default);
        Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, bool includeDeleted = false, CancellationToken ct = default);
        Task<PagedResult<Permission>> GetPagedPermissionsAsync(QueryOptions options, CancellationToken ct = default);
        Task<List<Permission>> GetByRoleIdAsync(Guid roleId, bool includeDeleted = false, CancellationToken ct = default);
    }
}
