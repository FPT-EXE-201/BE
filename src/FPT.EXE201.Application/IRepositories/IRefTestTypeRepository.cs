using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefTestTypeRepository : IGenericRepository<RefTestType>
{
    /// <summary>Lấy tất cả test types đang active, include translation theo lang. Optional filter by category.</summary>
    Task<List<RefTestType>> GetActiveWithTranslationsAsync(string langCode, string? category = null, CancellationToken cancellationToken = default);
}
