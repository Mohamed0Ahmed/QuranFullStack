using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

public sealed class MutashabihatReaderTests
{
    [Fact]
    public async Task JsonPhrasesReader_parses_tiny_phrases_json_with_expected_shapes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mut-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var phrasesPath = Path.Combine(tempDir, "phrases.json");
            await File.WriteAllTextAsync(phrasesPath,
                """
                {
                  "90001": {
                    "surahs": 1,
                    "ayahs": 2,
                    "count": 3,
                    "source": { "key": "900:1", "from": 1, "to": 2 },
                    "ayah": {
                      "900:1": [[1, 2], [3]],
                      "900:2": [[1, 4]]
                    }
                  },
                  "90002": {
                    "source": { "key": "900:3", "from": 5, "to": 6 },
                    "ayah": {
                      "900:3": [[5, 6]],
                      "900:4": [[7, 8], [9, 10]]
                    }
                  }
                }
                """);

            var result = await new JsonPhrasesReader().ReadAsync(phrasesPath, CancellationToken.None);

            result.RawOccurrenceCount.Should().Be(6);
            result.Groups.Should().HaveCount(2);

            var first = result.Groups.Single(group => group.SourceGroupId == 90001);
            first.Source.Key.Should().Be("900:1");
            first.Source.From.Should().Be(1);
            first.Source.To.Should().Be(2);
            first.RawSourceCountsJson.Should().NotBeNullOrWhiteSpace();
            first.OccurrencesByVerseKey.Should().ContainKey("900:1");
            first.OccurrencesByVerseKey["900:1"].Should().BeEquivalentTo(
            [
                new WordRange(1, 2),
                new WordRange(3, 3)
            ]);
            first.OccurrencesByVerseKey["900:2"].Single().Should().Be(new WordRange(1, 4));

            var second = result.Groups.Single(group => group.SourceGroupId == 90002);
            second.Source.Key.Should().Be("900:3");
            second.OccurrencesByVerseKey["900:4"].Should().BeEquivalentTo(
            [
                new WordRange(7, 8),
                new WordRange(9, 10)
            ]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task JsonPhrasesReader_preserves_opaque_source_group_id_from_object_key()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mut-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var phrasesPath = Path.Combine(tempDir, "phrases.json");
            await File.WriteAllTextAsync(phrasesPath,
                """
                {
                  "16746": {
                    "source": { "key": "900:1", "from": 1, "to": 1 },
                    "ayah": { "900:1": [[1, 1]], "900:2": [[2, 2]] }
                  }
                }
                """);

            var result = await new JsonPhrasesReader().ReadAsync(phrasesPath, CancellationToken.None);

            result.Groups.Single().SourceGroupId.Should().Be(16746);
            result.RawOccurrenceCount.Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
