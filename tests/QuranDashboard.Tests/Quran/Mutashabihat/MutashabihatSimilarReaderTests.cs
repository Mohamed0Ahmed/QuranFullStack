using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

public sealed class MutashabihatSimilarReaderTests
{
    [Fact]
    public async Task JsonSimilarAyahReader_parses_tiny_matching_ayah_json_with_expected_shapes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mut-similar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filePath = Path.Combine(tempDir, "matching-ayah.json");
            await File.WriteAllTextAsync(filePath,
                """
                {
                  "900:1": [
                    {
                      "matched_ayah_key": "900:2",
                      "score": 80,
                      "coverage": 50,
                      "matched_words_count": 2,
                      "match_words": [[1, 2], [3]]
                    }
                  ],
                  "900:3": [
                    {
                      "matched_ayah_key": "900:4",
                      "score": 95,
                      "coverage": 200,
                      "matched_words_count": 1,
                      "match_words": [[7]]
                    }
                  ]
                }
                """);

            var result = await new JsonSimilarAyahReader().ReadAsync(filePath, CancellationToken.None);

            result.Should().HaveCount(2);

            var first = result.Single(source => source.SourceVerseKey == "900:1");
            first.Links.Should().ContainSingle();
            var firstLink = first.Links.Single();
            firstLink.MatchedAyahKey.Should().Be("900:2");
            firstLink.Score.Should().Be(80);
            firstLink.Coverage.Should().Be(50);
            firstLink.MatchedWordsCount.Should().Be(2);
            firstLink.MatchWordsJson.Should().Be("""[[1, 2], [3]]""");

            var second = result.Single(source => source.SourceVerseKey == "900:3");
            second.Links.Should().ContainSingle();
            var secondLink = second.Links.Single();
            secondLink.MatchedAyahKey.Should().Be("900:4");
            secondLink.Score.Should().Be(95);
            secondLink.Coverage.Should().Be(200, "coverage > 100 must be read unchanged");
            secondLink.MatchedWordsCount.Should().Be(1);
            secondLink.MatchWordsJson.Should().Be("""[[7]]""");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
