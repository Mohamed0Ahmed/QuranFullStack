using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Controllers.Words;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

public sealed class WordTypesTestFixture : IAsyncLifetime
{
    private const string SeedResourceSuffix = "word-types-explorer-seed.sql";

    private readonly OwnedServiceProviderRegistry _ownedProviders = new();
    private readonly object _apiFactoryLock = new();
    private PostgreSqlDatabaseLease? _databaseLease;
    private WebApplicationFactory<WordTypeGroupedDetailsController>? _apiFactory;
    private ServiceProvider? _rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var seedSql = await ReadEmbeddedSeedScriptAsync();

        _databaseLease =
            ExternalReadOnlyDatabaseOptIn.TryLease(ExternalReadOnlyDatabaseOptIn.WordTypesConnectionVariable)
            ?? await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(WordTypesTestFixture));

        ConnectionString = _databaseLease.ConnectionString;

        try
        {
            _rootProvider = _ownedProviders.Own(BuildServiceProvider());

            if (_databaseLease.IsExternal)
            {
                return;
            }

            await using var scope = _rootProvider.CreateAsyncScope();
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
        if (_apiFactory is not null)
        {
            await _apiFactory.DisposeAsync();
            _apiFactory = null;
        }

        _rootProvider = null;
        await _ownedProviders.DisposeAsync();

        if (_databaseLease is not null)
        {
            await _databaseLease.DisposeAsync();
            _databaseLease = null;
        }
    }

    public AsyncServiceScope CreateScope()
    {
        if (_rootProvider is null)
        {
            throw new InvalidOperationException($"{nameof(WordTypesTestFixture)} has not been initialized.");
        }

        return _rootProvider.CreateAsyncScope();
    }

    public HttpClient CreateApiClient()
    {
        // Guard the lazy init so concurrent callers reuse the single factory instead of racing to
        // construct (and leak) multiple WebApplicationFactory instances.
        WebApplicationFactory<WordTypeGroupedDetailsController> factory;
        lock (_apiFactoryLock)
        {
            factory = _apiFactory ??= new WebApplicationFactory<WordTypeGroupedDetailsController>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                            // ConnectionString can be an external read-only opt-in database this process
                            // does not own; the startup catalogue sync writes, so it must stay off.
                            ["Access:PermissionCatalogueStartupSync:Enabled"] = "false",
                        }));
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<QuranDashboardDbContext>();
                        services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                        services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));
                    });
                });
        }

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
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
        var assembly = typeof(WordTypesTestFixture).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(resource => resource.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));

        await using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed script '{name}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
