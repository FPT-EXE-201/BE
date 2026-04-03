using System.Text.Json;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Xac thuc va giai ma JWS token tu Apple su dung JWKS public key.
/// Khong can API key hay Shared Secret — Apple publish public key tai appleid.apple.com/auth/keys.
/// </summary>
public class AppleAppStoreService : IAppleAppStoreService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppleAppStoreService> _logger;

    private const string JwksCacheKey = "apple_jwks";
    private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    public AppleAppStoreService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AppleAppStoreService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AppleTransactionInfo> VerifyAndDecodeTransactionAsync(string signedTransactionInfo)
    {
        var claims = await ValidateAndExtractClaimsAsync(signedTransactionInfo);

        var originalTransactionId = GetRequiredClaim(claims, "originalTransactionId");
        var transactionId = GetRequiredClaim(claims, "transactionId");
        var productId = GetRequiredClaim(claims, "productId");
        var bundleId = GetRequiredClaim(claims, "bundleId");

        // Verify bundleId khop voi app
        var expectedBundleId = _configuration["AppStore:BundleId"];
        if (!string.IsNullOrEmpty(expectedBundleId) && bundleId != expectedBundleId)
            throw new BadRequestException($"BundleId mismatch: expected {expectedBundleId}, got {bundleId}");

        var purchaseDateMs = ParseLongClaim(claims, "purchaseDate");
        var expiresDateMs = TryParseLongClaim(claims, "expiresDate");

        return new AppleTransactionInfo(
            OriginalTransactionId: originalTransactionId,
            TransactionId: transactionId,
            ProductId: productId,
            PurchaseDateMs: purchaseDateMs,
            ExpiresDateMs: expiresDateMs);
    }

    public AppleNotificationPayload? DecodeServerNotification(string signedPayload)
    {
        try
        {
            // Decode outer JWS (khong verify de lay payload structure truoc)
            var outerClaims = DecodeJwtWithoutValidation(signedPayload);
            if (outerClaims == null) return null;

            var notificationType = outerClaims.TryGetValue("notificationType", out var nt) ? nt?.ToString() ?? "" : "";
            var subtype = outerClaims.TryGetValue("subtype", out var st) ? st?.ToString() ?? "" : "";

            // Lay data.signedTransactionInfo tu payload
            if (!outerClaims.TryGetValue("data", out var dataObj)) return null;

            var dataJson = JsonSerializer.Serialize(dataObj);
            var data = JsonSerializer.Deserialize<JsonElement>(dataJson);

            if (!data.TryGetProperty("signedTransactionInfo", out var signedTxProp)) return null;
            var signedTransactionInfo = signedTxProp.GetString();
            if (string.IsNullOrEmpty(signedTransactionInfo)) return null;

            // Decode inner JWS chua thong tin transaction
            var txClaims = DecodeJwtWithoutValidation(signedTransactionInfo);
            if (txClaims == null) return null;

            var originalTransactionId = txClaims.TryGetValue("originalTransactionId", out var otx) ? otx?.ToString() ?? "" : "";
            var productId = txClaims.TryGetValue("productId", out var pid) ? pid?.ToString() ?? "" : "";
            var expiresDateMs = txClaims.TryGetValue("expiresDate", out var exp)
                ? (long.TryParse(exp?.ToString(), out var ed) ? (long?)ed : null)
                : null;

            return new AppleNotificationPayload(
                NotificationType: notificationType,
                Subtype: subtype,
                OriginalTransactionId: originalTransactionId,
                ProductId: productId,
                ExpiresDateMs: expiresDateMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode Apple server notification");
            return null;
        }
    }

    public SubscriptionPlan MapProductIdToPlan(string productId)
    {
        var monthly = _configuration["AppStore:ProductIds:Monthly"];
        var sixMonths = _configuration["AppStore:ProductIds:SixMonths"];
        var yearly = _configuration["AppStore:ProductIds:Yearly"];

        if (productId == monthly) return SubscriptionPlan.Monthly;
        if (productId == sixMonths) return SubscriptionPlan.SixMonths;
        if (productId == yearly) return SubscriptionPlan.Yearly;

        throw new BadRequestException($"Unknown Apple productId: {productId}");
    }

    // ── Private helpers ──

    private async Task<IDictionary<string, object>> ValidateAndExtractClaimsAsync(string jws)
    {
        var signingKeys = await GetAppleSigningKeysAsync();

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKeys = signingKeys,
        };

        tokenHandler.ValidateToken(jws, validationParams, out var validatedToken);
        var jwt = (JwtSecurityToken)validatedToken;

        return jwt.Claims.ToDictionary(c => c.Type, c => (object)c.Value);
    }

    private async Task<IEnumerable<SecurityKey>> GetAppleSigningKeysAsync()
    {
        if (_cache.TryGetValue(JwksCacheKey, out IEnumerable<SecurityKey>? cached) && cached != null)
            return cached;

        var response = await _httpClient.GetStringAsync(AppleJwksUrl);
        var keySet = new JsonWebKeySet(response);
        var keys = keySet.GetSigningKeys();

        _cache.Set(JwksCacheKey, keys, TimeSpan.FromHours(24));
        return keys;
    }

    private static Dictionary<string, object?>? DecodeJwtWithoutValidation(string jws)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jws)) return null;

            var token = handler.ReadJwtToken(jws);
            return token.Claims.GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => (object?)g.First().Value);
        }
        catch
        {
            return null;
        }
    }

    private static string GetRequiredClaim(IDictionary<string, object> claims, string key)
    {
        if (!claims.TryGetValue(key, out var value) || value == null)
            throw new BadRequestException($"Missing required Apple claim: {key}");
        return value.ToString()!;
    }

    private static long ParseLongClaim(IDictionary<string, object> claims, string key)
    {
        var value = GetRequiredClaim(claims, key);
        if (!long.TryParse(value, out var result))
            throw new BadRequestException($"Invalid long claim: {key} = {value}");
        return result;
    }

    private static long? TryParseLongClaim(IDictionary<string, object> claims, string key)
    {
        if (!claims.TryGetValue(key, out var value) || value == null) return null;
        return long.TryParse(value.ToString(), out var result) ? result : null;
    }
}
