using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.IServices
{
    public interface IRoleService
    {
        Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<RoleDto?> GetByCodeAsync(string code, CancellationToken ct = default);
        Task<List<RoleDto>> GetAllAsync(bool includePermissions = false, CancellationToken ct = default);
        Task<PagedResult<RoleDto>> GetPagedAsync(QueryOptions options, CancellationToken ct = default);
        Task<RoleDto> CreateAsync(CreateRoleDto dto, CancellationToken ct = default);
        Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);
        Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken ct = default);
    }
}
