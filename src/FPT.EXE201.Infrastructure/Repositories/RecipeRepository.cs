using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class RecipeRepository : GenericRepository<Recipe>, IRecipeRepository
{
    public RecipeRepository(AppDbContext context) : base(context) { }

    public async Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(r => r.MealItems)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
