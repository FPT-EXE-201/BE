using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.PrenatalTests;

namespace FPT.EXE201.Application.IServices;

public interface IPrenatalTestService
{
    /// <summary>Tạo test mới + upload ảnh (nếu có) lên storage.</summary>
    Task<PrenatalTestDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalTestDto dto,
        List<FileUploadItem>? images, string langCode, CancellationToken cancellationToken = default);

    Task<List<PrenatalTestDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<PagedResult<PrenatalTestDto>> GetByPregnancyIdPagedAsync(Guid pregnancyId, Guid userId, QueryOptions options, string langCode, CancellationToken cancellationToken = default);
    Task<PrenatalTestDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default);
    Task<List<PrenatalTestDto>> GetByVisitIdAsync(Guid visitId, Guid userId, string langCode, CancellationToken cancellationToken = default);

    /// <summary>Update test + upload ảnh mới (nếu có), xóa ảnh cũ không giữ.</summary>
    Task<PrenatalTestDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalTestDto dto,
        List<FileUploadItem>? newImages, string langCode, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>DTO trung gian cho file upload — tách IFormFile khỏi Application layer.</summary>
public record FileUploadItem(Stream Stream, string FileName, string ContentType, long Length);
