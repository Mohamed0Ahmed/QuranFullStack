using Microsoft.Extensions.Configuration;
using QuranDashboard.Application;
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

public sealed class WordsDisplayTestFixture : IAsyncLifetime
{
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

    public async Task SeedReadableWordsAsync(IEnumerable<QuranWord> words, IEnumerable<Ayah> ayahs)
    {
        var wordList = words.ToList();
        var ayahList = ayahs.ToList();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        await EnsurePrerequisitesAsync(dbContext, ayahList, wordList);
        dbContext.ChangeTracker.Clear();

        dbContext.QuranAyahs.AddRange(ayahList);
        dbContext.QuranWords.AddRange(wordList);
        await dbContext.SaveChangesAsync();
    }

    public RebuildDisplayWordsHandler CreateHandler() =>
        CreateServiceProvider().GetRequiredService<RebuildDisplayWordsHandler>();

    public async Task SeedDefaultSyntheticDataAsync()
    {
        await TruncateAllAsync();
        await SeedReadableWordsAsync(
            DisplayWordsSyntheticSeed.Words.Append(DisplayWordsSyntheticSeed.AyahMarkerWord),
            DisplayWordsSyntheticSeed.Ayahs);
    }

    public async Task TruncateAllAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_words_ordered_tashkeel,
                quran_words_ordered_simple,
                quran_words_unique_tashkeel,
                quran_words_unique_simple,
                quran_words,
                quran_mushaf_lines,
                quran_ayahs,
                quran_mushaf_pages,
                quran_surahs
            RESTART IDENTITY CASCADE;
            """);
    }

    private static async Task EnsurePrerequisitesAsync(
        QuranDashboardDbContext dbContext,
        IReadOnlyList<Ayah> ayahs,
        IReadOnlyList<QuranWord> words)
    {
        var surahNumbers = ayahs.Select(a => a.SurahNumber)
            .Concat(words.Select(w => w.SurahNumber))
            .Distinct()
            .ToList();

        var existingSurahs = await dbContext.QuranSurahs
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .Select(s => s.SurahNumber)
            .ToListAsync();

        foreach (var surahNumber in surahNumbers.Except(existingSurahs))
        {
            dbContext.QuranSurahs.Add(new Surah
            {
                SurahNumber = surahNumber,
                NameArabic = $"سورة-اختبار-{surahNumber}",
                NameSimple = $"TEST_SURAH_{surahNumber}",
                NameTransliteration = $"TEST_SURAH_{surahNumber}",
                RevelationPlace = RevelationPlace.Makkah,
                RevelationOrder = surahNumber,
                VersesCount = 10,
                BismillahPre = true
            });
        }

        var pageNumbers = words.Select(w => w.PageNumber).Distinct().ToList();
        var existingPages = await dbContext.QuranMushafPages
            .Where(p => pageNumbers.Contains(p.PageNumber))
            .Select(p => p.PageNumber)
            .ToListAsync();

        foreach (var pageNumber in pageNumbers.Except(existingPages))
        {
            dbContext.QuranMushafPages.Add(new MushafPage
            {
                PageNumber = pageNumber,
                FirstSurahNumber = 1,
                FirstAyahNumber = 1,
                LastSurahNumber = 1,
                LastAyahNumber = 1,
                LinesCount = 15
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
