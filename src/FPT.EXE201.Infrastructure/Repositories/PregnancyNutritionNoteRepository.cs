using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyNutritionNoteRepository
    : GenericRepository<PregnancyNutritionNote>, IPregnancyNutritionNoteRepository
{
    public PregnancyNutritionNoteRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyNutritionNote>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(n => n.PregnancyId == pregnancyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }
}
