using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Application.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.FullI3rab;

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

    public async Task<FullI3rabTableSnapshot> CaptureFullI3rabTableSnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new FullI3rabTableSnapshot(
            await dbContext.FullI3rabSources.CountAsync(),
            await dbContext.FullI3rabEntries.CountAsync(),
            await dbContext.FullI3rabAyahEntries.CountAsync());
    }

    public async Task<QuranFoundationSnapshot> CaptureQuranFoundationSnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var ayahTexts = await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .Select(ayah => ayah.TextUthmani)
            .ToListAsync();

        return new QuranFoundationSnapshot(
            await dbContext.QuranSurahs.CountAsync(),
            await dbContext.QuranAyahs.CountAsync(),
            Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(string.Join('|', ayahTexts)))));
    }
}

public sealed record FullI3rabTableSnapshot(int SourceRows, int EntryRows, int AyahLinkRows);

public sealed record QuranFoundationSnapshot(int SurahRows, int AyahRows, string AyahTextFingerprint);

[CollectionDefinition(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabImportTestCollection : ICollectionFixture<FullI3rabImportTestFixture>;
