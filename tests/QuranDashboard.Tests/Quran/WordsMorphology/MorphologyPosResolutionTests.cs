using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyPosResolutionTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Pos_tags_are_seeded_with_all_required_fields()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var posTags = await dbContext.PosTags.AsNoTracking().ToListAsync();
        posTags.Should().NotBeEmpty();

        foreach (var tag in posTags)
        {
            tag.Code.Should().NotBeNullOrEmpty();
            tag.ArabicLabel.Should().NotBeNullOrEmpty();
            tag.EnglishLabel.Should().NotBeNullOrEmpty();
            tag.Category.Should().NotBeNullOrEmpty();
            tag.Category.Should().BeOneOf("noun", "verb", "particle", "other");
            tag.SortOrder.Should().BeGreaterThan((short)0);
        }
    }

    [Fact]
    public async Task Every_head_pos_resolves_to_a_known_pos_tag_code()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var unknownHeadPos = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            LEFT JOIN quran_pos_tags t ON t.code = m.head_pos
            WHERE t.code IS NULL
            """).FirstAsync();

        unknownHeadPos.Should().Be(0, "every head_pos must resolve to a quran_pos_tags.code");
    }

    [Fact]
    public async Task Every_segment_pos_resolves_to_a_known_pos_tag_code()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var unknownSegmentPos = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments s
            LEFT JOIN quran_pos_tags t ON t.code = s.pos
            WHERE t.code IS NULL
            """).FirstAsync();

        unknownSegmentPos.Should().Be(0, "every segment pos must resolve to a quran_pos_tags.code");
    }

    [Fact]
    public async Task Grouping_by_pos_category_returns_rows()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var categoryGroups = await dbContext.Database.SqlQueryRaw<CategoryCount>(
            """
            SELECT t.category AS "Category", count(*)::int AS "Count"
            FROM quran_word_morphology m
            JOIN quran_pos_tags t ON t.code = m.head_pos
            GROUP BY t.category
            ORDER BY t.category
            """).ToListAsync();

        categoryGroups.Should().NotBeEmpty();
        categoryGroups.Sum(g => g.Count).Should().Be(readableCount);
    }

    [Fact]
    public async Task Grouping_by_verb_tense_returns_rows_for_verbs()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var tenseGroups = await dbContext.Database.SqlQueryRaw<TenseVoiceCount>(
            """
            SELECT verb_tense AS "Tense", verb_voice AS "Voice", count(*)::int AS "Count"
            FROM quran_word_morphology
            WHERE is_verb = true
            GROUP BY verb_tense, verb_voice
            """).ToListAsync();

        tenseGroups.Should().NotBeEmpty();
        tenseGroups.Should().OnlyContain(g => g.Tense != null && g.Voice != null);
    }

    [Fact]
    public async Task Grouping_by_case_feature_returns_rows()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var caseGroups = await dbContext.Database.SqlQueryRaw<CaseCount>(
            """
            SELECT case_feature AS "Case", count(*)::int AS "Count"
            FROM quran_word_morphology
            WHERE case_feature IS NOT NULL
            GROUP BY case_feature
            """).ToListAsync();

        caseGroups.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Pos_resolution_check_is_recorded_in_the_report()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);
        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.ReportOutDir.Should().NotBeNull();

        var check = await ReadHardCheckFromReportAsync(result.ReportOutDir!, "MORPH-POS-RESOLVES");

        check.Should().NotBeNull("the POS-resolution hard check must be recorded in the import report");
        check!.Severity.Should().Be("hard");
        check.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task Unknown_pos_code_fails_resolution_and_writes_no_rows()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var unknownPosSegments = new List<object>
        {
            new { segmentNumber = 1, kind = "STEM", pos = "ZZZ", form = "l~Ahi", features = "GEN", root = "Alh", lemma = "ilAh" }
        };
        await fixture.PatchCorpusSegmentsAsync(sourcePath, "1:1:2", unknownPosSegments);

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Succeeded.Should().BeFalse();
        result.ReportOutDir.Should().NotBeNull();

        var check = await ReadHardCheckFromReportAsync(result.ReportOutDir!, "MORPH-POS-RESOLVES");
        check.Should().NotBeNull("an unknown POS code must surface as a failed hard check in the report");
        check!.Passed.Should().BeFalse();
        check.Observed.Should().Contain("ZZZ");

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.AsNoTracking().AnyAsync())
            .Should().BeFalse("a failed POS-resolution gate must roll back without persisting morphology rows");
        (await dbContext.PosTags.AsNoTracking().AnyAsync())
            .Should().BeFalse("the refusal happens before the quran_pos_tags COPY, so no rows are written");
    }

    [Fact]
    public void PosTagSeed_covers_synthetic_fixture_pos_codes()
    {
        var seedCodes = PosTagSeed.GetAll().Select(t => t.Code).ToHashSet();

        var syntheticPosCodes = new[] { "P", "N", "V", "PN" };

        foreach (var code in syntheticPosCodes)
        {
            seedCodes.Should().Contain(code, $"POS code '{code}' used in synthetic fixtures must be in the seed");
        }
    }

    [Fact]
    public void PosTagSeed_covers_all_observed_corpus_pos_codes()
    {
        var seedCodes = PosTagSeed.GetAll().Select(t => t.Code).ToHashSet();

        var corpusPosCodes = new[]
        {
            "ACC", "ADJ", "AMD", "ANS", "AVR", "CAUS", "CERT", "CIRC", "COM", "COND", "CONJ", "DEM",
            "DET", "EMPH", "EQ", "EXH", "EXL", "EXP", "FUT", "IMPN", "IMPV", "INC", "INL", "INT",
            "INTG", "LOC", "N", "NEG", "P", "PN", "PREV", "PRO", "PRON", "PRP", "REL", "REM", "RES",
            "RET", "RSLT", "SUB", "SUP", "SUR", "T", "V", "VOC",
        };

        var missing = corpusPosCodes.Where(code => !seedCodes.Contains(code)).ToList();

        missing.Should().BeEmpty(
            "PosTagSeed must cover every POS code the corpus emits, or the import fails closed");
    }

    private static async Task<ReportCheck?> ReadHardCheckFromReportAsync(string reportDir, string checkId)
    {
        var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
        File.Exists(jsonPath).Should().BeTrue($"the import report JSON must be written to {jsonPath}");

        await using var stream = File.OpenRead(jsonPath);
        var report = await JsonSerializer.DeserializeAsync<ReportDocument>(
            stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return report?.Checks?.FirstOrDefault(check => check.Id == checkId);
    }

    private sealed record ReportDocument(IReadOnlyList<ReportCheck>? Checks);

    private sealed record ReportCheck(string Id, string Severity, string Expected, string Observed, bool Passed);

    private sealed class CategoryCount
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private sealed class TenseVoiceCount
    {
        public string? Tense { get; set; }
        public string? Voice { get; set; }
        public int Count { get; set; }
    }

    private sealed class CaseCount
    {
        public string? Case { get; set; }
        public int Count { get; set; }
    }
}
