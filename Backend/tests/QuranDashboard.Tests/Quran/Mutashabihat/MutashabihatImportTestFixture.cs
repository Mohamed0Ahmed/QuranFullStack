using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

public sealed class MutashabihatImportTestFixture : IAsyncLifetime
{
    private readonly List<string> tempSourceDirs = new();
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private string? scratchConnectionString;
    private ServiceProvider? readerProvider;
    private ServiceProvider? importProvider;

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(MutashabihatImportTestFixture),
            [DestructiveRehearsalSubtype.CanonicalImport, DestructiveRehearsalSubtype.CanonicalRebuild]);

        try
        {
            readerProvider = ownedProviders.Own(BuildServiceProvider(configure: null));
            importProvider = ownedProviders.Own(
                BuildServiceProvider(services => services.AddMutashabihatImportServices()));
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        readerProvider = null;
        importProvider = null;
        await ownedProviders.DisposeAsync();
        scratchConnectionString = null;

        DeleteTempSourceDirs();
    }

    private void DeleteTempSourceDirs()
    {
        foreach (var dir in tempSourceDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        tempSourceDirs.Clear();
    }

    public AsyncServiceScope CreateScope() => Initialized(readerProvider).CreateAsyncScope();

    public AsyncServiceScope CreateImportScope() => Initialized(importProvider).CreateAsyncScope();

    public ServiceProvider CreateCallerDisposedServiceProvider(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return BuildServiceProvider(configure);
    }

    private ServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure)
    {
        var connectionString = scratchConnectionString
            ?? throw new InvalidOperationException(
                $"{nameof(MutashabihatImportTestFixture)} holds no database lease. Use it as a collection fixture "
                + $"through [Collection(nameof({nameof(MutashabihatImportTestCollection)}))].");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddMutashabihatReaderServices();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static ServiceProvider Initialized(ServiceProvider? provider) =>
        provider ?? throw new InvalidOperationException(
            $"{nameof(MutashabihatImportTestFixture)} has not been initialized. Use it as a collection fixture "
            + $"through [Collection(nameof({nameof(MutashabihatImportTestCollection)}))].");

    public async Task<ImportMutashabihatResult> RunImportAsync(
        string sourcePath,
        bool force = false,
        MutashabihatExpectedCounts? expectedCounts = null,
        string? reportOutDir = null)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"mutashabihat-report-{Guid.NewGuid():N}");
        await using var scope = CreateImportScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMutashabihatHandler>();

        return await handler.HandleAsync(
            new ImportMutashabihatCommand(sourcePath, force, expectedCounts, reportOutDir),
            CancellationToken.None);
    }

    public async Task<(ImportMutashabihatResult Result, string ReportDir)> RunImportWithReportAsync(
        string sourcePath,
        bool force = false,
        MutashabihatExpectedCounts? expectedCounts = null)
    {
        var reportDir = Path.Combine(Path.GetTempPath(), $"mutashabihat-report-{Guid.NewGuid():N}");
        tempSourceDirs.Add(reportDir);

        var result = await RunImportAsync(sourcePath, force, expectedCounts, reportDir);
        return (result, reportDir);
    }

    public static string GetJsonReportPath(string reportDir) =>
        Path.Combine(reportDir, "mutashabihat-import-report.json");

    public static string GetMarkdownReportPath(string reportDir) =>
        Path.Combine(reportDir, "mutashabihat-import-report.md");

    public async Task<string> WriteSourceFolderWithCoverageGt100Async()
    {
        var similarAyahs = new Dictionary<string, object>
        {
            ["900:1"] = new object[]
            {
                new
                {
                    matched_ayah_key = "900:2",
                    score = 90,
                    coverage = 200,
                    matched_words_count = 1,
                    match_words = new[] { new[] { 1 } }
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(similarAyahs: similarAyahs);
    }

    public async Task<string> WriteSourceFolderWithDuplicateOccurrenceAsync()
    {
        var phrases = new Dictionary<string, object>
        {
            ["90001"] = new
            {
                surahs = 1,
                ayahs = 2,
                count = 3,
                source = new { key = "900:1", from = 17, to = 19 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 17, 19 }, new[] { 17, 19 }],
                    ["900:2"] = [new[] { 17, 19 }]
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(phrases: phrases);
    }

    public async Task<string> WriteSourceFolderWithSourceKeyAbsentAsync()
    {
        var phrases = new Dictionary<string, object>
        {
            ["91782"] = new
            {
                surahs = 1,
                ayahs = 2,
                count = 2,
                source = new { key = "900:99", from = 1, to = 1 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 1, 2 }],
                    ["900:2"] = [new[] { 1, 2 }]
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(
            phrases: phrases,
            similarAyahs: new Dictionary<string, object>());
    }

    public async Task<string> WriteSourceFolderWithStaleCountersAsync()
    {
        var phrases = new Dictionary<string, object>
        {
            ["90010"] = new
            {
                surahs = 9,
                ayahs = 99,
                count = 999,
                source = new { key = "900:1", from = 1, to = 2 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 1, 2 }],
                    ["900:2"] = [new[] { 1, 2 }]
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(
            phrases: phrases,
            similarAyahs: new Dictionary<string, object>());
    }

    public async Task<string> WriteSourceFolderWithWordRangeUpperBoundAsync()
    {
        var phrases = new Dictionary<string, object>
        {
            ["90020"] = new
            {
                surahs = 1,
                ayahs = 2,
                count = 2,
                source = new { key = "900:1", from = 1, to = 2 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 1, 6 }],
                    ["900:2"] = [new[] { 1, 2 }]
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(
            phrases: phrases,
            similarAyahs: new Dictionary<string, object>());
    }

    public async Task SeedSyntheticWordsAsync()
    {
        await SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"));

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var readableWords = new List<QuranWord>
        {
            CreateWord(1, 1, 900, 1, 1, "GLYPH-1", "اختبار-كلمة-١"),
            CreateWord(2, 1, 900, 1, 2, "GLYPH-2", "اختبار-كلمة-٢"),
            CreateWord(3, 2, 900, 2, 1, "GLYPH-3", "اختبار-كلمة-٣"),
            CreateWord(4, 2, 900, 2, 2, "GLYPH-4", "اختبار-كلمة-٤")
        };

        var markerWord = CreateWord(99, 1, 900, 1, 99, "GLYPH-MARKER", "اختبار-علامة", isAyahMarker: true);

        dbContext.QuranWords.AddRange(readableWords);
        dbContext.QuranWords.Add(markerWord);
        await dbContext.SaveChangesAsync();
    }

    public async Task<MutashabihatImportResult> RunImportWriterAsync(
        string sourcePath,
        bool force = false,
        MutashabihatExpectedCounts? expectedCounts = null)
    {
        await using var scope = CreateImportScope();
        var importSource = scope.ServiceProvider.GetRequiredService<IMutashabihatImportSource>();
        var importWriter = scope.ServiceProvider.GetRequiredService<IMutashabihatImportWriter>();

        var source = await importSource.LoadAsync(sourcePath, CancellationToken.None);
        return await importWriter.ImportAsync(
            source,
            force,
            expectedCounts ?? new MutashabihatExpectedCounts(1, 2, 2, 1, 1, 2),
            token => importSource.SourceUnchangedAsync(sourcePath, token),
            CancellationToken.None);
    }

    public async Task<string> WriteSourceFolderWithValidationViolationAsync()
    {
        var similarAyahs = new Dictionary<string, object>
        {
            ["900:1"] = new object[]
            {
                new
                {
                    matched_ayah_key = "900:2",
                    score = 40,
                    coverage = 50,
                    matched_words_count = 1,
                    match_words = new[] { new[] { 1 } }
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(similarAyahs: similarAyahs);
    }

    public async Task<string> WriteSourceFolderWithSelfLinkViolationAsync()
    {
        var similarAyahs = new Dictionary<string, object>
        {
            ["900:1"] = new object[]
            {
                new
                {
                    matched_ayah_key = "900:1",
                    score = 80,
                    coverage = 50,
                    matched_words_count = 1,
                    match_words = new[] { new[] { 1 } }
                }
            }
        };

        return await WriteSyntheticSourceFolderAsync(similarAyahs: similarAyahs);
    }

    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var surah = new Surah
        {
            SurahNumber = 900,
            NameArabic = "سورة-اختبار-٩٠٠",
            NameSimple = "TEST_SURAH_900",
            NameTransliteration = "TEST_SURAH_900",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 900,
            VersesCount = (short)ayahs.Length,
            BismillahPre = false
        };
        dbContext.QuranSurahs.Add(surah);

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 900,
            FirstSurahNumber = 900,
            FirstAyahNumber = 1,
            LastSurahNumber = 900,
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

    public async Task SeedEmptyFoundationAsync()
    {
        await TruncateFoundationAsync();
    }

    public async Task<string> WriteSyntheticSourceFolderAsync(
        IReadOnlyDictionary<string, object>? phrases = null,
        IReadOnlyDictionary<string, object>? similarAyahs = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mutashabihat-source-{Guid.NewGuid():N}");
        tempSourceDirs.Add(tempDir);
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "mutashabihat-ul-quran"));
        Directory.CreateDirectory(Path.Combine(tempDir, "similar-ayahs"));

        var phrasesObject = phrases ?? MutashabihatSyntheticSeed.DefaultPhrases;
        var similarObject = similarAyahs ?? MutashabihatSyntheticSeed.DefaultSimilarAyahs;

        await WriteJsonAsync(
            Path.Combine(tempDir, "mutashabihat-ul-quran", "phrases.json"),
            phrasesObject);
        await WriteJsonAsync(
            Path.Combine(tempDir, "similar-ayahs", "matching-ayah.json"),
            similarObject);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "README.md"), "Test mutashabihat source");

        var manifest = BuildManifest(tempDir, phrasesObject, similarObject);
        await WriteJsonAsync(Path.Combine(tempDir, "manifest.json"), manifest);

        return tempDir;
    }

    public async Task RefreshManifestChecksumsAsync(string sourcePath)
    {
        var manifestPath = Path.Combine(sourcePath, "manifest.json");

        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var files = new List<object>();
        foreach (var fileEntry in root.GetProperty("files").EnumerateArray())
        {
            var localPath = ReadManifestLocalPath(fileEntry);
            var fullPath = Path.Combine(sourcePath, localPath);
            var fileInfo = new FileInfo(fullPath);
            var sha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fullPath)));

            files.Add(new
            {
                role = fileEntry.TryGetProperty("role", out var role) ? role.GetString() : localPath,
                path = localPath,
                originPath = fileEntry.TryGetProperty("originPath", out var originPath) ? originPath.GetString() : null,
                expectedRecordCount = fileEntry.TryGetProperty("expectedRecordCount", out var erc) && erc.ValueKind != JsonValueKind.Null
                    ? erc.GetInt32()
                    : (int?)null,
                fileSizeBytes = (long?)fileInfo.Length,
                sha256,
                notes = fileEntry.TryGetProperty("notes", out var notes) ? notes.GetString() : null
            });
        }

        await WriteJsonAsync(manifestPath, new { files });
    }

    public async Task<string> WriteSourceFolderWithExtraFileAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        await File.WriteAllTextAsync(
            Path.Combine(tempDir, "mutashabihat-ul-quran", "phrase_verses.json"),
            "{}");
        return tempDir;
    }

    public async Task<string> WriteSourceFolderWithMissingFileAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        File.Delete(Path.Combine(tempDir, "similar-ayahs", "matching-ayah.json"));
        return tempDir;
    }

    public async Task<string> WriteSourceFolderWithChecksumMismatchAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        var manifestPath = Path.Combine(tempDir, "manifest.json");

        await using var stream = File.OpenRead(manifestPath);
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var files = new List<Dictionary<string, object?>>();
        foreach (var fileEntry in root.GetProperty("files").EnumerateArray())
        {
            var dict = ReadManifestEntry(fileEntry);
            dict["sha256"] = "DEADBEEF" + Guid.NewGuid().ToString("N")[..8];
            files.Add(dict);
        }

        await WriteJsonAsync(manifestPath, new { files });
        return tempDir;
    }

    public async Task TruncateMutashabihatTablesAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_mutashabihat_groups,
                quran_mutashabihat_occurrences,
                quran_similar_ayah_links
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task TruncateFoundationAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_mutashabihat_groups,
                quran_mutashabihat_occurrences,
                quran_similar_ayah_links,
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

    public async Task<MutashabihatTableSnapshot> CaptureTableSnapshotAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new MutashabihatTableSnapshot(
            GroupRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_mutashabihat_groups""").FirstAsync(),
            OccurrenceRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_mutashabihat_occurrences""").FirstAsync(),
            LinkRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_similar_ayah_links""").FirstAsync(),
            AyahRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_ayahs""").FirstAsync(),
            WordRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_words""").FirstAsync(),
            GroupContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_mutashabihat_groups t
                """),
            OccurrenceContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_mutashabihat_occurrences t
                """),
            LinkContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_similar_ayah_links t
                """),
            AyahContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_ayahs t
                """),
            WordContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_words t
                """));
    }

    private static QuranWord CreateWord(
        int id,
        int ayahId,
        short surahNumber,
        short ayahNumber,
        short wordNumber,
        string qpcGlyph,
        string textUthmani,
        bool isAyahMarker = false) => new()
    {
        Id = id,
        AyahId = ayahId,
        SurahNumber = surahNumber,
        AyahNumber = ayahNumber,
        WordNumber = wordNumber,
        PageNumber = 900,
        LineNumber = 1,
        LineWordOrder = wordNumber,
        Location = $"{surahNumber}:{ayahNumber}:{wordNumber}",
        QpcGlyph = qpcGlyph,
        TextUthmani = textUthmani,
        TextUthmaniSimple = textUthmani,
        TextImlaeiSimple = $"إ-{textUthmani}",
        WordKeyImlaeiSimple = $"ك-{textUthmani}",
        IsAyahMarker = isAyahMarker
    };

    public async Task<bool> AreSourceFilesUnchangedAsync(
        string sourcePath, MutashabihatFileDigests before)
    {
        var reader = new MutashabihatManifestReader();
        return await reader.VerifyDigestsUnchangedAsync(sourcePath, before, CancellationToken.None);
    }

    public async Task<MutashabihatFileDigests> CaptureSourceDigestsAsync(string sourcePath)
    {
        var reader = new MutashabihatManifestReader();
        return await reader.CaptureDigestsAsync(sourcePath, CancellationToken.None);
    }

    private static async Task<string> ComputeTableContentHashAsync(
        QuranDashboardDbContext dbContext, string sql) =>
        await dbContext.Database.SqlQueryRaw<string>(sql).FirstAsync();

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private static object BuildManifest(
        string sourceRoot,
        IReadOnlyDictionary<string, object> phrases,
        IReadOnlyDictionary<string, object> similarAyahs)
    {
        var files = new List<object>();
        foreach (var relativePath in MutashabihatSyntheticSeed.GetManifestPaths())
        {
            var fullPath = Path.Combine(sourceRoot, relativePath);
            var fileInfo = new FileInfo(fullPath);
            var sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
            var expectedRecordCount = relativePath switch
            {
                "mutashabihat-ul-quran/phrases.json" => phrases.Count,
                "similar-ayahs/matching-ayah.json" => similarAyahs.Count,
                _ => (int?)null
            };

            files.Add(new
            {
                role = relativePath,
                path = relativePath,
                originPath = $"~/Desktop/test-provenance/{relativePath}",
                expectedRecordCount,
                fileSizeBytes = (long?)fileInfo.Length,
                sha256,
                notes = (string?)null
            });
        }

        return new { files };
    }

    private static string ReadManifestLocalPath(JsonElement fileEntry)
    {
        if (fileEntry.TryGetProperty("path", out var pathProp))
        {
            return pathProp.GetString() ?? string.Empty;
        }

        if (fileEntry.TryGetProperty("relativePath", out var relativePathProp))
        {
            return relativePathProp.GetString() ?? string.Empty;
        }

        return fileEntry.TryGetProperty("originPath", out var originPathProp)
            ? originPathProp.GetString() ?? string.Empty
            : string.Empty;
    }

    private static Dictionary<string, object?> ReadManifestEntry(JsonElement fileEntry) => new()
    {
        ["role"] = fileEntry.GetProperty("role").GetString(),
        ["path"] = ReadManifestLocalPath(fileEntry),
        ["originPath"] = fileEntry.TryGetProperty("originPath", out var op) ? op.GetString() : null,
        ["expectedRecordCount"] = fileEntry.TryGetProperty("expectedRecordCount", out var erc) && erc.ValueKind != JsonValueKind.Null
            ? erc.GetInt32()
            : null,
        ["fileSizeBytes"] = fileEntry.TryGetProperty("fileSizeBytes", out var fsb) ? fsb.GetInt64() : null,
        ["sha256"] = fileEntry.TryGetProperty("sha256", out var sha) ? sha.GetString() : null,
        ["notes"] = fileEntry.TryGetProperty("notes", out var n) ? n.GetString() : null
    };
}

