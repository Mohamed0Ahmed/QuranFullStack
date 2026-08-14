using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingConfirmationJobProcessorService(
    IServiceScopeFactory scopeFactory,
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
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ILinkingConfirmationJobProcessor>();
                if (!await processor.ProcessNextAsync(cancellationToken))
                {
                    await Task.Delay(options.PollAfterMilliseconds, cancellationToken);
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
}
