using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class WordLemmaNormalizationReaderLoadTests
{
    [Fact]
    public void Loads_embedded_artifact_and_returns_entries_and_hash()
    {
        var loaded = new WordLemmaNormalizationReader().Load();

        loaded.Artifact.Entries.Should().NotBeEmpty();
        loaded.ArtifactSha256.Should().NotBeNullOrWhiteSpace();
        loaded.Counts.Total.Should().Be(loaded.Artifact.Entries.Count);
    }

    [Fact]
    public void Artifact_hash_is_64_char_lowercase_hex_sha256()
    {
        var loaded = new WordLemmaNormalizationReader().Load();

        loaded.ArtifactSha256.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Final_counts_match_phase0_summary()
    {
        var counts = new WordLemmaNormalizationReader().Load().Counts;

        counts.Add.Should().Be(1659);
        counts.Remove.Should().Be(69);
        counts.Replace.Should().Be(90);
        counts.Keep.Should().Be(92);
        counts.Exception.Should().Be(8);
        counts.Total.Should().Be(1918);
        counts.CandidateOrNeedsReview.Should().Be(0);
    }

    [Fact]
    public void Schema_version_is_two()
    {
        new WordLemmaNormalizationReader().Load().Artifact.SchemaVersion
            .Should().Be(WordLemmaNormalizationArtifact.SupportedSchemaVersion);
    }
}
