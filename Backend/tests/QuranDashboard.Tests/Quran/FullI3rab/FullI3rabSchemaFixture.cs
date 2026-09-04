using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class FullI3rabSchemaFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private string? scratchConnectionString;
    private ServiceProvider? rootProvider;

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(FullI3rabSchemaFixture),
            DestructiveRehearsalSubtype.CanonicalImport);

        try
        {
            rootProvider = ownedProviders.Own(BuildServiceProvider(scratchConnectionString));
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

        scratchConnectionString = null;
    }

    public AsyncServiceScope CreateScope()
    {
        if (rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FullI3rabSchemaFixture)} has not been initialized. Ensure it is used as a collection fixture.");
        }

        return rootProvider.CreateAsyncScope();
    }

    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
