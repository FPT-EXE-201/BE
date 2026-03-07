using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class NutritionFeedbackService : INutritionFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;

    public NutritionFeedbackService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealPlanFeedbackDto> CreatePlanFeedbackAsync(
        Guid planId, Guid userId, CreateMealPlanFeedbackDto dto,
        CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        // Check unique including soft-deleted (DB unique constraint includes them)
        var existing = await _unitOfWork.MealPlanFeedbacks
            .FindByKeyIncludingDeletedAsync(planId, userId, ct);

        MealPlanFeedback feedback;
        if (existing != null && existing.DeletedAt == null)
        {
            throw new ConflictException("You have already rated this meal plan.");
        }
        else if (existing != null && existing.DeletedAt != null)
        {
            // Restore soft-deleted entry and update with new data
            existing.DeletedAt = null;
            existing.Rating = dto.Rating;
            existing.Comment = dto.Comment;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.MealPlanFeedbacks.Update(existing);
            feedback = existing;
        }
        else
        {
            feedback = new MealPlanFeedback
            {
                MealPlanId = planId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };
            await _unitOfWork.MealPlanFeedbacks.AddAsync(feedback, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new MealPlanFeedbackDto(
            feedback.Id, feedback.MealPlanId, feedback.UserId,
            feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }

    public async Task<MealItemFeedbackDto> CreateItemFeedbackAsync(
        Guid itemId, Guid userId, CreateMealItemFeedbackDto dto,
        CancellationToken ct = default)
    {
        var item = await _unitOfWork.MealItems.GetByIdAsync(itemId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal item not found.");

        // Verify ownership through meal day → meal plan → pregnancy
        var day = await _unitOfWork.MealPlanDays
            .GetByIdAsync(item.MealDayId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan day not found.");

        var plan = await _unitOfWork.MealPlans.GetByIdAsync(day.MealPlanId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        // Check unique including soft-deleted (DB unique constraint includes them)
        var existing = await _unitOfWork.MealItemFeedbacks
            .FindByKeyIncludingDeletedAsync(itemId, userId, ct);

        MealItemFeedback feedback;
        if (existing != null && existing.DeletedAt == null)
        {
            throw new ConflictException("You have already rated this meal item.");
        }
        else if (existing != null && existing.DeletedAt != null)
        {
            // Restore soft-deleted entry and update with new data
            existing.DeletedAt = null;
            existing.Liked = dto.Liked;
            existing.Comment = dto.Comment;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.MealItemFeedbacks.Update(existing);
            feedback = existing;
        }
        else
        {
            feedback = new MealItemFeedback
            {
                MealItemId = itemId,
                UserId = userId,
                Liked = dto.Liked,
                Comment = dto.Comment
            };
            await _unitOfWork.MealItemFeedbacks.AddAsync(feedback, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new MealItemFeedbackDto(
            feedback.Id, feedback.MealItemId, feedback.UserId,
            feedback.Liked, feedback.Comment, feedback.CreatedAt);
    }

    private async Task VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
    }
}
