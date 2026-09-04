using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

public sealed class WordTypesTestFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry _ownedProviders = new();
    private readonly PersistentTestDatabaseReader _database = new();
    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<HealthController>? _apiFactory;
    private ServiceProvider? _rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public string ConnectionString => _database.ReadOnlyConnectionString;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        try
        {
            _rootProvider = _ownedProviders.Own(BuildServiceProvider());
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

        await _database.DisposeAsync();
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
        WebApplicationFactory<HealthController> factory;
        lock (_apiFactoryLock)
        {
            factory = _apiFactory ??= SmokeApiHost.Build(
                _database.BaseConnectionString,
                new FakeExternalUserProfileSource(),
                new TestSqlCommandCapture(),
                readOnlySharedState: true);
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
                ["ConnectionStrings:QuranDashboardDb"] = _database.BaseConnectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerProvider>(LoggingProvider)
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(
                configuration,
                DatabaseActivityPolicy.Testing(DatabaseActivityProfile.ReadOnly, []))
            .BuildServiceProvider();
    }
}
