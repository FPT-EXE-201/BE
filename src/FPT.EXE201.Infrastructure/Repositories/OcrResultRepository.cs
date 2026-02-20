using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class OcrResultRepository : GenericRepository<OcrResult>, IOcrResultRepository
{
    public OcrResultRepository(AppDbContext context) : base(context) { }

    public async Task<OcrResult?> GetLatestByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.DocumentId == documentId)
            .OrderByDescending(o => o.OcrRunNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<OcrResult>> GetByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.DocumentId == documentId)
            .OrderByDescending(o => o.OcrRunNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OcrResult>> GetPendingAsync(
        int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.Status == OcrStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
