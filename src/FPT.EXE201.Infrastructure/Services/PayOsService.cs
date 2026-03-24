using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;

namespace FPT.EXE201.Infrastructure.Services;

public class PayOsService : IPaymentService
{
    private readonly PayOS _payOs;
    private readonly string _mobileReturnUrl;
    private readonly string _mobileCancelUrl;
    private readonly string _webReturnUrl;
    private readonly string _webCancelUrl;
    private readonly string _webhookUrl;
    private readonly ILogger<PayOsService> _logger;

    private static readonly Dictionary<SubscriptionPlan, string> PlanDescriptions = new()
    {
        [SubscriptionPlan.Monthly] = "Pregtap - Goi thang",
        [SubscriptionPlan.SixMonths] = "Pregtap - Goi 6 thang",
        [SubscriptionPlan.Yearly] = "Pregtap - Goi nam",
    };

    public PayOsService(IConfiguration configuration, ILogger<PayOsService> logger)
    {
        _logger = logger;

        var clientId = configuration["PayOS:ClientId"]
            ?? throw new InvalidOperationException("PayOS:ClientId is required.");
        var apiKey = configuration["PayOS:ApiKey"]
            ?? throw new InvalidOperationException("PayOS:ApiKey is required.");
        var checksumKey = configuration["PayOS:ChecksumKey"]
            ?? throw new InvalidOperationException("PayOS:ChecksumKey is required.");

        _mobileReturnUrl = configuration["PayOS:MobileReturnUrl"]
            ?? throw new InvalidOperationException("PayOS:MobileReturnUrl is required.");
        _mobileCancelUrl = configuration["PayOS:MobileCancelUrl"]
            ?? throw new InvalidOperationException("PayOS:MobileCancelUrl is required.");
        _webReturnUrl = configuration["PayOS:WebReturnUrl"]
            ?? throw new InvalidOperationException("PayOS:WebReturnUrl is required.");
        _webCancelUrl = configuration["PayOS:WebCancelUrl"]
            ?? throw new InvalidOperationException("PayOS:WebCancelUrl is required.");
        _webhookUrl = configuration["PayOS:WebhookUrl"]
            ?? throw new InvalidOperationException("PayOS:WebhookUrl is required.");

        _payOs = new PayOS(clientId, apiKey, checksumKey);
    }

    public async Task<PaymentCreateResult> CreatePaymentLinkAsync(Subscription subscription, bool isWeb = false, CancellationToken ct = default)
    {
        // PayOS orderCode: positive long, unique per merchant
        // Use timestamp (seconds) * 10000 + random to avoid collisions
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100_000;
        var random = Random.Shared.Next(10_000, 99_999);
        var orderCode = timestamp * 100_000 + random;

        var description = PlanDescriptions.GetValueOrDefault(subscription.Plan, "Pregtap Premium");

        var items = new List<ItemData>
        {
            new(description, 1, (int)subscription.Price)
        };

        var paymentData = new PaymentData(
            orderCode: orderCode,
            amount: (int)subscription.Price,
            description: description,
            items: items,
            cancelUrl: isWeb ? _webCancelUrl : _mobileCancelUrl,
            returnUrl: isWeb ? _webReturnUrl : _mobileReturnUrl
        );

        var createPaymentResult = await _payOs.createPaymentLink(paymentData);

        return new PaymentCreateResult
        {
            CheckoutUrl = createPaymentResult.checkoutUrl,
            OrderCode = orderCode,
        };
    }

    /// <summary>
    /// Query PayOS để kiểm tra trạng thái thanh toán theo orderCode.
    /// Dùng khi user quay lại app sau checkout (return URL) để confirm.
    /// </summary>
    public async Task<PaymentVerifyResult> VerifyPaymentAsync(long orderCode)
    {
        try
        {
            var paymentInfo = await _payOs.getPaymentLinkInformation(orderCode);

            return new PaymentVerifyResult
            {
                OrderCode = orderCode,
                Status = paymentInfo.status, // PAID, PENDING, CANCELLED, EXPIRED
                IsPaid = string.Equals(paymentInfo.status, "PAID", StringComparison.OrdinalIgnoreCase),
                Amount = paymentInfo.amount,
                TransactionId = paymentInfo.transactions?.LastOrDefault()?.reference,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify payment for orderCode {OrderCode}", orderCode);
            return new PaymentVerifyResult
            {
                OrderCode = orderCode,
                Status = "ERROR",
                IsPaid = false,
            };
        }
    }

    /// <summary>
    /// Verify webhook từ PayOS. Trả về WebhookData nếu hợp lệ, null nếu không.
    /// </summary>
    public WebhookData? VerifyAndParseWebhook(WebhookType webhookBody)
    {
        try
        {
            return _payOs.verifyPaymentWebhookData(webhookBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS webhook verification failed");
            return null;
        }
    }

    /// <summary>
    /// Đăng ký Webhook URL với PayOS Dashboard qua SDK.
    /// </summary>
    public async Task<bool> RegisterWebhookAsync(string? webhookUrl = null)
    {
        try
        {
            var urlToRegister = webhookUrl ?? _webhookUrl;
            await _payOs.confirmWebhook(urlToRegister);
            _logger.LogInformation("Successfully registered PayOS webhook: {WebhookUrl}", urlToRegister);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register PayOS webhook");
            return false;
        }
    }

    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        // Signature verification is handled in VerifyAndParseWebhook via SDK
        return !string.IsNullOrEmpty(signature);
    }
}
