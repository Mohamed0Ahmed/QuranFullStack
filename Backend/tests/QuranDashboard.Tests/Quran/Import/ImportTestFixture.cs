using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestSupport.DependencyInjection;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class ImportTestFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private ServiceProvider? rootProvider;

    public string SourceRoot => FoundationImportSourceGate.SourceRoot;

    public async Task InitializeAsync()
    {
        if (FoundationImportSourceGate.IsMissing)
        {
            return;
        }

        try
        {
            var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(
                QuranDashboard.Tests.TestRuntime.TestRuntimeTestPaths.ContractPath);
            rootProvider = ownedProviders.Own(BuildServiceProvider(scratch.ConnectionString));

            await using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await dbContext.Database.MigrateAsync();
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
    }

    public AsyncServiceScope CreateScope()
    {
        return InitializedRootProvider.CreateAsyncScope();
    }

    public async Task<ImportQuranFoundationHandler> CreateHandlerAsync()
    {
        await TruncateQuranTablesAsync();
        return CreateHandlerWithoutTruncate();
    }

    public ImportQuranFoundationHandler CreateHandlerWithoutTruncate()
    {
        return InitializedRootProvider.GetRequiredService<ImportQuranFoundationHandler>();
    }

    public async Task TruncateQuranTablesAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE;");
    }

    private ServiceProvider InitializedRootProvider =>
        rootProvider ?? throw new InvalidOperationException(
            $"{nameof(ImportTestFixture)} holds no runner-owned empty-scratch database. Use it as a collection fixture, and mark every case "
            + $"that reaches the database with [{nameof(FoundationImportSourceFactAttribute)}] so it skips when "
            + $"{FoundationImportSourceGate.SourceRoot} is absent.");

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
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
