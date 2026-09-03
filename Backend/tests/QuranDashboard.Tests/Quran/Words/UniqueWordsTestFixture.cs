using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Tests.Quran.Words;

public sealed class UniqueWordsTestFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();
    private readonly PersistentTestDatabaseReader database = new();

    private ServiceProvider? rootProvider;

    public RecordingLoggerProvider LoggingProvider { get; } = new();

    public string ConnectionString => database.ReadOnlyConnectionString;

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();

        try
        {
            rootProvider = ownedProviders.Own(BuildServiceProvider());
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

        await database.DisposeAsync();
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
                ["ConnectionStrings:QuranDashboardDb"] = database.BaseConnectionString,
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
