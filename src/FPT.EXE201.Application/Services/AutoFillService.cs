using System.Text.Json;
using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.DTOs.AutoFill;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Application.Services;

public class AutoFillService : IAutoFillService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AutoFillService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Document type codes → auto-fill strategy
    private static readonly HashSet<string> VisitCreatingTypes = new() { "PRENATAL_CHECKUP" };
    private static readonly HashSet<string> TestCreatingTypes = new()
    {
        "BLOOD_TEST", "URINE_TEST", "ULTRASOUND",
        "HIV_TEST", "HEPATITIS_B_TEST", "THYROID_TEST",
        "GLUCOSE_TEST", "CBC_TEST", "NT_SCAN"
    };
    private static readonly HashSet<string> NotesOnlyTypes = new()
    {
        "PRESCRIPTION", "VACCINATION_RECORD", "MEDICAL_REPORT", "OTHER"
    };

    // Mapping: DocumentType code → RefTestType code (direct match)
    private static readonly Dictionary<string, string> DocTypeToTestTypeCode = new()
    {
        ["BLOOD_TEST"] = "BLOOD_TEST",
        ["ULTRASOUND"] = "ULTRASOUND",
        ["URINE_TEST"] = "URINE_TEST",
        ["HIV_TEST"] = "HIV_SCREEN",
        ["HEPATITIS_B_TEST"] = "HEPATITIS_B",
        ["THYROID_TEST"] = "TSH",
        ["GLUCOSE_TEST"] = "OGTT",
        ["CBC_TEST"] = "CBC_TEST",
        ["NT_SCAN"] = "NT_SCAN",
    };

    public AutoFillService(IUnitOfWork unitOfWork, ILogger<AutoFillService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ═══════════════════════════════════════
    // ReviewAsync — Parse + return review form
    // ═══════════════════════════════════════

    public async Task<ExtractionReviewDto> ReviewAsync(
        Guid ocrResultId, Guid currentUserId,
        string langCode = "vi",
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy OcrResult (read-only — chỉ đọc, không cần tracking)
        var ocrResult = await _unitOfWork.OcrResults.GetByIdAsync(
            ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        // 2. Lấy Document + Pregnancy (ownership check)
        //    GetByIdWithDetailsAsync includes: Files, DocumentType + Translations, OcrResults (latest 1), Pregnancy
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(
            ocrResult.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        // 3. Check status
        if (ocrResult.Status == OcrStatus.Confirmed)
            throw new BadRequestException("This extraction has already been confirmed.");

        if (ocrResult.Status != OcrStatus.Succeeded)
            throw new BadRequestException(
                $"Cannot review extraction when status is '{ocrResult.Status}'. Must be 'Succeeded'.");

        // 4. Parse StructuredJson → MedicalRecordExtractionResult
        //    VitalsData ĐÃ LÀ VitalsJsonDto — không cần mapping
        var extraction = ParseStructuredJson(ocrResult.StructuredJson);

        // 5. Get document type info (already loaded via GetByIdWithDetailsAsync includes)
        Guid? docTypeId = null;
        string? docTypeCode = null;
        string? docTypeDisplayName = null;
        if (document.DocumentType != null)
        {
            docTypeId = document.DocumentType.Id;
            docTypeCode = document.DocumentType.Code;
            var translation = document.DocumentType.Translations
                ?.FirstOrDefault(t => t.LanguageCode == langCode);
            docTypeDisplayName = translation?.DisplayName ?? document.DocumentType.Code;
        }

        // 6. Build review DTO
        var review = new ExtractionReviewDto
        {
            OcrResultId = ocrResultId,
            DocumentId = document.Id,
            PregnancyId = document.PregnancyId,
            DocumentTypeId = docTypeId,
            DocumentTypeCode = docTypeCode,
            DocumentTypeDisplayName = docTypeDisplayName,
            Status = ocrResult.Status.ToString(),
            ConfidenceScore = ocrResult.ConfidenceScore,
            RawStructuredJson = ocrResult.StructuredJson,
            FileUrls = document.Files?
                .OrderBy(f => f.SortOrder)
                .Select(f => f.StorageFile?.PublicUrl)
                .Where(u => !string.IsNullOrEmpty(u))
                .Select(u => u!)
                .ToList() ?? new List<string>(),
        };

        if (extraction != null)
        {
            review.OverallConfidence = extraction.OverallConfidence;

            // ⚠️ VitalsData ĐÃ LÀ VitalsJsonDto — assign trực tiếp
            // Chỉ trả vitals cho PRENATAL_CHECKUP (tạo Visit)
            if (docTypeCode == null || VisitCreatingTypes.Contains(docTypeCode))
            {
                review.Vitals = extraction.VitalsData;
            }

            // Lab results → không còn dùng, tất cả test types đều dùng direct mapping
        }

        // 7. Determine if auto-fill is possible
        review.CanAutoFill = DetermineCanAutoFill(review, docTypeCode);
        if (!review.CanAutoFill)
        {
            review.CannotAutoFillReason = GetCannotAutoFillReason(review, docTypeCode);
        }

        return review;
    }

    // ═══════════════════════════════════════
    // ConfirmAsync — Create entities
    // ═══════════════════════════════════════

    public async Task<AutoFillResultDto> ConfirmAsync(
        Guid ocrResultId, ConfirmExtractionDto dto,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy OcrResult — PHẢI dùng GetByIdTrackedAsync vì sẽ UPDATE status
        var ocrResult = await _unitOfWork.OcrResults.GetByIdTrackedAsync(
            ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        if (ocrResult.Status != OcrStatus.Succeeded)
            throw new BadRequestException(ocrResult.Status == OcrStatus.Confirmed
                ? "This extraction has already been confirmed."
                : $"Can only confirm when status is 'Succeeded'. Current: '{ocrResult.Status}'.");

        // 2. Lấy Document + Pregnancy (ownership check)
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(
            ocrResult.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        // 3. Validate DocumentTypeId
        var docType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
            dto.DocumentTypeId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Document type not found.");

        // 4. Update document's DocumentTypeId nếu chưa có hoặc khác
        if (!document.DocumentTypeId.HasValue || document.DocumentTypeId != dto.DocumentTypeId)
        {
            document.DocumentTypeId = dto.DocumentTypeId;
            _unitOfWork.MedicalDocuments.Update(document);
        }

        // 5. Execute strategy dựa vào document type code
        var result = new AutoFillResultDto
        {
            OcrResultId = ocrResultId,
            DocumentId = document.Id,
            DocumentTypeCode = docType.Code
        };

        if (VisitCreatingTypes.Contains(docType.Code))
        {
            await HandlePrenatalCheckup(document, dto, ocrResult, result, cancellationToken);
        }
        else if (TestCreatingTypes.Contains(docType.Code))
        {
            await HandleTestCreation(document, dto, docType.Code, result, cancellationToken);
        }
        else
        {
            // PRESCRIPTION, VACCINATION_RECORD, MEDICAL_REPORT, OTHER
            HandleNotesOnly(document, dto, result);
        }

        // 6. Update OcrResult → Confirmed
        //    Entity đã tracked từ GetByIdTrackedAsync — chỉ set properties
        ocrResult.Status = OcrStatus.Confirmed;
        ocrResult.ConfirmedAt = DateTime.UtcNow;
        ocrResult.ConfirmedBy = currentUserId;
        ocrResult.ConfirmedJson = JsonSerializer.Serialize(dto, JsonOptions);
        ocrResult.AutoFillResultJson = JsonSerializer.Serialize(result, JsonOptions);

        // 7. Save all changes in one transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-fill confirmed for OcrResult {OcrId}, DocType {DocType}. " +
            "Visit: {VisitId}, Tests: {TestCount}",
            ocrResultId, docType.Code, result.CreatedVisitId, result.CreatedTestIds.Count);

        return result;
    }

    // ═══════════════════════════════════════
    // Strategy: PRENATAL_CHECKUP → Visit
    // ═══════════════════════════════════════

    private async Task HandlePrenatalCheckup(
        MedicalDocument document, ConfirmExtractionDto dto,
        OcrResult ocrResult, AutoFillResultDto result,
        CancellationToken cancellationToken)
    {
        // Resolve vitals: FE gửi → dùng luôn, FE không gửi → lấy từ StructuredJson
        var vitals = dto.Vitals ?? ParseStructuredJson(ocrResult.StructuredJson)?.VitalsData;
        string? vitalsJson = vitals != null
            ? JsonSerializer.Serialize(vitals, JsonOptions)
            : null;

        PrenatalVisit visit;

        if (dto.ExistingVisitId.HasValue)
        {
            // Link vào visit có sẵn — dùng TrackedAsync vì sẽ update
            visit = await _unitOfWork.PrenatalVisits.GetByIdTrackedAsync(
                dto.ExistingVisitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Visit not found.");

            if (visit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Visit does not belong to this pregnancy.");

            // Update vitals (từ FE hoặc fallback từ StructuredJson)
            if (vitalsJson != null)
            {
                visit.VitalsJson = vitalsJson;
                visit.Location = dto.Location ?? visit.Location;
                visit.Notes = CombineNotes(visit.Notes, dto.Notes);
                // Entity tracked → EF auto-detects changes
            }
        }
        else
        {
            // Tạo Visit mới
            visit = new PrenatalVisit
            {
                PregnancyId = document.PregnancyId,
                VisitDate = dto.EventDate,          // DateOnly
                VisitType = VisitType.Routine,
                Location = dto.Location,
                Notes = dto.Notes,
                VitalsJson = vitalsJson
            };
            await _unitOfWork.PrenatalVisits.AddAsync(visit, cancellationToken);
        }

        // Auto-link document → visit
        document.VisitId = visit.Id;
        _unitOfWork.MedicalDocuments.Update(document);

        result.CreatedVisitId = visit.Id;
        result.DocumentLinkedToVisit = true;
        result.Summary = dto.ExistingVisitId.HasValue
            ? $"Updated prenatal visit for {dto.EventDate:dd/MM/yyyy}."
            : $"Created prenatal visit for {dto.EventDate:dd/MM/yyyy}.";
    }

    // ═══════════════════════════════════════
    // Strategy: Test-creating document types → Test(s)
    // ═══════════════════════════════════════

    private async Task HandleTestCreation(
        MedicalDocument document, ConfirmExtractionDto dto,
        string docTypeCode, AutoFillResultDto result,
        CancellationToken cancellationToken)
    {
        Guid? visitId = dto.ExistingVisitId;

        if (visitId.HasValue)
        {
            // Validate visit nếu FE truyền lên
            var existingVisit = await _unitOfWork.PrenatalVisits.GetByIdAsync(
                visitId.Value, cancellationToken: cancellationToken)
                ?? throw new NotFoundException("Visit not found.");
            if (existingVisit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Visit does not belong to this pregnancy.");
        }
        else
        {
            // Không có visit → auto-create một Routine visit để test không bị lơ lửng
            var newVisit = new PrenatalVisit
            {
                PregnancyId = document.PregnancyId,
                VisitDate = dto.EventDate,
                VisitType = VisitType.Routine,
                Location = dto.Location,
                Notes = dto.Notes,
            };
            await _unitOfWork.PrenatalVisits.AddAsync(newVisit, cancellationToken);
            visitId = newVisit.Id;
            result.CreatedVisitId = newVisit.Id;
        }

        // All test types use direct mapping → 1 PrenatalTest
        if (!DocTypeToTestTypeCode.TryGetValue(docTypeCode, out var directTestTypeCode))
        {
            result.Summary = $"No direct test type mapping for '{docTypeCode}'."; 
            // Still link document → visit
            document.VisitId = visitId;
            _unitOfWork.MedicalDocuments.Update(document);
            result.DocumentLinkedToVisit = true;
            return;
        }

        var testType = await FindTestTypeByCode(directTestTypeCode, cancellationToken);

        var test = new PrenatalTest
        {
            PregnancyId = document.PregnancyId,
            VisitId = visitId,
            TestTypeId = testType.Id,
            TestDate = dto.EventDate,
            Notes = dto.Notes,
            IsAbnormalResult = false,
            ImageUrlsJson = BuildImageUrlsJson(document),
            DocumentId = document.Id
        };
        await _unitOfWork.PrenatalTests.AddAsync(test, cancellationToken);
        result.CreatedTestIds.Add(test.Id);
        result.Summary = $"Created {directTestTypeCode} test result for {dto.EventDate:dd/MM/yyyy}.";

        // Always link document → visit (visitId luôn có: either FE-provided or auto-created)
        document.VisitId = visitId;
        _unitOfWork.MedicalDocuments.Update(document);
        result.DocumentLinkedToVisit = true;
    }

    // ═══════════════════════════════════════
    // Strategy: Notes-only (PRESCRIPTION, VACCINATION, etc.)
    // ═══════════════════════════════════════

    private void HandleNotesOnly(
        MedicalDocument document, ConfirmExtractionDto dto,
        AutoFillResultDto result)
    {
        bool updated = false;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            document.Notes = CombineNotes(document.Notes, dto.Notes);
            updated = true;
        }

        if (dto.EventDate != default && !document.DocumentDate.HasValue)
        {
            document.DocumentDate = dto.EventDate;  // DateOnly
            updated = true;
        }

        if (updated)
            _unitOfWork.MedicalDocuments.Update(document);

        result.Summary = "Document notes updated.";
    }

    // ═══════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════

    /// <summary>Parse StructuredJson → MedicalRecordExtractionResult.</summary>
    private MedicalRecordExtractionResult? ParseStructuredJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<MedicalRecordExtractionResult>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse StructuredJson");
            return null;
        }
    }

    private async Task<RefTestType> FindTestTypeByCode(string code, CancellationToken cancellationToken)
    {
        var allTypes = await _unitOfWork.RefTestTypes
            .GetActiveWithTranslationsAsync("vi", null, cancellationToken);
        return allTypes.FirstOrDefault(t => t.Code == code)
            ?? throw new NotFoundException($"Test type '{code}' not found in seed data.");
    }

    private bool DetermineCanAutoFill(ExtractionReviewDto review, string? docTypeCode)
    {
        if (review.Status != OcrStatus.Succeeded.ToString()) return false;
        if (string.IsNullOrEmpty(docTypeCode)) return false;

        // Notes-only types luôn có thể "auto-fill" (chỉ save notes)
        if (NotesOnlyTypes.Contains(docTypeCode)) return true;

        // Test types luôn có thể auto-fill
        if (TestCreatingTypes.Contains(docTypeCode)) return true;

        // PRENATAL_CHECKUP — BE tự lấy vitals từ StructuredJson nếu FE không gửi
        if (docTypeCode == "PRENATAL_CHECKUP") return true;

        return review.OverallConfidence >= 0.3;
    }

    private string? GetCannotAutoFillReason(ExtractionReviewDto review, string? docTypeCode)
    {
        if (review.Status != OcrStatus.Succeeded.ToString())
            return "Extraction process has not completed.";
        if (string.IsNullOrEmpty(docTypeCode))
            return "Please select a document type before confirming.";
        return "Extracted data quality is insufficient.";
    }

    private static string? BuildImageUrlsJson(MedicalDocument document)
    {
        var urls = document.Files?
            .OrderBy(f => f.SortOrder)
            .Select(f => f.StorageFile?.PublicUrl)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToArray();
        if (urls == null || urls.Length == 0) return null;
        return JsonSerializer.Serialize(urls, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string? CombineNotes(string? existing, string? additional)
    {
        if (string.IsNullOrWhiteSpace(additional)) return existing;
        if (string.IsNullOrWhiteSpace(existing)) return additional;
        return $"{existing}\n---\n{additional}";
    }
}
