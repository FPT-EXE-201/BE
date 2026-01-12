using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository
    {
        public UserProfileRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await GetSingleAsync(
                p => p.UserId == userId,
                include: query => query.Include(p => p.User),
                includeDeleted: false,
                cancellationToken: ct);
        }

        public async Task<UserProfile?> GetByUserIdTrackedAsync(Guid userId, CancellationToken ct = default)
        {
            return await GetSingleTrackedAsync(
                p => p.UserId == userId,
                include: query => query.Include(p => p.User),
                includeDeleted: false,
                cancellationToken: ct);
        }
    }
}
