using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Infrastructure.Abwab.Caching;

// 028 has no Abwab caches to invalidate yet, but the post-commit publication point must already exist so
// the commit protocol calls it (only after commit) and 029+ can bind real caches without moving the hook.
public sealed class NullAbwabCachePublisher : IAbwabCachePublisher
{
    public Task PublishAsync(AbwabCommitResult commit, CancellationToken cancellationToken) => Task.CompletedTask;
}
