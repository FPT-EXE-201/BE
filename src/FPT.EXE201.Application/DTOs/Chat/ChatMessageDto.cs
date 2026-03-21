using FPT.EXE201.Application.DTOs.Common;

namespace FPT.EXE201.Application.DTOs.Chat;
public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid? ReceiverUserId { get; set; }
    public string? ReceiverName { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? AttachmentFileId { get; set; }
    public FileDto? AttachmentFile { get; set; }
    public DateTime SentAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
public class FileDto
{
    public Guid Id { get; set; }
    public string? OriginalFileName { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? FileUrl { get; set; }
}