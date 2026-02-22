using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

/// <summary>
/// WeightAlert KHÔNG kế thừa BaseEntity → KHÔNG dùng GenericRepository.
/// </summary>
public class WeightAlertRepository : IWeightAlertRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<WeightAlert> _dbSet;

    public WeightAlertRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<WeightAlert>();
    }

    public async Task<List<WeightAlert>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(a => a.PregnancyId == pregnancyId)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(ct);
    }

    public async Task<WeightAlert?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(WeightAlert alert, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(alert, ct);
    }

    public void Update(WeightAlert alert)
    {
        _dbSet.Update(alert);
    }

    public async Task<bool> HasRecentAlertAsync(
        Guid pregnancyId, WeightAlertType alertType, int days = 7, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _dbSet.AnyAsync(
            a => a.PregnancyId == pregnancyId
                 && a.AlertType == alertType
                 && a.TriggeredAt >= cutoff,
            ct);
    }
}
