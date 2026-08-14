using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed record LinkingScalabilityOptions : ILinkingScalabilityPolicy
{
    public const string SectionName = "LinkingScalability";

    public int PageSizeMaximum { get; init; } = 100;
    public long CompactSourceCacheBudgetReferences { get; init; } = 60_000;
    public long AyahTextCacheBudgetReferences { get; init; } = 60_000;
    public TimeSpan CacheSlidingExpiration { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan CacheAbsoluteExpiration { get; init; } = TimeSpan.FromHours(4);
    public int PreflightProcessorConcurrency { get; init; } = 2;
    public int ConfirmationProcessorConcurrency { get; init; } = 2;
    public int ActiveWorkflowsPerActor { get; init; } = 4;
    public int PersistenceBatchSize { get; init; } = 500;
    public TimeSpan WorkerLease { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan WorkerHeartbeat { get; init; } = TimeSpan.FromSeconds(30);
    public int MaximumAutomaticAttempts { get; init; } = 3;
    public int PollAfterMilliseconds { get; init; } = 1_500;
    public TimeSpan ReadyPreflightLifetime { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan AbandonedPreflightLifetime { get; init; } = TimeSpan.FromHours(2);
    public TimeSpan TerminalRetention { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(5);

    public MemoryCacheEntryOptions CacheEntry(long size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return new MemoryCacheEntryOptions
        {
            Size = size,
            SlidingExpiration = CacheSlidingExpiration,
            AbsoluteExpirationRelativeToNow = CacheAbsoluteExpiration,
        };
    }

    public MemoryCacheEntryOptions CacheEntry(long size, DateTimeOffset absoluteExpiration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return new MemoryCacheEntryOptions
        {
            Size = size,
            SlidingExpiration = CacheSlidingExpiration,
            AbsoluteExpiration = absoluteExpiration,
        };
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(PageSizeMaximum, 100);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(CompactSourceCacheBudgetReferences);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AyahTextCacheBudgetReferences);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(CacheSlidingExpiration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(CacheAbsoluteExpiration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(CacheSlidingExpiration, CacheAbsoluteExpiration);
        ArgumentOutOfRangeException.ThrowIfLessThan(PreflightProcessorConcurrency, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(ConfirmationProcessorConcurrency, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(ActiveWorkflowsPerActor, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(PersistenceBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(WorkerLease, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(WorkerHeartbeat, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(WorkerHeartbeat, WorkerLease);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaximumAutomaticAttempts, 1);
        if (PollAfterMilliseconds is < 1_000 or > 5_000)
        {
            throw new ArgumentOutOfRangeException(nameof(PollAfterMilliseconds));
        }
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ReadyPreflightLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(AbandonedPreflightLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(TerminalRetention, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(CleanupInterval, TimeSpan.Zero);
    }
}
