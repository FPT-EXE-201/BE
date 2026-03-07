using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class RecipeService : IRecipeService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RecipeDetailDto> GetByIdAsync(
        Guid recipeId, Guid userId, CancellationToken ct = default)
    {
        var recipe = await _unitOfWork.Recipes.GetByIdWithDetailsAsync(recipeId, ct)
            ?? throw new NotFoundException("Recipe not found.");

        // Verify ownership through pregnancy
        await VerifyPregnancyOwnership(recipe.PregnancyId, userId, ct);

        return new RecipeDetailDto(
            recipe.Id, recipe.PregnancyId, recipe.Title,
            recipe.Instructions, recipe.Servings,
            recipe.PrepMinutes, recipe.CookMinutes,
            recipe.CreatedAt);
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
