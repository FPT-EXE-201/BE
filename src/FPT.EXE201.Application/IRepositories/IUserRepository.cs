using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// User repository interface with auth-specific queries
    /// Inherits Add/Update/Delete from IGenericRepository
    /// </summary>
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<bool> ExistsByEmailAsync(string email, bool includeDeleted = false, CancellationToken ct = default);
        Task<bool> ExistsByPhoneAsync(string phone, bool includeDeleted = false, CancellationToken ct = default);
        Task<User?> GetByIdWithProfileAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string emailNormalized, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default);
        Task<User?> GetByEmailNormalizedAsync(string emailNormalized, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default);
        Task<User?> GetByPhoneAsync(string phone, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default);
        Task<PagedResult<User>> GetPagedUsersAsync(QueryOptions options, CancellationToken ct = default);
        Task<IEnumerable<User>> GetByRoleAsync(string roleName, CancellationToken ct = default);
    }
}
