using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Morphology;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class I3rabGenerationTestFixture : IAsyncLifetime
{

    public static I3rabExpectedCounts PartialMorphologyCounts { get; } = new(SegmentCount: 6, ReadableWordCount: 5, NullFormCount: 0);

    public static I3rabExpectedCounts CompleteMorphologyCounts { get; } = new(SegmentCount: 6, ReadableWordCount: 5, NullFormCount: 1);

    public static I3rabExpectedCounts BihamdikaWordCompositionCounts { get; } = new(SegmentCount: 3, ReadableWordCount: 1, NullFormCount: 0);

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

    public ServiceProvider CreateServiceProvider(
        I3rabExpectedCounts? expectedCounts = null,
        Action<IServiceCollection>? configure = null)
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

        if (expectedCounts is not null)
        {
            services.AddSingleton(expectedCounts);
        }

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public async Task<GenerateI3rabResult> RunGenerationAsync(
        I3rabExpectedCounts? expectedCounts = null,
        bool force = false,
        string? reportOutDir = null,
        Action<IServiceCollection>? configure = null)
    {
        await using var scope = CreateServiceProvider(expectedCounts, configure).CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<GenerateI3rabHandler>();
        return await handler.HandleAsync(new GenerateI3rabCommand(force, reportOutDir), CancellationToken.None);
    }

    public async Task<I3rabSourceSafetySnapshot> CaptureSourceSafetySnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var segmentFingerprint = await dbContext.Database
            .SqlQueryRaw<string>($"""SELECT ({SourceSafetySql.SegmentSourceFingerprint}) AS "Value" """)
            .FirstAsync();
        var wordsFingerprint = await dbContext.Database
            .SqlQueryRaw<string>($"""SELECT ({SourceSafetySql.QuranWordsFingerprint}) AS "Value" """)
            .FirstAsync();
        var posTagsFingerprint = await dbContext.Database
            .SqlQueryRaw<string>($"""SELECT ({SourceSafetySql.PosTagsFingerprint}) AS "Value" """)
            .FirstAsync();
        var segmentCount = await dbContext.WordMorphologySegments.AsNoTracking().CountAsync();
        var nullFormIds = await dbContext.WordMorphologySegments.AsNoTracking()
            .Where(segment => segment.FormArabicNormalized == null)
            .Select(segment => segment.Id)
            .OrderBy(id => id)
            .ToListAsync();

        return new I3rabSourceSafetySnapshot(
            segmentCount,
            segmentFingerprint,
            wordsFingerprint,
            posTagsFingerprint,
            nullFormIds);
    }

    public async Task<I3rabCommittedStateSnapshot> CaptureCommittedStateAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var segmentStates = await dbContext.WordMorphologySegments.AsNoTracking()
            .OrderBy(segment => segment.Id)
            .Select(segment => new I3rabSegmentState(
                segment.Id,
                segment.I3rabArabic,
                segment.I3rabRuleId,
                segment.I3rabStatus,
                segment.I3rabReviewReason))
            .ToListAsync();

        var rules = await dbContext.QuranI3rabRules.AsNoTracking()
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.SignatureKey)
            .Select(rule => new I3rabRuleState(
                rule.SignatureKey,
                rule.RuleFamily,
                rule.I3rabArabic,
                rule.DefaultStatus,
                rule.Description,
                rule.SortOrder))
            .ToListAsync();

        return new I3rabCommittedStateSnapshot(segmentStates, rules);
    }

    public async Task<int> CountPopulatedI3rabSegmentsAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return await dbContext.WordMorphologySegments.AsNoTracking()
            .CountAsync(segment =>
                segment.I3rabRuleId != null
                || (segment.I3rabStatus != null && segment.I3rabStatus != I3rabStatusMapping.Unsupported));
    }

    public async Task ResetToWordsOnlyAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await SeedFoundationInContextAsync(dbContext);
        await SeedPosTagsAsync(dbContext);
        await ClearMorphologyDataAsync(dbContext);
        await dbContext.QuranWords.ExecuteDeleteAsync();
        dbContext.QuranWords.AddRange(CreateSyntheticReadableWords());
        await dbContext.SaveChangesAsync();
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
            CreateSegment(1, 1, "1:1:1", 1, "STEM", "N", "GEN", lemma: "rab~"),
            CreateSegment(2, 2, "1:1:2", 1, "STEM", "PN", "GEN", lemma: "{ll~ah"),
            CreateSegment(3, 3, "1:2:1", 1, "STEM", "V", "PERF|ACT|3MS", lemma: "qAl"),
            CreateSegment(4, 4, "1:1:3", 1, "PREFIX", "P", "P", lemma: null),
            CreateSegment(5, 4, "1:1:3", 2, "STEM", "N", "GEN", lemma: "Hamd"),
            CreateSegment(6, 5, "1:2:2", 1, "STEM", "N", "NOM", lemma: "nAs", formArabicNormalized: null));

        await dbContext.SaveChangesAsync();
    }

    public async Task ResetToBihamdikaWordCompositionAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        await SeedFoundationInContextAsync(dbContext);
        await SeedPosTagsAsync(dbContext);
        await ClearMorphologyDataAsync(dbContext);
        await dbContext.QuranWords.ExecuteDeleteAsync();

        dbContext.QuranWords.Add(CreateWord(
            6, 1, 2, 30, 20, "GLYPH-TEST-6", "اختبار-كلمة-٦"));

        dbContext.WordMorphologies.Add(
            CreateMorphology(6, "2:30:20", "N", 3));

        dbContext.WordMorphologySegments.AddRange(
            CreateSegment(1, 6, "2:30:20", 1, "PREFIX", "P", "P", lemma: null),
            CreateSegment(2, 6, "2:30:20", 2, "STEM", "N", "GEN", lemma: "Hamd"),
            CreateSegment(3, 6, "2:30:20", 3, "SUFFIX", "PRON", "2MS", lemma: null));

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

