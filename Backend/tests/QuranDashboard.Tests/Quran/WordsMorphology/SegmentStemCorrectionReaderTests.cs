using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class SegmentStemCorrectionReaderTests
{
    private static readonly string[] ExpectedUnresolved =
        ["78:1:1:2", "86:5:3:2", "72:16:1:3", "20:94:2:3"];

    [Fact]
    public void Load_returns_479_approved_and_4_unresolved_of_483()
    {
        var loaded = new SegmentStemCorrectionReader().Load();

        loaded.Counts.Total.Should().Be(483);
        loaded.Counts.Approved.Should().Be(479);
        loaded.Counts.UnresolvedExceptions.Should().Be(4);
        loaded.ApprovedStemTextByLocation.Should().HaveCount(479);
        loaded.UnresolvedLocations.Should().BeEquivalentTo(ExpectedUnresolved);
    }

    [Fact]
    public void Load_indexes_an_approved_secondary_to_a_clean_stem_text()
    {
        var loaded = new SegmentStemCorrectionReader().Load();

        loaded.ApprovedStemTextByLocation.Should().ContainKey("2:3:6:3");
        loaded.ApprovedStemTextByLocation["2:3:6:3"].Should().NotBeNullOrWhiteSpace();
        loaded.UnresolvedLocations.Should().NotContain("2:3:6:3");
    }

    [Fact]
    public void Unresolved_exceptions_are_never_also_approved()
    {
        var loaded = new SegmentStemCorrectionReader().Load();

        foreach (var location in loaded.UnresolvedLocations)
        {
            loaded.ApprovedStemTextByLocation.Should().NotContainKey(location);
        }
    }
}
