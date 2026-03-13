namespace FPT.EXE201.Application.DTOs.Chat;
public class SendMessageRequestDto
{
    public Guid ReceiverUserId { get; set; }
    public string Content { get; set; } = string.Empty;
}