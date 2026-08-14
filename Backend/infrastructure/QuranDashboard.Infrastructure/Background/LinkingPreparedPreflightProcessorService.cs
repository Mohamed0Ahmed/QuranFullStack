using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingPreparedPreflightProcessorService(
    IServiceScopeFactory scopeFactory,
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
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<ILinkingPreparedPreflightProcessor>();
                if (!await processor.ProcessOneAsync(cancellationToken))
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
                logger.LogError(exception, "Prepared linking preflight processing failed.");
                await Task.Delay(options.PollAfterMilliseconds, cancellationToken);
            }
        }
    }
}
