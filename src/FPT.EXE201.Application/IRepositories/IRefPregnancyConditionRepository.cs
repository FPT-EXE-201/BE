using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefPregnancyConditionRepository : IGenericRepository<RefPregnancyCondition>
{
    /// <summary>Lấy tất cả conditions đang active, include translation theo lang.</summary>
    Task<List<RefPregnancyCondition>> GetActiveWithTranslationsAsync(string langCode, CancellationToken cancellationToken = default);
}
