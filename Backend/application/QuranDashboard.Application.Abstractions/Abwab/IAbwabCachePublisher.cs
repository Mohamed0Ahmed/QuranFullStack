namespace QuranDashboard.Application.Abstractions.Abwab;

// Cache publication for an audited write. Invoked ONLY after the transaction commits, so a rolled-back
// write never publishes. 028 ships a no-op default (no Abwab caches exist yet); 029+ bind real caches.
public interface IAbwabCachePublisher
{
    Task PublishAsync(AbwabCommitResult commit, CancellationToken cancellationToken);
}
