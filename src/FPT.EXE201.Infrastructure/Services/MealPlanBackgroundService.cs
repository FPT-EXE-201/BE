using FPT.EXE201.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Background worker that continuously dequeues meal plan generation jobs.
/// Same pattern as OcrBackgroundService (Week 5).
/// Runs as a hosted service — starts with the app, stops on shutdown.
/// </summary>
public class MealPlanBackgroundService : BackgroundService
{
    private readonly IMealPlanJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MealPlanBackgroundService> _logger;

    public MealPlanBackgroundService(
        IMealPlanJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<MealPlanBackgroundService> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MealPlan Background Service started. Waiting for jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            MealPlanJobItem? job = null;
            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Processing meal plan job: MealPlanId={MealPlanId}, Weeks={Weeks}",
                    job.MealPlanId, job.DurationWeeks);

                using var scope = _scopeFactory.CreateScope();
                var mealPlanService = scope.ServiceProvider.GetRequiredService<IMealPlanService>();

                await mealPlanService.ProcessGenerationAsync(job, stoppingToken);

                _logger.LogInformation(
                    "Meal plan job completed: MealPlanId={MealPlanId}",
                    job.MealPlanId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Meal plan job failed: MealPlanId={MealPlanId}. Error: {Message}",
                    job?.MealPlanId, ex.Message);

                // ProcessGenerationAsync already sets MealPlan.Status = Failed internally.
            }
        }

        _logger.LogInformation("MealPlan Background Service stopped.");
    }
}
