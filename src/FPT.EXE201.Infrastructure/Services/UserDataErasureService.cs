using FPT.EXE201.Application.IServices;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Permanently erases all personal / clinical data that belongs to a user,
/// called as part of the account-deletion flow (GDPR / App Store right-to-erasure).
///
/// Deletion order follows FK dependency chains (children before parents) to
/// avoid FK-constraint violations.  EF bulk ExecuteDeleteAsync does NOT follow
/// cascade paths automatically.
///
/// The calling service (AuthService.DeleteAccountAsync) already owns the
/// database transaction, so this service simply issues DELETE statements within it.
/// </summary>
public sealed class UserDataErasureService : IUserDataErasureService
{
    private readonly AppDbContext _db;

    public UserDataErasureService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EraseUserPersonalDataAsync(Guid userId, CancellationToken ct = default)
    {
        // ── 1. Chat messages ───────────────────────────────────────────────────
        await _db.ChatMessages
            .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId)
            .ExecuteDeleteAsync(ct);

        // ── 2. AI request logs ─────────────────────────────────────────────────
        var pregnancyIds = _db.Pregnancies
            .Where(p => p.UserId == userId)
            .Select(p => p.Id);

        await _db.AiRequestLogs
            .Where(l => l.UserId == userId || (l.PregnancyId != null && pregnancyIds.Contains(l.PregnancyId!.Value)))
            .ExecuteDeleteAsync(ct);

        // ── 3. Meal planning — leaf to root ────────────────────────────────────
        // MealItemFeedback has a direct UserId
        await _db.MealItemFeedbacks
            .Where(f => f.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // MealPlanFeedback has a direct UserId
        await _db.MealPlanFeedbacks
            .Where(f => f.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // MealItems: MealDay → MealPlan → Pregnancy
        var mealPlanIds = _db.MealPlans
            .Where(mp => pregnancyIds.Contains(mp.PregnancyId))
            .Select(mp => mp.Id);

        var mealDayIds = _db.MealPlanDays
            .Where(d => mealPlanIds.Contains(d.MealPlanId))
            .Select(d => d.Id);

        // MealItemNutrients cascade via DB FK when MealItem is deleted
        await _db.MealItems
            .Where(i => mealDayIds.Contains(i.MealDayId))
            .ExecuteDeleteAsync(ct);

        await _db.MealPlanDays
            .Where(d => mealPlanIds.Contains(d.MealPlanId))
            .ExecuteDeleteAsync(ct);

        await _db.MealPlans
            .Where(mp => pregnancyIds.Contains(mp.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 4. Recipes ─────────────────────────────────────────────────────────
        await _db.Recipes
            .Where(r => pregnancyIds.Contains(r.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 5. Nutrition notes & food preferences ──────────────────────────────
        await _db.PregnancyNutritionNotes
            .Where(n => pregnancyIds.Contains(n.PregnancyId))
            .ExecuteDeleteAsync(ct);

        await _db.PregnancyFoodPreferences
            .Where(fp => pregnancyIds.Contains(fp.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 6. Medical documents: OCR results → document files → documents ────
        var medDocIds = _db.MedicalDocuments
            .Where(md => pregnancyIds.Contains(md.PregnancyId))
            .Select(md => md.Id);

        await _db.OcrResults
            .Where(o => medDocIds.Contains(o.DocumentId))
            .ExecuteDeleteAsync(ct);

        await _db.DocumentFiles
            .Where(df => medDocIds.Contains(df.DocumentId))
            .ExecuteDeleteAsync(ct);

        await _db.MedicalDocuments
            .Where(md => pregnancyIds.Contains(md.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 7. Storage files owned by user ─────────────────────────────────────
        await _db.StorageFiles
            .Where(sf => sf.OwnerUserId == userId)
            .ExecuteDeleteAsync(ct);

        // ── 8. Weight alerts & logs (via PregnancyId) ──────────────────────────
        await _db.WeightAlerts
            .Where(a => pregnancyIds.Contains(a.PregnancyId))
            .ExecuteDeleteAsync(ct);

        await _db.WeightLogs
            .Where(l => pregnancyIds.Contains(l.PregnancyId))
            .ExecuteDeleteAsync(ct);

        await _db.WeightGoalRanges
            .Where(g => pregnancyIds.Contains(g.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 9. Prenatal tests & visits ─────────────────────────────────────────
        await _db.PrenatalTests
            .Where(t => pregnancyIds.Contains(t.PregnancyId))
            .ExecuteDeleteAsync(ct);

        await _db.PrenatalVisits
            .Where(v => pregnancyIds.Contains(v.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 10. Pregnancy conditions ───────────────────────────────────────────
        await _db.PregnancyConditions
            .Where(c => pregnancyIds.Contains(c.PregnancyId))
            .ExecuteDeleteAsync(ct);

        // ── 11. Pregnancies ────────────────────────────────────────────────────
        await _db.Pregnancies
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // ── 12. Subscriptions ──────────────────────────────────────────────────
        await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // ── 13. Refresh tokens (hard-delete for complete erasure) ──────────────
        // AuthService also calls RevokeAllUserTokensAsync (soft-revoke);
        // here we hard-delete to erase all session history.
        await _db.AuthRefreshTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // NOTE: UserProfile (FullName/AvatarUrl/DOB) is nulled + soft-deleted by
        // AuthService directly to keep the FK row intact.
        // UserRoles are removed by AuthService before this method is called.
        // The User row itself is scrubbed (email/phone → null, Status = Deleted)
        // but NOT hard-deleted so audit/financial records remain traceable.
    }
}
