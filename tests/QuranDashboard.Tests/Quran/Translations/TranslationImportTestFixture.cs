using System.Security.Cryptography;
using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;

namespace QuranDashboard.Tests.Quran.Translations;

/// <summary>
/// Shared, source-safe test fixture for Feature 008 (Quran Translations Foundation).
/// <para>
/// Provides the PostgreSQL/Testcontainers lifecycle, a configured service provider, synthetic
/// <c>quran_ayahs</c> seeding for verse-key resolution, and a synthetic translation source-package writer.
/// </para>
/// <para>
/// Source safety: all translation text is clearly synthetic and every verse key lives in the non-existent
/// surah <c>901</c>. No real Quran ayah text and no real translation content is used here.
/// </para>
/// <para>
/// Import-run helpers (handler invocation) are introduced in Phase 3 story tests on top of this fixture.
/// </para>
/// </summary>
public sealed class TranslationImportTestFixture : IAsyncLifetime
{
    private const int SyntheticSurahNumber = TranslationSyntheticSeed.SyntheticSurahNumber;

    private readonly List<string> tempDirs = new();

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

    /// <summary>
    /// Seeds synthetic ayahs into <c>quran_ayahs</c> so translation verse keys can resolve to ayah ids.
    /// Existing foundation rows are cleared first.
    /// </summary>
    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
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
        await using var scope = CreateServiceProvider().CreateAsyncScope();
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
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new TranslationTableSnapshot(
            await dbContext.TranslationSources.CountAsync(),
            await dbContext.TranslationAyahEntries.CountAsync());
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

