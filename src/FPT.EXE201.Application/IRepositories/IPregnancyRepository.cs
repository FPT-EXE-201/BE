using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyRepository : IGenericRepository<Pregnancy>
{
    /// <summary>Lấy thai kỳ Active của user (chỉ có tối đa 1).</summary>
    Task<Pregnancy?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả thai kỳ của user (bao gồm ended).</summary>
    Task<List<Pregnancy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy pregnancy_no tiếp theo cho user (max + 1).</summary>
    Task<int> GetNextPregnancyNumberAsync(Guid userId, CancellationToken cancellationToken = default);
}
