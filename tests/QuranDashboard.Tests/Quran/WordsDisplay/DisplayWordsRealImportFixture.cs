using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

public sealed class DisplayWordsRealImportFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public int ImportRunCount { get; private set; }

    public int RebuildRunCount { get; private set; }

    public IReadOnlyList<SourceWordColumns> SourceWordsAfterImport { get; private set; } = [];

    public string RebuildReportDir { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (CanonicalImportSourceTestGate.IsMissing)
        {
            throw new InvalidOperationException(CanonicalImportSourceTestGate.MissingReason);
        }

        await postgresContainer.StartAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();

        await ImportAndRebuildAsync();
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(RebuildReportDir))
        {
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

        await postgresContainer.DisposeAsync();
    }

    public ServiceProvider CreateServiceProvider()
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

    public async Task<IReadOnlyList<SourceWordColumns>> ReadSourceWordColumnsAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
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
        var importHandler = CreateServiceProvider().GetRequiredService<ImportQuranFoundationHandler>();
        var importResult = await importHandler.HandleAsync(
            new ImportQuranFoundationCommand(CanonicalImportSourceTestGate.SourceRoot, ReportOutDir: null),
            CancellationToken.None);
        importResult.Succeeded.Should().BeTrue(importResult.Message);
        ImportRunCount++;

        SourceWordsAfterImport = await ReadSourceWordColumnsAsync();

        RebuildReportDir = Path.Combine(Path.GetTempPath(), $"words-display-real-{Guid.NewGuid():N}");
        var rebuildHandler = CreateServiceProvider().GetRequiredService<RebuildDisplayWordsHandler>();
        var rebuildResult = await rebuildHandler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: true,
                ReportOutDir: RebuildReportDir,
                ExpectedReadableWords: DisplayWordsInvariants.ExpectedReadableWords),
            CancellationToken.None);
        rebuildResult.Succeeded.Should().BeTrue(rebuildResult.Message);
        RebuildRunCount++;
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
