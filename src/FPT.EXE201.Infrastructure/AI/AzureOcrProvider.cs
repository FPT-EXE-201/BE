using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.Exceptions;

namespace FPT.EXE201.Infrastructure.AI;

/// <summary>
/// Azure Document Intelligence REST API client.
/// Uses prebuilt-read model for general document OCR.
/// Async pattern: POST → poll GET until succeeded.
/// </summary>
public class AzureOcrProvider : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly string _apiVersion;
    private readonly int _pollingIntervalMs;
    private readonly int _timeoutSeconds;
    private readonly ILogger<AzureOcrProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public IReadOnlyList<string> SupportedContentTypes { get; } = new List<string>
    {
        "image/jpeg", "image/png", "image/bmp", "image/tiff", "image/heif",
        "application/pdf"
    };

    public AzureOcrProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureOcrProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["AI:AzureDocumentIntelligence:ApiKey"]
            ?? throw new InvalidOperationException("AI:AzureDocumentIntelligence:ApiKey is not configured.");
        _modelId = configuration["AI:AzureDocumentIntelligence:ModelId"] ?? "prebuilt-read";
        _apiVersion = configuration["AI:AzureDocumentIntelligence:ApiVersion"] ?? "2024-11-30";
        _pollingIntervalMs = int.Parse(configuration["AI:AzureDocumentIntelligence:PollingIntervalMs"] ?? "1000");
        _timeoutSeconds = int.Parse(configuration["AI:AzureDocumentIntelligence:TimeoutSeconds"] ?? "120");
    }

    public async Task<OcrResponse> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        if (!SupportedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            throw new BadRequestException($"Content type '{request.ContentType}' is not supported for OCR. Supported: {string.Join(", ", SupportedContentTypes)}");
        }

        var stopwatch = Stopwatch.StartNew();

        // Step 1: Submit analyze request
        var operationLocation = await SubmitAnalyzeRequestAsync(request, cancellationToken);

        // Step 2: Poll for results
        var result = await PollForResultAsync(operationLocation, cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Azure OCR completed in {Time}ms, extracted {Length} chars, confidence: {Confidence:F2}",
            stopwatch.ElapsedMilliseconds, result.RawText.Length, result.ConfidenceScore);

        return result with
        {
            ProcessingTime = stopwatch.Elapsed,
            EngineUsed = $"azure-document-intelligence-{_apiVersion}"
        };
    }

    // ═══ Private: Submit Analyze ═══

    private async Task<string> SubmitAnalyzeRequestAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        var url = $"documentintelligence/documentModels/{_modelId}:analyze?api-version={_apiVersion}";

        if (!string.IsNullOrEmpty(request.LanguageHint))
        {
            url += $"&locale={request.LanguageHint}";
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

        // Send file as binary
        var streamContent = new StreamContent(request.FileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        httpRequest.Content = streamContent;

        _logger.LogDebug("Submitting Azure OCR for {FileName} ({ContentType})", request.FileName, request.ContentType);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Azure OCR submit failed {StatusCode}: {Body}", httpResponse.StatusCode, errorBody);
            throw new BadRequestException($"Azure OCR failed to start: {httpResponse.StatusCode}");
        }

        // Get operation-location header for polling
        if (!httpResponse.Headers.TryGetValues("Operation-Location", out var values))
        {
            throw new BadRequestException("Azure OCR response missing Operation-Location header.");
        }

        return values.First();
    }

    // ═══ Private: Poll for Results ═══

    private async Task<OcrResponse> PollForResultAsync(string operationLocation, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(_pollingIntervalMs, cancellationToken);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            pollRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            using var pollResponse = await _httpClient.SendAsync(pollRequest, cancellationToken);
            var responseBody = await pollResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!pollResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Azure OCR poll failed {StatusCode}: {Body}", pollResponse.StatusCode, responseBody);
                throw new BadRequestException($"Azure OCR polling failed: {pollResponse.StatusCode}");
            }

            var analyzeResponse = JsonSerializer.Deserialize<AzureAnalyzeResponse>(responseBody, JsonOptions);

            switch (analyzeResponse?.Status?.ToLowerInvariant())
            {
                case "succeeded":
                    return ExtractFromAnalyzeResult(analyzeResponse);

                case "failed":
                    var errorMsg = analyzeResponse.Error?.Message ?? "Unknown OCR error";
                    throw new BadRequestException($"Azure OCR failed: {errorMsg}");

                case "running":
                case "notstarted":
                    _logger.LogDebug("Azure OCR still processing...");
                    continue;

                default:
                    _logger.LogWarning("Unknown Azure OCR status: {Status}", analyzeResponse?.Status);
                    continue;
            }
        }

        throw new BadRequestException($"Azure OCR timed out after {_timeoutSeconds} seconds.");
    }

    // ═══ Private: Extract text from result ═══

    private static OcrResponse ExtractFromAnalyzeResult(AzureAnalyzeResponse response)
    {
        var result = response.AnalyzeResult;
        if (result == null)
        {
            return new OcrResponse("", 0m, TimeSpan.Zero, "");
        }

        // Full extracted text
        var rawText = result.Content ?? "";

        // Average confidence across all pages/lines
        // Azure returns word.Confidence in range 0.0-1.0 → convert to 0.00-100.00
        double totalConfidence = 0;
        int wordCount = 0;

        if (result.Pages != null)
        {
            foreach (var page in result.Pages)
            {
                if (page.Words != null)
                {
                    foreach (var word in page.Words)
                    {
                        totalConfidence += word.Confidence;
                        wordCount++;
                    }
                }
            }
        }

        // Convert from 0.0-1.0 ratio → 0.00-100.00 percentage (matches DECIMAL(5,2) column)
        var avgConfidencePercent = wordCount > 0
            ? (decimal)(totalConfidence / wordCount * 100)
            : 0m;

        return new OcrResponse(
            RawText: rawText,
            ConfidenceScore: Math.Round(avgConfidencePercent, 2),
            ProcessingTime: TimeSpan.Zero, // Set by caller
            EngineUsed: "" // Set by caller
        );
    }
}

// ═══ Azure API Response Models (internal) ═══

internal class AzureAnalyzeResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("analyzeResult")]
    public AzureAnalyzeResult? AnalyzeResult { get; set; }

    [JsonPropertyName("error")]
    public AzureErrorInfo? Error { get; set; }
}

internal class AzureAnalyzeResult
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("pages")]
    public List<AzureOcrPage>? Pages { get; set; }
}

internal class AzureOcrPage
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("lines")]
    public List<AzureOcrLine>? Lines { get; set; }

    [JsonPropertyName("words")]
    public List<AzureOcrWord>? Words { get; set; }
}

internal class AzureOcrLine
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

internal class AzureOcrWord
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal class AzureErrorInfo
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
