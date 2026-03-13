using FPT.EXE201.Api.Hubs;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Chat;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT.EXE201.Api.Controllers;

[Route("api/[controller]")]
[Tags("Chat")]
public class ChatController : BaseApiController
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
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

        // Broadcast to sender & receiver in real-time
        await BroadcastMessageToParticipantsAsync("ReceiveMessage", message);

        return Created(message, "Message sent successfully");
    }

    [HttpPut("edit/{messageId}")]
    [RequirePermission("chat.send")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequestDto request, CancellationToken ct = default)
    {
        var editorId = GetCurrentUserId();
        var message = await _chatService.EditMessageAsync(messageId, editorId, request, ct);

        await BroadcastMessageToParticipantsAsync("MessageEdited", message);

        return Success(message, "Message updated successfully");
    }

    [HttpDelete("{messageId}")]
    [RequirePermission("chat.send")]
    public async Task<IActionResult> DeleteMessage(Guid messageId, CancellationToken ct = default)
    {
        var editorId = GetCurrentUserId();
        var message = await _chatService.DeleteMessageAsync(messageId, editorId, ct);

        await BroadcastMessageToParticipantsAsync("MessageDeleted", message);

        return NoContentResponse();
    }

    private Task BroadcastMessageToParticipantsAsync(string method, ChatMessageDto message)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(ChatHub.GetUserGroupName(message.SenderUserId)).SendAsync(method, message)
        };

        if (message.ReceiverUserId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group(ChatHub.GetUserGroupName(message.ReceiverUserId.Value)).SendAsync(method, message));
        }

        return Task.WhenAll(tasks);
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