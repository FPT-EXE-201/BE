using FPT.EXE201.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Background worker that continuously dequeues OCR jobs and processes them.
/// Runs as a hosted service — starts with the app, stops on shutdown.
/// Uses IServiceScopeFactory to create scoped services (UnitOfWork, etc.) per job.
/// </summary>
public class OcrBackgroundService : BackgroundService
{
    private readonly IOcrJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OcrBackgroundService> _logger;

    public OcrBackgroundService(
        IOcrJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<OcrBackgroundService> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Background Service started. Waiting for jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            OcrJobItem? job = null;
            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Processing OCR job: DocumentId={DocumentId}, OcrResultId={OcrResultId}",
                    job.DocumentId, job.OcrResultId);

                // ⚠️ MUST create a new scope per job.
                // BackgroundService is Singleton, but MedicalRecordAiService/UnitOfWork are Scoped.
                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IMedicalRecordAiService>();

                if (job.IsReExtract)
                {
                    _logger.LogInformation(
                        "Re-extracting AI for OcrResultId={OcrResultId}", job.OcrResultId);
                    await aiService.ReExtractAsync(job.OcrResultId, job.UserId, stoppingToken);
                }
                else
                {
                    await aiService.ProcessDocumentAsync(
                        job.DocumentId, job.UserId, job.LanguageHint, stoppingToken);
                }

                _logger.LogInformation(
                    "OCR job completed: DocumentId={DocumentId}, IsReExtract={IsReExtract}",
                    job.DocumentId, job.IsReExtract);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App shutting down — normal exit
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OCR job failed: DocumentId={DocumentId}, OcrResultId={OcrResultId}. Error: {Message}",
                    job?.DocumentId, job?.OcrResultId, ex.Message);

                // ⚠️ ProcessDocumentAsync already sets OcrResult.Status = Failed internally.
                // No need to update here — just log and continue to next job.
            }
        }

        _logger.LogInformation("OCR Background Service stopped.");
    }
}
