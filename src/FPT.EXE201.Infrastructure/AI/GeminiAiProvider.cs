using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.Exceptions;

namespace FPT.EXE201.Infrastructure.AI;

/// <summary>
/// Google Gemini REST API client.
/// Implements IAiProvider cho cả single completion và multi-turn chat.
/// Reads all config from appsettings AI:Gemini section.
/// Supports automatic retry with exponential backoff for transient errors.
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly double _defaultTemperature;
    private readonly int _defaultMaxOutputTokens;
    private readonly int _maxRetries;
    private readonly ILogger<GeminiAiProvider> _logger;

    /// <summary>HTTP status codes that are safe to retry (transient errors).</summary>
    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes = new()
    {
        HttpStatusCode.TooManyRequests,         // 429
        HttpStatusCode.InternalServerError,     // 500
        HttpStatusCode.BadGateway,              // 502
        HttpStatusCode.ServiceUnavailable,      // 503
        HttpStatusCode.GatewayTimeout           // 504
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["AI:Gemini:ApiKey"]
            ?? throw new InvalidOperationException("AI:Gemini:ApiKey is not configured.");
        _defaultModel = configuration["AI:Gemini:DefaultModel"] ?? "gemini-2.5-flash";
        _defaultTemperature = double.TryParse(configuration["AI:Gemini:DefaultTemperature"], out var temp) ? temp : 0.1;
        _defaultMaxOutputTokens = int.TryParse(configuration["AI:Gemini:DefaultMaxOutputTokens"], out var tokens) ? tokens : 16384;
        _maxRetries = int.TryParse(configuration["AI:Gemini:MaxRetries"], out var retries) ? retries : 3;
    }

    public async Task<AiResponse> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var modelName = string.IsNullOrEmpty(prompt.ModelName) ? _defaultModel : prompt.ModelName;

        var request = new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = prompt.UserMessage } }
                }
            },
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = new List<GeminiPart> { new() { Text = prompt.SystemMessage } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = prompt.Temperature,
                MaxOutputTokens = prompt.MaxOutputTokens,
                ResponseMimeType = prompt.JsonMode ? "application/json" : null
            }
        };

        return await SendRequestWithRetryAsync(modelName, request, cancellationToken);
    }

    public async Task<AiResponse> ChatAsync(
        List<AiMessage> messages,
        string systemMessage,
        string modelName,
        double temperature = -1,
        int maxOutputTokens = -1,
        CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrEmpty(modelName) ? _defaultModel : modelName;
        // Use config defaults if caller passes sentinel values (-1)
        var effectiveTemp = temperature < 0 ? _defaultTemperature : temperature;
        var effectiveMaxTokens = maxOutputTokens < 0 ? _defaultMaxOutputTokens : maxOutputTokens;

        var request = new GeminiRequest
        {
            Contents = messages.Select(m => new GeminiContent
            {
                Role = m.Role, // "user" or "model"
                Parts = new List<GeminiPart> { new() { Text = m.Content } }
            }).ToList(),
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = new List<GeminiPart> { new() { Text = systemMessage } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = effectiveTemp,
                MaxOutputTokens = effectiveMaxTokens
            }
        };

        return await SendRequestWithRetryAsync(model, request, cancellationToken);
    }

    // ═══ Private: Retry wrapper ═══

    private async Task<AiResponse> SendRequestWithRetryAsync(
        string modelName, GeminiRequest request, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await SendRequestAsync(modelName, request, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s
                _logger.LogWarning(
                    "Gemini request failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s: {Error}",
                    attempt + 1, _maxRetries + 1, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
            catch (GeminiRetryableException ex) when (attempt < _maxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    "Gemini API returned {StatusCode} (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s: {Error}",
                    ex.StatusCode, attempt + 1, _maxRetries + 1, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new BadRequestException("AI processing failed after all retries.");
    }

    private async Task<AiResponse> SendRequestAsync(
        string modelName, GeminiRequest request, CancellationToken cancellationToken)
    {
        var url = $"models/{modelName}:generateContent?key={_apiKey}";
        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);

        _logger.LogDebug("Gemini request to {Model}, content length: {Length}", modelName, jsonContent.Length);

        var stopwatch = Stopwatch.StartNew();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        stopwatch.Stop();

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error {StatusCode}: {Body}", httpResponse.StatusCode, responseBody);

            // Throw retryable exception for transient errors (429, 500, 502, 503, 504)
            if (RetryableStatusCodes.Contains(httpResponse.StatusCode))
            {
                throw new GeminiRetryableException(httpResponse.StatusCode,
                    $"Gemini API returned {httpResponse.StatusCode}");
            }

            var errorResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);
            var errorMessage = errorResponse?.Error?.Message ?? $"Gemini API returned {httpResponse.StatusCode}";

            throw new BadRequestException($"AI processing failed: {errorMessage}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
        {
            throw new BadRequestException("AI returned no response. The content may have been blocked by safety filters.");
        }

        var candidate = geminiResponse.Candidates[0];
        var content = candidate.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        var usage = geminiResponse.UsageMetadata;

        _logger.LogInformation(
            "Gemini response from {Model}: {Tokens} tokens in {Time}ms",
            modelName,
            usage?.TotalTokenCount ?? 0,
            stopwatch.ElapsedMilliseconds);

        return new AiResponse(
            Content: content,
            PromptTokens: usage?.PromptTokenCount ?? 0,
            CompletionTokens: usage?.CandidatesTokenCount ?? 0,
            TotalTokens: usage?.TotalTokenCount ?? 0,
            ModelUsed: modelName,
            ProcessingTime: stopwatch.Elapsed
        );
    }

    /// <summary>
    /// Internal exception for retryable Gemini API errors (429, 5xx).
    /// Caught by retry loop — never exposed to callers.
    /// </summary>
    private sealed class GeminiRetryableException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public GeminiRetryableException(HttpStatusCode statusCode, string message)
            : base(message) => StatusCode = statusCode;
    }
}
