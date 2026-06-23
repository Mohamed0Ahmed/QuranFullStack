using Microsoft.Extensions.Logging;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsRoots;

/// <summary>
/// Integration-test fixture for the Roots Explorer feature. Mirrors the Feature
/// 014 <c>UniqueWordsTestFixture</c>: a representative content slice is loaded
/// from a committed, embedded SQL script (<c>roots-explorer-seed.sql</c>) — NOT
/// the full DB and NOT the developer's local DB. Canonical Quranic Uthmani text
/// is used verbatim; no text is invented or altered.
/// </summary>
/// <para>
/// Real-run escape hatch: set <c>ROOTS_EXPLORER_REAL_DB_CONNECTION</c> to a live
/// connection string to run the tests against a fully-seeded local database
/// (no container, no slice seeding).
/// </para>
public sealed class RootsExplorerTestFixture : IAsyncLifetime
{
    private const string RealDbConnectionEnvKey = "ROOTS_EXPLORER_REAL_DB_CONNECTION";
    private const string SeedResourceSuffix = "roots-explorer-seed.sql";

    private readonly PostgreSqlContainer? _container;
    private ServiceProvider? _rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public RootsExplorerTestFixture()
    {
        var realDb = Environment.GetEnvironmentVariable(RealDbConnectionEnvKey);
        if (!string.IsNullOrWhiteSpace(realDb))
        {
            ConnectionString = realDb;
            IsRealDb = true;
            return;
        }

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        IsRealDb = false;
    }

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsRealDb { get; }

    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        // Single owned root provider for the whole fixture; disposed in
        // DisposeAsync so neither providers nor scopes leak across tests.
        _rootProvider = BuildServiceProvider();

        await using var scope = _rootProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        if (IsRealDb)
        {
            // The real database is the source of truth in real-run mode: do not
            // migrate or seed the slice. Tests assert against the live data.
            return;
        }

        // EnsureCreated builds the current EF model snapshot; the slice seed is
        // then applied via raw SQL. See the Unique Words fixture for why
        // MigrateAsync is not used here.
        await dbContext.Database.EnsureCreatedAsync();
        await SeedSliceAsync(dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_rootProvider is not null)
        {
            await _rootProvider.DisposeAsync();
            _rootProvider = null;
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Opens a new DI scope from the fixture's root provider for a single test.
    /// The caller MUST dispose the returned scope (e.g. <c>await using var</c>).
    /// </summary>
    public AsyncServiceScope CreateScope()
    {
        if (_rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RootsExplorerTestFixture)} has not been initialized. Ensure it is used as a shared fixture (IClassFixture / collection fixture).");
        }

        return _rootProvider.CreateAsyncScope();
    }

    /// <summary>
    /// Builds the root service provider with Application + Infrastructure
    /// registered against the container (or real) connection string.
    /// </summary>
    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerProvider>(LoggingProvider)
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static async Task SeedSliceAsync(QuranDashboardDbContext dbContext)
    {
        var sql = await ReadEmbeddedSeedScriptAsync();

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadEmbeddedSeedScriptAsync()
    {
        var assembly = typeof(RootsExplorerTestFixture).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));

        await using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed script '{name}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
