using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab.Kernel._Support;

internal sealed class FixedServerClock(DateTimeOffset now) : IServerClock
{
    public DateTimeOffset UtcNow { get; } = now;
}

internal sealed class RecordingCachePublisher(Func<AbwabCommitResult, Task>? probe = null) : IAbwabCachePublisher
{
    private readonly List<AbwabCommitResult> _published = [];

    public IReadOnlyList<AbwabCommitResult> Published => _published;

    public async Task PublishAsync(AbwabCommitResult commit, CancellationToken cancellationToken)
    {
        if (probe is not null)
        {
            await probe(commit);
        }

        _published.Add(commit);
    }
}
