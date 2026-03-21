using AutoMapper;
using FPT.EXE201.Application.DTOs.Chat;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;

    public ChatService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid senderUserId, SendMessageRequestDto request, FileUploadInfo? attachment, CancellationToken ct = default)
    {
        // Validate users exist
        var sender = await _unitOfWork.Users.GetByIdAsync(senderUserId, null, false, ct)
            ?? throw new NotFoundException("Sender not found");

        var receiver = await _unitOfWork.Users.GetByIdAsync(request.ReceiverUserId, null, false, ct)
            ?? throw new NotFoundException("Receiver not found");

        Guid? attachmentFileId = null;
        if (attachment != null)
        {
            // Upload file
            var uploadResult = await _fileStorageService.UploadAsync(
                attachment.Stream,
                attachment.FileName,
                attachment.ContentType,
                attachment.FileSize,
                senderUserId,
                ct);

            // Save StorageFile
            var storageFile = new StorageFile
            {
                OwnerUserId = senderUserId,
                StorageProvider = "local", 
                ObjectKey = uploadResult.ObjectKey,
                PublicUrl = uploadResult.PublicUrl,
                OriginalFileName = uploadResult.OriginalFileName,
                MimeType = uploadResult.MimeType,
                FileSizeBytes = uploadResult.FileSizeBytes
            };

            await _unitOfWork.StorageFiles.AddAsync(storageFile, ct);
            attachmentFileId = storageFile.Id;
        }

        // Create message
        var message = new ChatMessage
        {
            SenderUserId = senderUserId,
            ReceiverUserId = request.ReceiverUserId,
            Content = request.Content,
            AttachmentFileId = attachmentFileId,
            SentAt = DateTime.UtcNow
        };

        await _unitOfWork.ChatMessages.AddAsync(message, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Map to DTO
        var dto = _mapper.Map<ChatMessageDto>(message);
        dto.SenderName = sender.Profile?.FullName ?? sender.Email ?? "Unknown";
        dto.ReceiverName = receiver.Profile?.FullName ?? receiver.Email;

        if (attachmentFileId.HasValue)
        {
            var file = await _unitOfWork.StorageFiles.GetByIdAsync(attachmentFileId.Value, null, false, ct);
            if (file != null)
            {
                dto.AttachmentFile = new FileDto
                {
                    Id = file.Id,
                    OriginalFileName = file.OriginalFileName,
                    MimeType = file.MimeType,
                    FileSizeBytes = file.FileSizeBytes,
                    FileUrl = file.PublicUrl
                };
            }
        }

        return dto;
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var messages = await _unitOfWork.ChatMessages.GetMessagesBetweenUsersAsync(userId1, userId2, ct);

        var dtos = new List<ChatMessageDto>();
        foreach (var message in messages)
        {
            var dto = _mapper.Map<ChatMessageDto>(message);

            var sender = await _unitOfWork.Users.GetByIdWithProfileAsync(message.SenderUserId, false, ct);
            dto.SenderName = sender?.Profile?.FullName ?? sender?.Email ?? "Unknown";

            if (message.ReceiverUserId.HasValue)
            {
                var receiver = await _unitOfWork.Users.GetByIdWithProfileAsync(message.ReceiverUserId.Value, false, ct);
                dto.ReceiverName = receiver?.Profile?.FullName ?? receiver?.Email;
            }

            if (message.AttachmentFileId.HasValue)
            {
                var file = await _unitOfWork.StorageFiles.GetByIdAsync(message.AttachmentFileId.Value, null, false, ct);
                if (file != null)
                {
                    dto.AttachmentFile = new FileDto
                    {
                        Id = file.Id,
                        OriginalFileName = file.OriginalFileName,
                        MimeType = file.MimeType,
                        FileSizeBytes = file.FileSizeBytes,
                        FileUrl = file.PublicUrl
                    };
                }
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<ChatMessageDto> EditMessageAsync(Guid messageId, Guid editorUserId, EditMessageRequestDto request, CancellationToken ct = default)
    {
        var message = await _unitOfWork.ChatMessages.GetByIdTrackedAsync(messageId, null, false, ct)
            ?? throw new NotFoundException("Message not found");

        if (message.SenderUserId != editorUserId)
            throw new ForbiddenException("You are not allowed to edit this message");

        message.Content = request.Content;

        await _unitOfWork.SaveChangesAsync(ct);

        // Map to DTO
        var dto = _mapper.Map<ChatMessageDto>(message);

        var sender = await _unitOfWork.Users.GetByIdWithProfileAsync(message.SenderUserId, false, ct);
        dto.SenderName = sender?.Profile?.FullName ?? sender?.Email ?? "Unknown";

        if (message.ReceiverUserId.HasValue)
        {
            var receiver = await _unitOfWork.Users.GetByIdWithProfileAsync(message.ReceiverUserId.Value, false, ct);
            dto.ReceiverName = receiver?.Profile?.FullName ?? receiver?.Email;
        }

        if (message.AttachmentFileId.HasValue)
        {
            var file = await _unitOfWork.StorageFiles.GetByIdAsync(message.AttachmentFileId.Value, null, false, ct);
            if (file != null)
            {
                dto.AttachmentFile = new FileDto
                {
                    Id = file.Id,
                    OriginalFileName = file.OriginalFileName,
                    MimeType = file.MimeType,
                    FileSizeBytes = file.FileSizeBytes,
                    FileUrl = file.PublicUrl
                };
            }
        }

        return dto;
    }

    public async Task<ChatMessageDto> DeleteMessageAsync(Guid messageId, Guid editorUserId, CancellationToken ct = default)
    {
        var message = await _unitOfWork.ChatMessages.GetByIdTrackedAsync(messageId, null, false, ct)
            ?? throw new NotFoundException("Message not found");

        if (message.SenderUserId != editorUserId)
            throw new ForbiddenException("You are not allowed to delete this message");

        message.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        // Map to DTO (keep sender/receiver info for broadcasting)
        var dto = _mapper.Map<ChatMessageDto>(message);

        var sender = await _unitOfWork.Users.GetByIdWithProfileAsync(message.SenderUserId, false, ct);
        dto.SenderName = sender?.Profile?.FullName ?? sender?.Email ?? "Unknown";

        if (message.ReceiverUserId.HasValue)
        {
            var receiver = await _unitOfWork.Users.GetByIdWithProfileAsync(message.ReceiverUserId.Value, false, ct);
            dto.ReceiverName = receiver?.Profile?.FullName ?? receiver?.Email;
        }

        return dto;
    }

    public async Task<FileDownloadResult?> GetFileAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _unitOfWork.StorageFiles.GetByIdAsync(fileId, null, false, ct);
        if (file == null)
            return null;

        var stream = await _fileStorageService.DownloadAsync(file.ObjectKey, ct);
        return new FileDownloadResult(stream, file.OriginalFileName ?? "file", file.MimeType);
    }

    public async Task<IEnumerable<ChatPartnerDto>> GetDoctorUsersAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var doctors = await _unitOfWork.Users.GetByRoleAsync("DOCTOR", ct);
        return doctors
            .Where(u => u.Id != currentUserId) // Exclude self
            .Select(u => new ChatPartnerDto
            {
                Id = u.Id,
                FullName = u.Profile?.FullName ?? u.Email ?? "Unknown Doctor",
                AvatarUrl = u.Profile?.AvatarUrl
            }).ToList();
    }

    public async Task<IEnumerable<ChatPartnerDto>> GetActiveConversationsAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var participantIds = await _unitOfWork.ChatMessages.GetConversationParticipantsAsync(currentUserId, ct);
        var result = new List<ChatPartnerDto>();

        foreach (var otherUserId in participantIds)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfileAsync(otherUserId, false, ct);
            if (user == null) continue;

            result.Add(new ChatPartnerDto
            {
                Id = user.Id,
                FullName = user.Profile?.FullName ?? user.Email ?? "Unknown",
                AvatarUrl = user.Profile?.AvatarUrl
            });
        }

        return result;
    }
}