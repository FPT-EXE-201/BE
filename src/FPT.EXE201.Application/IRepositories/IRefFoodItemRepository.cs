using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefFoodItemRepository : IGenericRepository<RefFoodItem>
{
    Task<List<RefFoodItem>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default);
}
