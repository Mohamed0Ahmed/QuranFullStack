using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingConfirmationJobProcessorService(
    IServiceScopeFactory scopeFactory,
    LinkingJobQueueSignal queueSignal,
    LinkingScalabilityOptions options,
    ILogger<LinkingConfirmationJobProcessorService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, options.ConfirmationProcessorConcurrency)
            .Select(_ => RunWorkerAsync(stoppingToken)));

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await ProcessNextAsync(cancellationToken))
                {
                    await queueSignal.WaitForConfirmationJobAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Linking confirmation job processing failed.");
                await Task.Delay(options.PollAfterMilliseconds, cancellationToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<ILinkingConfirmationJobProcessor>()
            .ProcessNextAsync(cancellationToken);
    }
}
