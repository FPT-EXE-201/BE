using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PrenatalTestRepository : GenericRepository<PrenatalTest>, IPrenatalTestRepository
{
    public PrenatalTestRepository(AppDbContext context) : base(context) { }

    public async Task<List<PrenatalTest>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.PregnancyId == pregnancyId && t.DeletedAt == null)
            .Include(t => t.TestType)
                .ThenInclude(tt => tt.Translations.Where(tr => tr.LanguageCode == langCode))
            .OrderByDescending(t => t.TestDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PrenatalTest?> GetByIdWithTranslationsAsync(Guid id, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Id == id && t.DeletedAt == null)
            .Include(t => t.TestType)
                .ThenInclude(tt => tt.Translations.Where(tr => tr.LanguageCode == langCode))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PrenatalTest>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.VisitId == visitId && t.DeletedAt == null)
            .Include(t => t.TestType)
            .OrderByDescending(t => t.TestDate)
            .ToListAsync(cancellationToken);
    }
}
