using System.Security.Cryptography;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

/// <summary>
/// Shared, source-safe test fixture for Feature 007 (Quran Tafsir Foundation).
/// <para>
/// Provides the PostgreSQL/Testcontainers lifecycle, a configured service provider, synthetic
/// <c>quran_ayahs</c> seeding for verse-key resolution, and a synthetic tafsir source-package writer.
/// </para>
/// <para>
/// Source safety: all tafsir text is clearly synthetic and all verse keys live in the non-existent
/// surah <c>900</c>. No real Quran ayah text and no real tafsir content is used here. A real source
/// excerpt may only be added later if it is copied with traceable provenance.
/// </para>
/// <para>
/// Import-run helpers (loading the source, invoking the writer/handler) are intentionally not present
/// yet: the tafsir abstractions, handler, and DI registrations are introduced in Phase 2 / Phase 3 and
/// the story-specific test files will build their run helpers on top of this fixture then.
/// </para>
/// </summary>
public sealed class TafsirImportTestFixture : IAsyncLifetime
{
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
    /// Seeds synthetic ayahs into <c>quran_ayahs</c> (with a synthetic surah and mushaf page) so tafsir
    /// verse keys can be resolved to ayah ids. Existing foundation rows are cleared first.
    /// </summary>
    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
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
        await using var scope = CreateServiceProvider().CreateAsyncScope();
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
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new TafsirTableSnapshot(
            await dbContext.TafsirSources.CountAsync(),
            await dbContext.TafsirEntries.CountAsync(),
            await dbContext.TafsirAyahEntries.CountAsync());
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
                System.Text.Encoding.UTF8.GetBytes(string.Join('|', ayahTexts)))));
    }

    public async Task<string> ComputePackageFileSha256Async(string packageDir, string relativePath)
    {
        var fullPath = Path.Combine(packageDir, relativePath.Replace('\\', '/'));
        return Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fullPath)));
    }

    public async Task TamperManifestFieldAsync(string packageDir, string fieldPath, object value)
    {
        var manifestPath = Path.Combine(packageDir, "manifest.json");
        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(document.RootElement.GetRawText())!;

        var rebuilt = new Dictionary<string, object?>();
        foreach (var (key, element) in root)
        {
            rebuilt[key] = key == fieldPath
                ? value
                : JsonElementToObject(element);
        }

        await WriteJsonAsync(manifestPath, rebuilt);
    }

    public async Task TamperManifestSummaryFieldAsync(string packageDir, string summaryField, object value)
    {
        var manifestPath = Path.Combine(packageDir, "manifest.json");
        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(document.RootElement.GetRawText())!;

        var summary = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            root["summary"].GetRawText())!;
        summary[summaryField] = value;

        var rebuilt = new Dictionary<string, object?>();
        foreach (var (key, element) in root)
        {
            rebuilt[key] = key == "summary" ? summary : JsonElementToObject(element);
        }

        await WriteJsonAsync(manifestPath, rebuilt);
    }

    public async Task<ImportTafsirsResult> RunImportAsync(
        string packageDir,
        TafsirExpectedCounts expectedCounts,
        string? reportOutDir = null,
        bool force = false)
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTafsirsHandler>();
        return await handler.HandleAsync(
            new ImportTafsirsCommand(packageDir, force, expectedCounts, reportOutDir),
            CancellationToken.None);
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
    /// Writes a synthetic tafsir source package (README, manifest, package-report, and one JSON file per
    /// source under <c>sources/</c>) to a fresh temp directory and returns its path. The manifest mirrors
    /// the final package shape with synthetic, source-safe content and self-consistent size/sha256 values.
    /// </summary>
    public async Task<string> WriteSyntheticPackageAsync(
        IReadOnlyList<SyntheticTafsirSourceSpec>? sources = null,
        IReadOnlyList<string>? excludedSourceKeys = null,
        string manifestType = TafsirSyntheticSeed.ManifestType,
        bool isFinalImportManifest = true)
    {
        var sourceSpecs = sources ?? TafsirSyntheticSeed.DefaultSources;
        var excluded = excludedSourceKeys ?? Array.Empty<string>();

        var packageDir = Path.Combine(Path.GetTempPath(), $"quran-tafsirs-{Guid.NewGuid():N}");
        tempDirs.Add(packageDir);
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(Path.Combine(packageDir, "sources"));

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "README.md"),
            "Synthetic tafsir source package for tests. Source-safe; not for import into production.");
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "package-report.md"),
            "# Synthetic tafsir package report\n\nGenerated by TafsirImportTestFixture for tests.");

        var sourceRecords = new List<object>();
        foreach (var spec in sourceSpecs)
        {
            var packageFile = $"sources/{spec.SourceKey}.json";
            var fullPath = Path.Combine(packageDir, packageFile);
            await WriteJsonAsync(fullPath, spec.Entries);

            var fileInfo = new FileInfo(fullPath);
            sourceRecords.Add(BuildSourceRecord(spec, packageFile, fileInfo.Length, ComputeSha256(fullPath)));
        }

        var manifest = BuildManifest(
            manifestType,
            isFinalImportManifest,
            sourceSpecs,
            sourceRecords,
            excluded);

        await WriteJsonAsync(Path.Combine(packageDir, "manifest.json"), manifest);
        return packageDir;
    }

    /// <summary>
    /// Recomputes every approved source file's size and sha256 and rewrites them into the manifest. Use
    /// after a test mutates a source file but wants the manifest to still match (or vice versa).
    /// </summary>
    public async Task RefreshManifestChecksumsAsync(string packageDir)
    {
        var manifestPath = Path.Combine(packageDir, "manifest.json");

        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement.Clone();
        await stream.DisposeAsync();

        var manifest = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText())!;

        var refreshedSources = new List<object>();
        foreach (var sourceElement in root.GetProperty("sources").EnumerateArray())
        {
            var record = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceElement.GetRawText())!;
            var packageFile = record["packageFile"].GetString()!;
            var fullPath = Path.Combine(packageDir, packageFile);
            var fileInfo = new FileInfo(fullPath);

            var rebuilt = new Dictionary<string, object?>();
            foreach (var (key, value) in record)
            {
                rebuilt[key] = JsonElementToObject(value);
            }

            rebuilt["fileSizeBytes"] = fileInfo.Length;
            rebuilt["sha256"] = ComputeSha256(fullPath);
            refreshedSources.Add(rebuilt);
        }

        var rebuiltManifest = new Dictionary<string, object?>();
        foreach (var (key, value) in manifest)
        {
            rebuiltManifest[key] = key == "sources" ? refreshedSources : JsonElementToObject(value);
        }

        await WriteJsonAsync(manifestPath, rebuiltManifest);
    }

    public const short SyntheticSurahNumber = 900;

    private static object BuildSourceRecord(
        SyntheticTafsirSourceSpec spec,
        string packageFile,
        long fileSizeBytes,
        string sha256) => new
        {
            sourceKey = spec.SourceKey,
            languageCode = spec.LanguageCode,
            languageNameAr = spec.LanguageNameAr,
            languageNameEn = spec.LanguageNameEn,
            direction = spec.Direction,
            displayNameAr = $"تفسير اختباري {spec.SourceKey}",
            shortNameAr = $"اختبار {spec.SourceKey}",
            displayNameEn = $"Synthetic tafsir {spec.SourceKey}",
            shortNameEn = $"Test {spec.SourceKey}",
            contributorKey = $"{spec.SourceKey}-contributor",
            contributorNameAr = $"مساهم اختباري {spec.SourceKey}",
            contributorNameEn = $"Synthetic contributor {spec.SourceKey}",
            contributorType = "person",
            resourceKind = "tafsir",
            tafsirKind = spec.TafsirKind,
            contentCoverageCount = spec.ContentCoverageCount,
            sourceFileOriginal = $"/synthetic/provenance/{spec.SourceKey}.json",
            packageFile,
            sha256,
            fileSizeBytes,
            license = "unknown",
            provenance = "unknown"
        };

    private static object BuildManifest(
        string manifestType,
        bool isFinalImportManifest,
        IReadOnlyList<SyntheticTafsirSourceSpec> specs,
        IReadOnlyList<object> sourceRecords,
        IReadOnlyList<string> excludedSourceKeys)
    {
        var arabicApproved = specs.Count(s => s.LanguageCode == "ar");
        var languages = specs
            .GroupBy(s => s.LanguageCode)
            .Select(g => new
            {
                code = g.Key,
                nameAr = g.First().LanguageNameAr,
                nameEn = g.First().LanguageNameEn,
                nativeName = g.First().NativeName,
                direction = g.First().Direction
            })
            .ToList();

        var excludedSummary = excludedSourceKeys
            .Select(key => new
            {
                sourceKey = key,
                status = "excluded",
                includeInFutureImport = false,
                resourceKind = "tafsir",
                contentCoverageCount = 0,
                sourceFileOriginal = $"/synthetic/provenance/{key}.json",
                reviewReason = "synthetic-excluded-for-test"
            })
            .ToList();

        return new
        {
            manifestType,
            isFinalImportManifest,
            createdAtUtc = "2026-06-14T00:00:00Z",
            sourceRoot = "sources",
            sourceCount = specs.Count,
            licenseWarning =
                "License/provenance is unknown for all sources; synthetic test package, not for publishing.",
            summary = new
            {
                rawInspectedSources = specs.Count + excludedSourceKeys.Count,
                copiedApprovedTafsirSources = specs.Count,
                excludedSources = excludedSourceKeys.Count,
                arabicApprovedCopied = arabicApproved,
                nonArabicApprovedCopied = specs.Count - arabicApproved,
                languageCount = languages.Count,
                contributorCount = specs.Count
            },
            selectionRules = new
            {
                status = "approved_candidate",
                includeInFutureImport = true,
                contentCoverageCount = 6236,
                resourceKind = "tafsir"
            },
            languages,
            sources = sourceRecords,
            excludedSourceSummary = excludedSummary
        };
    }

    private static string ComputeSha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private static object? JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
        _ => element.GetRawText()
    };
}

