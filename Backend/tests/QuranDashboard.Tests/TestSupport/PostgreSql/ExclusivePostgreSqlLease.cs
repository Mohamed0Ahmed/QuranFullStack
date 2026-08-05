namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class ExclusivePostgreSqlLease : IAsyncDisposable
{
    private readonly PostgreSqlContainer container;
    private readonly CrossProcessPostgreSqlLock crossProcessLock;
    private readonly Action release;

    private int disposed;

    private ExclusivePostgreSqlLease(
        string image,
        PostgreSqlContainer container,
        CrossProcessPostgreSqlLock crossProcessLock,
        Action release)
    {
        Image = image;
        this.container = container;
        this.crossProcessLock = crossProcessLock;
        this.release = release;
    }

    internal string Image { get; }

    internal Guid ServerInstanceId { get; } = Guid.NewGuid();

    internal string ConnectionString => container.GetConnectionString();

    internal static async Task<ExclusivePostgreSqlLease> AcquireAsync(
        string owner,
        string image,
        Action release,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(image);

        var crossProcessLock = await CrossProcessPostgreSqlLock.AcquireProjectLockAsync(
            $"pid {Environment.ProcessId} exclusive {image} for {owner}",
            cancellationToken);

        try
        {
            var builder = new PostgreSqlBuilder().WithImage(image);
            foreach (var label in PostgreSqlResourceLabels.ForPostgreSql())
            {
                builder = builder.WithLabel(label.Key, label.Value);
            }

            var container = builder.Build();
            try
            {
                await container.StartAsync(cancellationToken);
            }
            catch
            {
                await container.DisposeAsync();
                throw;
            }

            return new ExclusivePostgreSqlLease(image, container, crossProcessLock, release);
        }
        catch
        {
            crossProcessLock.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await container.DisposeAsync();
        }
        finally
        {
            crossProcessLock.Dispose();
            release();
        }
    }
}
