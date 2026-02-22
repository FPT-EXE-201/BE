using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMotivationalTemplateRepository : IGenericRepository<MotivationalTemplate>
{
    Task<List<MotivationalTemplate>> GetByWeekAsync(
        int gestationalWeek, string? category = null, string langCode = "vi",
        CancellationToken ct = default);
}
