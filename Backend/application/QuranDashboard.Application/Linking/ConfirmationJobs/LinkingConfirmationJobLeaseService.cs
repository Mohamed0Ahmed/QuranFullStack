using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class LinkingConfirmationJobLeaseService(
    IServiceScopeFactory scopeFactory,
    ILinkingScalabilityPolicy policy)
{
    public async Task RunHeartbeatAsync(
        LinkingConfirmationJobLease lease,
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
                    .GetRequiredService<ILinkingConfirmationJobStore>()
                    .RenewLeaseAsync(lease, cancellationToken);
                if (!renewed)
                {
                    throw new LinkingConfirmationJobLeaseLostException();
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
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJobStage stage,
        int processedItems,
        int totalItems,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<ILinkingConfirmationJobStore>()
            .PublishProgressAsync(
                lease,
                stage,
                processedItems,
                totalItems,
                cancellationToken);
    }
}

internal sealed class LinkingConfirmationJobLeaseLostException : Exception;
