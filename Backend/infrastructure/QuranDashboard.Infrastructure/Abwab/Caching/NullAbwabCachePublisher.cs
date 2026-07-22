using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Infrastructure.Abwab.Caching;

public sealed class NullAbwabCachePublisher : IAbwabCachePublisher
{
    public Task PublishAsync(AbwabCommitResult commit, CancellationToken cancellationToken) => Task.CompletedTask;
}
