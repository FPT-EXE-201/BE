using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

/// <summary>
/// Standalone repository — RefNutrient KHÔNG kế thừa BaseEntity.
/// Pattern giống WeightAlertRepository.
/// </summary>
public class RefNutrientRepository : IRefNutrientRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<RefNutrient> _dbSet;

    public RefNutrientRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<RefNutrient>();
    }

    public async Task<List<RefNutrient>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => n.IsActive)
            .Include(n => n.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(n => n.Code)
            .ToListAsync(ct);
    }

    public async Task<List<RefNutrient>> GetByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => codes.Contains(n.Code) && n.IsActive)
            .ToListAsync(ct);
    }
}
