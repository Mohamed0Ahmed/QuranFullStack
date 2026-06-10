using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Application.Quran.Words.ImportMorphology;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class MorphologyImportTestFixture : IAsyncLifetime
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
            .AddInfrastructure(configuration)
            .AddMorphologyImportServices();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public async Task<ImportMorphologyResult> RunImportAsync(
        string sourcePath,
        bool force = false,
        int? expectedReadableWords = null)
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
        var readableCount = expectedReadableWords ?? GetReadableWordCount();

        return await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, force, readableCount),
            CancellationToken.None);
    }

    public async Task SeedSyntheticWordsAsync()
    {
        await TruncateAllAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var surah = new Surah
        {
            SurahNumber = 1,
            NameArabic = "سورة-اختبار-١",
            NameSimple = "TEST_SURAH_1",
            NameTransliteration = "TEST_SURAH_1",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 1,
            VersesCount = 7,
            BismillahPre = true
        };
        dbContext.QuranSurahs.Add(surah);

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 1,
            FirstSurahNumber = 1,
            FirstAyahNumber = 1,
            LastSurahNumber = 1,
            LastAyahNumber = 7,
            LinesCount = 15
        });

        await dbContext.SaveChangesAsync();

        var ayah1 = new Ayah
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
        };

        var ayah2 = new Ayah
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
        };

        dbContext.QuranAyahs.AddRange(ayah1, ayah2);
        await dbContext.SaveChangesAsync();

        var readableWords = new List<QuranWord>
        {
            CreateWord(1, 1, 1, 1, 1, MorphologySyntheticSeed.TokenGlyph1, MorphologySyntheticSeed.TokenUthmani1),
            CreateWord(2, 1, 1, 1, 2, MorphologySyntheticSeed.TokenGlyph2, MorphologySyntheticSeed.TokenUthmani2),
            CreateWord(3, 1, 1, 1, 3, MorphologySyntheticSeed.TokenGlyph3, MorphologySyntheticSeed.TokenUthmani3),
            CreateWord(4, 1, 1, 2, 1, MorphologySyntheticSeed.TokenGlyph4, MorphologySyntheticSeed.TokenUthmani4),
            CreateWord(5, 1, 1, 2, 2, MorphologySyntheticSeed.TokenGlyph5, MorphologySyntheticSeed.TokenUthmani5)
        };

        var markerWord = CreateWord(99, 1, 1, 1, 99, "GLYPH-MARKER", "اختبار-علامة", isAyahMarker: true);

        dbContext.QuranWords.AddRange(readableWords);
        dbContext.QuranWords.Add(markerWord);
        await dbContext.SaveChangesAsync();
    }

    public async Task SeedEmptyFoundationAsync()
    {
        await TruncateAllAsync();
    }

    public async Task<string> WriteSyntheticSourceFolderAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"morph-source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "corpus"));
        Directory.CreateDirectory(Path.Combine(tempDir, "qul"));

        var readableWords = MorphologySyntheticSeed.ReadableWords;

        var corpusObject = new Dictionary<string, object>();
        foreach (var word in readableWords)
        {
            var segments = MorphologySyntheticSeed.GetSegmentsForWord(word.Location);
            corpusObject[word.Location] = new
            {
                qpcUthmani = word.TextUthmani,
                segments
            };
        }

        await WriteJsonAsync(
            Path.Combine(tempDir, "corpus", "quranic-corpus-morphology-qpc-aligned.json"),
            corpusObject);

        var alignmentMap = readableWords.ToDictionary(w => w.Location, w => w.Location);
        await WriteJsonAsync(
            Path.Combine(tempDir, "corpus", "corpus-qpc-location-alignment-map.json"),
            alignmentMap);

        await WriteJsonAsync(
            Path.Combine(tempDir, "qul", "word-root.json"),
            MorphologySyntheticSeed.GetRootMap());

        await WriteJsonAsync(
            Path.Combine(tempDir, "qul", "word-lemma.json"),
            MorphologySyntheticSeed.GetLemmaMap());

        await WriteJsonAsync(
            Path.Combine(tempDir, "qul", "word-stem-corrected-arabic.json"),
            MorphologySyntheticSeed.GetStemMap());

        await File.WriteAllTextAsync(Path.Combine(tempDir, "README.md"), "Test morphology source");

        var manifest = BuildManifest(tempDir);
        await WriteJsonAsync(Path.Combine(tempDir, "manifest.json"), manifest);

        return tempDir;
    }

    public async Task PatchCorpusSegmentsAsync(
        string sourcePath,
        string location,
        IReadOnlyList<object> segments)
    {
        var corpusPath = Path.Combine(
            sourcePath, "corpus", "quranic-corpus-morphology-qpc-aligned.json");

        await using var stream = File.OpenRead(corpusPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var corpusObject = new Dictionary<string, object?>();

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (string.Equals(entry.Name, location, StringComparison.Ordinal))
            {
                var qpcUthmani = entry.Value.GetProperty("qpcUthmani").GetString();
                corpusObject[entry.Name] = new { qpcUthmani, segments };
            }
            else
            {
                corpusObject[entry.Name] = JsonSerializer.Deserialize<object>(entry.Value.GetRawText());
            }
        }

        await WriteJsonAsync(corpusPath, corpusObject);
        await RefreshManifestChecksumsAsync(sourcePath);
    }

    public async Task PatchCorpusStemFeaturesAsync(
        string sourcePath,
        string location,
        string features)
    {
        var corpusPath = Path.Combine(
            sourcePath, "corpus", "quranic-corpus-morphology-qpc-aligned.json");

        await using var stream = File.OpenRead(corpusPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var corpusObject = new Dictionary<string, object?>();

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (!string.Equals(entry.Name, location, StringComparison.Ordinal))
            {
                corpusObject[entry.Name] = JsonSerializer.Deserialize<object>(entry.Value.GetRawText());
                continue;
            }

            var qpcUthmani = entry.Value.GetProperty("qpcUthmani").GetString();
            var segments = new List<object>();

            foreach (var segmentElement in entry.Value.GetProperty("segments").EnumerateArray())
            {
                var kind = segmentElement.GetProperty("kind").GetString();
                segments.Add(new
                {
                    segmentNumber = segmentElement.GetProperty("segmentNumber").GetInt16(),
                    kind,
                    pos = segmentElement.GetProperty("pos").GetString(),
                    form = segmentElement.GetProperty("form").GetString(),
                    features = string.Equals(kind, "STEM", StringComparison.Ordinal) ? features : segmentElement.GetProperty("features").GetString(),
                    root = segmentElement.TryGetProperty("root", out var root) && root.ValueKind == JsonValueKind.String ? root.GetString() : null,
                    lemma = segmentElement.TryGetProperty("lemma", out var lemma) && lemma.ValueKind == JsonValueKind.String ? lemma.GetString() : null
                });
            }

            corpusObject[entry.Name] = new { qpcUthmani, segments };
        }

        await WriteJsonAsync(corpusPath, corpusObject);
        await RefreshManifestChecksumsAsync(sourcePath);
    }

    public async Task<string> WriteSourceFolderWithExtraCorpusLocationAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        var corpusPath = Path.Combine(
            tempDir, "corpus", "quranic-corpus-morphology-qpc-aligned.json");

        await using var stream = File.OpenRead(corpusPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var corpusObject = new Dictionary<string, object?>();

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            corpusObject[entry.Name] = JsonSerializer.Deserialize<object>(entry.Value.GetRawText());
        }

        corpusObject["9:9:9"] = new
        {
            qpcUthmani = "ت-٩",
            segments = new[]
            {
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "TSTX",
                    features = "NOM",
                    root = (string?)null,
                    lemma = (string?)null
                }
            }
        };

        await WriteJsonAsync(corpusPath, corpusObject);
        await RefreshManifestChecksumsAsync(tempDir);
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

    public async Task<string> WriteSourceFolderWithMissingFileAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        File.Delete(Path.Combine(tempDir, "qul", "word-stem-corrected-arabic.json"));
        return tempDir;
    }

    public async Task<string> WriteSourceFolderWithMissingManifestEntryAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        var manifestPath = Path.Combine(tempDir, "manifest.json");

        await using var stream = File.OpenRead(manifestPath);
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var files = new List<Dictionary<string, object?>>();
        foreach (var fileEntry in root.GetProperty("files").EnumerateArray())
        {
            var localPath = ReadManifestLocalPath(fileEntry);
            if (string.Equals(localPath, "qul/word-stem-corrected-arabic.json", StringComparison.Ordinal))
            {
                continue;
            }

            files.Add(ReadManifestEntry(fileEntry));
        }

        await WriteJsonAsync(manifestPath, new { files });
        return tempDir;
    }

    public async Task<string> WriteSourceFolderWithExtraFileAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        await File.WriteAllTextAsync(Path.Combine(tempDir, "research-dump.db"), "bad");
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

    public async Task<string> WriteSourceFolderWithWrongRecordCountAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();
        const string corpusRelativePath = "corpus/quranic-corpus-morphology-qpc-aligned.json";
        var corpusPath = Path.Combine(tempDir, corpusRelativePath);

        await WriteJsonAsync(corpusPath, new Dictionary<string, object>());

        var manifestPath = Path.Combine(tempDir, "manifest.json");
        var files = new List<object>();
        foreach (var relPath in MorphologySyntheticSeed.GetManifestPaths())
        {
            var fullPath = Path.Combine(tempDir, relPath);
            var fi = new FileInfo(fullPath);
            var fileSha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fullPath)));

            files.Add(new
            {
                role = relPath,
                path = relPath,
                originPath = $"~/Desktop/test-provenance/{relPath}",
                expectedRecordCount = string.Equals(relPath, corpusRelativePath, StringComparison.Ordinal)
                    ? (int?)99999
                    : null,
                fileSizeBytes = (long?)fi.Length,
                sha256 = fileSha,
                notes = (string?)null
            });
        }

        await WriteJsonAsync(manifestPath, new { files });
        return tempDir;
    }

    public async Task TruncateMorphologyTablesAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_word_morphology_segments,
                quran_word_morphology,
                quran_lemmas,
                quran_roots,
                quran_stems,
                quran_pos_tags
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task TruncateAllAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_word_morphology_segments,
                quran_word_morphology,
                quran_lemmas,
                quran_roots,
                quran_stems,
                quran_pos_tags,
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

    public int GetReadableWordCount()
    {
        using var scope = CreateServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return dbContext.QuranWords.Count(w => !w.IsAyahMarker);
    }

    public async Task<TableSnapshot> CaptureTableSnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new TableSnapshot(
            MorphologyRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_word_morphology""").FirstAsync(),
            SegmentRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_word_morphology_segments""").FirstAsync(),
            RootRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_roots""").FirstAsync(),
            LemmaRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_lemmas""").FirstAsync(),
            StemRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_stems""").FirstAsync(),
            PosTagRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_pos_tags""").FirstAsync(),
            QuranWordRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_words""").FirstAsync(),
            MorphologyContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY quran_word_id)),
                    'empty') AS "Value"
                FROM quran_word_morphology t
                """),
            SegmentContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_word_morphology_segments t
                """),
            RootContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_roots t
                """),
            LemmaContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_lemmas t
                """),
            StemContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY id)),
                    'empty') AS "Value"
                FROM quran_stems t
                """),
            PosTagContentHash: await ComputeTableContentHashAsync(
                dbContext,
                """
                SELECT COALESCE(
                    md5(string_agg(t::text, ',' ORDER BY code)),
                    'empty') AS "Value"
                FROM quran_pos_tags t
                """));
    }

    public async Task<bool> AreSourceFilesUnchangedAsync(string sourcePath, MorphologyFileDigests before)
    {
        var reader = new MorphologyManifestReader();
        return await reader.VerifyDigestsUnchangedAsync(sourcePath, before, CancellationToken.None);
    }

    public async Task<MorphologyFileDigests> CaptureSourceDigestsAsync(string sourcePath)
    {
        var reader = new MorphologyManifestReader();
        return await reader.CaptureDigestsAsync(sourcePath, CancellationToken.None);
    }

    public async Task<string> WriteSourceFolderWithValidationViolationAsync()
    {
        var tempDir = await WriteSyntheticSourceFolderAsync();

        var badSegments = new List<object>
        {
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "TSTPR", features = "PREFIX", root = (string?)null, lemma = (string?)null }
        };

        await PatchCorpusSegmentsAsync(tempDir, "1:1:1", badSegments);

        return tempDir;
    }

    private static async Task<string> ComputeTableContentHashAsync(
        QuranDashboardDbContext dbContext, string sql) =>
        await dbContext.Database.SqlQueryRaw<string>(sql).FirstAsync();

    private static QuranWord CreateWord(
        int id, int ayahId, short surahNumber, short ayahNumber, short wordNumber,
        string textUthmaniSimple, string qpcGlyph, bool isAyahMarker = false)
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

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private static object BuildManifest(string sourceRoot)
    {
        var files = new List<object>();
        foreach (var relativePath in MorphologySyntheticSeed.GetManifestPaths())
        {
            var fullPath = Path.Combine(sourceRoot, relativePath);
            var fileInfo = new FileInfo(fullPath);
            var sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));

            files.Add(new
            {
                role = relativePath,
                path = relativePath,
                originPath = $"~/Desktop/test-provenance/{relativePath}",
                expectedRecordCount = (int?)null,
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

public sealed record TableSnapshot(
    int MorphologyRows,
    int SegmentRows,
    int RootRows,
    int LemmaRows,
    int StemRows,
    int PosTagRows,
    int QuranWordRows,
    string MorphologyContentHash,
    string SegmentContentHash,
    string RootContentHash,
    string LemmaContentHash,
    string StemContentHash,
    string PosTagContentHash);

internal static class MorphologySyntheticSeed
{
    public const string TokenUthmani1 = "ت-١";
    public const string TokenUthmani2 = "ت-٢";
    public const string TokenUthmani3 = "ت-٣";
    public const string TokenUthmani4 = "ت-٤";
    public const string TokenUthmani5 = "ت-٥";

    public const string TokenGlyph1 = "GLYPH-1";
    public const string TokenGlyph2 = "GLYPH-2";
    public const string TokenGlyph3 = "GLYPH-3";
    public const string TokenGlyph4 = "GLYPH-4";
    public const string TokenGlyph5 = "GLYPH-5";

    public const string RootValue1 = "TEST_ROOT_A";
    public const string RootValue2 = "TEST_ROOT_B";
    public const string RootValue3 = "TEST_ROOT_C";

    public const string LemmaValue1 = "TEST_LEMMA_A";
    public const string LemmaValue2 = "TEST_LEMMA_B";
    public const string LemmaValue3 = "TEST_LEMMA_C";

    public const string StemValue1 = "TEST_STEM_A";
    public const string StemValue2 = "TEST_STEM_B";
    public const string StemValue3 = "TEST_STEM_C";
    public const string StemValue4 = "TEST_STEM_D";
    public const string StemValue5 = "TEST_STEM_E";

    public static IReadOnlyList<(int Id, string Location, string TextUthmani)> ReadableWords =
    [
        (1, "1:1:1", TokenUthmani1),
        (2, "1:1:2", TokenUthmani2),
        (3, "1:1:3", TokenUthmani3),
        (4, "1:2:1", TokenUthmani4),
        (5, "1:2:2", TokenUthmani5)
    ];

    public static IReadOnlyList<object> GetSegmentsForWord(string location) => location switch
    {
        "1:1:1" =>
        [
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "TSTPR", features = "PREFIX", root = (string?)null, lemma = (string?)null },
            new { segmentNumber = 2, kind = "STEM", pos = "N", form = "TSTST", features = "NOM", root = "TSTRA", lemma = "TSTLA" }
        ],
        "1:1:2" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "N", form = "TSTSB", features = "GEN", root = "TSTRB", lemma = "TSTLB" }
        ],
        "1:1:3" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "PN", form = "TSTSC", features = "GEN", root = "TSTRC", lemma = "TSTLC" }
        ],
        "1:2:1" =>
        [
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "TSTPF", features = "PREFIX", root = (string?)null, lemma = (string?)null },
            new { segmentNumber = 2, kind = "STEM", pos = "V", form = "TSTSD", features = "PERF PASS", root = "TSTRD", lemma = "TSTLD" }
        ],
        "1:2:2" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "N", form = "TSTSE", features = "ACC", root = (string?)null, lemma = (string?)null }
        ],
        _ => []
    };

    public static Dictionary<string, string> GetRootMap() => new()
    {
        ["1:1:2"] = RootValue1,
        ["1:1:3"] = RootValue2,
        ["1:2:1"] = RootValue3
    };

    public static Dictionary<string, string> GetLemmaMap() => new()
    {
        ["1:1:2"] = LemmaValue1,
        ["1:1:3"] = LemmaValue2,
        ["1:2:1"] = LemmaValue3
    };

    public static Dictionary<string, string> GetStemMap() => new()
    {
        ["1:1:1"] = StemValue1,
        ["1:1:2"] = StemValue2,
        ["1:1:3"] = StemValue3,
        ["1:2:1"] = StemValue4,
        ["1:2:2"] = StemValue5
    };

    public static IReadOnlyList<string> GetManifestPaths() =>
    [
        "corpus/quranic-corpus-morphology-qpc-aligned.json",
        "corpus/corpus-qpc-location-alignment-map.json",
        "qul/word-root.json",
        "qul/word-lemma.json",
        "qul/word-stem-corrected-arabic.json"
    ];
}
