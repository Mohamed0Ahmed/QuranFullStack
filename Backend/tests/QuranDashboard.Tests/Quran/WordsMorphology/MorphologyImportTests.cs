using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyImportTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Import_produces_one_morphology_row_per_readable_word_with_matching_segments()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: expectedReadableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.Totals.Should().NotBeNull();
        result.Totals!.MorphologyRows.Should().Be(expectedReadableCount);
        result.Totals.SegmentRows.Should().Be(7);
        result.Totals.ReadableWords.Should().Be(expectedReadableCount);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var morphologyRows = await dbContext.WordMorphologies
            .AsNoTracking()
            .OrderBy(row => row.QuranWordId)
            .ToListAsync();

        morphologyRows.Should().HaveCount(expectedReadableCount);
        morphologyRows.Should().OnlyContain(row => row.SegmentCount >= 1);

        foreach (var row in morphologyRows)
        {
            var segments = await dbContext.WordMorphologySegments
                .AsNoTracking()
                .Where(segment => segment.QuranWordId == row.QuranWordId)
                .OrderBy(segment => segment.SegmentNumber)
                .ToListAsync();

            segments.Should().HaveCount(row.SegmentCount);

            var stemSegment = segments.First(segment => segment.Kind == "STEM");
            row.HeadPos.Should().Be(stemSegment.Pos);

            if (segments.Count(segment => segment.Kind == "STEM") == 1)
            {
                stemSegment.LemmaId.Should().Be(row.LemmaId);
            }
        }

        var markerMorphologyCount = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            JOIN quran_words w ON w.id = m.quran_word_id
            WHERE w.is_ayah_marker = true
            """).FirstAsync();

        markerMorphologyCount.Should().Be(0);

        var segmentStemIdColumns = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM information_schema.columns
            WHERE table_name = 'quran_word_morphology_segments'
              AND column_name = 'stem_id'
            """).FirstAsync();

        segmentStemIdColumns.Should().Be(1);
    }

    [Fact]
    public async Task Import_persists_segment_dimension_ids_in_copy_column_order()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: expectedReadableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var morphology = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:2:1");
        var segments = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .Where(segment => segment.QuranWordId == morphology.QuranWordId)
            .OrderBy(segment => segment.SegmentNumber)
            .ToListAsync();

        segments.Should().HaveCount(2);
        segments[0].Kind.Should().Be("PREFIX");
        segments[0].RootId.Should().BeNull();
        segments[0].LemmaId.Should().BeNull();
        segments[1].Kind.Should().Be("STEM");
        segments[1].RootBuckwalter.Should().Be("ktb");
        segments[1].LemmaBuckwalter.Should().Be("katab");
        segments[1].RootId.Should().Be(morphology.RootId);
        segments[1].LemmaId.Should().Be(morphology.LemmaId);
    }

    [Fact]
    public async Task Import_report_includes_segment_dimension_hard_checks()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: expectedReadableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var jsonPath = Path.Combine(result.ReportOutDir!, "morphology-import-report.json");
        await using var reportStream = File.OpenRead(jsonPath);
        var report = await JsonSerializer.DeserializeAsync<JsonElement>(reportStream);
        var checks = report.GetProperty("checks")
            .EnumerateArray()
            .Select(check => new
            {
                Id = check.GetProperty("id").GetString(),
                Passed = check.GetProperty("passed").GetBoolean()
            })
            .ToList();

        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegLemmaStemOnly && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegLemmaRequiredForStem && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegLemmaSingleStemHeadConsistent && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegLemmaMultiStemResolves && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegLemmaNoFanout && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegRootResolves && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegRootConsistent && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegDimNullSafe && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemStemOnly && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemRequiredForStem && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemHeadConsistent && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemMultiStemCurated && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemResolves && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemArtifactShape && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckWordLemmaNormalizationApplied && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckWordLemmaShiftClean && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckWordLemmaReplaceValid && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckWordLemmaMissingRecoveryClean && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckWordLemmaUncertainZero && check.Passed);
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSourceUnchanged && check.Passed);
    }

    [Fact]
    public async Task Import_report_includes_normalization_summary_and_spot_checks()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-summary-{Guid.NewGuid():N}");
        var expectedReadableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope(services =>
            {
                services.AddSingleton<IWordLemmaNormalizationReader>(new ReportingWordLemmaNormalizationReader());
            });

            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, expectedReadableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeTrue();

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            await using var reportStream = File.OpenRead(jsonPath);
            var report = await JsonSerializer.DeserializeAsync<JsonElement>(reportStream);

            var summary = report.GetProperty("correctionSummary");
            summary.GetProperty("artifactSha256").GetString().Should().MatchRegex("^[0-9a-f]{64}$");
            summary.GetProperty("rawLemmasSha256").GetString().Should().MatchRegex("^[0-9a-f]{64}$");
            summary.GetProperty("totalEntries").GetInt32().Should().Be(2);
            summary.GetProperty("appliedAdd").GetInt32().Should().Be(1);
            summary.GetProperty("problemClassCounts").GetProperty("shift-63").GetInt32().Should().Be(1);
            summary.GetProperty("problemClassCounts").GetProperty("shift-59").GetInt32().Should().Be(2);

            var spotChecks = summary.GetProperty("spotChecks").EnumerateArray().ToList();
            spotChecks.Should().Contain(check =>
                check.GetProperty("location").GetString() == "3:33:7"
                && check.GetProperty("operationKind").GetString() == "replace"
                && check.GetProperty("appliedLemmaArabic").GetString() == "إِبْرَاهِيم");
            spotChecks.Should().Contain(check =>
                check.GetProperty("location").GetString() == "28:50:10"
                && check.GetProperty("operationKind").GetString() == "add"
                && check.GetProperty("appliedLemmaArabic").GetString() == "أَضَلّ");

            var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, "morphology-import-report.md"));
            markdown.Should().Contain("Source unchanged: yes");
            markdown.Should().Contain("Remaining unapproved shift count: 0");
            markdown.Should().Contain("| original-63 | 2 |");
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }
}

