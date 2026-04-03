using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Subscriptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;

namespace FPT.EXE201.Api.Controllers;

[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly PayOsService _payOsService;
    private readonly IConfiguration _configuration;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        PayOsService payOsService,
        IConfiguration configuration)
    {
        _subscriptionService = subscriptionService;
        _payOsService = payOsService;
        _configuration = configuration;
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
        // Tự động nhận diện Web hay Mobile dựa trên Origin/User-Agent
        var origin = Request.Headers["Origin"].ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        // 1. Nếu có Origin và không phải từ các scheme mobile (capacitor, ionic) -> Web
        // 2. Nếu User-Agent chứa các trình duyệt phổ biến và không chứa mobile app keywords
        bool isWeb = !string.IsNullOrEmpty(origin) && !origin.StartsWith("capacitor://") && !origin.StartsWith("ionic://");
        
        if (string.IsNullOrEmpty(origin) && (userAgent.Contains("Mozilla") || userAgent.Contains("Chrome") || userAgent.Contains("Safari")))
        {
            isWeb = true;
        }

        var result = await _subscriptionService.PurchaseAsync(GetCurrentUserId(), dto, isWeb, ct);
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

    /// <summary>
    /// POST /api/subscriptions/setup-webhook — Đăng ký Webhook URL với PayOS Dashboard.
    /// Quyền: admin.all hoặc subscription.purchase (tùy cấu hình, ở đây dùng subscription.purchase để dev test).
    /// </summary>
    [HttpPost("setup-webhook")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> SetupWebhook(CancellationToken ct)
    {
        var success = await _subscriptionService.RegisterWebhookAsync(ct);
        if (!success) return BadRequest("Failed to register webhook with PayOS.");
        return Success<object?>(null, "Webhook registered successfully with PayOS.");
    }

    // ── Apple IAP ──

    /// <summary>
    /// POST /api/subscriptions/apple/verify — Verify signedTransactionInfo từ StoreKit 2.
    /// Flutter app gọi ngay sau khi purchase thành công trên thiết bị iOS.
    /// </summary>
    [HttpPost("apple/verify")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> AppleVerify([FromBody] AppleIapVerifyDto dto, CancellationToken ct)
    {
        var status = await _subscriptionService.VerifyAppleIapAsync(GetCurrentUserId(), dto, ct);
        return Success(status, "Apple IAP verified. Subscription activated.");
    }

    /// <summary>
    /// POST /api/subscriptions/apple/notifications — Apple App Store Server Notifications V2.
    /// Apple gọi endpoint này khi subscription gia hạn, hết hạn, hoàn tiền...
    /// </summary>
    [HttpPost("apple/notifications")]
    [AllowAnonymous]
    public async Task<IActionResult> AppleNotifications([FromBody] AppleNotificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request?.SignedPayload))
            return Ok();

        var appleService = HttpContext.RequestServices.GetRequiredService<IAppleAppStoreService>();
        var payload = appleService.DecodeServerNotification(request.SignedPayload);

        if (payload == null)
            return Ok();

        await _subscriptionService.HandleAppleNotificationAsync(
            payload.NotificationType,
            payload.Subtype,
            payload.OriginalTransactionId,
            payload.ProductId,
            payload.ExpiresDateMs,
            ct);

        return Ok();
    }

    /// <summary>
    /// POST /api/subscriptions/apple/test-activate — [SANDBOX ONLY] Kích hoạt Apple subscription trực tiếp không cần JWS.
    /// Dùng để test backend mà không cần iOS device. Bị chặn khi AppStore:Environment != Sandbox.
    /// </summary>
    [HttpPost("apple/test-activate")]
    [RequirePermission("subscription.purchase")]
    public async Task<IActionResult> AppleTestActivate([FromBody] AppleSandboxActivateRequest request, CancellationToken ct)
    {
        var env = _configuration["AppStore:Environment"];
        if (!string.Equals(env, "Sandbox", StringComparison.OrdinalIgnoreCase))
            return BadRequest("This endpoint is only available in Sandbox environment.");

        var status = await _subscriptionService.ActivateAppleIapSandboxAsync(
            GetCurrentUserId(),
            request.Plan,
            request.FakeTransactionId ?? string.Empty,
            ct);

        return Success(status, "[SANDBOX] Apple IAP activated for testing.");
    }
}

/// <summary>Body của Apple App Store Server Notification V2.</summary>
public record AppleNotificationRequest(string SignedPayload);

/// <summary>Body cho test endpoint sandbox.</summary>
public record AppleSandboxActivateRequest(
    /// <summary>Monthly | SixMonths | Yearly</summary>
    string Plan,
    /// <summary>Tùy chọn — nếu để trống sẽ tự sinh SANDBOX_xxx</summary>
    string? FakeTransactionId = null);
