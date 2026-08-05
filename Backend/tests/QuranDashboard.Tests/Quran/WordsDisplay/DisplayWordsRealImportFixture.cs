using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

public sealed class DisplayWordsRealImportFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private PostgreSqlDatabaseLease? databaseLease;
    private ServiceProvider? rootProvider;

    public int ImportRunCount { get; private set; }

    public int RebuildRunCount { get; private set; }

    public IReadOnlyList<SourceWordColumns> SourceWordsAfterImport { get; private set; } = [];

    public string RebuildReportDir { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (CanonicalImportSourceTestGate.IsMissing)
        {
            return;
        }

        databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(DisplayWordsRealImportFixture));

        try
        {
            rootProvider = ownedProviders.Own(BuildServiceProvider(databaseLease.ConnectionString));
            await ImportAndRebuildAsync();
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

        DeleteRebuildReportDir();
    }

    public AsyncServiceScope CreateScope()
    {
        return InitializedRootProvider.CreateAsyncScope();
    }

    public async Task<IReadOnlyList<SourceWordColumns>> ReadSourceWordColumnsAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return await dbContext.QuranWords
            .AsNoTracking()
            .OrderBy(word => word.Id)
            .Select(word => new SourceWordColumns(
                word.Id,
                word.TextUthmani,
                word.TextUthmaniSimple,
                word.TextImlaeiSimple,
                word.QpcGlyph,
                word.WordKeyImlaeiSimple))
            .ToListAsync();
    }

    private async Task ImportAndRebuildAsync()
    {
        var importReportDir = Path.Combine(Path.GetTempPath(), $"quran-foundation-report-{Guid.NewGuid():N}");

        await using (var importScope = CreateScope())
        {
            var importHandler = importScope.ServiceProvider.GetRequiredService<ImportQuranFoundationHandler>();
            var importResult = await importHandler.HandleAsync(
                new ImportQuranFoundationCommand(
                    CanonicalImportSourceTestGate.SourceRoot,
                    ReportOutDir: importReportDir),
                CancellationToken.None);
            importResult.Succeeded.Should().BeTrue(importResult.Message);
        }

        ImportRunCount++;

        SourceWordsAfterImport = await ReadSourceWordColumnsAsync();

        RebuildReportDir = Path.Combine(Path.GetTempPath(), $"words-display-real-{Guid.NewGuid():N}");

        await using (var rebuildScope = CreateScope())
        {
            var rebuildHandler = rebuildScope.ServiceProvider.GetRequiredService<RebuildDisplayWordsHandler>();
            var rebuildResult = await rebuildHandler.HandleAsync(
                new RebuildDisplayWordsCommand(
                    Force: true,
                    ReportOutDir: RebuildReportDir,
                    ExpectedReadableWords: DisplayWordsInvariants.ExpectedReadableWords),
                CancellationToken.None);
            rebuildResult.Succeeded.Should().BeTrue(rebuildResult.Message);
        }

        RebuildRunCount++;
    }

    private void DeleteRebuildReportDir()
    {
        if (string.IsNullOrEmpty(RebuildReportDir))
        {
            return;
        }

        try
        {
            if (Directory.Exists(RebuildReportDir))
            {
                Directory.Delete(RebuildReportDir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private ServiceProvider InitializedRootProvider =>
        rootProvider ?? throw new InvalidOperationException(
            $"{nameof(DisplayWordsRealImportFixture)} holds no database. Use it through the "
            + $"[{nameof(DisplayWordsRealImportCollection)}] collection fixture, and mark every case that reaches "
            + $"the database with [{nameof(CanonicalImportSourceFactAttribute)}] or "
            + $"[{nameof(CanonicalImportSourceTheoryAttribute)}] so it skips when "
            + $"{CanonicalImportSourceTestGate.SourceRoot} is absent.");

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

public sealed record SourceWordColumns(
    int Id,
    string TextUthmani,
    string TextUthmaniSimple,
    string TextImlaeiSimple,
    string QpcGlyph,
    string WordKeyImlaeiSimple);

[CollectionDefinition(nameof(DisplayWordsRealImportCollection))]
public sealed class DisplayWordsRealImportCollection : ICollectionFixture<DisplayWordsRealImportFixture>;
