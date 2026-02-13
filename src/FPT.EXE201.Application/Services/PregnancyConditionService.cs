using AutoMapper;
using FPT.EXE201.Application.DTOs.PregnancyConditions;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class PregnancyConditionService : IPregnancyConditionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PregnancyConditionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PregnancyConditionDto> AddAsync(Guid pregnancyId, Guid userId, CreatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        // Verify pregnancy ownership
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        // Verify condition exists in reference data
        var refCondition = await _unitOfWork.RefPregnancyConditions.GetByIdAsync(dto.ConditionId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Condition '{dto.ConditionId}' not found in reference data");

        // Check duplicate: same condition already assigned to this pregnancy
        var existingCondition = await _unitOfWork.PregnancyConditions
            .ExistsAsync(pc => pc.PregnancyId == pregnancyId && pc.ConditionId == dto.ConditionId && pc.DeletedAt == null, false, cancellationToken);
        if (existingCondition)
            throw new ConflictException($"Condition '{refCondition.Code}' is already assigned to this pregnancy");

        var entity = new PregnancyCondition
        {
            PregnancyId = pregnancyId,
            ConditionId = dto.ConditionId,
            DiagnosedDate = dto.DiagnosedDate,
            Severity = dto.Severity,
            Notes = dto.Notes
        };

        await _unitOfWork.PregnancyConditions.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        var saved = conditions.First(c => c.Id == entity.Id);
        return MapToDto(saved, langCode);
    }

    public async Task<List<PregnancyConditionDto>> GetByPregnancyIdAsync(Guid pregnancyId, Guid userId, string langCode, CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy '{pregnancyId}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(pregnancyId, langCode, cancellationToken);
        return conditions.Select(c => MapToDto(c, langCode)).ToList();
    }

    public async Task<PregnancyConditionDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyConditionDto dto, string langCode, CancellationToken cancellationToken = default)
    {
        var condition = await _unitOfWork.PregnancyConditions.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy condition '{id}' not found");

        // Verify ownership through pregnancy
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(condition.PregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Pregnancy not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        condition.DiagnosedDate = dto.DiagnosedDate;
        condition.Severity = dto.Severity;
        condition.Notes = dto.Notes;

        _unitOfWork.PregnancyConditions.Update(condition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations for response
        var conditions = await _unitOfWork.PregnancyConditions.GetByPregnancyIdAsync(condition.PregnancyId, langCode, cancellationToken);
        var updated = conditions.First(c => c.Id == id);
        return MapToDto(updated, langCode);
    }

    public async Task RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var condition = await _unitOfWork.PregnancyConditions.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy condition '{id}' not found");

        // Verify ownership through pregnancy
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(condition.PregnancyId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Pregnancy not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");

        await _unitOfWork.PregnancyConditions.SoftDeleteAsync(condition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PregnancyConditionDto MapToDto(PregnancyCondition entity, string langCode)
    {
        var translation = entity.Condition?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode);
        return new PregnancyConditionDto(
            Id: entity.Id,
            PregnancyId: entity.PregnancyId,
            ConditionId: entity.ConditionId,
            ConditionCode: entity.Condition?.Code ?? "",
            ConditionDisplayName: translation?.DisplayName ?? entity.Condition?.Code ?? "",
            ConditionDescription: translation?.Description,
            DiagnosedDate: entity.DiagnosedDate,
            Severity: entity.Severity?.ToString(),
            Notes: entity.Notes,
            CreatedAt: entity.CreatedAt
        );
    }
}
