using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingPreparedPreflightProcessorService(
    IServiceScopeFactory scopeFactory,
    LinkingJobQueueSignal queueSignal,
    LinkingScalabilityOptions options,
    ILogger<LinkingPreparedPreflightProcessorService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, options.PreflightProcessorConcurrency)
            .Select(_ => RunWorkerAsync(stoppingToken)));

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await ProcessOneAsync(cancellationToken))
                {
                    await queueSignal.WaitForPreparedPreflightAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Prepared linking preflight processing failed.");
                await Task.Delay(options.PollAfterMilliseconds, cancellationToken);
            }
        }
    }

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<ILinkingPreparedPreflightProcessor>()
            .ProcessOneAsync(cancellationToken);
    }
}
