using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

// quran-safety rule 3 (never hide invalid data): corrupt covered_ayah_keys (valid JSON, wrong shape)
// still returns an empty list (unchanged contract), but must now log a Warning naming the ayah+source
// instead of swallowing it silently as "no coverage".
[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudyCorruptCoveredAyahKeysTests(MushafReaderTestFixture fixture)
{
    private const string VerseKey = "114:7";
    private const string SourceKey = "ar-muyassar";

    [Fact]
    public async Task GetAyahStudy_corrupt_covered_ayah_keys_returns_empty_list_and_logs_warning_naming_ayah_and_source()
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);

        var recordingProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(recordingProvider));
        var logger = loggerFactory.CreateLogger<EfAyahStudyReader>();
        var reader = new EfAyahStudyReader(dbContext, logger);

        var response = await reader.GetAyahStudyAsync(VerseKey, SourceKey, null, null, CancellationToken.None);

        response.Should().NotBeNull();
        response!.Tafsir.Should().NotBeNull();
        response.Tafsir!.CoveredAyahKeys.Should().BeEmpty(
            "corrupt covered_ayah_keys must still surface as an empty list, not throw or crash the response");

        var warning = recordingProvider.Entries.Should()
            .ContainSingle(entry => entry.Level == LogLevel.Warning)
            .Subject;
        warning.Exception.Should().BeOfType<JsonException>();
        warning.Message.Should().Contain(VerseKey).And.Contain(SourceKey);
    }
}
