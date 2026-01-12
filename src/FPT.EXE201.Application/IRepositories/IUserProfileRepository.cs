using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// UserProfile repository interface
    /// Inherits Add/Update/Delete from IGenericRepository
    /// </summary>
    public interface IUserProfileRepository : IGenericRepository<UserProfile>
    {
        Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<UserProfile?> GetByUserIdTrackedAsync(Guid userId, CancellationToken ct = default);
    }
}
