using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyNutritionNoteRepository : IGenericRepository<PregnancyNutritionNote>
{
    Task<List<PregnancyNutritionNote>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);
}
