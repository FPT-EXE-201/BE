using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRecipeRepository : IGenericRepository<Recipe>
{
    Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
}
