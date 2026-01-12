using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.IRepositories;

namespace FPT.EXE201.Application
{
    /// <summary>
    /// Unit of Work pattern for transaction management
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IUserRepository Users { get; }
        IUserProfileRepository UserProfiles { get; }
        ILanguageRepository Languages { get; }

        // Transaction methods
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