public sealed record I3rabSourceSafetySnapshot(
    int SegmentCount,
    string SegmentFingerprint,
    string QuranWordsFingerprint,
    string PosTagsFingerprint,
    IReadOnlyList<int> NullFormSegmentIds);

public sealed record I3rabSegmentState(
    int Id,
    string? I3rabArabic,
    int? I3rabRuleId,
    string? I3rabStatus,
    string? I3rabReviewReason);

public sealed record I3rabRuleState(
    string SignatureKey,
    string RuleFamily,
    string I3rabArabic,
    string DefaultStatus,
    string? Description,
    short SortOrder);

public sealed record I3rabCommittedStateSnapshot(
    IReadOnlyList<I3rabSegmentState> Segments,
    IReadOnlyList<I3rabRuleState> Rules);

internal static class SourceSafetySql
{
    internal const string SegmentSourceFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|',
                    id::text,
                    quran_word_id::text,
                    segment_location,
                    segment_number::text,
                    kind,
                    pos,
                    form_buckwalter,
                    COALESCE(form_arabic_normalized, ''),
                    COALESCE(arabic_render_tier, ''),
                    arabic_render_source,
                    COALESCE(root_buckwalter, ''),
                    COALESCE(lemma_buckwalter, ''),
                    features_raw,
                    COALESCE(features_json::text, '')) ,
                ',' ORDER BY id)),
            'empty')
        FROM quran_word_morphology_segments
        """;

    internal const string QuranWordsFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|',
                    id::text,
                    location,
                    ayah_id::text,
                    surah_number::text,
                    ayah_number::text,
                    word_number::text,
                    page_number::text,
                    line_number::text,
                    line_word_order::text,
                    qpc_glyph,
                    text_uthmani,
                    text_uthmani_simple,
                    text_imlaei_simple,
                    word_key_imlaei_simple,
                    is_ayah_marker::text,
                    COALESCE(unique_tashkeel_word_id::text, ''),
                    COALESCE(unique_simple_word_id::text, '')),
                ',' ORDER BY id)),
            'empty')
        FROM quran_words
        """;

    internal const string PosTagsFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|', code, arabic_label, english_label, category, sort_order::text, COALESCE(description, '')),
                ',' ORDER BY code)),
            'empty')
        FROM quran_pos_tags
        """;
}
