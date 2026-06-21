using QuranDashboard.Application.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class ImportTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ServiceProvider? _rootProvider;

    public string SourceRoot { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        SourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "resources", "import-sources", "quran-foundation"));

        if (!Directory.Exists(SourceRoot))
        {
            throw new DirectoryNotFoundException($"Import source staging tree was not found: {SourceRoot}");
        }

        // Single owned root provider for the whole fixture; disposed in
        // DisposeAsync so neither providers nor scopes leak across tests or
        // per-call rebuilds. Mirrors MushafReaderTestFixture's provider strategy.
        _rootProvider = BuildServiceProvider();

        await using var scope = _rootProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_rootProvider is not null)
        {
            await _rootProvider.DisposeAsync();
            _rootProvider = null;
        }

        await postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Returns the fixture's single shared root service provider. Callers should
    /// resolve scoped services through <c>CreateAsyncScope()</c> and dispose that
    /// scope (the root provider is owned and disposed by the fixture).
    /// </summary>
    public ServiceProvider CreateServiceProvider()
    {
        if (_rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ImportTestFixture)} has not been initialized. Ensure it is used as a shared fixture (ICollectionFixture / collection fixture).");
        }

        return _rootProvider;
    }

    public async Task<ImportQuranFoundationHandler> CreateHandlerAsync()
    {
        await TruncateQuranTablesAsync();
        return CreateHandlerWithoutTruncate();
    }

    public ImportQuranFoundationHandler CreateHandlerWithoutTruncate()
    {
        return CreateServiceProvider().GetRequiredService<ImportQuranFoundationHandler>();
    }

    public async Task TruncateQuranTablesAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE;");
    }

    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
