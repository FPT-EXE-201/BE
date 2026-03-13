using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Chat;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/[controller]")]
[Tags("Chat")]
public class ChatController : BaseApiController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("send")]
    [RequirePermission("chat.send")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SendMessage(
        [FromForm] SendMessageRequestDto request,
        IFormFile? attachment = null,
        CancellationToken ct = default)
    {
        var senderUserId = GetCurrentUserId();
        FileUploadInfo? attachmentInfo = null;
        if (attachment != null)
        {
            attachmentInfo = new FileUploadInfo(attachment.OpenReadStream(), attachment.FileName, attachment.ContentType, attachment.Length);
        }
        var message = await _chatService.SendMessageAsync(senderUserId, request, attachmentInfo, ct);
        return Created(message, "Message sent successfully");
    }

    [HttpGet("messages/{otherUserId}")]
    [RequirePermission("chat.send")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(Guid otherUserId, CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();
        var messages = await _chatService.GetMessagesAsync(currentUserId, otherUserId, ct);
        return Success(messages);
    }

    [HttpGet("download/{fileId}")]
    [RequirePermission("chat.send")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();

        var file = await _chatService.GetFileAsync(fileId, ct);
        if (file == null)
            return NotFound("File not found");

        return File(file.Stream, file.MimeType, file.FileName);
    }
}