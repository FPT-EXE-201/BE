using System.Text.Json;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PrenatalVisitService : IPrenatalVisitService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public PrenatalVisitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PrenatalVisitDto> CreateAsync(Guid pregnancyId, Guid userId, CreatePrenatalVisitDto dto, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var visit = new PrenatalVisit
        {
            PregnancyId = pregnancyId,
            DoctorId = dto.DoctorId,
            VisitDateTime = dto.VisitDateTime,
            VisitType = dto.VisitType,
            Location = dto.Location,
            Notes = dto.Notes,
            VitalsJson = SerializeVitals(dto.Vitals)
        };

        await _unitOfWork.PrenatalVisits.AddAsync(visit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(visit);
    }

    public async Task<List<PrenatalVisitDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var visits = await _unitOfWork.PrenatalVisits.GetByPregnancyIdAsync(pregnancyId, cancellationToken);
        return visits.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<PrenatalVisitDto>> GetByPregnancyIdPagedAsync(Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken cancellationToken = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, cancellationToken);

        var pagedEntities = await _unitOfWork.PrenatalVisits.GetByPregnancyIdPagedAsync(pregnancyId, options, cancellationToken);

        var dtos = pagedEntities.Items.Select(MapToDto).ToList();
        return new PagedResult<PrenatalVisitDto>(dtos, pagedEntities.Page, pagedEntities.PageSize, pagedEntities.TotalItems);
    }

    public async Task<PrenatalVisitDto> UpdateAsync(Guid id, Guid userId, UpdatePrenatalVisitDto dto, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        visit.DoctorId = dto.DoctorId;
        visit.VisitDateTime = dto.VisitDateTime;
        visit.VisitType = dto.VisitType;
        visit.Location = dto.Location;
        visit.Notes = dto.Notes;
        visit.VitalsJson = SerializeVitals(dto.Vitals);

        _unitOfWork.PrenatalVisits.Update(visit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(visit);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        await _unitOfWork.PrenatalVisits.SoftDeleteAsync(visit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrenatalVisitDetailDto> GetByIdAsync(Guid id, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var visit = await _unitOfWork.PrenatalVisits.GetByIdWithTestsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Visit '{id}' not found");

        await VerifyPregnancyOwnership(visit.PregnancyId, userId, cancellationToken);

        return MapToDetailDto(visit, langCode);
    }

    private async Task VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
    }

    private static PrenatalVisitDto MapToDto(PrenatalVisit visit)
    {
        return new PrenatalVisitDto(
            Id: visit.Id,
            PregnancyId: visit.PregnancyId,
            DoctorId: visit.DoctorId,
            VisitDateTime: visit.VisitDateTime,
            VisitType: visit.VisitType.ToString(),
            Location: visit.Location,
            Notes: visit.Notes,
            Vitals: DeserializeVitals(visit.VitalsJson),
            TestCount: visit.Tests?.Count(t => t.DeletedAt == null) ?? 0,
            CreatedAt: visit.CreatedAt
        );
    }

    private static PrenatalVisitDetailDto MapToDetailDto(PrenatalVisit visit, string langCode)
    {
        var tests = visit.Tests?
            .Where(t => t.DeletedAt == null)
            .Select(t =>
            {
                var translation = t.TestType?.Translations?.FirstOrDefault(tr => tr.LanguageCode == langCode);
                return new PrenatalTestDto(
                    Id: t.Id,
                    PregnancyId: t.PregnancyId,
                    VisitId: t.VisitId,
                    TestTypeId: t.TestTypeId,
                    TestTypeCode: t.TestType?.Code ?? "",
                    TestTypeDisplayName: translation?.DisplayName ?? t.TestType?.Code ?? "",
                    TestDate: t.TestDate,
                    ImageUrls: DeserializeImageUrls(t.ImageUrlsJson),
                    Notes: t.Notes,
                    IsAbnormalResult: t.IsAbnormalResult,
                    CreatedAt: t.CreatedAt
                );
            })
            .OrderByDescending(t => t.TestDate)
            .ToList() ?? new List<PrenatalTestDto>();

        return new PrenatalVisitDetailDto(
            Id: visit.Id,
            PregnancyId: visit.PregnancyId,
            DoctorId: visit.DoctorId,
            VisitDateTime: visit.VisitDateTime,
            VisitType: visit.VisitType.ToString(),
            Location: visit.Location,
            Notes: visit.Notes,
            Vitals: DeserializeVitals(visit.VitalsJson),
            Tests: tests,
            CreatedAt: visit.CreatedAt
        );
    }

    private static string? SerializeVitals(VitalsJsonDto? vitals)
    {
        if (vitals == null) return null;
        return JsonSerializer.Serialize(vitals, JsonOptions);
    }

    private static VitalsJsonDto? DeserializeVitals(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<VitalsJsonDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static List<string>? DeserializeImageUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
