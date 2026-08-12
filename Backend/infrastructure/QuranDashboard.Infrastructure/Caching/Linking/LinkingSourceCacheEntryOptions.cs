using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed record LinkingSourceCacheEntryOptions
{
    public const string SectionName = "LinkingSourceCache";

    public TimeSpan SlidingExpiration { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan AbsoluteExpirationRelativeToNow { get; init; } = TimeSpan.FromHours(4);

    public long ResolvedSourceSizeLimitAyahs { get; init; } = 60_000;

    public long AyahTextSizeLimitAyahs { get; init; } = 6_500;

    public MemoryCacheEntryOptions Entry(long size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return new MemoryCacheEntryOptions
        {
            Size = size,
            SlidingExpiration = SlidingExpiration,
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
        };
    }

    public MemoryCacheEntryOptions Entry(long size, DateTimeOffset absoluteExpiration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return new MemoryCacheEntryOptions
        {
            Size = size,
            SlidingExpiration = SlidingExpiration,
            AbsoluteExpiration = absoluteExpiration,
        };
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(SlidingExpiration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(AbsoluteExpirationRelativeToNow, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(SlidingExpiration, AbsoluteExpirationRelativeToNow);
        ArgumentOutOfRangeException.ThrowIfLessThan(
            ResolvedSourceSizeLimitAyahs,
            LinkingLimits.MaxResolvedAyahs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AyahTextSizeLimitAyahs);
    }
}
