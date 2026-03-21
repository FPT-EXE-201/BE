using System;

namespace FPT.EXE201.Application.DTOs.Chat;

public class ChatPartnerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
