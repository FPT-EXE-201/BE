using FPT.EXE201.Application.DTOs.PrenatalVisits;

namespace FPT.EXE201.Application.IServices;

public interface IPrenatalVisitService
{
    Task<PrenatalVisitDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalVisitDto dto, CancellationToken cancellationToken = default);
    Task<List<PrenatalVisitDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PrenatalVisitDetailDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PrenatalVisitDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalVisitDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
