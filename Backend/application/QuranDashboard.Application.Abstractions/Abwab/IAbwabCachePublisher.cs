namespace QuranDashboard.Application.Abstractions.Abwab;

// Invoked ONLY after the transaction commits, so a rolled-back write never publishes.
public interface IAbwabCachePublisher
{
    Task PublishAsync(AbwabCommitResult commit, CancellationToken cancellationToken);
}