public sealed record MutashabihatTableSnapshot(
    int GroupRows,
    int OccurrenceRows,
    int LinkRows,
    int AyahRows,
    int WordRows,
    string GroupContentHash,
    string OccurrenceContentHash,
    string LinkContentHash,
    string AyahContentHash,
    string WordContentHash);

internal static class MutashabihatSyntheticSeed
{
    public static Dictionary<string, object> DefaultPhrases => new()
    {
        ["90001"] = new
        {
            surahs = 1,
            ayahs = 2,
            count = 2,
            source = new { key = "900:1", from = 1, to = 2 },
            ayah = new Dictionary<string, object[]>
            {
                ["900:1"] = [new[] { 1, 2 }],
                ["900:2"] = [new[] { 1, 2 }]
            }
        }
    };

    public static Dictionary<string, object> DefaultSimilarAyahs => new()
    {
        ["900:1"] = new object[]
        {
            new
            {
                matched_ayah_key = "900:2",
                score = 80,
                coverage = 50,
                matched_words_count = 2,
                match_words = new[] { new[] { 1, 2 } }
            }
        }
    };

    public static IReadOnlyList<string> GetManifestPaths() =>
    [
        "mutashabihat-ul-quran/phrases.json",
        "similar-ayahs/matching-ayah.json"
    ];
}
