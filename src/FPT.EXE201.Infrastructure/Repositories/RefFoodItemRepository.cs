using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RefFoodItemRepository : GenericRepository<RefFoodItem>, IRefFoodItemRepository
{
    public RefFoodItemRepository(AppDbContext context) : base(context) { }

    public async Task<List<RefFoodItem>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(f => f.IsActive)
            .Include(f => f.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(f => f.Code)
            .ToListAsync(ct);
    }
}
