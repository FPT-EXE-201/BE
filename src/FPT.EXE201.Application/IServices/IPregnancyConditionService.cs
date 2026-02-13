using FPT.EXE201.Application.DTOs.PregnancyConditions;

namespace FPT.EXE201.Application.IServices;

public interface IPregnancyConditionService
{
    Task<PregnancyConditionDto> AddAsync(Guid pregnancyId, Guid userId, CreatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default);
    Task<List<PregnancyConditionDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PregnancyConditionDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
