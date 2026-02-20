using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class DocumentFileRepository : GenericRepository<DocumentFile>, IDocumentFileRepository
{
    public DocumentFileRepository(AppDbContext context) : base(context) { }

    public async Task<List<DocumentFile>> GetByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(df => df.DocumentId == documentId)
            .Include(df => df.StorageFile)
            .OrderBy(df => df.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
