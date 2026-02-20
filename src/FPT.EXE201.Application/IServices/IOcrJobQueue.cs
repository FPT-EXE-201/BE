namespace FPT.EXE201.Application.IServices;

/// <summary>
/// In-process job queue for OCR processing.
/// Uses System.Threading.Channels internally — no external dependencies.
/// </summary>
public interface IOcrJobQueue
{
    /// <summary>Enqueue a document for OCR + AI processing. Returns immediately.</summary>
    ValueTask EnqueueAsync(OcrJobItem job, CancellationToken cancellationToken = default);

    /// <summary>Dequeue next job (blocks until available). Used by BackgroundService.</summary>
    ValueTask<OcrJobItem> DequeueAsync(CancellationToken cancellationToken);
}

/// <summary>Job payload for background OCR processing.</summary>
public record OcrJobItem(
    Guid OcrResultId,
    Guid DocumentId,
    Guid UserId,
    string LanguageHint = "vi",
    bool IsReExtract = false
);
