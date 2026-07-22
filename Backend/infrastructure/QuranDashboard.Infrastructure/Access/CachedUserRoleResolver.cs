using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class CachedUserRoleResolver(IMemoryCache cache, QuranDashboardDbContext db) : IUserRoleResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string KeyPrefix = "access:role:";

    // Static so an Evict on one scoped resolver invalidates entries the singleton cache holds from other
    // requests; each source snapshots its token so a read never touches a source a concurrent Evict disposed.
    private static readonly ConcurrentDictionary<string, EvictionSource> EvictionSources = new();

    public async Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct)
    {
        var key = KeyPrefix + logtoSub;
        if (cache.TryGetValue(key, out CachedRole? cached) && cached is not null)
        {
            return cached.Name;
        }

        // Capture the eviction token BEFORE the read so a concurrent Evict makes the Set store an
        // already-expired entry instead of re-caching a stale value.
        var evictionToken = EvictionSources.GetOrAdd(logtoSub, static _ => new EvictionSource()).ChangeToken;

        var roleName = await db.AccessUsers
            .AsNoTracking()
            .Where(u => u.LogtoSub == logtoSub && u.Status == UserStatus.Active && u.RoleId != null)
            .Select(u => u.Role!.Name)
            .SingleOrDefaultAsync(ct);

        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl }
            .AddExpirationToken(evictionToken);
        cache.Set(key, new CachedRole(roleName), options);
        return roleName;
    }

    public void Evict(string logtoSub)
    {
        cache.Remove(KeyPrefix + logtoSub);

        // Cancel the current token (kills the pending Set of any in-flight read that captured it), then
        // install a fresh one; Cancel/Dispose are idempotent so racing Evicts stay safe.
        var replacement = new EvictionSource();
        EvictionSources.AddOrUpdate(
            logtoSub,
            replacement,
            (_, previous) =>
            {
                previous.Cancel();
                return replacement;
            });
    }

    private sealed record CachedRole(string? Name);

    private sealed class EvictionSource
    {
        private readonly CancellationTokenSource _cts = new();

        public EvictionSource() => ChangeToken = new CancellationChangeToken(_cts.Token);

        public CancellationChangeToken ChangeToken { get; }

        public void Cancel()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
