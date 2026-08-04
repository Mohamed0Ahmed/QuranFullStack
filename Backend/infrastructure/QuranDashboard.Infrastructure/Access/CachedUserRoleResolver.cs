using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class CachedUserRoleResolver(IMemoryCache cache, QuranDashboardDbContext db) : IUserRoleResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string KeyPrefix = "access:role:";

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