internal sealed class ReportingWordLemmaNormalizationReader : IWordLemmaNormalizationReader
{
    public WordLemmaNormalizationLoaded Load() => new(
        new WordLemmaNormalizationArtifact
        {
            SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
            ArtifactId = "reporting-normalization-artifact",
            Entries = []
        },
        [],
        "2".PadRight(64, '0'),
        new WordLemmaNormalizationCounts(0, 0, 0, 0, 0, 0, 0, 0, 0));

    public WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationLoaded loaded,
        IReadOnlySet<string>? readableWordLocations = null,
        string? rawLemmasSha256 = null)
    {
        var corrected = new Dictionary<string, string>(rawLemmas, StringComparer.Ordinal)
        {
            ["3:33:7"] = "إِبْرَاهِيم",
            ["28:50:10"] = "أَضَلّ",
        };

        var summary = new WordLemmaCorrectionSummary(
            ArtifactSha256: loaded.ArtifactSha256,
            RawLemmasSha256: rawLemmasSha256,
            TotalEntries: 2,
            AppliedAdd: 1,
            AppliedRemove: 0,
            AppliedReplace: 1,
            ReviewedKeep: 0,
            ReviewedException: 0,
            FailedOrSkipped: 0,
            SpotChecks: new[]
            {
                new WordLemmaNormalizationSpotCheck("3:33:7", "replace", "إِبْرَاهِيم"),
                new WordLemmaNormalizationSpotCheck("28:50:10", "add", "أَضَلّ"),
            })
        {
            ProblemClassCounts = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["shift-63"] = 1,
                ["shift-63-replace"] = 1,
                ["shift-59"] = 2,
                ["missing-recovery"] = 1,
                ["uncertain"] = 1,
                ["multi-stem"] = 1,
            },
        };

        return new WordLemmaNormalizationResult(corrected, summary);
    }
}

[CollectionDefinition(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyImportTestCollection : ICollectionFixture<MorphologyImportTestFixture>;
