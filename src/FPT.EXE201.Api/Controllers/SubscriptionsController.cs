using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Subscriptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Infrastructure.Services;
using Net.payOS.Types;

namespace FPT.EXE201.Api.Controllers;

[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly PayOsService _payOsService;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        PayOsService payOsService)
    {
        _subscriptionService = subscriptionService;
        _payOsService = payOsService;
    }

    /// <summary>
    /// GET /api/subscriptions/plans — Lấy danh sách gói Premium (public).
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans()
    {
        var plans = _subscriptionService.GetPlans();
        return Success(plans);
    }

    /// <summary>
    /// POST /api/subscriptions/purchase — Mua gói Premium, trả về checkout URL.
    /// </summary>
    [HttpPost("purchase")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseSubscriptionDto dto, CancellationToken ct)
    {
        var result = await _subscriptionService.PurchaseAsync(GetCurrentUserId(), dto, ct);
        return Created(result, "Payment link created. Redirect user to CheckoutUrl.");
    }

    /// <summary>
    /// POST /api/subscriptions/webhook — PayOS webhook callback (no auth).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOsWebhook([FromBody] WebhookType webhookBody, CancellationToken ct)
    {
        // Verify signature via PayOS SDK
        var webhookData = _payOsService.VerifyAndParseWebhook(webhookBody);
        if (webhookData == null)
            return Ok(new { success = false }); // Invalid signature, return 200 to stop retry

        var isSuccess = webhookBody.success;
        var orderCode = webhookData.orderCode;
        var transactionId = webhookData.reference;

        await _subscriptionService.HandlePaymentWebhookAsync(orderCode, transactionId, isSuccess, ct);

        return Ok(new { success = true });
    }

    /// <summary>
    /// GET /api/subscriptions/status — Trạng thái subscription hiện tại.
    /// </summary>
    [HttpGet("status")]
    [RequirePermission("subscription.read")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _subscriptionService.GetStatusAsync(GetCurrentUserId(), ct);
        return Success(status);
    }

    /// <summary>
    /// GET /api/subscriptions/history — Lịch sử subscription.
    /// </summary>
    [HttpGet("history")]
    [RequirePermission("subscription.read")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var history = await _subscriptionService.GetHistoryAsync(GetCurrentUserId(), ct);
        return Success(history);
    }

    /// <summary>
    /// DELETE /api/subscriptions/cancel — Hủy subscription đang active.
    /// </summary>
    [HttpDelete("cancel")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        await _subscriptionService.CancelAsync(GetCurrentUserId(), ct);
        return Success<object?>(null, "Subscription cancelled successfully");
    }

    /// <summary>
    /// GET /api/subscriptions/verify?orderCode=xxx — Verify payment sau khi user quay về app từ PayOS.
    /// Mobile app gọi endpoint này khi nhận được return URL.
    /// </summary>
    [HttpGet("verify")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> VerifyPayment([FromQuery] long orderCode, CancellationToken ct)
    {
        var status = await _subscriptionService.VerifyAndActivateAsync(GetCurrentUserId(), orderCode, ct);
        return Success(status);
    }
}
