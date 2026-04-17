using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Interface xac thuc va giai ma cac JWS token tu Apple (StoreKit 2 transaction va Server Notification V2).
/// Khong yeu cau API key — su dung Apple JWKS public key de verify chu ky.
/// </summary>
public interface IAppleAppStoreService
{
    /// <summary>
    /// Verify va giai ma signedTransactionInfo (JWS) tu StoreKit 2.
    /// Nem exception neu JWS khong hop le hoac bundleId khong khop.
    /// </summary>
    Task<AppleTransactionInfo> VerifyAndDecodeTransactionAsync(string signedTransactionInfo);

    /// <summary>
    /// Giai ma App Store Server Notification V2 signedPayload (JWS).
    /// Tra ve null neu payload khong hop le.
    /// </summary>
    AppleNotificationPayload? DecodeServerNotification(string signedPayload);

    /// <summary>
    /// Map Apple productId sang SubscriptionPlan dua tren AppStore:ProductIds config.
    /// Nem exception neu productId khong khop.
    /// </summary>
    SubscriptionPlan MapProductIdToPlan(string productId);

    /// <summary>
    /// Lấy Apple productId tu SubscriptionPlan dua tren AppStore:ProductIds config.
    /// </summary>
    string GetProductIdByPlan(SubscriptionPlan plan);
}

/// <summary>Thong tin transaction da duoc Apple verify.</summary>
public record AppleTransactionInfo(
    string OriginalTransactionId,
    string TransactionId,
    string ProductId,
    long PurchaseDateMs,
    long? ExpiresDateMs);

/// <summary>Thong tin tu App Store Server Notification V2.</summary>
public record AppleNotificationPayload(
    string NotificationType,
    string Subtype,
    string OriginalTransactionId,
    string ProductId,
    long? ExpiresDateMs);
