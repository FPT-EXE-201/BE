using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> ExistsByEmailAsync(string email, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await ExistsAsync(u => u.Email == email, includeDeleted, ct);
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await ExistsAsync(u => u.Phone == phone, includeDeleted, ct);
        }

        public async Task<User?> GetByIdWithProfileAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
        {
            return await GetByIdAsync(
                id,
                include: query => query.Include(u => u.Profile),
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<User?> GetByEmailAsync(string email, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default)
        {
            Func<IQueryable<User>, IQueryable<User>>? include = null;
            if (includeProfile)
            {
                include = query => query.Include(u => u.Profile);
            }

            return await GetSingleAsync(
                u => u.Email == email,
                include: include,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<User?> GetByEmailNormalizedAsync(string emailNormalized, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default)
        {
            Func<IQueryable<User>, IQueryable<User>>? include = null;
            if (includeProfile)
            {
                include = query => query.Include(u => u.Profile);
            }

            return await GetSingleAsync(
                u => u.EmailNormalized == emailNormalized,
                include: include,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<User?> GetByPhoneAsync(string phone, bool includeProfile = false, bool includeDeleted = false, CancellationToken ct = default)
        {
            Func<IQueryable<User>, IQueryable<User>>? include = null;
            if (includeProfile)
            {
                include = query => query.Include(u => u.Profile);
            }

            return await GetSingleAsync(
                u => u.Phone == phone,
                include: include,
                includeDeleted: includeDeleted,
                cancellationToken: ct);
        }

        public async Task<PagedResult<User>> GetPagedUsersAsync(QueryOptions options, CancellationToken ct = default)
        {
            // Search builder: search in email, phone, profile name
            Func<IQueryable<User>, string, IQueryable<User>> searchBuilder = (query, searchTerm) =>
            {
                var term = searchTerm.ToLower();
                return query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.Phone != null && u.Phone.Contains(term)) ||
                    (u.Profile != null && u.Profile.FullName != null && u.Profile.FullName.ToLower().Contains(term))
                );
            };

            // Include profile by default for user management
            Func<IQueryable<User>, IQueryable<User>> include = query => query.Include(u => u.Profile);

            return await GetPagedAsync(
                options,
                predicate: null,
                include: include,
                searchBuilder: searchBuilder,
                sortMap: null,
                defaultSort: null,
                cancellationToken: ct);
        }
        public async Task<User?> GetByGoogleIdAsync(string googleId, bool includeProfile = false, CancellationToken ct = default)
        {
            Func<IQueryable<User>, IQueryable<User>>? include = null;
            if (includeProfile)
                include = query => query.Include(u => u.Profile);

            return await GetSingleAsync(
                u => u.GoogleId == googleId,
                include: include,
                includeDeleted: false,
                cancellationToken: ct);
        }
    
        public async Task<IEnumerable<User>> GetByRoleAsync(string roleCode, CancellationToken ct = default)
        {
            return await ((AppDbContext)_context).Users
                .Include(u => u.Profile)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Code == roleCode))
                .ToListAsync(ct);
        }
    }
}
