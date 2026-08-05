namespace QuranDashboard.Tests.TestSupport.DependencyInjection;

internal sealed class OwnedServiceProviderRegistry : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> owned = [];
    private readonly List<Exception> disposalFailures = [];
    private readonly Lock gate = new();

    private int disposed;

    internal IReadOnlyList<Exception> DisposalFailures
    {
        get
        {
            lock (gate)
            {
                return [.. disposalFailures];
            }
        }
    }

    internal T Own<T>(T disposable)
        where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);

        lock (gate)
        {
            owned.Add(disposable);
        }

        return disposable;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        List<IAsyncDisposable> pending;
        lock (gate)
        {
            pending = [.. owned];
            owned.Clear();
        }

        pending.Reverse();

        foreach (var disposable in pending)
        {
            try
            {
                await disposable.DisposeAsync();
            }
            catch (Exception exception)
            {
                lock (gate)
                {
                    disposalFailures.Add(exception);
                }

                Console.Error.WriteLine(
                    $"[owned-providers] disposing {disposable.GetType().Name} failed: {exception.Message}");
            }
        }
    }
}
