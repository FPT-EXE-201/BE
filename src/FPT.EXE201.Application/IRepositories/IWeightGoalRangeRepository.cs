using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightGoalRangeRepository : IGenericRepository<WeightGoalRange>
{
    Task<WeightGoalRange?> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);
}
