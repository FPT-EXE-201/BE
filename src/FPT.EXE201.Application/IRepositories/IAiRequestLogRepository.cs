using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IAiRequestLogRepository : IGenericRepository<AiRequestLog>
{
    Task<int> CountTodayByUserAsync(Guid userId, CancellationToken ct = default);
}
