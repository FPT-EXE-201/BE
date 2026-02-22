using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IRepositories;

public interface IWeightAlertRepository
{
    Task<List<WeightAlert>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default);

    Task<WeightAlert?> GetByIdAsync(
        Guid id, CancellationToken ct = default);

    Task AddAsync(WeightAlert alert, CancellationToken ct = default);

    void Update(WeightAlert alert);

    /// <summary>
    /// Check if an alert of the same type was created within the last <paramref name="days"/> days.
    /// Used for cooldown to prevent duplicate alerts.
    /// </summary>
    Task<bool> HasRecentAlertAsync(
        Guid pregnancyId, WeightAlertType alertType, int days = 7, CancellationToken ct = default);
}
