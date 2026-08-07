using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.Words;

public sealed class UniqueWordsTestFixture : IAsyncLifetime
{
    private const string SeedResourceSuffix = "unique-words-seed.sql";

    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private PostgreSqlDatabaseLease? databaseLease;
    private ServiceProvider? rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var seedSql = await ReadEmbeddedSeedScriptAsync();

        databaseLease =
            ExternalReadOnlyDatabaseOptIn.TryLease(ExternalReadOnlyDatabaseOptIn.UniqueWordsConnectionVariable)
            ?? await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(UniqueWordsTestFixture));

        ConnectionString = databaseLease.ConnectionString;

        try
        {
            rootProvider = ownedProviders.Own(BuildServiceProvider());

            if (databaseLease.IsExternal)
            {
                return;
            }

            await using var scope = rootProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await SeedSliceAsync(dbContext, seedSql);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        rootProvider = null;
        await ownedProviders.DisposeAsync();

        if (databaseLease is not null)
        {
            await databaseLease.DisposeAsync();
            databaseLease = null;
        }
    }

    public AsyncServiceScope CreateScope()
    {
        if (rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(UniqueWordsTestFixture)} has not been initialized. Ensure it is used as a shared fixture (IClassFixture / collection fixture).");
        }

        return rootProvider.CreateAsyncScope();
    }

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

    private static async Task SeedSliceAsync(QuranDashboardDbContext dbContext, string sql)
    {
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
        var assembly = typeof(UniqueWordsTestFixture).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));

        await using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed script '{name}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
