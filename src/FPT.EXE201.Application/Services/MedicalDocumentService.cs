using AutoMapper;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Application.DTOs.Timeline;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MedicalDocumentService : IMedicalDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOcrService _ocrService;

    public MedicalDocumentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService fileStorageService,
        IOcrService ocrService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
        _ocrService = ocrService;
    }

    public async Task<MedicalDocumentDto> CreateWithFilesAsync(
        Guid pregnancyId, CreateMedicalDocumentDto dto,
        IReadOnlyList<FileUploadInfo> files,
        Guid currentUserId, CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        if (files.Count == 0)
            throw new BadRequestException("At least one file is required.");
        if (dto.Title != null && dto.Title.Length > 200)
            throw new BadRequestException("Title must not exceed 200 characters.");
        if (dto.DocumentDate.HasValue && dto.DocumentDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BadRequestException("Document date cannot be in the future.");

        // 2. Verify pregnancy ownership
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this pregnancy.");

        // 3. Validate documentTypeId (if provided)
        if (dto.DocumentTypeId.HasValue)
        {
            var docType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
                dto.DocumentTypeId.Value, cancellationToken: cancellationToken);
            if (docType == null)
                throw new NotFoundException("Document type not found.");
        }

        // 4. Create MedicalDocument record (without StorageFileId — files via junction)
        var document = new MedicalDocument
        {
            PregnancyId = pregnancyId,
            DocumentTypeId = dto.DocumentTypeId,
            Title = dto.Title,
            DocumentDate = dto.DocumentDate,
            CapturedAt = DateTime.UtcNow,
            Source = dto.Source,
            Notes = dto.Notes
        };
        await _unitOfWork.MedicalDocuments.AddAsync(document, cancellationToken);

        // 5. Upload each file and create StorageFile + DocumentFile records
        var hasOcrCompatibleFile = false;
        for (var i = 0; i < files.Count; i++)
        {
            var fileInfo = files[i];

            var storageResult = await _fileStorageService.UploadAsync(
                fileInfo.Stream, fileInfo.FileName, fileInfo.ContentType,
                fileInfo.FileSize, currentUserId, cancellationToken);

            var storageFile = new StorageFile
            {
                OwnerUserId = currentUserId,
                StorageProvider = "supabase",
                ObjectKey = storageResult.ObjectKey,
                PublicUrl = storageResult.PublicUrl,
                OriginalFileName = storageResult.OriginalFileName,
                MimeType = storageResult.MimeType,
                FileSizeBytes = storageResult.FileSizeBytes,
                ChecksumSha256 = storageResult.ChecksumSha256,
                UploadedAt = DateTime.UtcNow
            };
            await _unitOfWork.StorageFiles.AddAsync(storageFile, cancellationToken);

            var documentFile = new DocumentFile
            {
                DocumentId = document.Id,
                StorageFileId = storageFile.Id,
                SortOrder = i + 1,
                PageLabel = files.Count > 1 ? $"Trang {i + 1}" : null
            };
            await _unitOfWork.DocumentFiles.AddAsync(documentFile, cancellationToken);

            if (fileInfo.ContentType.StartsWith("image/") || fileInfo.ContentType == "application/pdf")
                hasOcrCompatibleFile = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Handle post-upload processing based on document type
        if (dto.DocumentTypeId.HasValue && hasOcrCompatibleFile)
        {
            var uploadedDocType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
                dto.DocumentTypeId.Value, cancellationToken: cancellationToken);

            if (uploadedDocType != null)
            {
                if (uploadedDocType.Code == "PRENATAL_CHECKUP")
                {
                    // PRENATAL_CHECKUP → queue OCR + AI pipeline (non-blocking)
                    await _ocrService.QueueOcrAsync(document.Id, "vi", cancellationToken);
                }
                else if (IsTestCreatingType(uploadedDocType.Code))
                {
                    // Test types (BLOOD_TEST, ULTRASOUND, etc.) → tạo OcrResult trực tiếp
                    // với Status = Succeeded, bỏ qua OCR+AI vì không cần extract data.
                    // User sẽ nhập metadata thủ công qua Review → Confirm flow.
                    var ocrResult = new OcrResult
                    {
                        DocumentId = document.Id,
                        OcrRunNumber = 1,
                        Status = OcrStatus.Succeeded,
                        LanguageHint = "vi",
                        ConfidenceScore = 100m // User tự nhập → confidence = 100%
                    };
                    await _unitOfWork.OcrResults.AddAsync(ocrResult, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                // Others (PRESCRIPTION, VACCINATION_RECORD, etc.) → không tạo gì, archive only
            }
        }

        // 7. Reload with details for response
        var result = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(document.Id, cancellationToken);
        return _mapper.Map<MedicalDocumentDto>(result!);
    }

    public async Task<List<MedicalDocumentDto>> GetByPregnancyIdAsync(
        Guid pregnancyId, Guid currentUserId,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this pregnancy.");

        var documents = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, isFavorite, cancellationToken);
        return _mapper.Map<List<MedicalDocumentDto>>(documents);
    }

    public async Task<MedicalDocumentDto> GetByIdAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        return _mapper.Map<MedicalDocumentDto>(document);
    }

    public async Task<MedicalDocumentDto> UpdateAsync(
        Guid id, UpdateMedicalDocumentDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        // Verify visit belongs to same pregnancy (if provided)
        if (dto.VisitId.HasValue)
        {
            var visit = await _unitOfWork.PrenatalVisits.GetByIdAsync(dto.VisitId.Value,
                cancellationToken: cancellationToken);
            if (visit == null || visit.PregnancyId != document.PregnancyId)
                throw new BadRequestException("Visit not found or does not belong to this pregnancy.");
        }

        // Validate documentTypeId (if provided)
        if (dto.DocumentTypeId.HasValue)
        {
            var docType = await _unitOfWork.RefDocumentTypes.GetByIdAsync(
                dto.DocumentTypeId.Value, cancellationToken: cancellationToken);
            if (docType == null)
                throw new NotFoundException("Document type not found.");
        }

        _mapper.Map(dto, document);
        _unitOfWork.MedicalDocuments.Update(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<MedicalDocumentDto>(result!);
    }

    public async Task DeleteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        await _unitOfWork.MedicalDocuments.SoftDeleteAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MedicalDocumentDto> ToggleFavoriteAsync(
        Guid id, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        if (document == null)
            throw new NotFoundException("Medical document not found.");
        if (document.Pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this document.");

        document.IsFavorite = !document.IsFavorite;
        _unitOfWork.MedicalDocuments.Update(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _unitOfWork.MedicalDocuments.GetByIdWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<MedicalDocumentDto>(result!);
    }

    public async Task<List<TimelineEventDto>> GetTimelineAsync(
        Guid pregnancyId, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: cancellationToken);
        if (pregnancy == null)
            throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this pregnancy.");

        var events = new List<TimelineEventDto>();

        // Documents
        var documents = await _unitOfWork.MedicalDocuments
            .GetByPregnancyIdWithDetailsAsync(pregnancyId, null, cancellationToken);
        foreach (var doc in documents)
        {
            events.Add(new TimelineEventDto(
                EventType: "Document",
                EventId: doc.Id,
                EventDate: doc.DocumentDate?.ToDateTime(TimeOnly.MinValue) ?? doc.CapturedAt,
                Title: doc.Title ?? "Medical Document",
                Description: doc.Notes
            ));
        }

        // Visits (from Week 3)
        var visits = await _unitOfWork.PrenatalVisits.GetAllAsync(
            v => v.PregnancyId == pregnancyId, cancellationToken: cancellationToken);
        foreach (var visit in visits)
        {
            events.Add(new TimelineEventDto(
                EventType: "Visit",
                EventId: visit.Id,
                EventDate: visit.VisitDate.ToDateTime(TimeOnly.MinValue),
                Title: $"Prenatal Visit — {visit.VisitType}",
                Description: visit.Notes
            ));
        }

        // TODO: Future weeks — add weight logs, nutrition logs, etc.

        return events.OrderByDescending(e => e.EventDate).ToList();
    }

    // ═══ Helpers ═══

    /// <summary>
    /// Test-creating document types — cần tạo OcrResult (Status=Succeeded)
    /// để user có thể Review → Confirm → auto-create PrenatalTest.
    /// </summary>
    private static readonly HashSet<string> _testCreatingTypes = new()
    {
        "BLOOD_TEST", "URINE_TEST", "ULTRASOUND",
        "HIV_TEST", "HEPATITIS_B_TEST", "THYROID_TEST",
        "GLUCOSE_TEST", "CBC_TEST", "NT_SCAN"
    };

    private static bool IsTestCreatingType(string code) => _testCreatingTypes.Contains(code);
}
