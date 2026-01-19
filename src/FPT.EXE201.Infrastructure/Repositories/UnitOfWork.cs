using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace FPT.EXE201.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing repositories and transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-initialized repositories
        private IUserRepository? _users;
        private IUserProfileRepository? _userProfiles;
        private ILanguageRepository? _languages;
        private IRefreshTokenRepository? _refreshTokens;
        private IRoleRepository? _roles;
        private IPermissionRepository? _permissions;
        private IUserRoleRepository? _userRoles;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IUserProfileRepository UserProfiles => _userProfiles ??= new UserProfileRepository(_context);
        public ILanguageRepository Languages => _languages ??= new LanguageRepository(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
        public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
        public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
        public IUserRoleRepository UserRoles => _userRoles ??= new UserRoleRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
