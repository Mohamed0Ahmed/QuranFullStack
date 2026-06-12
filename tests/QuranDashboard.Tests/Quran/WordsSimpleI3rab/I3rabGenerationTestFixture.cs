using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Application.Quran.Words.GenerateI3rab;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Morphology;
using QuranDashboard.Infrastructure;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class I3rabGenerationTestFixture : IAsyncLifetime
{
    /// <summary>Counts of the <see cref="ResetToPartialMorphologyAsync"/> fixture (6 segments / 5 readable words / 0 null forms).</summary>
    public static I3rabExpectedCounts PartialMorphologyCounts { get; } = new(SegmentCount: 6, ReadableWordCount: 5, NullFormCount: 0);

    /// <summary>Counts of the <see cref="ResetToCompleteMorphologyAsync"/> fixture (6 segments / 5 readable words / 1 null form).</summary>
    public static I3rabExpectedCounts CompleteMorphologyCounts { get; } = new(SegmentCount: 6, ReadableWordCount: 5, NullFormCount: 1);

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

    public ServiceProvider CreateServiceProvider(I3rabExpectedCounts? expectedCounts = null)
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

        // Override the locked production invariants so the gate can run against a small fixture.
        // Last registration wins, so the production default from AddInfrastructure is replaced.
        if (expectedCounts is not null)
        {
            services.AddSingleton(expectedCounts);
        }

        return services.BuildServiceProvider();
    }

    public async Task<GenerateI3rabResult> RunGenerationAsync(
        I3rabExpectedCounts? expectedCounts = null,
        bool force = false,
        string? reportOutDir = null)
    {
        await using var scope = CreateServiceProvider(expectedCounts).CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<GenerateI3rabHandler>();
        return await handler.HandleAsync(new GenerateI3rabCommand(force, reportOutDir), CancellationToken.None);
    }

    public async Task ResetToWordsOnlyAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await SeedFoundationInContextAsync(dbContext);
        await SeedPosTagsAsync(dbContext);
        await ClearMorphologyDataAsync(dbContext);

        if (!await dbContext.QuranWords.AnyAsync(word => word.Id == 1))
        {
            dbContext.QuranWords.AddRange(CreateSyntheticReadableWords());
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task ResetToPartialMorphologyAsync()
    {
        await ResetToWordsOnlyAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.WordMorphologies.AddRange(
            CreateMorphology(1, "1:1:1", "N", 1),
            CreateMorphology(2, "1:1:2", "PN", 1),
            CreateMorphology(3, "1:1:3", "N", 1),
            CreateMorphology(4, "1:2:1", "V", 1),
            CreateMorphology(5, "1:2:2", "N", 2));

        dbContext.WordMorphologySegments.AddRange(
            CreateSegment(1, 1, "1:1:1", 1, "PREFIX", "P", "PREFIX", lemma: null),
            CreateSegment(2, 2, "1:1:2", 1, "STEM", "PN", "GEN", lemma: "{ll~ah"),
            CreateSegment(3, 3, "1:1:3", 1, "STEM", "N", "GEN", lemma: "rab~"),
            CreateSegment(4, 4, "1:2:1", 1, "STEM", "V", "PERF|PASS|3MS", lemma: "qAl"),
            CreateSegment(5, 5, "1:2:2", 1, "STEM", "N", "NOM", lemma: "nAs"),
            CreateSegment(6, 5, "1:2:2", 2, "SUFFIX", "PRON", "3MP", lemma: null));

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a representative fixture where every segment maps to an approved catalogue signature and
    /// every readable word is fully displayable — so a real run reaches a committed PASS. Includes a
    /// two-segment word and one NULL-form segment to exercise the displayability and null-form invariants.
    /// </summary>
    public async Task ResetToCompleteMorphologyAsync()
    {
        await ResetToWordsOnlyAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.WordMorphologies.AddRange(
            CreateMorphology(1, "1:1:1", "N", 1),
            CreateMorphology(2, "1:1:2", "PN", 1),
            CreateMorphology(3, "1:2:1", "V", 1),
            CreateMorphology(4, "1:1:3", "N", 2),
            CreateMorphology(5, "1:2:2", "N", 1));

        dbContext.WordMorphologySegments.AddRange(
            CreateSegment(1, 1, "1:1:1", 1, "STEM", "N", "GEN", lemma: "rab~"),                 // STEM:N:GEN
            CreateSegment(2, 2, "1:1:2", 1, "STEM", "PN", "GEN", lemma: "{ll~ah"),              // STEM:PN:ALLAH:GEN
            CreateSegment(3, 3, "1:2:1", 1, "STEM", "V", "PERF|ACT|3MS", lemma: "qAl"),         // STEM:V:PERF:ACT:3MS
            CreateSegment(4, 4, "1:1:3", 1, "PREFIX", "P", "P", lemma: null),                   // PREFIX:P
            CreateSegment(5, 4, "1:1:3", 2, "STEM", "N", "GEN", lemma: "Hamd"),                 // STEM:N:GEN
            CreateSegment(6, 5, "1:2:2", 1, "STEM", "N", "NOM", lemma: "nAs", formArabicNormalized: null)); // STEM:N:NOM, null form

        await dbContext.SaveChangesAsync();
    }

    private static async Task ClearMorphologyDataAsync(QuranDashboardDbContext dbContext)
    {
        await dbContext.WordMorphologySegments.ExecuteDeleteAsync();
        await dbContext.WordMorphologies.ExecuteDeleteAsync();
    }

    private async Task SeedFoundationInContextAsync(QuranDashboardDbContext dbContext)
    {
        if (await dbContext.QuranSurahs.AnyAsync())
        {
            return;
        }

        dbContext.QuranSurahs.Add(new Surah
        {
            SurahNumber = 1,
            NameArabic = "سورة-اختبار-١",
            NameSimple = "TEST_SURAH_1",
            NameTransliteration = "TEST_SURAH_1",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 1,
            VersesCount = 7,
            BismillahPre = true
        });

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 1,
            FirstSurahNumber = 1,
            FirstAyahNumber = 1,
            LastSurahNumber = 1,
            LastAyahNumber = 7,
            LinesCount = 15
        });

        dbContext.QuranAyahs.AddRange(
            new Ayah
            {
                Id = 1,
                SurahNumber = 1,
                AyahNumber = 1,
                VerseKey = "1:1",
                TextUthmani = "اختبار-١:١",
                WordsCountSource = 3,
                WordsCountReal = 3,
                PageFrom = 1,
                PageTo = 1
            },
            new Ayah
            {
                Id = 2,
                SurahNumber = 1,
                AyahNumber = 2,
                VerseKey = "1:2",
                TextUthmani = "اختبار-١:٢",
                WordsCountSource = 2,
                WordsCountReal = 2,
                PageFrom = 1,
                PageTo = 1
            });

        await dbContext.SaveChangesAsync();
    }

    private static IEnumerable<QuranWord> CreateSyntheticReadableWords()
    {
        yield return CreateWord(1, 1, 1, 1, 1, "GLYPH-TEST-1", "اختبار-كلمة-١");
        yield return CreateWord(2, 1, 1, 1, 2, "GLYPH-TEST-2", "اختبار-كلمة-٢");
        yield return CreateWord(3, 1, 1, 1, 3, "GLYPH-TEST-3", "اختبار-كلمة-٣");
        yield return CreateWord(4, 1, 1, 2, 1, "GLYPH-TEST-4", "اختبار-كلمة-٤");
        yield return CreateWord(5, 1, 1, 2, 2, "GLYPH-TEST-5", "اختبار-كلمة-٥");
        yield return CreateWord(99, 1, 1, 1, 99, "GLYPH-MARKER", "اختبار-علامة", isAyahMarker: true);
    }

    private static WordMorphology CreateMorphology(int wordId, string location, string headPos, short segmentCount) =>
        new()
        {
            QuranWordId = wordId,
            Location = location,
            HeadPos = headPos,
            SegmentCount = segmentCount,
            IsVerb = headPos == "V"
        };

    private static WordMorphologySegment CreateSegment(
        int id,
        int wordId,
        string location,
        short segmentNumber,
        string kind,
        string pos,
        string features,
        string? lemma,
        string? formArabicNormalized = "اختبار") =>
        new()
        {
            Id = id,
            QuranWordId = wordId,
            SegmentLocation = location,
            SegmentNumber = segmentNumber,
            Kind = kind,
            Pos = pos,
            FormBuckwalter = "test",
            FormArabicNormalized = formArabicNormalized,
            ArabicRenderSource = "buckwalter",
            FeaturesRaw = features,
            LemmaBuckwalter = lemma
        };

    private static QuranWord CreateWord(
        int id, int ayahId, short surahNumber, short ayahNumber, short wordNumber,
        string qpcGlyph, string textUthmaniSimple, bool isAyahMarker = false)
    {
        return new QuranWord
        {
            Id = id,
            AyahId = ayahId,
            SurahNumber = surahNumber,
            AyahNumber = ayahNumber,
            WordNumber = wordNumber,
            PageNumber = 1,
            LineNumber = 1,
            LineWordOrder = wordNumber,
            Location = $"{surahNumber}:{ayahNumber}:{wordNumber}",
            QpcGlyph = qpcGlyph,
            TextUthmani = textUthmaniSimple,
            TextUthmaniSimple = textUthmaniSimple,
            TextImlaeiSimple = textUthmaniSimple,
            WordKeyImlaeiSimple = textUthmaniSimple,
            IsAyahMarker = isAyahMarker
        };
    }

    private static async Task SeedPosTagsAsync(QuranDashboardDbContext dbContext)
    {
        if (await dbContext.PosTags.AnyAsync())
        {
            return;
        }

        dbContext.PosTags.AddRange(
            CreatePosTag("P", "حرف جر", "preposition", "particle", 1),
            CreatePosTag("PN", "اسم علم", "proper noun", "noun", 2),
            CreatePosTag("N", "اسم", "noun", "noun", 3),
            CreatePosTag("V", "فعل", "verb", "verb", 4),
            CreatePosTag("PRON", "ضمير", "pronoun", "pronoun", 5));

        await dbContext.SaveChangesAsync();
    }

    private static PosTag CreatePosTag(
        string code,
        string arabicLabel,
        string englishLabel,
        string category,
        short sortOrder) =>
        new()
        {
            Code = code,
            ArabicLabel = arabicLabel,
            EnglishLabel = englishLabel,
            Category = category,
            SortOrder = sortOrder
        };
}

[CollectionDefinition(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabGenerationTestCollection : ICollectionFixture<I3rabGenerationTestFixture>;
