using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class AiPromptTemplateRepository : GenericRepository<AiPromptTemplate>, IAiPromptTemplateRepository
{
    public AiPromptTemplateRepository(AppDbContext context) : base(context) { }

    public async Task<AiPromptTemplate?> GetActiveByKeyAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TemplateKey == templateKey && t.IsActive && t.DeletedAt == null)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
