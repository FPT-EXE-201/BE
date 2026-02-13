using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.PrenatalTests;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Api.Controllers;

[Authorize]
public class PrenatalTestsController : BaseApiController
{
    private readonly IPrenatalTestService _testService;

    public PrenatalTestsController(IPrenatalTestService testService)
    {
        _testService = testService;
    }

    /// <summary>Tạo test mới. Gửi ảnh qua multipart/form-data, metadata qua form fields.</summary>
    [HttpPost("api/pregnancies/{pregnancyId:guid}/tests")]
    [RequirePermission("pregnancy.test.write")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        Guid pregnancyId,
        [FromForm] CreatePrenatalTestDto dto,
        [FromForm] List<IFormFile>? images,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var uploadItems = MapToUploadItems(images);
        var result = await _testService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, uploadItems, lang, ct);
        return Created(result, "Test created successfully");
    }

    [HttpGet("api/pregnancies/{pregnancyId:guid}/tests")]
    [RequirePermission("pregnancy.test.read")]
    public async Task<IActionResult> GetByPregnancy(Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.GetByPregnancyIdAsync(pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpGet("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.read")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _testService.GetByIdAsync(id, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    /// <summary>Update test. Gửi ảnh mới + metadata qua multipart/form-data.</summary>
    [HttpPut("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.write")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromForm] UpdatePrenatalTestDto dto,
        [FromForm] List<IFormFile>? newImages,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var uploadItems = MapToUploadItems(newImages);
        var result = await _testService.UpdateAsync(id, GetCurrentUserId(), dto, uploadItems, lang, ct);
        return Success(result, "Test updated successfully");
    }

    [HttpDelete("api/tests/{id:guid}")]
    [RequirePermission("pregnancy.test.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _testService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Test deleted successfully");
    }

    /// <summary>Convert IFormFile list → FileUploadItem list (tách ASP.NET dependency khỏi Application layer).</summary>
    private static List<FileUploadItem>? MapToUploadItems(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0) return null;
        return files.Select(f => new FileUploadItem(
            Stream: f.OpenReadStream(),
            FileName: f.FileName,
            ContentType: f.ContentType,
            Length: f.Length
        )).ToList();
    }
}
