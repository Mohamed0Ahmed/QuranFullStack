using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.FullI3rab;
using QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.FullI3rab;

/// <summary>
/// Shared, source-safe test fixture for Feature 010 (Quran Full I'rab Foundation) import tests.
/// Provides the PostgreSQL/Testcontainers lifecycle, a configured service provider with Application +
/// Infrastructure services, synthetic <c>quran_ayahs</c> seeding for verse-key resolution, and an
/// import-run helper.
/// <para>
/// Source safety: all i'rab HTML is clearly synthetic and all verse keys live in the non-existent
/// surah <c>900</c>. No real Quran ayah text and no real i'rab content is used here.
/// </para>
/// </summary>
public sealed class FullI3rabImportTestFixture : IAsyncLifetime
{
    private readonly List<string> tempDirs = [];

    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var dir in tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        await postgresContainer.DisposeAsync();
    }

    public ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration);

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.QuranSurahs.Add(new Surah
        {
            SurahNumber = FullI3rabSyntheticPackage.SyntheticSurahNumber,
            NameArabic = "سورة-اختبار-٩٠٠",
            NameSimple = "TEST_SURAH_900",
            NameTransliteration = "TEST_SURAH_900",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 900,
            VersesCount = (short)ayahs.Length,
            BismillahPre = false
        });

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 900,
            FirstSurahNumber = FullI3rabSyntheticPackage.SyntheticSurahNumber,
            FirstAyahNumber = 1,
            LastSurahNumber = FullI3rabSyntheticPackage.SyntheticSurahNumber,
            LastAyahNumber = (short)ayahs.Length,
            LinesCount = 15
        });

        await dbContext.SaveChangesAsync();

        foreach (var (id, verseKey) in ayahs)
        {
            var parts = verseKey.Split(':');
            dbContext.QuranAyahs.Add(new Ayah
            {
                Id = id,
                SurahNumber = short.Parse(parts[0]),
                AyahNumber = short.Parse(parts[1]),
                VerseKey = verseKey,
                TextUthmani = $"اختبار-{verseKey}",
                WordsCountSource = 5,
                WordsCountReal = 5,
                PageFrom = 900,
                PageTo = 900
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task TruncateFullI3rabTablesAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_full_i3rab_ayah_entries,
                quran_full_i3rab_entries,
                quran_full_i3rab_sources
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task TruncateFoundationAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_ayahs,
                quran_mushaf_pages,
                quran_surahs
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task<ImportFullI3rabResult> RunImportAsync(
        string packageDir,
        FullI3rabExpectedCounts expectedCounts,
        string? reportOutDir = null,
        bool force = false)
    {
        reportOutDir ??= CreateTempDir();
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportFullI3rabHandler>();
        return await handler.HandleAsync(
            new ImportFullI3rabCommand(packageDir, force, expectedCounts, reportOutDir),
            CancellationToken.None);
    }

    public string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"quran-full-i3rab-{Guid.NewGuid():N}");
        tempDirs.Add(dir);
        Directory.CreateDirectory(dir);
        return dir;
    }
}

[CollectionDefinition(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabImportTestCollection : ICollectionFixture<FullI3rabImportTestFixture>;
