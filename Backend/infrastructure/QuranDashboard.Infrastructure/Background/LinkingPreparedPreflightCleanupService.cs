using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingPreparedPreflightCleanupService(
    IServiceScopeFactory scopeFactory,
    LinkingScalabilityOptions options,
    ILogger<LinkingPreparedPreflightCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.CleanupInterval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ILinkingPreparedPreflightStore>()
                    .RunMaintenanceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Prepared linking preflight cleanup failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
