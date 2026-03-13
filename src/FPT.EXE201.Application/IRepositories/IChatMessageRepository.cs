using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

/// <summary>
/// Chat message repository interface
/// </summary>
public interface IChatMessageRepository : IGenericRepository<ChatMessage>
{
    Task<IEnumerable<ChatMessage>> GetMessagesBetweenUsersAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
}