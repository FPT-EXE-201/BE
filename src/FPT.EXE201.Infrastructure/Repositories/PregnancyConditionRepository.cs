using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyConditionRepository : GenericRepository<PregnancyCondition>, IPregnancyConditionRepository
{
    public PregnancyConditionRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyCondition>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pc => pc.PregnancyId == pregnancyId && pc.DeletedAt == null)
            .Include(pc => pc.Condition)
                .ThenInclude(c => c.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(pc => pc.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
