using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    public interface IUserRoleRepository
    {
        Task<List<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<List<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default);
        Task<UserRole?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default);
        Task AddAsync(UserRole userRole, CancellationToken ct = default);
        Task RemoveAsync(UserRole userRole, CancellationToken ct = default);
        Task RemoveRangeAsync(List<UserRole> userRoles, CancellationToken ct = default);
        Task<List<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default);
        Task<List<string>> GetUserRoleCodesAsync(Guid userId, CancellationToken ct = default);
    }
}
