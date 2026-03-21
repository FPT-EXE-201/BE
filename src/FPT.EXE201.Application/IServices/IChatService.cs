using FPT.EXE201.Application.DTOs.Chat;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.IServices;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(Guid senderUserId, SendMessageRequestDto request, FileUploadInfo? attachment, CancellationToken ct = default);
    Task<ChatMessageDto> EditMessageAsync(Guid messageId, Guid editorUserId, EditMessageRequestDto request, CancellationToken ct = default);
    Task<ChatMessageDto> DeleteMessageAsync(Guid messageId, Guid editorUserId, CancellationToken ct = default);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<FileDownloadResult?> GetFileAsync(Guid fileId, CancellationToken ct = default);
}
public record FileDownloadResult(Stream Stream, string FileName, string MimeType);