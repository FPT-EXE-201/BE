using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.IServices
{
    public interface IUserRoleService
    {
        Task<List<UserRoleDto>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
        Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);
        Task<List<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default);
        Task<List<string>> GetUserRoleCodesAsync(Guid userId, CancellationToken ct = default);
        Task AssignRolesToUserAsync(Guid userId, List<Guid> roleIds, CancellationToken ct = default);
        Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken ct = default);
        Task ReplaceUserRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken ct = default);
        Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default);
        Task<bool> HasRoleAsync(Guid userId, string roleCode, CancellationToken ct = default);
    }
}
