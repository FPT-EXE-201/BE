using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MotivationalTemplateRepository : GenericRepository<MotivationalTemplate>, IMotivationalTemplateRepository
{
    public MotivationalTemplateRepository(AppDbContext context) : base(context) { }

    public async Task<List<MotivationalTemplate>> GetByWeekAsync(
        int gestationalWeek, string? category = null, string langCode = "vi",
        CancellationToken ct = default)
    {
        IQueryable<MotivationalTemplate> query = _dbSet
            .Where(m => m.IsActive && m.WeekStart <= gestationalWeek && m.WeekEnd >= gestationalWeek)
            .Include(m => m.Translations.Where(t => t.LanguageCode == langCode));

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<MotivationalCategory>(category, ignoreCase: true, out var parsedCategory))
        {
            query = query.Where(m => m.Category == parsedCategory);
        }

        return await query
            .OrderBy(m => m.Category)
            .ThenBy(m => m.WeekStart)
            .ToListAsync(ct);
    }
}
