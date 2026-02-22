using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightLogRepository : IGenericRepository<WeightLog>
{
    Task<PagedResult<WeightLog>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default);

    Task<List<WeightLog>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<WeightLog?> GetByPregnancyAndDateAsync(
        Guid pregnancyId, DateOnly loggedOn, CancellationToken ct = default);

    Task<WeightLog?> GetLatestByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<List<WeightLog>> GetRecentByPregnancyIdAsync(
        Guid pregnancyId, int count = 5, CancellationToken ct = default);
}
