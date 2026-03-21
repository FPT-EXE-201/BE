using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        return await ((AppDbContext)_context).ChatMessages
            .Where(m => (m.SenderUserId == userId1 && m.ReceiverUserId == userId2) ||
                        (m.SenderUserId == userId2 && m.ReceiverUserId == userId1))
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Guid>> GetConversationParticipantsAsync(Guid userId, CancellationToken ct = default)
    {
        var senders = await ((AppDbContext)_context).ChatMessages
            .Where(m => m.ReceiverUserId == userId && m.SenderUserId != userId)
            .Select(m => m.SenderUserId)
            .Distinct()
            .ToListAsync(ct);

        var receivers = await ((AppDbContext)_context).ChatMessages
            .Where(m => m.SenderUserId == userId && m.ReceiverUserId.HasValue && m.ReceiverUserId != userId)
            .Select(m => m.ReceiverUserId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return senders.Union(receivers).Distinct();
    }
}