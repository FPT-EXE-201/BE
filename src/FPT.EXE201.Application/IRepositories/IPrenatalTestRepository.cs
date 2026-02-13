using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPrenatalTestRepository : IGenericRepository<PrenatalTest>
{
    /// <summary>Lấy tất cả tests của 1 pregnancy, include test type translations.</summary>
    Task<List<PrenatalTest>> GetByPregnancyIdAsync(Guid pregnancyId, string langCode, CancellationToken cancellationToken = default);

    /// <summary>Lấy 1 test theo ID, include test type + translations theo lang.</summary>
    Task<PrenatalTest?> GetByIdWithTranslationsAsync(Guid id, string langCode, CancellationToken cancellationToken = default);

    /// <summary>Lấy tests theo visit.</summary>
    Task<List<PrenatalTest>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
}
