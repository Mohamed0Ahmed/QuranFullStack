namespace QuranDashboard.Tests.Quran.MushafReader;

/// <summary>
/// Integration-test fixture for the Mushaf reader feature.
/// </summary>
/// <para>
/// Seeding strategy (pinned, G2): a <b>representative content slice</b> is loaded
/// from a committed, embedded SQL script (<c>mushaf-reader-seed.sql</c>) — NOT the
/// full DB and NOT the developer's local DB — covering exactly the pages / ayah /
/// word / sources the Mushaf reader tests assert on:
/// </para>
/// <list type="bullet">
/// <item>Pages 1, 5, 604 (lines + words, ordered).</item>
/// <item>Ayah <c>2:25</c> with tafsir <c>ar-muyassar</c> (grouped over 2:25–2:26),
/// translation <c>en-sahih-international</c>, and full i3rab <c>muyassar</c>.</item>
/// <item>Word <c>2:25:3</c> with head morphology, two ordered segments (one with an
/// empty segment form for the fallback test), and ordered/unique identity rows;
/// plus an ayah-end marker word for the marker-rejection test.</item>
/// </list>
/// <para>
/// Real-run escape hatch: set <c>MUSHAF_READER_REAL_DB_CONNECTION</c> to a live
/// connection string to run the reader tests against a fully-seeded local database
/// (no container, no slice seeding) — gated like Feature 009's real-run mode.
/// </para>
public sealed class MushafReaderTestFixture : IAsyncLifetime
{
    private const string RealDbConnectionEnvKey = "MUSHAF_READER_REAL_DB_CONNECTION";
    private const string SeedResourceSuffix = "mushaf-reader-seed.sql";

    private readonly PostgreSqlContainer? _container;
    private ServiceProvider? _rootProvider;

    public MushafReaderTestFixture()
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
            // migrate or seed the slice. Reader tests assert against the live data.
            return;
        }

        await dbContext.Database.MigrateAsync();
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
    /// Reader/handler registrations are added by their stories (T020–T041).
    /// </summary>
    public AsyncServiceScope CreateScope()
    {
        if (_rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(MushafReaderTestFixture)} has not been initialized. Ensure it is used as a shared fixture (IClassFixture / collection fixture).");
        }

        return _rootProvider.CreateAsyncScope();
    }

    /// <summary>
    /// Builds the root service provider with Application + Infrastructure registered
    /// and the MushafReader options bound from configuration.
    /// </summary>
    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                ["MushafReader:DefaultTafsirSourceKey"] = "ar-muyassar",
                ["MushafReader:DefaultTranslationSourceKey"] = "en-sahih-international",
                ["MushafReader:DefaultFullI3rabSourceKey"] = "muyassar",
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static async Task SeedSliceAsync(QuranDashboardDbContext dbContext)
    {
        var sql = await ReadEmbeddedSeedScriptAsync();
        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task<string> ReadEmbeddedSeedScriptAsync()
    {
        var assembly = typeof(MushafReaderTestFixture).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));

        await using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed script '{name}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
