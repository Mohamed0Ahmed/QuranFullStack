using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Tests.Quran.MushafReader;

public sealed class MushafReaderTestFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();
    private readonly PersistentTestDatabaseReader database = new();

    private WebApplicationFactory<HealthController>? apiFactory;
    private ServiceProvider? rootProvider;

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
        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
            apiFactory = null;
        }

        rootProvider = null;
        await ownedProviders.DisposeAsync();

        await database.DisposeAsync();
    }

    public AsyncServiceScope CreateScope()
    {
        if (rootProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(MushafReaderTestFixture)} has not been initialized. Ensure it is used as a shared fixture (IClassFixture / collection fixture).");
        }

        return rootProvider.CreateAsyncScope();
    }

    public HttpClient CreateClient()
    {
        apiFactory ??= SmokeApiHost.Build(
            database.BaseConnectionString,
            new FakeExternalUserProfileSource(),
            new TestSqlCommandCapture(),
            readOnlySharedState: true);
        return SmokeApiHost.CreateClient(apiFactory);
    }

    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = database.BaseConnectionString,
                ["MushafReader:DefaultTafsirSourceKey"] = "ar-muyassar",
                ["MushafReader:DefaultTranslationSourceKey"] = "en-sahih-international",
                ["MushafReader:DefaultFullI3rabSourceKey"] = "muyassar",
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(
                configuration,
                DatabaseActivityPolicy.Testing(DatabaseActivityProfile.ReadOnly, []))
            .BuildServiceProvider();
    }
}
