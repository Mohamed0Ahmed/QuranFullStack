using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class JsonWordSourceReaderTests
{
    private readonly JsonWordSourceReader reader = new();

    [Theory]
    [InlineData(true, "CLEAN_KEY_PLACEHOLDER")]
    [InlineData(false, null)]
    public async Task ReadAsync_BindsOptionalTextClean_FromSource(bool includeTextClean, string? expectedClean)
    {
        var cleanProperty = includeTextClean ? ", \"text_clean\": \"CLEAN_KEY_PLACEHOLDER\"" : string.Empty;
        var json =
            $$"""
            {
              "1:1:1": { "id": 1, "surah": "1", "ayah": "1", "word": "1", "location": "1:1:1", "text": "RAW_TEXT_PLACEHOLDER"{{cleanProperty}} }
            }
            """;
        var path = await WriteTempWordFileAsync(json);

        try
        {
            var records = await reader.ReadAsync(path, CancellationToken.None);

            var record = records.Should().ContainSingle().Subject;
            record.Text.Should().Be("RAW_TEXT_PLACEHOLDER");
            record.TextClean.Should().Be(expectedClean);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> WriteTempWordFileAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"word-source-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
