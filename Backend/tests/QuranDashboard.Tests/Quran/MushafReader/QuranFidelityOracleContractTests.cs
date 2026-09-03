using System.Text;

namespace QuranDashboard.Tests.Quran.MushafReader;

public sealed class QuranFidelityOracleContractTests
{
    [Fact]
    public void Oracle_keeps_reviewed_source_identities_independent_from_database_artifacts()
    {
        var oracleBytes = QuranFidelityOracleDocument.ReadOracleBytes();
        var oracle = QuranFidelityOracleDocument.ReadOracle();

        oracle.ContractVersion.Should().Be(1);
        oracle.Review.Authority.Should().Be("source-review");
        oracle.Review.Method.Should().Contain("not generated from the runtime database");
        oracleBytes.Should().NotBeEmpty();
        Encoding.UTF8.GetString(oracleBytes).Should().NotContain("artifactId");
        oracle.SourceIdentities.Should().OnlyContain(source => source.Sha256.Length == 64);
        oracle.RowCounts.Should().ContainKey("quran_surahs").WhoseValue.Should().Be(114);
        oracle.RowCountsReview.Authority.Should().Be("source-review");
    }
}
