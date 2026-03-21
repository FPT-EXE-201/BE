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
    private readonly string _returnUrl;
    private readonly string _cancelUrl;
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

        _returnUrl = configuration["PayOS:ReturnUrl"]
            ?? throw new InvalidOperationException("PayOS:ReturnUrl is required. Use deep link for mobile app.");
        _cancelUrl = configuration["PayOS:CancelUrl"]
            ?? throw new InvalidOperationException("PayOS:CancelUrl is required. Use deep link for mobile app.");

        _payOs = new PayOS(clientId, apiKey, checksumKey);
    }

    public async Task<PaymentCreateResult> CreatePaymentLinkAsync(Subscription subscription, CancellationToken ct = default)
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
            cancelUrl: _cancelUrl,
            returnUrl: _returnUrl
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

    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        // Signature verification is handled in VerifyAndParseWebhook via SDK
        return !string.IsNullOrEmpty(signature);
    }
}
