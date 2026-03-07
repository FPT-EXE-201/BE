using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IRecipeService
{
    Task<RecipeDetailDto> GetByIdAsync(
        Guid recipeId, Guid userId, CancellationToken ct = default);
}
