using Microsoft.EntityFrameworkCore.Metadata;

namespace QuranDashboard.Tests.Abwab._Fixtures;

// Shared real-PostgreSQL harness for the Abwab safety-foundation suite: it stands up a fresh
// container and applies the EF *migrations* (never EnsureCreated), so every Abwab test observes
// the real production schema/model instead of a synthesized one.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ServiceProvider? _provider;

    public string ConnectionString { get; private set; } = string.Empty;

    // Captured from the migrated context so reflection-based guards see the true production model.
    public IModel Model { get; private set; } = default!;

    public ServiceProvider Services => _provider
        ?? throw new InvalidOperationException(
            $"{nameof(PostgresFixture)} has not been initialized. Ensure it is used via {nameof(AbwabDbCollection)}.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        _provider = BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
        Model = dbContext.Model;
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
            _provider = null;
        }

        await _container.DisposeAsync();
    }

    private ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .BuildServiceProvider();
    }
}
