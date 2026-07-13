using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Controllers.Words;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

public sealed class WordTypesTestFixture : IAsyncLifetime
{
    private const string RealDbConnectionEnvKey = "WORD_TYPES_REAL_DB_CONNECTION";
    private const string SeedResourceSuffix = "word-types-explorer-seed.sql";

    private readonly PostgreSqlContainer? _container;
    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<WordTypeGroupedDetailsController>? _apiFactory;
    private ServiceProvider? _rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public WordTypesTestFixture()
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
        _apiFactory?.Dispose();
        _apiFactory = null;

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
        var assembly = typeof(WordTypesTestFixture).Assembly;
        var name = assembly.GetManifestResourceNames()
            .First(resource => resource.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));

        await using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed script '{name}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
