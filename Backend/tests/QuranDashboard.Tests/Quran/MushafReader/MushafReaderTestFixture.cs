namespace QuranDashboard.Tests.Quran.MushafReader;

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

        _rootProvider = BuildServiceProvider();

        await using var scope = _rootProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        if (IsRealDb)
        {

            return;
        }

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

    public AsyncServiceScope CreateScope()
    {
        if (_rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(MushafReaderTestFixture)} has not been initialized. Ensure it is used as a shared fixture (IClassFixture / collection fixture).");
        }

        return _rootProvider.CreateAsyncScope();
    }

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

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
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
