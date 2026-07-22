using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Infrastructure.Security.Caching;

public sealed class EffectivePermissionCache(IMemoryCache cache) : IEffectivePermissionCache
{
    private long _generation;

    public Task<IReadOnlyList<string>?> GetAsync(string subject, CancellationToken cancellationToken) =>
        Task.FromResult(cache.TryGetValue(KeyFor(subject), out IReadOnlyList<string>? cached) ? cached : null);

    public Task SetAsync(string subject, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        cache.Set(KeyFor(subject), permissions, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    public void Invalidate() => Interlocked.Increment(ref _generation);

    private string KeyFor(string subject) => $"qd.permissions.v{Interlocked.Read(ref _generation)}.{subject}";
}
