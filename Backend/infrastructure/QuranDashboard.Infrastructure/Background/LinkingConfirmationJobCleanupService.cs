using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingConfirmationJobCleanupService(
    IServiceScopeFactory scopeFactory,
    LinkingJobQueueSignal queueSignal,
    LinkingScalabilityOptions options,
    ILogger<LinkingConfirmationJobCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.CleanupInterval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ILinkingConfirmationJobStore>()
                    .RunMaintenanceAsync(stoppingToken);
                queueSignal.NotifyConfirmationJobQueued();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Linking confirmation job cleanup failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
