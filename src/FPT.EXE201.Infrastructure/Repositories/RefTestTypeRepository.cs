using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefTestTypeRepository : GenericRepository<RefTestType>, IRefTestTypeRepository
{
    public RefTestTypeRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefTestType>> GetActiveWithTranslationsAsync(string langCode, string? category = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(r => r.IsActive && r.DeletedAt == null);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(r => r.Category == category);

        return await query
            .Include(r => r.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }
}
