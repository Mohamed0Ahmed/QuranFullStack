using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Quran.DataPipelines.Translations;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.Translations;

public class TranslationImportTestFixture : IAsyncLifetime
{
    private const int SyntheticSurahNumber = TranslationSyntheticSeed.SyntheticSurahNumber;

    private readonly TranslationSyntheticPackage packages = new();
    private readonly OwnedServiceProviderRegistry ownedProviders = new();
    private readonly DestructiveRehearsalSubtype expectedSubtype;

    private string? scratchConnectionString;
    private ServiceProvider? rootProvider;

    public TranslationImportTestFixture()
        : this(DestructiveRehearsalSubtype.CanonicalImport)
    {
    }

    private protected TranslationImportTestFixture(DestructiveRehearsalSubtype expectedSubtype)
    {
        this.expectedSubtype = expectedSubtype;
    }

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(TranslationImportTestFixture),
            expectedSubtype);

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
        packages.Dispose();
    }

    public AsyncServiceScope CreateScope()
    {
        return InitializedRootProvider.CreateAsyncScope();
    }

    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.QuranSurahs.Add(new Surah
        {
            SurahNumber = SyntheticSurahNumber,
            NameArabic = "سورة-اختبار-٩٠١",
            NameSimple = "TEST_SURAH_901",
            NameTransliteration = "TEST_SURAH_901",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 901,
            VersesCount = (short)ayahs.Length,
            BismillahPre = false
        });

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 901,
            FirstSurahNumber = SyntheticSurahNumber,
            FirstAyahNumber = 1,
            LastSurahNumber = SyntheticSurahNumber,
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
                PageFrom = 901,
                PageTo = 901
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task TruncateTranslationTablesAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_translation_ayah_entries,
                quran_translation_sources
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task<TranslationTableSnapshot> CaptureTranslationTableSnapshotAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new TranslationTableSnapshot(
            await dbContext.TranslationSources.CountAsync(),
            await dbContext.TranslationAyahEntries.CountAsync());
    }

    public async Task TruncateFoundationAsync()
    {
        await using var scope = CreateScope();
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

    public Task<string> WriteSyntheticPackageAsync(
        IReadOnlyList<SyntheticTranslationSourceSpec>? sources = null,
        IReadOnlyList<string>? excludedSourceKeys = null,
        string manifestType = TranslationSyntheticSeed.ManifestType,
        bool isFinalImportManifest = true,
        string excludedCategory = "wordByWord") =>
        packages.WriteAsync(sources, excludedSourceKeys, manifestType, isFinalImportManifest, excludedCategory);

    public async Task<ImportTranslationsResult> RunImportAsync(
        string packageDir,
        TranslationExpectedCounts expectedCounts,
        string? reportOutDir = null,
        bool force = false)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"translation-report-default-{Guid.NewGuid():N}");
        await using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTranslationsHandler>();
        return await handler.HandleAsync(
            new ImportTranslationsCommand(packageDir, force, expectedCounts, reportOutDir),
            CancellationToken.None);
    }

    private ServiceProvider InitializedRootProvider =>
        rootProvider ?? throw new InvalidOperationException(
            $"{nameof(TranslationImportTestFixture)} holds no database. Every helper on it needs the "
            + $"[{nameof(TranslationImportTestCollection)}] collection fixture, which runs "
            + $"{nameof(InitializeAsync)} first.");

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

public sealed class TranslationRebuildTestFixture : TranslationImportTestFixture
{
    public TranslationRebuildTestFixture()
        : base(DestructiveRehearsalSubtype.CanonicalRebuild)
    {
    }
}

public sealed record SyntheticTranslationSourceSpec(
    string SourceKey,
    string LanguageCode,
    string LanguageNameEn,
    string LanguageNameAr,
    string NativeName,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string? TranslatorKey,
    string? TranslatorNameEn,
    string? TranslatorNameAr,
    int ContentCoverageCount,
    IReadOnlyDictionary<string, string> Entries);

public sealed record TranslationTableSnapshot(int SourceRows, int AyahEntryRows);

internal static class TranslationSyntheticSeed
{
    public const string ManifestType = "quran-translation-import-source-package";
    public const int SyntheticSurahNumber = 901;

    public static string SyntheticText(string sourceKey, string verseKey) =>
        $"SYNTHETIC-TRANSLATION-{sourceKey}-{verseKey}";

    public static string SyntheticVerseKey(int ayahNumber) =>
        $"{SyntheticSurahNumber}:{ayahNumber}";

    public static IReadOnlyDictionary<string, string> BuildEntries(string sourceKey, int ayahCount)
    {
        var entries = new Dictionary<string, string>(ayahCount);
        for (var ayah = 1; ayah <= ayahCount; ayah++)
        {
            var verseKey = SyntheticVerseKey(ayah);
            entries[verseKey] = SyntheticText(sourceKey, verseKey);
        }

        return entries;
    }

    public static (int Id, string VerseKey)[] BuildAyahs(int ayahCount)
    {
        var ayahs = new (int Id, string VerseKey)[ayahCount];
        for (var ayah = 1; ayah <= ayahCount; ayah++)
        {
            ayahs[ayah - 1] = (ayah, SyntheticVerseKey(ayah));
        }

        return ayahs;
    }

