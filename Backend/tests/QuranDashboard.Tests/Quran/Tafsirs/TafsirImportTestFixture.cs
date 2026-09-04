using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.Tafsirs;

public sealed class TafsirImportTestFixture : IAsyncLifetime
{
    private readonly TafsirSyntheticPackage packages = new();
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private string? scratchConnectionString;
    private ServiceProvider? rootProvider;

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(TafsirImportTestFixture),
            [DestructiveRehearsalSubtype.CanonicalImport, DestructiveRehearsalSubtype.CanonicalRebuild]);

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
        await TruncateTafsirTablesAsync();
        await TruncateFoundationAsync();

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.QuranSurahs.Add(new Surah
        {
            SurahNumber = SyntheticSurahNumber,
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
                PageFrom = 900,
                PageTo = 900
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task TruncateTafsirTablesAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_tafsir_ayah_entries,
                quran_tafsir_entries,
                quran_tafsir_sources
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task<TafsirTableSnapshot> CaptureTafsirTableSnapshotAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new TafsirTableSnapshot(
            await dbContext.TafsirSources.CountAsync(),
            await dbContext.TafsirEntries.CountAsync(),
            await dbContext.TafsirAyahEntries.CountAsync());
    }

    public async Task<QuranFoundationSnapshot> CaptureQuranFoundationSnapshotAsync()
    {
        await using var scope = CreateScope();
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
                System.Text.Encoding.UTF8.GetBytes(string.Join('|', ayahTexts)))));
    }

    public Task<string> ComputePackageFileSha256Async(string packageDir, string relativePath) =>
        TafsirSyntheticPackage.ComputePackageFileSha256Async(packageDir, relativePath);

    public Task TamperManifestFieldAsync(string packageDir, string fieldPath, object value) =>
        packages.TamperManifestFieldAsync(packageDir, fieldPath, value);

    public Task TamperManifestSummaryFieldAsync(string packageDir, string summaryField, object value) =>
        packages.TamperManifestSummaryFieldAsync(packageDir, summaryField, value);

    public async Task<ImportTafsirsResult> RunImportAsync(
        string packageDir,
        TafsirExpectedCounts expectedCounts,
        string? reportOutDir = null,
        bool force = false)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"tafsir-report-default-{Guid.NewGuid():N}");
        await using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTafsirsHandler>();
        return await handler.HandleAsync(
            new ImportTafsirsCommand(packageDir, force, expectedCounts, reportOutDir),
            CancellationToken.None);
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
        IReadOnlyList<SyntheticTafsirSourceSpec>? sources = null,
        IReadOnlyList<string>? excludedSourceKeys = null,
        string manifestType = TafsirSyntheticSeed.ManifestType,
        bool isFinalImportManifest = true) =>
        packages.WriteAsync(sources, excludedSourceKeys, manifestType, isFinalImportManifest);

    public Task RefreshManifestChecksumsAsync(string packageDir) =>
        packages.RefreshManifestChecksumsAsync(packageDir);

    public const short SyntheticSurahNumber = 900;

    private ServiceProvider InitializedRootProvider =>
        rootProvider ?? throw new InvalidOperationException(
            $"{nameof(TafsirImportTestFixture)} holds no database. Only its package-writing helpers work on a "
            + "directly constructed instance; every database helper needs the "
            + $"[{nameof(TafsirImportTestCollection)}] collection fixture.");

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

public sealed record SyntheticTafsirSourceSpec(
    string SourceKey,
    string LanguageCode,
    string LanguageNameAr,
    string LanguageNameEn,
    string NativeName,
    string Direction,
    string TafsirKind,
    int ContentCoverageCount,
    IReadOnlyDictionary<string, object> Entries);

public sealed record TafsirTableSnapshot(int SourceRows, int EntryRows, int AyahLinkRows);

public sealed record QuranFoundationSnapshot(int SurahRows, int AyahRows, string AyahTextFingerprint);

internal static class TafsirSyntheticSeed
{
    public const string ManifestType = "quran-tafsir-import-source-package";

    public static string SyntheticText(string verseKey) =>
        $"<p>نص تفسير اختباري مُصطنع للمفتاح {verseKey}.</p>";

