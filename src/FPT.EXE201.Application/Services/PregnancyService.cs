using AutoMapper;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class PregnancyService : IPregnancyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PregnancyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PregnancyDto> CreateAsync(Guid userId, CreatePregnancyDto dto, CancellationToken cancellationToken = default)
    {
        // Business rule: chỉ 1 active pregnancy per user
        var existing = await _unitOfWork.Pregnancies.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existing != null)
            throw new ConflictException("You already have an active pregnancy. Please end or deliver the current pregnancy before creating a new one.");

        var nextNo = await _unitOfWork.Pregnancies.GetNextPregnancyNumberAsync(userId, cancellationToken);

        // Validate gravida / para
        ValidateGravidaPara(dto.Gravida, dto.Para, nextNo);

        var pregnancy = new Pregnancy
        {
            UserId = userId,
            PregnancyNumber = nextNo,
            Status = PregnancyStatus.Active,
            LastMenstrualPeriodDate = dto.LastMenstrualPeriodDate,
            EstimatedConceptionDate = dto.EstimatedConceptionDate,
            Notes = dto.Notes,
            // Nhóm 1
            BabyNickname = dto.BabyNickname,
            BabyGender = dto.BabyGender,
            PregnancyType = dto.PregnancyType,
            // Nhóm 2
            MotherBloodType = dto.MotherBloodType,
            PrePregnancyWeightKg = dto.PrePregnancyWeightKg,
            HeightCm = dto.HeightCm,
            // Nhóm 3
            DueDateSource = dto.DueDateSource,
            Gravida = dto.Gravida,
            Para = dto.Para,
            CoverImageUrl = dto.CoverImageUrl
        };

        // Auto-calculate EDD from LMP
        if (dto.LastMenstrualPeriodDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.LastMenstrualPeriodDate.Value.AddDays(280);
            pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(dto.LastMenstrualPeriodDate);
        }

        await _unitOfWork.Pregnancies.AddAsync(pregnancy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetActiveByUserIdAsync(userId, cancellationToken);
        if (pregnancy == null) return null;

        // Recalculate gestational week on read
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        return MapToDto(pregnancy);
    }

    public async Task<List<PregnancyDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancies = await _unitOfWork.Pregnancies.GetByUserIdAsync(userId, cancellationToken);
        return pregnancies.Select(p =>
        {
            p.CurrentGestationalWeek = CalculateCurrentGestationalWeek(p.LastMenstrualPeriodDate);
            return MapToDto(p);
        }).ToList();
    }

    public async Task<PregnancyDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnership(id, userId, cancellationToken);
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto> UpdateAsync(Guid id, Guid userId, UpdatePregnancyDto dto, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);

        pregnancy.LastMenstrualPeriodDate = dto.LastMenstrualPeriodDate ?? pregnancy.LastMenstrualPeriodDate;
        pregnancy.EstimatedConceptionDate = dto.EstimatedConceptionDate ?? pregnancy.EstimatedConceptionDate;
        pregnancy.Notes = dto.Notes ?? pregnancy.Notes;

        // Nhóm 1
        pregnancy.BabyNickname = dto.BabyNickname ?? pregnancy.BabyNickname;
        if (dto.BabyGender.HasValue) pregnancy.BabyGender = dto.BabyGender.Value;
        if (dto.PregnancyType.HasValue) pregnancy.PregnancyType = dto.PregnancyType.Value;

        // Nhóm 2
        pregnancy.MotherBloodType = dto.MotherBloodType ?? pregnancy.MotherBloodType;
        pregnancy.PrePregnancyWeightKg = dto.PrePregnancyWeightKg ?? pregnancy.PrePregnancyWeightKg;
        pregnancy.HeightCm = dto.HeightCm ?? pregnancy.HeightCm;

        // Nhóm 3
        if (dto.DueDateSource.HasValue) pregnancy.DueDateSource = dto.DueDateSource.Value;

        var newGravida = dto.Gravida ?? pregnancy.Gravida;
        var newPara = dto.Para ?? pregnancy.Para;
        ValidateGravidaPara(newGravida, newPara, pregnancy.PregnancyNumber);
        pregnancy.Gravida = newGravida;
        pregnancy.Para = newPara;

        pregnancy.CoverImageUrl = dto.CoverImageUrl ?? pregnancy.CoverImageUrl;

        // Recalculate EDD if LMP changed
        if (dto.LastMenstrualPeriodDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.LastMenstrualPeriodDate.Value.AddDays(280);
            pregnancy.DueDateSource = DueDateSource.LMP;
        }
        // Nếu FE truyền EDD mới (bác sĩ điều chỉnh), dùng EDD đó thay thế
        if (dto.ExpectedDeliveryDate.HasValue)
        {
            pregnancy.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        }
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);

        _unitOfWork.Pregnancies.Update(pregnancy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task<PregnancyDto> ChangeStatusAsync(Guid id, Guid userId, ChangePregnancyStatusDto dto, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);

        // Business rule: only Active → terminal states
        if (pregnancy.Status != PregnancyStatus.Active)
            throw new BadRequestException("Can only change status of an active pregnancy");
        if (dto.Status == PregnancyStatus.Active)
            throw new BadRequestException("Cannot revert to Active status");

        pregnancy.Status = dto.Status;

        // Lưu thông tin sinh khi status = Delivered
        if (dto.Status == PregnancyStatus.Delivered)
        {
            pregnancy.ActualDeliveryDate = dto.ActualDeliveryDate;
            pregnancy.DeliveryMethod = dto.DeliveryMethod;
        }

        // Recalculate gestational week on status change
        pregnancy.CurrentGestationalWeek = CalculateCurrentGestationalWeek(pregnancy.LastMenstrualPeriodDate);

        _unitOfWork.Pregnancies.Update(pregnancy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(pregnancy);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var pregnancy = await GetAndVerifyOwnershipTracked(id, userId, cancellationToken);
        await _unitOfWork.Pregnancies.SoftDeleteAsync(pregnancy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ═══ Private Helpers ═══

    /// <summary>
    /// Gravida = tổng số lần mang thai (tính cả lần này) → phải >= pregnancyNumber.
    /// Para = số lần sinh trước đó → phải >= 0 và < Gravida.
    /// </summary>
    private static void ValidateGravidaPara(int? gravida, int? para, int pregnancyNumber)
    {
        if (gravida.HasValue)
        {
            if (gravida.Value < pregnancyNumber)
                throw new BadRequestException(
                    $"Gravida ({gravida.Value}) must be >= pregnancy number ({pregnancyNumber}). " +
                    $"Gravida counts ALL pregnancies including those before using the app.");

            if (para.HasValue && para.Value >= gravida.Value)
                throw new BadRequestException(
                    $"Para ({para.Value}) must be less than Gravida ({gravida.Value}). " +
                    $"Para counts deliveries before the current pregnancy.");
        }

        if (para.HasValue && para.Value < 0)
            throw new BadRequestException("Para cannot be negative.");
    }

    private async Task<Pregnancy> GetAndVerifyOwnership(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy with id '{id}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
        return pregnancy;
    }

    private async Task<Pregnancy> GetAndVerifyOwnershipTracked(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdTrackedAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Pregnancy with id '{id}' not found");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("You do not have access to this pregnancy");
        return pregnancy;
    }

    private static int? CalculateCurrentGestationalWeek(DateOnly? lastMenstrualPeriodDate)
    {
        if (!lastMenstrualPeriodDate.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDays = today.DayNumber - lastMenstrualPeriodDate.Value.DayNumber;
        if (totalDays < 0) return null;
        var weeks = totalDays / 7;
        return weeks <= 45 ? weeks : null;
    }

    private static string? FormatGestationalAge(DateOnly? lastMenstrualPeriodDate)
    {
        if (!lastMenstrualPeriodDate.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDays = today.DayNumber - lastMenstrualPeriodDate.Value.DayNumber;
        if (totalDays < 0) return null;
        var weeks = totalDays / 7;
        var remainingDays = totalDays % 7;
        return weeks <= 45 ? $"{weeks}w{remainingDays}d" : null;
    }

    private static decimal? CalculateBmi(decimal? weightKg, decimal? heightCm)
    {
        if (!weightKg.HasValue || !heightCm.HasValue || heightCm.Value <= 0) return null;
        var heightM = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (heightM * heightM), 1);
    }

    private static string? FormatObstetricFormula(int? gravida, int? para)
    {
        if (!gravida.HasValue) return null;
        return para.HasValue ? $"G{gravida}P{para}" : $"G{gravida}";
    }

    private PregnancyDto MapToDto(Pregnancy pregnancy)
    {
        return new PregnancyDto(
            Id: pregnancy.Id,
            UserId: pregnancy.UserId,
            PregnancyNumber: pregnancy.PregnancyNumber,
            Status: pregnancy.Status.ToString(),
            LastMenstrualPeriodDate: pregnancy.LastMenstrualPeriodDate,
            ExpectedDeliveryDate: pregnancy.ExpectedDeliveryDate,
            EstimatedConceptionDate: pregnancy.EstimatedConceptionDate,
            CurrentGestationalWeek: pregnancy.CurrentGestationalWeek,
            GestationalAgeDisplay: FormatGestationalAge(pregnancy.LastMenstrualPeriodDate),
            Notes: pregnancy.Notes,
            // Nhóm 1
            BabyNickname: pregnancy.BabyNickname,
            BabyGender: pregnancy.BabyGender.ToString(),
            PregnancyType: pregnancy.PregnancyType.ToString(),
            // Nhóm 2
            MotherBloodType: pregnancy.MotherBloodType,
            PrePregnancyWeightKg: pregnancy.PrePregnancyWeightKg,
            HeightCm: pregnancy.HeightCm,
            PrePregnancyBmi: CalculateBmi(pregnancy.PrePregnancyWeightKg, pregnancy.HeightCm),
            // Nhóm 3
            DueDateSource: pregnancy.DueDateSource.ToString(),
            Gravida: pregnancy.Gravida,
            Para: pregnancy.Para,
            ObstetricFormula: FormatObstetricFormula(pregnancy.Gravida, pregnancy.Para),
            ActualDeliveryDate: pregnancy.ActualDeliveryDate,
            DeliveryMethod: pregnancy.DeliveryMethod?.ToString(),
            CoverImageUrl: pregnancy.CoverImageUrl,
            CreatedAt: pregnancy.CreatedAt,
            UpdatedAt: pregnancy.UpdatedAt
        );
    }
}
