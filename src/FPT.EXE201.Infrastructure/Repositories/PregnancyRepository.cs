using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyRepository : GenericRepository<Pregnancy>, IPregnancyRepository
{
    public PregnancyRepository(AppDbContext context) : base(context) { }

    public async Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.Status == PregnancyStatus.Active && p.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Pregnancy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Include deleted pregnancies to avoid pregnancy_no collisions
        var maxNo = await _dbSet
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .MaxAsync(p => (int?)p.PregnancyNumber, cancellationToken);

        return (maxNo ?? 0) + 1;
    }
}
