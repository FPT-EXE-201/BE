using AutoMapper;
using FPT.EXE201.Application;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOcrJobQueue _jobQueue;
    private readonly IMapper _mapper;

    public OcrService(IUnitOfWork unitOfWork, IOcrJobQueue jobQueue, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _jobQueue = jobQueue;
        _mapper = mapper;
    }

    public async Task<Guid> QueueOcrAsync(
        Guid documentId, string? languageHint = null,
        CancellationToken cancellationToken = default)
    {
        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = 1,
            Status = OcrStatus.Pending,
            LanguageHint = languageHint ?? "vi"
        };

        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get document owner for background job
        var document2 = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");

        // Enqueue background job — returns immediately, does NOT block
        await _jobQueue.EnqueueAsync(new OcrJobItem(
            OcrResultId: ocrResult.Id,
            DocumentId: documentId,
            UserId: document2.Pregnancy.UserId,
            LanguageHint: languageHint ?? "vi"
        ), cancellationToken);

        return ocrResult.Id;
    }

    public async Task<OcrResultDto> QueueProcessAsync(
        Guid documentId, Guid currentUserId, string? languageHint = "vi",
        CancellationToken cancellationToken = default)
    {
        // 1. Verify document exists + ownership + type
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        var docTypeCode = document.DocumentType?.Code;
        if (docTypeCode != "PRENATAL_CHECKUP")
            throw new BadRequestException(
                "OCR + AI extraction is only supported for PRENATAL_CHECKUP documents. " +
                $"This document is '{docTypeCode ?? "unset"}'.");

        // 2. Get next run number
        var latestOcr = await _unitOfWork.OcrResults.GetLatestByDocumentIdAsync(documentId, cancellationToken);
        var nextRunNo = (latestOcr?.OcrRunNumber ?? 0) + 1;

        // 3. Create OcrResult with Pending status
        var ocrResult = new OcrResult
        {
            DocumentId = documentId,
            OcrRunNumber = nextRunNo,
            Status = OcrStatus.Pending,
            LanguageHint = languageHint ?? "vi"
        };
        await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Enqueue background job — returns immediately
        await _jobQueue.EnqueueAsync(new OcrJobItem(
            OcrResultId: ocrResult.Id,
            DocumentId: documentId,
            UserId: currentUserId,
            LanguageHint: languageHint ?? "vi",
            IsReExtract: false
        ), cancellationToken);

        return _mapper.Map<OcrResultDto>(ocrResult);
    }

    public async Task<OcrResultDto> QueueReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get existing OCR result — MUST use Tracked so EF persists status changes
        var existingOcr = await _unitOfWork.OcrResults.GetByIdTrackedAsync(ocrResultId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("OCR result not found.");

        if (string.IsNullOrWhiteSpace(existingOcr.RawText))
            throw new BadRequestException("No raw text available for re-extraction. Please run the full pipeline first.");

        // 2. Verify ownership through document → pregnancy chain
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(existingOcr.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to process this document.");

        // 3. Set status to AiExtracting BEFORE returning — so FE polling sees "in progress" immediately
        // Entity is tracked from GetByIdTrackedAsync — just set properties + SaveChangesAsync.
        existingOcr.Status = OcrStatus.AiExtracting;
        existingOcr.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Enqueue background job — returns immediately
        await _jobQueue.EnqueueAsync(new OcrJobItem(
            OcrResultId: existingOcr.Id,
            DocumentId: existingOcr.DocumentId,
            UserId: currentUserId,
            LanguageHint: existingOcr.LanguageHint ?? "vi",
            IsReExtract: true
        ), cancellationToken);

        return _mapper.Map<OcrResultDto>(existingOcr);
    }

    public async Task<OcrResultDto> GetResultAsync(
        Guid ocrResultId, CancellationToken cancellationToken = default)
    {
        var ocr = await _unitOfWork.OcrResults.GetByIdAsync(ocrResultId, cancellationToken: cancellationToken);
        if (ocr == null)
            throw new NotFoundException("OCR result not found.");

        return _mapper.Map<OcrResultDto>(ocr);
    }

    public async Task<List<OcrResultDto>> GetByDocumentIdAsync(
        Guid documentId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Verify document exists + ownership
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(documentId, cancellationToken);
        if (document == null)
            throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        var ocrResults = await _unitOfWork.OcrResults.GetByDocumentIdAsync(documentId, cancellationToken);
        return _mapper.Map<List<OcrResultDto>>(ocrResults);
    }
}