    public static IReadOnlyList<SyntheticTranslationSourceSpec> DefaultSources =>
    [
        CreateSourceSpec(
            sourceKey: "en-test-simple",
            translationType: "simple",
            ayahCount: TranslationInvariants.ExpectedAyahsPerSource)
    ];

    public static IReadOnlyList<SyntheticTranslationSourceSpec> MinimalSources =>
    [
        CreateSourceSpec(
            sourceKey: "en-test-minimal",
            translationType: "simple",
            ayahCount: 3)
    ];

    public static (int Id, string VerseKey)[] DefaultAyahs =>
        BuildAyahs(TranslationInvariants.ExpectedAyahsPerSource);

    public static (int Id, string VerseKey)[] MinimalAyahs => BuildAyahs(3);

    public static TranslationExpectedCounts DefaultTestExpectedCounts => new(
        ApprovedSources: 1,
        SimpleSources: 1,
        WithFootnotesSources: 0,
        ExcludedSources: 0,
        Languages: 1,
        AyahsPerSource: TranslationInvariants.ExpectedAyahsPerSource,
        SourceAyahMappings: TranslationInvariants.ExpectedAyahsPerSource);

    public static TranslationExpectedCounts MinimalTestExpectedCounts => new(
        ApprovedSources: 1,
        SimpleSources: 1,
        WithFootnotesSources: 0,
        ExcludedSources: 0,
        Languages: 1,
        AyahsPerSource: 3,
        SourceAyahMappings: 3);

    public static IReadOnlyList<SyntheticTranslationSourceSpec> IntegrationSources =>
    [
        CreateSourceSpec(
            sourceKey: "en-test-simple",
            translationType: "simple",
            ayahCount: 3),
        CreateSourceSpec(
            sourceKey: "en-test-footnotes",
            translationType: "with_footnotes",
            ayahCount: 3,
            entryTextFactory: (sourceKey, verseKey) => $"SYNTHETIC-TRANSLATION-{sourceKey}-{verseKey} [[footnote]]")
    ];

    public static TranslationExpectedCounts IntegrationTestExpectedCounts => new(
        ApprovedSources: 2,
        SimpleSources: 1,
        WithFootnotesSources: 1,
        ExcludedSources: 0,
        Languages: 1,
        AyahsPerSource: 3,
        SourceAyahMappings: 6);

    public static IReadOnlyList<SyntheticTranslationSourceSpec> ReportTestSources =>
    [
        CreateSourceSpec(
            sourceKey: "en-test-simple",
            translationType: "simple",
            ayahCount: TranslationInvariants.ExpectedAyahsPerSource),
        CreateSourceSpec(
            sourceKey: "en-test-footnotes",
            translationType: "with_footnotes",
            ayahCount: TranslationInvariants.ExpectedAyahsPerSource,
            entryTextFactory: (sourceKey, verseKey) =>
                $"SYNTHETIC-TRANSLATION-{sourceKey}-{verseKey} [[footnote]]"),
        CreateSourceSpec(
            sourceKey: "en-test-reclassified",
            translationType: "simple",
            ayahCount: TranslationInvariants.ExpectedAyahsPerSource,
            entryTextFactory: (sourceKey, verseKey) =>
                verseKey.EndsWith(":1", StringComparison.Ordinal)
                    ? $"SYNTHETIC-TRANSLATION-{sourceKey}-{verseKey} [[footnote]]"
                    : SyntheticText(sourceKey, verseKey))
    ];

    public static TranslationExpectedCounts ReportTestExpectedCounts => new(
        ApprovedSources: 3,
        SimpleSources: 1,
        WithFootnotesSources: 2,
        ExcludedSources: 0,
        Languages: 1,
        AyahsPerSource: TranslationInvariants.ExpectedAyahsPerSource,
        SourceAyahMappings: TranslationInvariants.ExpectedAyahsPerSource * 3);

    private static SyntheticTranslationSourceSpec CreateSourceSpec(
        string sourceKey,
        string translationType,
        int ayahCount,
        Func<string, string, string>? entryTextFactory = null) =>
        new(
            SourceKey: sourceKey,
            LanguageCode: "en",
            LanguageNameEn: "English",
            LanguageNameAr: "الإنجليزية",
            NativeName: "English",
            Direction: "ltr",
            TranslationType: translationType,
            DisplayNameEn: $"Synthetic English {sourceKey}",
            DisplayNameAr: $"ترجمة اختبارية {sourceKey}",
            TranslatorKey: "test-translator",
            TranslatorNameEn: "Test Translator",
            TranslatorNameAr: "مترجم اختباري",
            ContentCoverageCount: ayahCount,
            Entries: BuildEntries(sourceKey, ayahCount, entryTextFactory));

    private static IReadOnlyDictionary<string, string> BuildEntries(
        string sourceKey,
        int ayahCount,
        Func<string, string, string>? entryTextFactory = null)
    {
        var entries = new Dictionary<string, string>(ayahCount);
        for (var ayah = 1; ayah <= ayahCount; ayah++)
        {
            var verseKey = SyntheticVerseKey(ayah);
            entries[verseKey] = entryTextFactory?.Invoke(sourceKey, verseKey)
                ?? SyntheticText(sourceKey, verseKey);
        }

        return entries;
    }
}
