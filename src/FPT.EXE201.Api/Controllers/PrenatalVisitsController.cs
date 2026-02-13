using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Authorize]
public class PrenatalVisitsController : BaseApiController
{
    private readonly IPrenatalVisitService _visitService;

    public PrenatalVisitsController(IPrenatalVisitService visitService)
    {
        _visitService = visitService;
    }

    [HttpPost("api/pregnancies/{pregnancyId:guid}/visits")]
    [RequirePermission("pregnancy.visit.write")]
    public async Task<IActionResult> Create(Guid pregnancyId, [FromBody] CreatePrenatalVisitDto dto, CancellationToken ct)
    {
        var result = await _visitService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Visit created successfully");
    }

    [HttpGet("api/pregnancies/{pregnancyId:guid}/visits")]
    [RequirePermission("pregnancy.visit.read")]
    public async Task<IActionResult> GetByPregnancy(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _visitService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.read")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _visitService.GetByIdAsync(id, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPut("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePrenatalVisitDto dto, CancellationToken ct)
    {
        var result = await _visitService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Visit updated successfully");
    }

    [HttpDelete("api/visits/{id:guid}")]
    [RequirePermission("pregnancy.visit.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _visitService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Visit deleted successfully");
    }
}
