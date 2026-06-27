using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

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

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
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

        segmentStemIdColumns.Should().Be(0);
    }

    [Fact]
    public async Task Import_persists_segment_dimension_ids_in_copy_column_order()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: expectedReadableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
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
        checks.Should().Contain(check => check.Id == MorphologyInvariants.CheckSegStemIdAbsent && check.Passed);
    }
}

[CollectionDefinition(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyImportTestCollection : ICollectionFixture<MorphologyImportTestFixture>;
