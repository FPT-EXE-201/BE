using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.IServices
{
    public interface IPermissionService
    {
        Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<PermissionDto?> GetByCodeAsync(string code, CancellationToken ct = default);
        Task<List<PermissionDto>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<PermissionDto>> GetPagedAsync(QueryOptions options, CancellationToken ct = default);
    }
}
