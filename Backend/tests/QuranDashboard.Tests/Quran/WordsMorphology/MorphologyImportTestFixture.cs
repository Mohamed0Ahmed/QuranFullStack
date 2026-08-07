using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;
using QuranDashboard.Tests.TestSupport.DependencyInjection;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class MorphologyImportTestFixture : IAsyncLifetime
{
    private readonly OwnedServiceProviderRegistry ownedProviders = new();

    private PostgreSqlDatabaseLease? databaseLease;
    private ServiceProvider? rootProvider;

    public async Task InitializeAsync()
    {
        databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(MorphologyImportTestFixture));

        try
        {
            rootProvider = ownedProviders.Own(
                BuildServiceProvider(databaseLease.ConnectionString, configure: null));
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

        if (databaseLease is not null)
        {
            await databaseLease.DisposeAsync();
            databaseLease = null;
        }
    }

    public AsyncServiceScope CreateScope()
    {
        return InitializedRootProvider.CreateAsyncScope();
    }

    public AsyncServiceScope CreateScope(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return ownedProviders
            .Own(BuildServiceProvider(LeasedConnectionString, configure))
            .CreateAsyncScope();
    }

    public async Task<ImportMorphologyResult> RunImportAsync(
        string sourcePath,
        bool force = false,
        int? expectedReadableWords = null,
        string? reportOutDir = null)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"morph-report-{Guid.NewGuid():N}");
        await using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
        var readableCount = expectedReadableWords ?? GetReadableWordCount();

        return await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, force, readableCount, reportOutDir),
            CancellationToken.None);
    }

    public async Task SeedSyntheticWordsAsync()
    {
        await TruncateAllAsync();

        await using var scope = CreateScope();
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
            CreateWord(4, 2, 1, 2, 1, MorphologySyntheticSeed.TokenGlyph4, MorphologySyntheticSeed.TokenUthmani4),
            CreateWord(5, 2, 1, 2, 2, MorphologySyntheticSeed.TokenGlyph5, MorphologySyntheticSeed.TokenUthmani5)
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

    public async Task PatchQulRootMapAsync(
        string sourcePath,
        IReadOnlyDictionary<string, string> roots)
    {
        await WriteJsonAsync(Path.Combine(sourcePath, "qul", "word-root.json"), roots);
        await RefreshManifestChecksumsAsync(sourcePath);
    }

    public async Task PatchQulLemmaMapAsync(
        string sourcePath,
        IReadOnlyDictionary<string, string> lemmas)
    {
        await WriteJsonAsync(Path.Combine(sourcePath, "qul", "word-lemma.json"), lemmas);
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
                    form = "kataba",
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
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_word_morphology_segments,
                quran_word_morphology,
                quran_lemma_analyses,
                quran_lemmas,
                quran_roots,
                quran_stems,
                quran_pos_tags
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task TruncateAllAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_word_morphology_segments,
                quran_word_morphology,
                quran_lemma_analyses,
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
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return dbContext.QuranWords.Count(w => !w.IsAyahMarker);
    }

    public async Task<TableSnapshot> CaptureTableSnapshotAsync()
    {
        await using var scope = CreateScope();
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
            LemmaAnalysisRows: await dbContext.Database.SqlQueryRaw<int>(
                """SELECT count(*)::int AS "Value" FROM quran_lemma_analyses""").FirstAsync(),
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

    public async Task<IReadOnlyList<LemmaAnalysisRow>> QueryLemmaAnalysesByTextAsync(string lemmaText)
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return await dbContext.Database.SqlQueryRaw<LemmaAnalysisRow>(
            """
            SELECT a.lemma_buckwalter AS "Buckwalter", a.root_id AS "RootId", a.head_pos AS "HeadPos",
                   a.words_count AS "WordsCount", a.first_location AS "FirstLocation"
            FROM quran_lemma_analyses a
            JOIN quran_lemmas l ON l.id = a.lemma_id
            WHERE l.lemma_text = {0}
            ORDER BY a.first_word_order_in_mushaf
            """,
            lemmaText).ToListAsync();
    }

    public async Task<SmallYehStemIdentitySnapshot> QuerySmallYehStemIdentityRowsAsync()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var stems = await dbContext.Database.SqlQueryRaw<string>(
            """
            SELECT stem_text AS "Value"
            FROM quran_stems
            ORDER BY id
            """).ToListAsync();

        var suffix = await dbContext.Database.SqlQueryRaw<SmallYehSuffixRow>(
            """
            SELECT form_buckwalter AS "FormBuckwalter",
                   form_arabic_normalized AS "FormArabicNormalized",
                   pos AS "Pos",
                   kind AS "Kind",
                   stem_id AS "StemId"
            FROM quran_word_morphology_segments
            WHERE segment_location = '1:1:3:2'
            """).SingleAsync();

        var headRows = await dbContext.Database.SqlQueryRaw<SmallYehHeadStemRow>(
            """
            SELECT s.segment_location AS "SegmentLocation",
                   s.form_arabic_normalized AS "FormArabicNormalized",
                   st.stem_text AS "StemText"
            FROM quran_word_morphology_segments s
            JOIN quran_stems st ON st.id = s.stem_id
            WHERE s.segment_location IN ('1:1:1:1', '1:1:2:1')
            ORDER BY s.segment_location
            """).ToListAsync();

        return new SmallYehStemIdentitySnapshot(
            stems,
            (suffix.FormBuckwalter, suffix.FormArabicNormalized, suffix.Pos, suffix.Kind, suffix.StemId),
            headRows);
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
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "bi", features = "PREFIX", root = (string?)null, lemma = (string?)null }
        };

        await PatchCorpusSegmentsAsync(tempDir, "1:1:1", badSegments);

        return tempDir;
    }

    private ServiceProvider InitializedRootProvider =>
        rootProvider ?? throw UninitializedFixture();

    private string LeasedConnectionString =>
        databaseLease?.ConnectionString ?? throw UninitializedFixture();

    private static InvalidOperationException UninitializedFixture() => new(
        $"{nameof(MorphologyImportTestFixture)} holds no database. Every helper needs the "
        + $"[{nameof(MorphologyImportTestCollection)}] collection fixture.");

    private static ServiceProvider BuildServiceProvider(
        string connectionString,
        Action<IServiceCollection>? configure)
    {
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
            .AddMorphologyImportServices();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
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

public sealed record LemmaAnalysisRow(
    string Buckwalter,
    int? RootId,
    string? HeadPos,
    int WordsCount,
    string FirstLocation);

public sealed record SmallYehStemIdentitySnapshot(
    IReadOnlyList<string> Stems,
    (string? FormBuckwalter, string? FormArabicNormalized, string? Pos, string? Kind, int? StemId) SuffixRender,
    IReadOnlyList<SmallYehHeadStemRow> HeadStemRenderAndIdentity);

public sealed record SmallYehSuffixRow(
    string? FormBuckwalter,
    string? FormArabicNormalized,
    string? Pos,
    string? Kind,
    int? StemId);

public sealed record SmallYehHeadStemRow(
    string SegmentLocation,
    string? FormArabicNormalized,
    string? StemText);

public sealed record TableSnapshot(
    int MorphologyRows,
    int SegmentRows,
    int RootRows,
    int LemmaRows,
    int LemmaAnalysisRows,
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

    public const string RootValue1 = "أ ل ه";
    public const string RootValue2 = "ر ب ب";
    public const string RootValue3 = "ك ت ب";

    public const string LemmaValue1 = "إِلَٰه";
    public const string LemmaValue2 = "رَبّ";
    public const string LemmaValue3 = "كَتَبَ";

    public const string StemValue1 = "بِسْمِ";
    public const string StemValue2 = "لِٰهِ";
    public const string StemValue3 = "رَبّ";
    public const string StemValue4 = "كَتَبَ";
    public const string StemValue5 = "كِتَابِ";

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
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "bi", features = "PREFIX", root = (string?)null, lemma = (string?)null },
            new { segmentNumber = 2, kind = "STEM", pos = "N", form = "somi", features = "NOM", root = (string?)null, lemma = (string?)null }
        ],
        "1:1:2" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "N", form = "l~Ahi", features = "GEN", root = (string?)null, lemma = (string?)null }
        ],
        "1:1:3" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "PN", form = "raboni", features = "GEN", root = "rbb", lemma = "rabb" }
        ],
        "1:2:1" =>
        [
            new { segmentNumber = 1, kind = "PREFIX", pos = "P", form = "wa", features = "PREFIX", root = (string?)null, lemma = (string?)null },
            new { segmentNumber = 2, kind = "STEM", pos = "V", form = "kataba", features = "PERF PASS", root = "ktb", lemma = "katab" }
        ],
        "1:2:2" =>
        [
            new { segmentNumber = 1, kind = "STEM", pos = "N", form = "kitAbi", features = "ACC", root = "ktb", lemma = "kitAb" }
        ],
        _ => []
    };

    public static Dictionary<string, string> GetRootMap() => new()
    {
        ["1:1:3"] = RootValue2,
        ["1:2:1"] = RootValue3,
        ["1:2:2"] = RootValue3
    };

    public static Dictionary<string, string> GetLemmaMap() => new()
    {
        ["1:1:3"] = LemmaValue2,
        ["1:2:1"] = LemmaValue3,
        ["1:2:2"] = "كِتَاب"
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
