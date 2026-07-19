using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

// Short-TTL cache in front of the DB, keyed by the Logto sub. Only an Active user with an assigned role
// yields a role name; every other case (no user, not Active, no role) resolves to null and is cached
// too, so a role-less caller does not hit the DB on every request. The TTL only bounds staleness for
// out-of-band DB edits that never call Evict — a role/status change calls Evict for immediate, race-free
// correctness (see the eviction-token handling below).
public sealed class CachedUserRoleResolver(IMemoryCache cache, QuranDashboardDbContext db) : IUserRoleResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string KeyPrefix = "access:role:";

    // One eviction source per subject (bounded by the user count). Static so an Evict on a scoped resolver
    // invalidates an entry the singleton IMemoryCache holds from another request's resolver instance. Each
    // source snapshots its change token at construction, so a read never reads Token off a source that a
    // concurrent Evict may have disposed.
    private static readonly ConcurrentDictionary<string, EvictionSource> EvictionSources = new();

    public async Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct)
    {
        var key = KeyPrefix + logtoSub;
        if (cache.TryGetValue(key, out CachedRole? cached) && cached is not null)
        {
            return cached.Name;
        }

        // Capture the eviction token BEFORE the read: a concurrent Evict cancels it, and the Set below then
        // stores an already-expired entry instead of re-caching the stale value for the TTL window.
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

        // Cancel the subject's current token — killing the pending Set of any in-flight read that captured
        // it — and install a fresh one for subsequent reads. Cancel/Dispose are idempotent, so racing
        // Evicts stay safe.
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

    // Wraps the nullable role name so a cached negative (Name == null) is distinguishable from a cache
    // miss (TryGetValue returns false) without a sentinel string.
    private sealed record CachedRole(string? Name);

    // Pairs a CancellationTokenSource with a change token snapshotted at construction. The cache observes
    // cancellation through the token; because the token is captured up front, a read never calls Token on a
    // source a concurrent Evict may already have disposed.
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
