using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/recipes")]
[Authorize]
public class RecipesController : BaseApiController
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet("{recipeId:guid}")]
    [RequirePermission("recipe.read")]
    public async Task<IActionResult> GetById(Guid recipeId, CancellationToken ct = default)
    {
        var result = await _recipeService.GetByIdAsync(
            recipeId, GetCurrentUserId(), ct);
        return Success(result);
    }
}
