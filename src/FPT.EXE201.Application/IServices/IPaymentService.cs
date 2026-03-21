using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Interface cho payment gateway (PayOS).
/// </summary>
public interface IPaymentService
{
    /// <summary>Tạo payment link qua PayOS.</summary>
    Task<PaymentCreateResult> CreatePaymentLinkAsync(Subscription subscription, CancellationToken ct = default);

    /// <summary>Verify webhook signature từ PayOS.</summary>
    bool VerifyWebhookSignature(string rawBody, string signature);

    /// <summary>Query PayOS để verify trạng thái thanh toán (dùng cho return URL flow).</summary>
    Task<PaymentVerifyResult> VerifyPaymentAsync(long orderCode);
}

public class PaymentCreateResult
{
    public string CheckoutUrl { get; set; } = null!;
    public long OrderCode { get; set; }
}

public class PaymentVerifyResult
{
    public long OrderCode { get; set; }
    public string Status { get; set; } = null!;
    public bool IsPaid { get; set; }
    public int Amount { get; set; }
    public string? TransactionId { get; set; }
}