/// <summary>
/// Synthetic specification for a single tafsir source file. <paramref name="Entries"/> maps a verse key to
/// either an object value (<c>{ text, ayah_keys? }</c>) for a text-owning entry, or a string value pointing
/// at a leader verse key for a grouped member.
/// </summary>
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

/// <summary>
/// Source-safe synthetic seed values for tafsir tests. All tafsir text is clearly synthetic and every verse
/// key lives in the non-existent surah <c>900</c>.
/// </summary>
internal static class TafsirSyntheticSeed
{
    public const string ManifestType = "quran-tafsir-import-source-package";

    public static string SyntheticText(string verseKey) =>
        $"<p>نص تفسير اختباري مُصطنع للمفتاح {verseKey}.</p>";

    public static string SyntheticTextForSource(string sourceKey, string verseKey) =>
        $"<p>نص تفسير اختباري {sourceKey} للمفتاح {verseKey}.</p>";

    /// <summary>
    /// A single synthetic Arabic source exercising the three source shapes: a grouped leader
    /// (<c>900:1</c> covering <c>900:1</c> and <c>900:2</c>), a member pointer (<c>900:2</c> -&gt;
    /// <c>900:1</c>), and a flat entry (<c>900:3</c>).
    /// </summary>
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

    /// <summary>Synthetic ayahs matching <see cref="DefaultSources"/> for verse-key resolution.</summary>
    public static (int Id, string VerseKey)[] DefaultAyahs =>
    [
        (1, "900:1"),
        (2, "900:2"),
        (3, "900:3")
    ];

    /// <summary>Expected counts for <see cref="DefaultSources"/> integration runs.</summary>
    public static TafsirExpectedCounts DefaultTestExpectedCounts => new(
        ApprovedSources: 1,
        ExcludedSources: 0,
        ArabicSources: 1,
        NonArabicSources: 0,
        Languages: 1,
        AyahsPerSource: 3,
        SourceAyahMappings: 3);

    /// <summary>
    /// Same as <see cref="DefaultSources"/> but with manifest/content coverage set to 6,236 for the DB check
    /// constraint while the JSON still contains only the three synthetic ayah keys.
    /// </summary>
    public static IReadOnlyList<SyntheticTafsirSourceSpec> IntegrationSources =>
    [
        DefaultSources[0] with { ContentCoverageCount = TafsirInvariants.ExpectedAyahsPerSource }
    ];

    /// <summary>
    /// Two approved sources that share verse key <c>900:1</c> but store different synthetic text per source.
    /// Exercises post-copy <c>TAFSIR-TEXT-UNCHANGED</c> keyed by (source, entry).
    /// </summary>
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
