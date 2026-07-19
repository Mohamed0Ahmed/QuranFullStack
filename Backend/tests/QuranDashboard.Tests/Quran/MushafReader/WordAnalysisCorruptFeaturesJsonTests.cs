using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

// quran-safety rule 3 (never hide invalid data): corrupt features_json (valid JSON, wrong shape)
// still returns an empty list (unchanged contract), but must now log a Warning naming the segment
// instead of swallowing it silently as "segment has no features".
[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisCorruptFeaturesJsonTests(MushafReaderTestFixture fixture)
{
    private const string WordLocation = "114:8:1";
    private const string SegmentLocation = "114:8:1:1";

    [Fact]
    public async Task GetWordAnalysis_corrupt_features_json_returns_empty_list_and_logs_warning_naming_segment()
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);

        var recordingProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(recordingProvider));
        var logger = loggerFactory.CreateLogger<EfWordAnalysisReader>();
        var reader = new EfWordAnalysisReader(dbContext, logger);

        var outcome = await reader.GetWordAnalysisAsync(WordLocation, CancellationToken.None);

        var response = outcome.Should().BeOfType<WordAnalysisOutcome.Found>().Subject.Response;
        var segment = response.RenderedWordSegments.Single();
        segment.SegmentFeatures.Should().NotBeNull(
            "features_raw is present even when features_json is corrupt, so the block itself is not dropped");
        segment.SegmentFeatures!.Json.Should().BeEmpty(
            "corrupt features_json must still surface as an empty list, not throw or crash the response");

        var warning = recordingProvider.Entries.Should()
            .ContainSingle(entry => entry.Level == LogLevel.Warning)
            .Subject;
        warning.Exception.Should().BeOfType<JsonException>();
        warning.Message.Should().Contain(SegmentLocation);
    }
}