    /// <summary>
    /// Writes a synthetic translation source package to a fresh temp directory and returns its path.
    /// </summary>
    public async Task<string> WriteSyntheticPackageAsync(
        IReadOnlyList<SyntheticTranslationSourceSpec>? sources = null,
        IReadOnlyList<string>? excludedSourceKeys = null,
        string manifestType = TranslationSyntheticSeed.ManifestType,
        bool isFinalImportManifest = true)
    {
        var sourceSpecs = sources ?? TranslationSyntheticSeed.DefaultSources;
        var excluded = excludedSourceKeys ?? Array.Empty<string>();

        var packageDir = Path.Combine(Path.GetTempPath(), $"quran-translations-{Guid.NewGuid():N}");
        tempDirs.Add(packageDir);
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(Path.Combine(packageDir, "sources"));

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "README.md"),
            "Synthetic translation source package for tests. Source-safe; not for import into production.");
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "package-report.md"),
            "# Synthetic translation package report\n\nGenerated by TranslationImportTestFixture for tests.");

        var sourceRecords = new List<object>();
        var displayRecords = new List<object>();

        foreach (var spec in sourceSpecs)
        {
            var packageFile = $"sources/{spec.SourceKey}.json";
            var fullPath = Path.Combine(packageDir, packageFile);
            var entries = spec.Entries.ToDictionary(
                pair => pair.Key,
                pair => (object)new { t = pair.Value });

            await WriteJsonAsync(fullPath, entries);

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var sha256 = Convert.ToHexString(SHA256.HashData(fileBytes));
            var fileSize = fileBytes.LongLength;

            sourceRecords.Add(new
            {
                sourceKey = spec.SourceKey,
                packageFile,
                languageCode = spec.LanguageCode,
                translationType = spec.TranslationType,
                contentCoverageCount = spec.ContentCoverageCount,
                fileSizeBytes = fileSize,
                sha256
            });

            displayRecords.Add(new
            {
                sourceKey = spec.SourceKey,
                packageFile,
                sourceFileOriginal = $"/synthetic/provenance/{spec.SourceKey}.json",
                languageCode = spec.LanguageCode,
                languageNameEn = spec.LanguageNameEn,
                languageNameAr = spec.LanguageNameAr,
                nativeName = spec.NativeName,
                direction = spec.Direction,
                translationType = spec.TranslationType,
                translatorKey = spec.TranslatorKey,
                displayNameEn = spec.DisplayNameEn,
                displayNameAr = spec.DisplayNameAr,
                translatorNameEn = spec.TranslatorNameEn,
                translatorNameAr = spec.TranslatorNameAr,
                metadataStatus = "final_display_ready"
            });
        }

        var simpleCount = sourceSpecs.Count(spec => spec.TranslationType == "simple");
        var withFootnotesCount = sourceSpecs.Count(spec => spec.TranslationType == "with_footnotes");
        var languages = sourceSpecs.Select(spec => spec.LanguageCode).Distinct().ToList();
        var mappingCount = sourceSpecs.Sum(spec => spec.Entries.Count);
        var contentCoverageCount = sourceSpecs.Max(spec => spec.ContentCoverageCount);

        var manifest = new
        {
            manifestType,
            isFinalImportManifest,
            createdAtUtc = "2026-06-15T00:00:00Z",
            sourceRoot = "sources",
            sourceCount = sourceSpecs.Count,
            typeCounts = new
            {
                simple = simpleCount,
                with_footnotes = withFootnotesCount
            },
            languageCount = languages.Count,
            approvedAyahMappingCount = mappingCount,
            contentCoverageCount,
            excludedCounts = new
            {
                wordByWord = 0,
                emptyText = 0,
                unattributedNearDuplicate = 0,
                total = excluded.Count
            },
            selectionRules = new
            {
                resourceKind = "translation",
                levels = new[] { "ayah" },
                includedTypes = new[] { "simple", "with_footnotes" },
                excludedTypes = new[] { "word_by_word" },
                requireExact6236KeySet = true,
                requireNonEmptyText = true,
                classifyByContentNotFolder = true,
                preserveTextExactly = true
            },
            licenseWarning =
                "License/provenance is unknown for all sources; synthetic test package, not for publishing.",
            languages = languages.Select(code => new
            {
                code,
                nameEn = $"TEST_LANG_{code}",
                nameAr = $"لغة-اختبار-{code}",
                nativeName = $"TEST_NATIVE_{code}",
                direction = sourceSpecs.First(spec => spec.LanguageCode == code).Direction
            }),
            sources = sourceRecords,
            excludedSourceSummary = excluded.Select(key => new
            {
                sourceKey = key,
                status = "excluded_word_by_word",
                reason = "synthetic-excluded-for-test"
            })
        };

        await WriteJsonAsync(Path.Combine(packageDir, "manifest.json"), manifest);

        var displayMetadata = new
        {
            metadataType = "quran-translation-source-display-metadata",
            status = "final",
            sourceCount = sourceSpecs.Count,
            sourceOfTruthManifest = "manifest.json",
            displayContract = new
            {
                required = new[]
                {
                    "displayNameEn",
                    "displayNameAr",
                    "languageCode",
                    "languageNameEn",
                    "languageNameAr",
                    "nativeName",
                    "direction",
                    "translationType",
                    "sourceKey",
                    "packageFile"
                }
            },
            records = displayRecords
        };

        await WriteJsonAsync(Path.Combine(packageDir, "source-display-metadata.json"), displayMetadata);

        return packageDir;
    }

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
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

    /// <summary>
    /// Default valid synthetic source with the full 6,236-key set required by schema and import checks.
    /// </summary>
    public static IReadOnlyList<SyntheticTranslationSourceSpec> DefaultSources =>
    [
        CreateSourceSpec(
            sourceKey: "en-test-simple",
            translationType: "simple",
            ayahCount: TranslationInvariants.ExpectedAyahsPerSource)
    ];

    /// <summary>
    /// Small three-ayah package for fast negative/minimal reader tests. Not schema-valid for persistence.
    /// </summary>
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

    private static SyntheticTranslationSourceSpec CreateSourceSpec(
        string sourceKey,
        string translationType,
        int ayahCount) =>
        new(
            SourceKey: sourceKey,
            LanguageCode: "en",
            LanguageNameEn: "English",
            LanguageNameAr: "الإنجليزية",
            NativeName: "English",
            Direction: "ltr",
            TranslationType: translationType,
            DisplayNameEn: "Synthetic English Test",
            DisplayNameAr: "ترجمة اختبارية إنجليزية",
            TranslatorKey: "test-translator",
            TranslatorNameEn: "Test Translator",
            TranslatorNameAr: "مترجم اختباري",
            ContentCoverageCount: ayahCount,
            Entries: BuildEntries(sourceKey, ayahCount));
}
