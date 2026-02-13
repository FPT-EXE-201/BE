using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPrenatalVisitRepository : IGenericRepository<PrenatalVisit>
{
    /// <summary>Lấy tất cả visits của 1 pregnancy, sắp xếp theo ngày khám desc.</summary>
    Task<List<PrenatalVisit>> GetByPregnancyIdAsync(Guid pregnancyId, CancellationToken cancellationToken = default);

    /// <summary>Lấy visit kèm danh sách tests.</summary>
    Task<PrenatalVisit?> GetByIdWithTestsAsync(Guid id, CancellationToken cancellationToken = default);
}
