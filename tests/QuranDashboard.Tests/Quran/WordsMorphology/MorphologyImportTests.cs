using QuranDashboard.Application.Quran.Words.ImportMorphology;

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
        }

        var markerMorphologyCount = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            JOIN quran_words w ON w.id = m.quran_word_id
            WHERE w.is_ayah_marker = true
            """).FirstAsync();

        markerMorphologyCount.Should().Be(0);
    }
}

[CollectionDefinition(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyImportTestCollection : ICollectionFixture<MorphologyImportTestFixture>;
