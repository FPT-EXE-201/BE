using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Route("api/pregnancies")]
[Authorize]
public class PregnanciesController : BaseApiController
{
    private readonly IPregnancyService _pregnancyService;

    public PregnanciesController(IPregnancyService pregnancyService)
    {
        _pregnancyService = pregnancyService;
    }

    [HttpPost]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> Create([FromBody] CreatePregnancyDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.CreateAsync(GetCurrentUserId(), dto, ct);
        return Created(result, "Pregnancy created successfully");
    }

    [HttpGet("active")]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _pregnancyService.GetActiveAsync(GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _pregnancyService.GetAllByUserAsync(GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("pregnancy.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _pregnancyService.GetByIdAsync(id, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePregnancyDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Pregnancy updated successfully");
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission("pregnancy.write")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangePregnancyStatusDto dto, CancellationToken ct)
    {
        var result = await _pregnancyService.ChangeStatusAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Pregnancy status changed successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("pregnancy.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _pregnancyService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Pregnancy deleted successfully");
    }
}
