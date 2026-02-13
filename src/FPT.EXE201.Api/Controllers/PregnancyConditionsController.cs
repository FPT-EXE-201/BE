using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PregnancyConditions;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Route("api/pregnancies/{pregnancyId:guid}/conditions")]
[Authorize]
public class PregnancyConditionsController : BaseApiController
{
    private readonly IPregnancyConditionService _conditionService;

    public PregnancyConditionsController(IPregnancyConditionService conditionService)
    {
        _conditionService = conditionService;
    }

    [HttpPost]
    [RequirePermission("pregnancy.condition.write")]
    public async Task<IActionResult> Add(Guid pregnancyId, [FromBody] CreatePregnancyConditionDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.AddAsync(pregnancyId, GetCurrentUserId(), dto, lang, ct);
        return Created(result, "Condition added successfully");
    }

    [HttpGet]
    [RequirePermission("pregnancy.condition.read")]
    public async Task<IActionResult> GetAll(Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("pregnancy.condition.write")]
    public async Task<IActionResult> Update(Guid pregnancyId, Guid id, [FromBody] UpdatePregnancyConditionDto dto, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _conditionService.UpdateAsync(id, GetCurrentUserId(), dto, lang, ct);
        return Success(result, "Condition updated successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("pregnancy.condition.delete")]
    public async Task<IActionResult> Remove(Guid pregnancyId, Guid id, CancellationToken ct)
    {
        await _conditionService.RemoveAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Condition removed successfully");
    }
}