    public static string SyntheticTextForSource(string sourceKey, string verseKey) =>
        $"<p>نص تفسير اختباري {sourceKey} للمفتاح {verseKey}.</p>";

    public static IReadOnlyList<SyntheticTafsirSourceSpec> DefaultSources =>
    [
        new SyntheticTafsirSourceSpec(
            SourceKey: "ar-test-tafsir",
            LanguageCode: "ar",
            LanguageNameAr: "العربية",
            LanguageNameEn: "Arabic",
            NativeName: "العربية",
            Direction: "rtl",
            TafsirKind: "brief",
            ContentCoverageCount: 3,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new
                {
                    text = SyntheticText("900:1"),
                    ayah_keys = new[] { "900:1", "900:2" }
                },
                ["900:2"] = "900:1",
                ["900:3"] = new { text = SyntheticText("900:3") }
            })
    ];

    public static (int Id, string VerseKey)[] DefaultAyahs =>
    [
        (1, "900:1"),
        (2, "900:2"),
        (3, "900:3")
    ];

    public static TafsirExpectedCounts DefaultTestExpectedCounts => new(
        ApprovedSources: 1,
        ExcludedSources: 0,
        ArabicSources: 1,
        NonArabicSources: 0,
        Languages: 1,
        AyahsPerSource: 3,
        SourceAyahMappings: 3);

    public static IReadOnlyList<SyntheticTafsirSourceSpec> IntegrationSources =>
    [
        DefaultSources[0] with { ContentCoverageCount = TafsirInvariants.ExpectedAyahsPerSource }
    ];

    public static IReadOnlyList<SyntheticTafsirSourceSpec> TwoSourceIntegrationSources =>
    [
        BuildSharedVerseKeySource(
            sourceKey: "ar-test-tafsir-a",
            languageCode: "ar",
            languageNameAr: "العربية",
            languageNameEn: "Arabic",
            nativeName: "العربية",
            direction: "rtl"),
        BuildSharedVerseKeySource(
            sourceKey: "en-test-tafsir-b",
            languageCode: "en",
            languageNameAr: "الإنجليزية",
            languageNameEn: "English",
            nativeName: "English",
            direction: "ltr")
    ];

    public static TafsirExpectedCounts TwoSourceTestExpectedCounts => new(
        ApprovedSources: 2,
        ExcludedSources: 0,
        ArabicSources: 1,
        NonArabicSources: 1,
        Languages: 2,
        AyahsPerSource: 3,
        SourceAyahMappings: 6);

    private static SyntheticTafsirSourceSpec BuildSharedVerseKeySource(
        string sourceKey,
        string languageCode,
        string languageNameAr,
        string languageNameEn,
        string nativeName,
        string direction) =>
        new(
            SourceKey: sourceKey,
            LanguageCode: languageCode,
            LanguageNameAr: languageNameAr,
            LanguageNameEn: languageNameEn,
            NativeName: nativeName,
            Direction: direction,
            TafsirKind: "brief",
            ContentCoverageCount: TafsirInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new
                {
                    text = SyntheticTextForSource(sourceKey, "900:1"),
                    ayah_keys = new[] { "900:1", "900:2" }
                },
                ["900:2"] = "900:1",
                ["900:3"] = new { text = SyntheticTextForSource(sourceKey, "900:3") }
            });

    public static SyntheticTafsirSourceSpec ExcludedSourceAsApproved(string excludedKey) =>
        DefaultSources[0] with
        {
            SourceKey = excludedKey,
            ContentCoverageCount = TafsirInvariants.ExpectedAyahsPerSource,
            Entries = BuildFullSyntheticEntries(TafsirInvariants.ExpectedAyahsPerSource)
        };

    public static IReadOnlyDictionary<string, object> BuildFullSyntheticEntries(int count)
    {
        var entries = new Dictionary<string, object>(StringComparer.Ordinal);
        for (var i = 1; i <= count; i++)
        {
            entries[$"900:{i}"] = new { text = SyntheticText($"900:{i}") };
        }

        return entries;
    }
}
