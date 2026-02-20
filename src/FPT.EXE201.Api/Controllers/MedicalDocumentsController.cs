using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
public class MedicalDocumentsController : BaseApiController
{
    private readonly IMedicalDocumentService _documentService;

    public MedicalDocumentsController(IMedicalDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// Upload 1-N ảnh/file + tạo document trong 1 bước (multipart/form-data).
    /// Hỗ trợ multi-file: phiếu khám dài → chụp nhiều tấm.
    /// </summary>
    [HttpPost("pregnancies/{pregnancyId}/documents")]
    [RequirePermission("document.create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        Guid pregnancyId,
        List<IFormFile> files,
        [FromForm] Guid? documentTypeId = null,
        [FromForm] string? title = null,
        [FromForm] DateOnly? documentDate = null,
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
            return BadRequest("At least one file is required.");

        var userId = GetCurrentUserId();
        var dto = new CreateMedicalDocumentDto(documentTypeId, title, documentDate, DocumentSource.Upload, notes);

        var fileInfos = new List<FileUploadInfo>();
        var streams = new List<Stream>();
        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                fileInfos.Add(new FileUploadInfo(stream, file.FileName, file.ContentType, file.Length));
            }

            var result = await _documentService.CreateWithFilesAsync(
                pregnancyId, dto, fileInfos, userId, cancellationToken);

            return Created(result, "Document created successfully.");
        }
        finally
        {
            foreach (var stream in streams)
                stream.Dispose();
        }
    }

    /// <summary>List documents của thai kỳ. Supports optional isFavorite filter.</summary>
    [HttpGet("pregnancies/{pregnancyId}/documents")]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetByPregnancy(
        Guid pregnancyId,
        [FromQuery] bool? isFavorite,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetByPregnancyIdAsync(pregnancyId, userId, isFavorite, cancellationToken);
        return Success(result);
    }

    /// <summary>Chi tiết 1 document.</summary>
    [HttpGet("documents/{id}")]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetByIdAsync(id, userId, cancellationToken);
        return Success(result);
    }

    /// <summary>Update metadata (title, notes, visit link).</summary>
    [HttpPut("documents/{id}")]
    [RequirePermission("document.update")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateMedicalDocumentDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.UpdateAsync(id, dto, userId, cancellationToken);
        return Success(result, "Document updated successfully.");
    }

    /// <summary>Toggle yêu thích.</summary>
    [HttpPatch("documents/{id}/favorite")]
    [RequirePermission("document.favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.ToggleFavoriteAsync(id, userId, cancellationToken);
        return Success(result, "Favorite status updated.");
    }

    /// <summary>Soft delete document.</summary>
    [HttpDelete("documents/{id}")]
    [RequirePermission("document.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _documentService.DeleteAsync(id, userId, cancellationToken);
        return Success<object?>(null, "Document deleted successfully.");
    }
}
