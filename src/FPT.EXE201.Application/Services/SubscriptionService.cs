using FPT.EXE201.Application.DTOs.Subscriptions;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRoleService _userRoleService;
    private readonly IPaymentService _paymentService;

    // Pricing configuration (VND)
    private static readonly Dictionary<SubscriptionPlan, (decimal Price, int Months, string Name)> PlanConfig = new()
    {
        [SubscriptionPlan.Monthly] = (39_000m, 1, "Gói tháng"),
        [SubscriptionPlan.SixMonths] = (199_000m, 6, "Gói 6 tháng"),
        [SubscriptionPlan.Yearly] = (399_000m, 12, "Gói năm"),
    };

    public SubscriptionService(
        IUnitOfWork unitOfWork,
        IUserRoleService userRoleService,
        IPaymentService paymentService)
    {
        _unitOfWork = unitOfWork;
        _userRoleService = userRoleService;
        _paymentService = paymentService;
    }

    public async Task<PurchaseResultDto> PurchaseAsync(Guid userId, PurchaseSubscriptionDto dto, bool isWeb = false, CancellationToken ct = default)
    {
        // 1. Parse plan
        if (!Enum.TryParse<SubscriptionPlan>(dto.Plan, true, out var plan) || !PlanConfig.ContainsKey(plan))
            throw new BadRequestException($"Invalid plan: {dto.Plan}. Valid plans: Monthly, SixMonths, Yearly");

        // 2. Check user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: ct);
        if (user == null)
            throw new NotFoundException("User not found");

        // 3. Check no active subscription
        var activeSub = await _unitOfWork.Subscriptions.GetActiveByUserIdAsync(userId, ct);
        if (activeSub != null)
            throw new BadRequestException("You already have an active subscription");

        // 4. Cancel any pending subscription
        var pendingSub = await _unitOfWork.Subscriptions.GetPendingByUserIdAsync(userId, ct);
        if (pendingSub != null)
        {
            pendingSub.Status = SubscriptionStatus.Cancelled;
            _unitOfWork.Subscriptions.Update(pendingSub);
        }

        // 5. Create subscription record (Pending)
        var config = PlanConfig[plan];
        var subscription = new Subscription
        {
            UserId = userId,
            Plan = plan,
            Price = config.Price,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(config.Months),
            Status = SubscriptionStatus.Pending,
        };

        // 6. Create PayOS payment link first
        var paymentResult = await _paymentService.CreatePaymentLinkAsync(subscription, isWeb, ct);
        subscription.OrderCode = paymentResult.OrderCode;

        // 7. Save to DB (Single save)
        await _unitOfWork.Subscriptions.AddAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PurchaseResultDto
        {
            SubscriptionId = subscription.Id,
            OrderCode = paymentResult.OrderCode,
            CheckoutUrl = paymentResult.CheckoutUrl,
        };
    }

    public async Task HandlePaymentWebhookAsync(long orderCode, string? transactionId, bool isSuccess, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByOrderCodeAsync(orderCode, ct);
        if (subscription == null) return; // Unknown order, ignore

        if (subscription.Status != SubscriptionStatus.Pending) return; // Already processed

        if (isSuccess)
        {
            // Activate subscription
            subscription.Status = SubscriptionStatus.Active;
            subscription.PaymentTransactionId = transactionId;
            subscription.StartDate = DateTime.UtcNow;

            var config = PlanConfig[subscription.Plan];
            subscription.EndDate = DateTime.UtcNow.AddMonths(config.Months);

            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync(ct);

            // Assign PREMIUM role
            await AssignPremiumRoleAsync(subscription.UserId, ct);
        }
        else
        {
            // Payment failed
            subscription.Status = SubscriptionStatus.Cancelled;
            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<SubscriptionStatusDto> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var activeSub = await _unitOfWork.Subscriptions.GetActiveByUserIdAsync(userId, ct);

        if (activeSub == null)
        {
            return new SubscriptionStatusDto { IsPremium = false };
        }

        var daysRemaining = (int)Math.Ceiling((activeSub.EndDate - DateTime.UtcNow).TotalDays);
        return new SubscriptionStatusDto
        {
            IsPremium = true,
            Plan = activeSub.Plan.ToString(),
            Price = activeSub.Price,
            StartDate = activeSub.StartDate,
            EndDate = activeSub.EndDate,
            DaysRemaining = Math.Max(0, daysRemaining),
            IsExpiringSoon = daysRemaining <= 7,
        };
    }

    public async Task<List<SubscriptionHistoryDto>> GetHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        var subscriptions = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);

        return subscriptions.Select(s => new SubscriptionHistoryDto
        {
            Id = s.Id,
            Plan = s.Plan.ToString(),
            Price = s.Price,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status.ToString(),
            OrderCode = s.OrderCode,
            CreatedAt = s.CreatedAt,
        }).ToList();
    }

    public async Task CancelAsync(Guid userId, CancellationToken ct = default)
    {
        var activeSub = await _unitOfWork.Subscriptions.GetActiveByUserIdAsync(userId, ct);
        if (activeSub == null)
            throw new NotFoundException("No active subscription found");

        activeSub.Status = SubscriptionStatus.Cancelled;
        _unitOfWork.Subscriptions.Update(activeSub);
        await _unitOfWork.SaveChangesAsync(ct);

        // Remove PREMIUM role
        await RemovePremiumRoleAsync(userId, ct);
    }

    public List<SubscriptionPlanDto> GetPlans()
    {
        return PlanConfig.Select(kv => new SubscriptionPlanDto
        {
            Plan = kv.Key.ToString(),
            Name = kv.Value.Name,
            Price = kv.Value.Price,
            DurationMonths = kv.Value.Months,
            PricePerMonth = Math.Round(kv.Value.Price / kv.Value.Months, 0),
            SavePercent = kv.Key == SubscriptionPlan.Monthly ? null
                : (int)Math.Round((1 - kv.Value.Price / kv.Value.Months / PlanConfig[SubscriptionPlan.Monthly].Price) * 100),
        }).ToList();
    }

    public async Task<bool> RegisterWebhookAsync(CancellationToken ct = default)
    {
        return await _paymentService.RegisterWebhookAsync();
    }

    public async Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        var expiredSubs = await _unitOfWork.Subscriptions.GetExpiredActiveSubscriptionsAsync(ct);

        foreach (var sub in expiredSubs)
        {
            sub.Status = SubscriptionStatus.Expired;
            _unitOfWork.Subscriptions.Update(sub);
            await _unitOfWork.SaveChangesAsync(ct);

            await RemovePremiumRoleAsync(sub.UserId, ct);
        }
    }

    public async Task<SubscriptionStatusDto> VerifyAndActivateAsync(Guid userId, long orderCode, CancellationToken ct = default)
    {
        // 1. Find the subscription by orderCode
        var subscription = await _unitOfWork.Subscriptions.GetByOrderCodeAsync(orderCode, ct);
        if (subscription == null || subscription.UserId != userId)
            throw new NotFoundException("Subscription not found for this order");

        // 2. If already processed, return current status
        if (subscription.Status != SubscriptionStatus.Pending)
            return await GetStatusAsync(userId, ct);

        // 3. Query PayOS for payment status
        var paymentResult = await _paymentService.VerifyPaymentAsync(orderCode);

        if (paymentResult.IsPaid)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.PaymentTransactionId = paymentResult.TransactionId;
            subscription.StartDate = DateTime.UtcNow;

            var config = PlanConfig[subscription.Plan];
            subscription.EndDate = DateTime.UtcNow.AddMonths(config.Months);

            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync(ct);

            await AssignPremiumRoleAsync(subscription.UserId, ct);
        }

        return await GetStatusAsync(userId, ct);
    }

    // ── Private helpers ──

    private async Task AssignPremiumRoleAsync(Guid userId, CancellationToken ct)
    {
        var premiumRole = await _unitOfWork.Roles.GetByCodeAsync("PREMIUM", ct: ct);
        if (premiumRole == null) return;

        var hasRole = await _userRoleService.HasRoleAsync(userId, "PREMIUM", ct);
        if (!hasRole)
        {
            await _userRoleService.AssignRolesToUserAsync(userId, new List<Guid> { premiumRole.Id }, ct);
        }
    }

    private async Task RemovePremiumRoleAsync(Guid userId, CancellationToken ct)
    {
        var premiumRole = await _unitOfWork.Roles.GetByCodeAsync("PREMIUM", ct: ct);
        if (premiumRole == null) return;

        var hasRole = await _userRoleService.HasRoleAsync(userId, "PREMIUM", ct);
        if (hasRole)
        {
            await _userRoleService.RemoveRoleFromUserAsync(userId, premiumRole.Id, ct);
        }
    }
}
