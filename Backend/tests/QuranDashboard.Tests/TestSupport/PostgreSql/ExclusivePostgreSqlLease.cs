using DotNet.Testcontainers.Containers;

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

    internal Task<ExecResult> ExecAsync(IList<string> command, CancellationToken cancellationToken = default)
    {
        return container.ExecAsync(command, cancellationToken);
    }

    internal static async Task<ExclusivePostgreSqlLease> AcquireAsync(
        string owner,
        string image,
        Action release,
        Func<PostgreSqlBuilder, PostgreSqlBuilder>? configureContainer = null,
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
            if (configureContainer is not null)
            {
                builder = configureContainer(builder);
            }

            // After the caller's configuration, never before: cleanup-test-runtime selects containers by all
            // five labels, so a caller must not be able to overwrite one of them.
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
