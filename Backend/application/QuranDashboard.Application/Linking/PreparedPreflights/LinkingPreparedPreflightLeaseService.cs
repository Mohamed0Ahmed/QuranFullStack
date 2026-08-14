using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class LinkingPreparedPreflightLeaseService(
    IServiceScopeFactory scopeFactory,
    ILinkingScalabilityPolicy policy)
{
    public async Task RunHeartbeatAsync(
        LinkingPreparedPreflightLease lease,
        Action stopWork,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(policy.WorkerHeartbeat);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var renewed = await scope.ServiceProvider
                    .GetRequiredService<ILinkingPreparedPreflightStore>()
                    .RenewLeaseAsync(lease, cancellationToken);
                if (!renewed)
                {
                    throw new LinkingPreparedPreflightLeaseLostException();
                }
            }
        }
        catch
        {
            stopWork();
            throw;
        }
    }

    public async Task<bool> PublishProgressAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightStage stage,
        int processedSources,
        int processedAyahs,
        int? totalAyahs,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<ILinkingPreparedPreflightStore>()
            .PublishProgressAsync(
                lease,
                stage,
                processedSources,
                processedAyahs,
                totalAyahs,
                cancellationToken);
    }

    public async Task<bool> ProbeAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<ILinkingPreparedPreflightStore>()
            .ProbeLeaseAsync(lease, cancellationToken);
    }
}

internal sealed class LinkingPreparedPreflightLeaseLostException : Exception
{
    public LinkingPreparedPreflightLeaseLostException()
        : base("The prepared preflight lease is no longer owned by this attempt.")
    {
    }
}
