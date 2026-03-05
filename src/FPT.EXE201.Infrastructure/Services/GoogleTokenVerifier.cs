using System.Text;
using System.Text.Json;
using FPT.EXE201.Application.IServices;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Verify Google ID Token using the official Google.Apis.Auth library.
/// Validates JWT signature locally with Google's public keys — no tokeninfo HTTP call.
/// Supports multiple Client IDs (web + mobile).
/// </summary>
public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly ILogger<GoogleTokenVerifier> _logger;
    private readonly IReadOnlyList<string> _allowedClientIds;

    public GoogleTokenVerifier(
        IConfiguration configuration,
        ILogger<GoogleTokenVerifier> logger)
    {
        _logger = logger;

        // Support multiple client IDs (e.g. web + android + iOS)
        // Config: "Google:ClientId" (single) hoặc "Google:ClientIds" (array)
        var single = configuration["Google:ClientId"];
        var multiple = configuration.GetSection("Google:ClientIds").Get<string[]>();

        var ids = new List<string>();
        if (!string.IsNullOrWhiteSpace(single)) ids.Add(single);
        if (multiple?.Length > 0) ids.AddRange(multiple);

        _allowedClientIds = ids.AsReadOnly();
    }

    public async Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            // First log the RAW token length and start/end (safe for PII)
            _logger.LogInformation("[Google][Debug] RAW Token Length: {Length}, StartsWith: {Start}, EndsWith: {End}", 
                idToken?.Length, 
                idToken?.Length >= 10 ? idToken[..10] : idToken,
                idToken?.Length >= 10 ? idToken[^10..] : idToken);

            // Clean the token (remove spaces, quotes, or Bearer prefix)
            idToken = idToken?.Trim()?.Trim('"')?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase) ?? string.Empty;

            _logger.LogInformation("[Google][Debug] CLEANED Token Length: {Length}, StartsWith: {Start}, EndsWith: {End}", 
                idToken.Length, 
                idToken.Length >= 10 ? idToken[..10] : idToken,
                idToken.Length >= 10 ? idToken[^10..] : idToken);

            var parts = idToken.Split('.');
            _logger.LogInformation("[Google][Debug] Token parts count: {Count}", parts.Length);
            
            if (parts.Length == 3)
            {
                _logger.LogInformation("[Google][Debug] Header length: {L1}, Payload length: {L2}, Signature length: {L3}", 
                    parts[0].Length, parts[1].Length, parts[2].Length);

                // Decode header + payload to log debug info
                try
                {
                    // Decode header for 'alg'
                    var headerBase64 = parts[0].Replace('-', '+').Replace('_', '/');
                    switch (headerBase64.Length % 4)
                    {
                        case 2: headerBase64 += "=="; break;
                        case 3: headerBase64 += "="; break;
                    }
                    var headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(headerBase64));
                    using var headerDoc = JsonDocument.Parse(headerJson);
                    var alg = headerDoc.RootElement.TryGetProperty("alg", out var algEl) ? algEl.GetString() : "(missing)";
                    var kid = headerDoc.RootElement.TryGetProperty("kid", out var kidEl) ? kidEl.GetString() : "(missing)";

                    // Decode payload for aud, iss, exp, iat
                    var payloadBase64 = parts[1].Replace('-', '+').Replace('_', '/');
                    switch (payloadBase64.Length % 4)
                    {
                        case 2: payloadBase64 += "=="; break;
                        case 3: payloadBase64 += "="; break;
                    }
                    var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));
                    using var doc = JsonDocument.Parse(payloadJson);
                    var aud = doc.RootElement.TryGetProperty("aud", out var audEl) ? audEl.GetString() : "(missing)";
                    var iss = doc.RootElement.TryGetProperty("iss", out var issEl) ? issEl.GetString() : "(missing)";
                    var exp = doc.RootElement.TryGetProperty("exp", out var expEl) ? expEl.GetInt64().ToString() : "(missing)";
                    var iat = doc.RootElement.TryGetProperty("iat", out var iatEl) ? iatEl.GetInt64().ToString() : "(missing)";

                    var expTime = exp != "(missing)" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime.ToString("o") : "?";
                    var iatTime = iat != "(missing)" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(iat)).UtcDateTime.ToString("o") : "?";

                    _logger.LogInformation(
                        "[Google][Debug] alg={Alg}, kid={Kid}, aud={Aud}, iss={Iss}, iat={Iat} ({IatTime}), exp={Exp} ({ExpTime}), sigLen={SigLen} (RS256 expects ~342)",
                        alg, kid, aud, iss, iat, iatTime, exp, expTime, parts[2].Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[Google][Debug] Could not decode token for debug: {Msg}", ex.Message);
                }
            }

            GoogleJsonWebSignature.Payload? payload = null;

            if (_allowedClientIds.Count == 0)
            {
                // Không giới hạn audience — chỉ verify chữ ký
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            }
            else
            {
                // Thử từng ClientId cho đến khi validate thành công
                foreach (var clientId in _allowedClientIds)
                {
                    try
                    {
                        var settings = new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { clientId }
                        };
                        payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                        _logger.LogInformation("[Google] Token validated successfully with ClientId={ClientId}", clientId);
                        break;
                    }
                    catch (InvalidJwtException ex)
                    {
                        _logger.LogWarning("[Google][Debug] ValidateAsync failed for ClientId {ClientId}. Reason: {Reason}", clientId, ex.Message);
                        // Thử ClientId tiếp theo
                    }
                }
            }

            if (payload == null)
            {
                _logger.LogWarning("[Google] Token validation failed for all configured ClientIds");
                return null;
            }

            _logger.LogInformation("[Google] Parsed: sub={Sub}, email={Email}, emailVerified={EV}",
                payload.Subject, payload.Email, payload.EmailVerified);

            if (string.IsNullOrEmpty(payload.Subject) || string.IsNullOrEmpty(payload.Email))
            {
                _logger.LogWarning("[Google] Missing sub or email in token payload");
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("[Google] Invalid JWT: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Google] Exception during token verification");
            return null;
        }
    }
}

