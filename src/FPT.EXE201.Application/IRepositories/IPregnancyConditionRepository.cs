using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyConditionRepository : IGenericRepository<PregnancyCondition>
{
    /// <summary>Lấy tất cả conditions của 1 pregnancy, include ref data + translations.</summary>
    Task<List<PregnancyCondition>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default);
}
