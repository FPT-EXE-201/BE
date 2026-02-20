using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI;
using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MedicalRecordAiService : IMedicalRecordAiService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiProvider _aiProvider;
    private readonly IOcrProvider _ocrProvider;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalRecordAiService> _logger;

    private const string TemplateKey = "medical_record.extraction";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MedicalRecordAiService(
        IUnitOfWork unitOfWork,
        IAiProvider aiProvider,
        IOcrProvider ocrProvider,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<MedicalRecordAiService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _ocrProvider = ocrProvider;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OcrResultDto> ProcessDocumentAsync(
        Guid documentId, Guid currentUserId,
        string? languageHint = "vi",
        CancellationToken cancellationToken = default)
    {
        // 1. Verify document exists + ownership
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        // 1.5. ⚠️ Validate DocumentType = PRENATAL_CHECKUP (4-Phase Flow Rule)
        var docTypeCode = document.DocumentType?.Code;
        if (docTypeCode != "PRENATAL_CHECKUP")
            throw new BadRequestException(
                "OCR + AI extraction is only supported for PRENATAL_CHECKUP documents. " +
                $"This document is '{docTypeCode ?? "unset"}'.");

        // 2. Reuse existing Pending OcrResult (from auto-queue) or create new one
        var latestOcr = await _unitOfWork.OcrResults.GetLatestByDocumentIdAsync(documentId, cancellationToken);

        OcrResult ocrResult;
        int nextRunNo;

        if (latestOcr != null && latestOcr.Status == OcrStatus.Pending)
        {
            // Reuse the Pending record created by QueueOcrAsync (background auto-queue)
            ocrResult = latestOcr;
            nextRunNo = latestOcr.OcrRunNumber;
            // Update language hint if provided
            ocrResult.LanguageHint = languageHint ?? ocrResult.LanguageHint ?? "vi";
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Manual trigger — create new OcrResult
            nextRunNo = (latestOcr?.OcrRunNumber ?? 0) + 1;
            ocrResult = new OcrResult
            {
                DocumentId = documentId,
                OcrRunNumber = nextRunNo,
                Status = OcrStatus.Pending,
                LanguageHint = languageHint ?? "vi"
            };
            await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        try
        {
            // 4. Phase 1: OCR — Azure Document Intelligence
            ocrResult.Status = OcrStatus.OcrProcessing;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ocrResponse = await RunOcrAsync(document, languageHint, cancellationToken);

            ocrResult.RawText = ocrResponse.RawText;
            ocrResult.ConfidenceScore = ocrResponse.ConfidenceScore;
            ocrResult.OcrEngine = ocrResponse.EngineUsed;
            ocrResult.OcrProcessingTimeMs = (int)ocrResponse.ProcessingTime.TotalMilliseconds;
            ocrResult.Status = OcrStatus.OcrCompleted;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("OCR completed for document {DocId}, run {Run}. Text length: {Len}",
                documentId, nextRunNo, ocrResponse.RawText.Length);

            // 5. Validate OCR output
            if (string.IsNullOrWhiteSpace(ocrResponse.RawText))
            {
                ocrResult.Status = OcrStatus.Failed;
                ocrResult.ErrorMessage = "OCR returned empty text. The image may be blank or unreadable.";
                _unitOfWork.OcrResults.Update(ocrResult);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return _mapper.Map<OcrResultDto>(ocrResult);
            }

            // 6. Phase 2: AI Extraction — Gemini
            ocrResult.Status = OcrStatus.AiExtracting;
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var aiResult = await RunAiExtractionAsync(
                ocrResponse.RawText, document.PregnancyId, cancellationToken);

            ocrResult.StructuredJson = aiResult.StructuredJson;
            ocrResult.AiModelUsed = aiResult.ModelUsed;
            ocrResult.AiTokensUsed = aiResult.TotalTokens;
            ocrResult.AiProcessingTimeMs = (int)aiResult.ProcessingTime.TotalMilliseconds;
            ocrResult.AiPromptTemplateId = aiResult.TemplateId;
            ocrResult.Status = OcrStatus.Succeeded;

            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AI extraction completed for document {DocId}, run {Run}. Tokens: {Tokens}",
                documentId, nextRunNo, aiResult.TotalTokens);

            return _mapper.Map<OcrResultDto>(ocrResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed for document {DocId}, run {Run} at status {Status}",
                documentId, nextRunNo, ocrResult.Status);

            ocrResult.Status = OcrStatus.Failed;
            ocrResult.ErrorMessage = $"Pipeline failed at {ocrResult.Status}: {ex.Message}";
            _unitOfWork.OcrResults.Update(ocrResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    public async Task<OcrResultDto> ReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get existing OCR result — MUST use Tracked so EF persists status changes
        var existingOcr = await _unitOfWork.OcrResults.GetByIdTrackedAsync(ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        if (string.IsNullOrWhiteSpace(existingOcr.RawText))
            throw new BadRequestException("No raw text available for re-extraction. Please run the full pipeline again.");

        // 2. Verify ownership through document → pregnancy chain
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(existingOcr.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        // 3. Update existing OcrResult in-place (do NOT create a new run)
        // Entity is tracked from GetByIdTrackedAsync — just set properties + SaveChangesAsync.
        existingOcr.Status = OcrStatus.AiExtracting;
        existingOcr.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // 4. Run AI extraction only (skip OCR)
            var aiResult = await RunAiExtractionAsync(
                existingOcr.RawText, document.PregnancyId, cancellationToken);

            existingOcr.StructuredJson = aiResult.StructuredJson;
            existingOcr.AiModelUsed = aiResult.ModelUsed;
            existingOcr.AiTokensUsed = aiResult.TotalTokens;
            existingOcr.AiProcessingTimeMs = (int)aiResult.ProcessingTime.TotalMilliseconds;
            existingOcr.AiPromptTemplateId = aiResult.TemplateId;
            existingOcr.Status = OcrStatus.Succeeded;
            existingOcr.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OcrResultDto>(existingOcr);
        }
        catch (Exception ex)
        {
            existingOcr.Status = OcrStatus.Failed;
            existingOcr.ErrorMessage = $"Re-extraction failed: {ex.Message}";
            existingOcr.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // ═══════════════════════════════════════════════
    // Private: OCR Phase (multi-file support)
    // ═══════════════════════════════════════════════

    private async Task<AI.Models.OcrResponse> RunOcrAsync(
        MedicalDocument document, string? languageHint, CancellationToken cancellationToken)
    {
        var documentFiles = document.Files.OrderBy(f => f.SortOrder).ToList();
        if (documentFiles.Count == 0)
            throw new BadRequestException("Document has no files to process.");

        // Single file — original behavior
        if (documentFiles.Count == 1)
        {
            var sf = documentFiles[0].StorageFile;
            var fileStream = await _fileStorageService.DownloadAsync(sf.ObjectKey, cancellationToken);
            var ocrRequest = new OcrRequest(
                FileStream: fileStream,
                FileName: sf.OriginalFileName ?? sf.ObjectKey,
                ContentType: sf.MimeType,
                LanguageHint: languageHint
            );
            return await _ocrProvider.ExtractTextAsync(ocrRequest, cancellationToken);
        }

        // Multi-file — OCR each page, concatenate raw text
        var allRawTexts = new List<string>();
        decimal totalConfidence = 0;
        long totalProcessingMs = 0;
        string? engineUsed = null;

        for (var i = 0; i < documentFiles.Count; i++)
        {
            var sf = documentFiles[i].StorageFile;
            var fileStream = await _fileStorageService.DownloadAsync(sf.ObjectKey, cancellationToken);
            var ocrRequest = new OcrRequest(
                FileStream: fileStream,
                FileName: sf.OriginalFileName ?? sf.ObjectKey,
                ContentType: sf.MimeType,
                LanguageHint: languageHint
            );

            var pageResult = await _ocrProvider.ExtractTextAsync(ocrRequest, cancellationToken);

            var pageLabel = documentFiles[i].PageLabel ?? $"Page {i + 1}";
            allRawTexts.Add($"--- {pageLabel} ---\n{pageResult.RawText}");
            totalConfidence += pageResult.ConfidenceScore;
            totalProcessingMs += (long)pageResult.ProcessingTime.TotalMilliseconds;
            engineUsed ??= pageResult.EngineUsed;

            _logger.LogInformation(
                "OCR page {Page}/{Total} completed for document {DocId}. Text length: {Len}",
                i + 1, documentFiles.Count, document.Id, pageResult.RawText.Length);
        }

        var combinedText = string.Join("\n\n", allRawTexts);
        var avgConfidence = totalConfidence / documentFiles.Count;

        return new AI.Models.OcrResponse(
            RawText: combinedText,
            ConfidenceScore: Math.Round(avgConfidence, 2),
            ProcessingTime: TimeSpan.FromMilliseconds(totalProcessingMs),
            EngineUsed: engineUsed ?? ""
        );
    }

    // ═══════════════════════════════════════════════
    // Private: AI Extraction Phase (RAG + Prompt + Gemini)
    // ═══════════════════════════════════════════════

    private record AiExtractionPipelineResult(
        string StructuredJson, string ModelUsed, int TotalTokens,
        TimeSpan ProcessingTime, Guid? TemplateId);

    private async Task<AiExtractionPipelineResult> RunAiExtractionAsync(
        string rawText, Guid pregnancyId, CancellationToken cancellationToken)
    {
        // Step 1: Load prompt template from DB
        var template = await _unitOfWork.AiPromptTemplates
            .GetActiveByKeyAsync(TemplateKey, cancellationToken)
            ?? throw new NotFoundException($"AI prompt template '{TemplateKey}' not found. Please seed the database.");

        // Step 2: Retrieve RAG context
        var context = await RetrievePregnancyContextAsync(pregnancyId, cancellationToken);

        // Step 3: Build prompt using Rule Layers + RAG Context
        var prompt = PromptBuilder.FromTemplate(template)
            .WithContext("PATIENT CONTEXT", FormatPregnancyContext(context))
            .WithUserMessage($"Extract structured data from this OCR text:\n\n---\n{rawText}\n---")
            .Build();

        // Step 4: Call Gemini
        var aiResponse = await _aiProvider.GenerateAsync(prompt, cancellationToken);

        // Step 5: Validate JSON response
        var structuredJson = ValidateAndFormatJson(aiResponse.Content);

        return new AiExtractionPipelineResult(
            StructuredJson: structuredJson,
            ModelUsed: aiResponse.ModelUsed,
            TotalTokens: aiResponse.TotalTokens,
            ProcessingTime: aiResponse.ProcessingTime,
            TemplateId: template.Id
        );
    }

    // ═══════════════════════════════════════════════
    // Private: RAG Context Retrieval
    // ═══════════════════════════════════════════════

    private async Task<PregnancyContext> RetrievePregnancyContextAsync(
        Guid pregnancyId, CancellationToken cancellationToken)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null) return new PregnancyContext { PregnancyId = pregnancyId };

        // Calculate current gestational week from LMP
        // ⚠️ LastMenstrualPeriodDate is DateOnly (not DateTime) — use DayNumber arithmetic
        int? gestWeek = null;
        if (pregnancy.LastMenstrualPeriodDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalDays = today.DayNumber - pregnancy.LastMenstrualPeriodDate.Value.DayNumber;
            gestWeek = totalDays >= 0 && totalDays <= 315 ? totalDays / 7 : null;
        }

        // Get known conditions
        var conditions = await _unitOfWork.PregnancyConditions
            .GetByPregnancyIdAsync(pregnancyId, "vi", cancellationToken);
        var conditionNames = conditions
            .Select(c => c.Condition?.Translations?.FirstOrDefault()?.DisplayName ?? c.Condition?.Code ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        // Get most recent OCR result for consistency context
        string? previousSummary = null;
        var recentDocs = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, cancellationToken: cancellationToken);
        var recentOcr = recentDocs
            .SelectMany(d => d.OcrResults ?? Enumerable.Empty<OcrResult>())
            .Where(o => o.Status == OcrStatus.Succeeded && !string.IsNullOrEmpty(o.StructuredJson))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (recentOcr != null)
        {
            // Extract summary from previous extraction for consistency
            try
            {
                var prevResult = JsonSerializer.Deserialize<MedicalRecordExtractionResult>(
                    recentOcr.StructuredJson!, JsonOptions);
                var interview = prevResult?.VitalsData?.Interview;
                var vitals = prevResult?.VitalsData?.Examination?.VitalSigns;
                if (interview != null || vitals != null)
                {
                    previousSummary = $"Previous record ({recentOcr.CreatedAt:yyyy-MM-dd}): " +
                        $"Week {interview?.GestationalWeek}, " +
                        $"Weight {vitals?.WeightKg}kg, " +
                        $"BP {vitals?.BloodPressureSystolic}/{vitals?.BloodPressureDiastolic}";
                }
            }
            catch { /* Ignore deserialization errors in context */ }
        }

        return new PregnancyContext
        {
            PregnancyId = pregnancyId,
            CurrentGestationalWeek = gestWeek,
            PregnancyStatus = pregnancy.Status.ToString(),
            KnownConditions = conditionNames,
            PreviousRecordSummary = previousSummary
        };
    }

    private static string FormatPregnancyContext(PregnancyContext context)
    {
        var parts = new List<string>();

        if (context.CurrentGestationalWeek.HasValue)
            parts.Add($"Current gestational week: {context.CurrentGestationalWeek}");

        if (!string.IsNullOrEmpty(context.PregnancyStatus))
            parts.Add($"Pregnancy status: {context.PregnancyStatus}");

        if (context.KnownConditions.Any())
            parts.Add($"Known conditions: {string.Join(", ", context.KnownConditions)}");

        if (!string.IsNullOrEmpty(context.PreviousRecordSummary))
            parts.Add(context.PreviousRecordSummary);

        return parts.Any()
            ? string.Join("\n", parts)
            : "No prior pregnancy data available.";
    }

    // ═══════════════════════════════════════════════
    // Private: JSON Validation
    // ═══════════════════════════════════════════════

    private string ValidateAndFormatJson(string content)
    {
        // Clean AI response: strip markdown fences, thinking blocks, whitespace
        var cleaned = CleanAiJsonResponse(content);

        try
        {
            // Parse to validate + re-format (output follows C# property declaration order)
            var parsed = JsonSerializer.Deserialize<MedicalRecordExtractionResult>(cleaned, JsonOptions);
            if (parsed == null)
                throw new BadRequestException("AI returned null JSON.");

            // Re-serialize with formatting — ensures consistent property order & indentation
            var result = JsonSerializer.Serialize(parsed, JsonOptions);
            _logger.LogDebug("StructuredJson validated and re-serialized successfully. Length: {Len}", result.Length);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI response is not valid JSON for expected schema. Storing cleaned content. First 200 chars: {Preview}",
                cleaned.Length > 200 ? cleaned[..200] : cleaned);
            // Store cleaned AI response even if it doesn't match our schema exactly
            return cleaned;
        }
    }

    /// <summary>
    /// Strip markdown code fences, thinking blocks, and leading/trailing whitespace
    /// from Gemini AI response to get pure JSON.
    /// </summary>
    private static string CleanAiJsonResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var cleaned = content.Trim();

        // Remove markdown code fences: ```json ... ``` or ``` ... ```
        if (cleaned.StartsWith("```"))
        {
            // Find end of first line (skip ```json or ```)
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > 0)
                cleaned = cleaned[(firstNewline + 1)..];

            // Remove trailing ```
            if (cleaned.EndsWith("```"))
                cleaned = cleaned[..^3];

            cleaned = cleaned.Trim();
        }

        // If content starts with non-JSON text before first { or [, strip it
        var jsonStart = Math.Min(
            cleaned.IndexOf('{') is var ib && ib >= 0 ? ib : int.MaxValue,
            cleaned.IndexOf('[') is var ia && ia >= 0 ? ia : int.MaxValue
        );

        if (jsonStart > 0 && jsonStart < int.MaxValue)
            cleaned = cleaned[jsonStart..];

        // If content has trailing text after last } or ], strip it
        var jsonEndBrace = cleaned.LastIndexOf('}');
        var jsonEndBracket = cleaned.LastIndexOf(']');
        var jsonEnd = Math.Max(jsonEndBrace, jsonEndBracket);

        if (jsonEnd > 0 && jsonEnd < cleaned.Length - 1)
            cleaned = cleaned[..(jsonEnd + 1)];

        // Repair truncated JSON (Gemini may hit maxOutputTokens and cut off)
        cleaned = RepairTruncatedJson(cleaned);

        return cleaned;
    }

    /// <summary>
    /// Attempt to repair truncated JSON by closing open brackets/braces.
    /// Gemini 2.5 Flash with thinking mode may exhaust maxOutputTokens and produce incomplete JSON.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        // Count open vs close brackets/braces (ignoring those inside strings)
        var openBraces = 0;
        var openBrackets = 0;
        var inString = false;
        var prevChar = '\0';

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (c == '"' && prevChar != '\\')
            {
                inString = !inString;
            }
            else if (!inString)
            {
                switch (c)
                {
                    case '{': openBraces++; break;
                    case '}': openBraces--; break;
                    case '[': openBrackets++; break;
                    case ']': openBrackets--; break;
                }
            }

            prevChar = c;
        }

        // If balanced, return as-is
        if (openBraces == 0 && openBrackets == 0)
            return json;

        // Truncated — need to close. First, strip any trailing incomplete key/value
        var trimmed = json.TrimEnd();

        // Remove trailing comma or colon (incomplete entry)
        while (trimmed.Length > 0 && (trimmed[^1] == ',' || trimmed[^1] == ':'))
            trimmed = trimmed[..^1].TrimEnd();

        // If ends with an incomplete string (odd quotes), remove it
        if (inString && trimmed.Length > 0)
        {
            // Find the last unescaped quote that opened this string
            var lastQuote = trimmed.LastIndexOf('"');
            if (lastQuote > 0)
            {
                trimmed = trimmed[..lastQuote].TrimEnd();
                // Remove trailing comma/colon left over
                while (trimmed.Length > 0 && (trimmed[^1] == ',' || trimmed[^1] == ':' || trimmed[^1] == '"'))
                    trimmed = trimmed[..^1].TrimEnd();
            }
        }

        // Remove trailing comma before we close
        if (trimmed.Length > 0 && trimmed[^1] == ',')
            trimmed = trimmed[..^1];

        // Re-count after trimming
        openBraces = 0;
        openBrackets = 0;
        inString = false;
        prevChar = '\0';
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (c == '"' && prevChar != '\\') inString = !inString;
            else if (!inString)
            {
                switch (c)
                {
                    case '{': openBraces++; break;
                    case '}': openBraces--; break;
                    case '[': openBrackets++; break;
                    case ']': openBrackets--; break;
                }
            }
            prevChar = c;
        }

        // Append missing closers (innermost first)
        // We need to close in reverse order of what's still open
        // Simple heuristic: close brackets first, then braces (arrays[] inside objects{})
        var suffix = new string(']', Math.Max(0, openBrackets)) +
                     new string('}', Math.Max(0, openBraces));

        return trimmed + suffix;
    }
}
