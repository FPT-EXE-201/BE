using AutoMapper;
using FPT.EXE201.Application.DTOs.Chat;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.MapperProfiles;

/// <summary>
/// AutoMapper profile for Chat mappings
/// </summary>
public class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        // ChatMessage -> ChatMessageDto
        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.Ignore()) // Set manually
            .ForMember(dest => dest.ReceiverName, opt => opt.Ignore()) // Set manually
            .ForMember(dest => dest.AttachmentFile, opt => opt.Ignore()); // Set manually
    }
}