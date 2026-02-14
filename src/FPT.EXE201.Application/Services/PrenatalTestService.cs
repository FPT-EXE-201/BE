using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PrenatalTestService : IPrenatalTestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public PrenatalTestService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<PrenatalTestDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalTestDto dto,
        List<FileUploadItem>? images, string langCode, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        // Verify test type exists
        var testType = await _unitOfWork.RefTestTypes.GetByIdAsync(dto.TestTypeId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test type '{dto.TestTypeId}' not found");

        // If VisitId provided, verify it belongs to the SAME pregnancy
        if (dto.VisitId.HasValue)
        {
            var visit = await _unitOfWork.PrenatalVisits.GetByIdAsync(dto.VisitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException($"Visit '{dto.VisitId}' not found");
            if (visit.PregnancyId != pregnancyId)
                throw new BadRequestException("The specified visit does not belong to this pregnancy");
        }

        // Upload images to storage → collect URLs
        var imageUrls = await UploadImagesAsync(images, userId, cancellationToken);

        var test = new PrenatalTest
        {
            PregnancyId = pregnancyId,
            VisitId = dto.VisitId,
            TestTypeId = dto.TestTypeId,
            TestDate = dto.TestDate,
            ImageUrlsJson = SerializeImageUrls(imageUrls),
            Notes = dto.Notes,
            IsAbnormalResult = dto.IsAbnormalResult
        };

        await _unitOfWork.PrenatalTests.AddAsync(test, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        var saved = tests.First(t => t.Id == test.Id);
        return MapToDto(saved, langCode);
    }

    public async Task<List<PrenatalTestDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        return tests.Select(t => MapToDto(t, langCode)).ToList();
    }

    public async Task<PagedResult<PrenatalTestDto>> GetByPregnancyIdPagedAsync(Guid pregnancyId, Guid userId, QueryOptions options, string langCode, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var pagedEntities = await _unitOfWork.PrenatalTests.GetByPregnancyIdPagedAsync(pregnancyId, langCode, options, cancellationToken);

        var dtos = pagedEntities.Items.Select(t => MapToDto(t, langCode)).ToList();
        return new PagedResult<PrenatalTestDto>(dtos, pagedEntities.Page, pagedEntities.PageSize, pagedEntities.TotalItems);
    }

    public async Task<PrenatalTestDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalTestDto dto,
        List<FileUploadItem>? newImages, string langCode, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        // Upload new images (if any)
        var newImageUrls = await UploadImagesAsync(newImages, userId, cancellationToken);

        // Merge: existing URLs user wants to keep + newly uploaded URLs
        var finalUrls = new List<string>();
        if (dto.ExistingImageUrls != null) finalUrls.AddRange(dto.ExistingImageUrls);
        if (newImageUrls != null) finalUrls.AddRange(newImageUrls);

        test.ImageUrlsJson = SerializeImageUrls(finalUrls.Count > 0 ? finalUrls : null);
        test.Notes = dto.Notes;
        test.IsAbnormalResult = dto.IsAbnormalResult;

        _unitOfWork.PrenatalTests.Update(test);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations
        var tests = await _unitOfWork.PrenatalTests.GetByPregnancyIdAsync(test.PregnancyId, langCode, cancellationToken);
        var updated = tests.First(t => t.Id == id);
        return MapToDto(updated, langCode);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        await _unitOfWork.PrenatalTests.SoftDeleteAsync(test, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrenatalTestDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var test = await _unitOfWork.PrenatalTests.GetByIdWithTranslationsAsync(id, langCode, cancellationToken)
            ?? throw new NotFoundException($"Test '{id}' not found");

        await VerifyPregnancyOwnership(test.PregnancyId, userId, cancellationToken);

        return MapToDto(test, langCode);
    }

    // ─── Private helpers ─────────────────────────────────────────────

    /// <summary>Upload danh sách file lên storage, trả về list URLs.</summary>
    private async Task<List<string>?> UploadImagesAsync(
        List<FileUploadItem>? images, Guid userId, CancellationToken cancellationToken)
    {
        if (images == null || images.Count == 0) return null;

        var urls = new List<string>();
        foreach (var image in images)
        {
            var result = await _fileStorageService.UploadAsync(
                image.Stream, image.FileName, image.ContentType, image.Length,
                ownerUserId: userId, cancellationToken: cancellationToken);
            urls.Add(result.PublicUrl);
        }
        return urls;
    }

    private async Task VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
    }

    private static PrenatalTestDto MapToDto(PrenatalTest test, string langCode)
    {
        var translation = test.TestType?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode);
        return new PrenatalTestDto(
            Id: test.Id,
            PregnancyId: test.PregnancyId,
            VisitId: test.VisitId,
            TestTypeId: test.TestTypeId,
            TestTypeCode: test.TestType?.Code ?? "",
            TestTypeDisplayName: translation?.DisplayName ?? test.TestType?.Code ?? "",
            TestDate: test.TestDate,
            ImageUrls: DeserializeImageUrls(test.ImageUrlsJson),
            Notes: test.Notes,
            IsAbnormalResult: test.IsAbnormalResult,
            CreatedAt: test.CreatedAt
        );
    }

    private static List<string>? DeserializeImageUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeImageUrls(List<string>? urls)
    {
        if (urls == null || urls.Count == 0) return null;
        return System.Text.Json.JsonSerializer.Serialize(urls);
    }
}
