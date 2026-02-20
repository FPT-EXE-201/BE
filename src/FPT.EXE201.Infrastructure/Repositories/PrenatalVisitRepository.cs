using System.Linq.Expressions;
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.PrenatalVisits;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PrenatalVisitRepository : GenericRepository<PrenatalVisit>, IPrenatalVisitRepository
{
    public PrenatalVisitRepository(AppDbContext context) : base(context) { }

    public async Task<List<PrenatalVisit>> GetByPregnancyIdAsync(Guid pregnancyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.PregnancyId == pregnancyId && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
            .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                .ThenInclude(d => d.DocumentType)
            .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                .ThenInclude(d => d.Files.OrderBy(f => f.SortOrder))
                    .ThenInclude(f => f.StorageFile)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<PrenatalVisit>> GetByPregnancyIdPagedAsync(Guid pregnancyId, QueryOptions options, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(
            options,
            predicate: v => v.PregnancyId == pregnancyId,
            include: q => q
                .Include(v => v.Tests.Where(t => t.DeletedAt == null))
                .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                    .ThenInclude(d => d.DocumentType)
                .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                    .ThenInclude(d => d.Files.OrderBy(f => f.SortOrder))
                        .ThenInclude(f => f.StorageFile),
            searchBuilder: SearchHelper.CreateSearchBuilder(
                PrenatalVisitListQuerySpec.SearchMap,
                PrenatalVisitListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: PrenatalVisitListQuerySpec.SortMap,
            defaultSort: PrenatalVisitListQuerySpec.DefaultSort,
            cancellationToken: cancellationToken);
    }

    public async Task<PrenatalVisit?> GetByIdWithTestsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Id == id && v.DeletedAt == null)
            .Include(v => v.Tests.Where(t => t.DeletedAt == null))
                .ThenInclude(t => t.TestType)
                    .ThenInclude(tt => tt.Translations)
            .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                .ThenInclude(d => d.DocumentType)
            .Include(v => v.Documents.Where(d => d.DeletedAt == null))
                .ThenInclude(d => d.Files.OrderBy(f => f.SortOrder))
                    .ThenInclude(f => f.StorageFile)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
