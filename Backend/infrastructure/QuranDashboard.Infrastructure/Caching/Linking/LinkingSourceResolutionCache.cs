using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class LinkingSourceResolutionCache : IDisposable
{
    private const int MaxSharedLoadAttempts = 2;

    private readonly LinkingSourceCacheEntryOptions _options;
    private readonly MemoryCache _cache;
    private readonly Lock _pendingLock = new();

    public LinkingSourceResolutionCache(LinkingSourceCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.ResolvedSourceSizeLimitAyahs,
        });
    }

    public async Task<LinkingResolvedSourceCompact> GetOrLoadAsync(
        string key,
        string sourceIdentity,
        Func<CancellationToken, Task<LinkingResolvedSourceCompact>> load,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(sourceIdentity);
        ArgumentNullException.ThrowIfNull(load);

        for (var attempt = 0; attempt < MaxSharedLoadAttempts; attempt++)
        {
            var pending = GetOrStartAsync(key, load, cancellationToken, out var initiatedHere);
            LinkingResolvedSourceCompact compact;

            try
            {
                compact = await pending;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                continue;
            }
            catch (ObjectDisposedException) when (!initiatedHere)
            {
                continue;
            }

            if (string.Equals(compact.SourceIdentity, sourceIdentity, StringComparison.Ordinal))
            {
                return compact;
            }

            RemoveIfCurrent(key, pending);
        }

        return await load(cancellationToken);
    }

    public void Dispose() => _cache.Dispose();

    private Task<LinkingResolvedSourceCompact> GetOrStartAsync(
        string key,
        Func<CancellationToken, Task<LinkingResolvedSourceCompact>> load,
        CancellationToken cancellationToken,
        out bool initiatedHere)
    {
        initiatedHere = false;

        if (_cache.TryGetValue(key, out Task<LinkingResolvedSourceCompact>? pending) && pending is not null)
        {
            return pending;
        }

        lock (_pendingLock)
        {
            if (_cache.TryGetValue(key, out pending) && pending is not null)
            {
                return pending;
            }

            initiatedHere = true;

            var completion = new TaskCompletionSource<LinkingResolvedSourceCompact>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _ = _cache.Set(key, completion.Task, _options.Entry(LinkingLimits.MaxResolvedAyahs));
            _ = RunAsync(key, completion, load, cancellationToken);

            return completion.Task;
        }
    }

    private async Task RunAsync(
        string key,
        TaskCompletionSource<LinkingResolvedSourceCompact> completion,
        Func<CancellationToken, Task<LinkingResolvedSourceCompact>> load,
        CancellationToken cancellationToken)
    {
        try
        {
            var compact = await load(cancellationToken);

            lock (_pendingLock)
            {
                _ = _cache.Set(key, completion.Task, _options.Entry(Math.Max(1, compact.AyahCount)));
            }

            completion.SetResult(compact);
        }
        catch (OperationCanceledException exception)
        {
            RemoveIfCurrent(key, completion.Task);
            completion.SetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            RemoveIfCurrent(key, completion.Task);
            completion.SetException(exception);
        }
    }

    private void RemoveIfCurrent(string key, Task<LinkingResolvedSourceCompact> owned)
    {
        lock (_pendingLock)
        {
            if (_cache.TryGetValue(key, out Task<LinkingResolvedSourceCompact>? current)
                && ReferenceEquals(current, owned))
            {
                _cache.Remove(key);
            }
        }
    }
}
