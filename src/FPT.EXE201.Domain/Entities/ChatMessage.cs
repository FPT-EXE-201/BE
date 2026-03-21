using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;
public class ChatMessage : BaseEntity
{
    public Guid SenderUserId { get; set; }

    public Guid? ReceiverUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid? AttachmentFileId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}