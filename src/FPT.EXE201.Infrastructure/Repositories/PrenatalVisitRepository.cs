using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PrenatalVisitRepository : GenericRepository<PrenatalVisit>, IPrenatalVisitRepository
{
    public PrenatalVisitRepository(AppDbContext context) : base(context) { }

    public async Task<List<PrenatalVisit>> GetByPregnancyIdAsync(Guid pregnancyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.PregnancyId == pregnancyId && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
            .OrderByDescending(v => v.VisitDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PrenatalVisit?> GetByIdWithTestsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Id == id && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
                .ThenInclude(t => t.TestType)
                    .ThenInclude(tt => tt.Translations)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
