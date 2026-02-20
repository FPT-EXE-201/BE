using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.DTOs.Timeline;

namespace FPT.EXE201.Application.IServices;

public interface IMedicalDocumentService
{
    /// <summary>Upload 1-N files + tạo document trong 1 bước (multi-file support).</summary>
    Task<MedicalDocumentDto> CreateWithFilesAsync(
        Guid pregnancyId, CreateMedicalDocumentDto dto,
        IReadOnlyList<FileUploadInfo> files,
        Guid currentUserId, CancellationToken cancellationToken = default);

    Task<List<MedicalDocumentDto>> GetByPregnancyIdAsync(
        Guid pregnancyId, Guid currentUserId,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default);

    Task<MedicalDocumentDto> GetByIdAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<MedicalDocumentDto> UpdateAsync(
        Guid id, UpdateMedicalDocumentDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Toggle trạng thái yêu thích (IsFavorite).</summary>
    Task<MedicalDocumentDto> ToggleFavoriteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy timeline (documents + visits) của thai kỳ.</summary>
    Task<List<TimelineEventDto>> GetTimelineAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
