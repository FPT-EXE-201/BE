using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IDocumentFileRepository : IGenericRepository<DocumentFile>
{
    /// <summary>Lấy tất cả files của document, ordered by SortOrder.</summary>
    Task<List<DocumentFile>> GetByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default);
}
