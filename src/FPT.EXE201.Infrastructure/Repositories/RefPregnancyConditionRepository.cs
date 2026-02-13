using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefPregnancyConditionRepository : GenericRepository<RefPregnancyCondition>, IRefPregnancyConditionRepository
{
    public RefPregnancyConditionRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefPregnancyCondition>> GetActiveWithTranslationsAsync(string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive && r.DeletedAt == null)
            .Include(r => r.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }
}
