using FPT.EXE201.Application.DTOs.Pregnancies;

namespace FPT.EXE201.Application.IServices;

public interface IPregnancyService
{
    Task<PregnancyDto> CreateAsync(Guid userId, CreatePregnancyDto dto, CancellationToken cancellationToken = default);
    Task<PregnancyDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<PregnancyDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PregnancyDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<PregnancyDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyDto dto, CancellationToken cancellationToken = default);
    Task<PregnancyDto> ChangeStatusAsync(Guid id, Guid userId, ChangePregnancyStatusDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
