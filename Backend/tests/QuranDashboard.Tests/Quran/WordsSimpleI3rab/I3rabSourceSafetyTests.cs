using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabSourceSafetyTests(I3rabGenerationTestFixture fixture)
{
    [Fact]
    public async Task Successful_run_preserves_source_columns_rowcounts_and_null_forms()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var counts = I3rabGenerationTestFixture.CompleteMorphologyCounts;
        var before = await fixture.CaptureSourceSafetySnapshotAsync();

        var result = await fixture.RunGenerationAsync(counts);
        result.Succeeded.Should().BeTrue(result.Message);

        var after = await fixture.CaptureSourceSafetySnapshotAsync();
        after.SegmentCount.Should().Be(before.SegmentCount);
        after.SegmentFingerprint.Should().Be(before.SegmentFingerprint);
        after.QuranWordsFingerprint.Should().Be(before.QuranWordsFingerprint);
        after.PosTagsFingerprint.Should().Be(before.PosTagsFingerprint);
        after.NullFormSegmentIds.Should().Equal(before.NullFormSegmentIds);

        after.NullFormSegmentIds.Should().HaveCount(counts.NullFormCount);
        await AssertNullFormSegmentsStillHaveLabelsAsync();
    }

    private async Task AssertNullFormSegmentsStillHaveLabelsAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var nullFormSegments = await dbContext.WordMorphologySegments.AsNoTracking()
            .Where(segment => segment.FormArabicNormalized == null)
            .ToListAsync();

        nullFormSegments.Should().NotBeEmpty();
        nullFormSegments.Should().OnlyContain(segment =>
            segment.I3rabStatus == I3rabStatusMapping.Approved
            && !string.IsNullOrWhiteSpace(segment.I3rabArabic));
    }
}
