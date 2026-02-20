using Microsoft.EntityFrameworkCore;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MedicalDocumentRepository : GenericRepository<MedicalDocument>, IMedicalDocumentRepository
{
    public MedicalDocumentRepository(AppDbContext context) : base(context) { }

    public async Task<List<MedicalDocument>> GetByPregnancyIdWithDetailsAsync(
        Guid pregnancyId, bool? isFavorite = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(m => m.PregnancyId == pregnancyId);

        if (isFavorite.HasValue)
            query = query.Where(m => m.IsFavorite == isFavorite.Value);

        return await query
            .Include(m => m.Files.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.StorageFile)
            .Include(m => m.DocumentType)
                .ThenInclude(dt => dt!.Translations)
            .OrderByDescending(m => m.CapturedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalDocument?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Files.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.StorageFile)
            .Include(m => m.DocumentType)
                .ThenInclude(dt => dt!.Translations)
            .Include(m => m.OcrResults.OrderByDescending(o => o.OcrRunNumber).Take(1))
            .Include(m => m.Pregnancy)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}
