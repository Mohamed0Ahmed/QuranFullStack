using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

// Source-safe: the fixtures carry only identity metadata, no Quran text.
public sealed class ForbiddenSourceRefusalTests
{
    private static string FixtureDirectory(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Abwab", "ImportSafety", "_fixtures", name);

    [Fact]
    public void ForbiddenSourcePackage_IsRefused()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("forbidden-source"));

        result.Accepted.Should().BeFalse("an un-pinned/forbidden source package must never be imported");
        result.Status.Should().Be(SourceIdentityStatus.Forbidden);
    }

    [Fact]
    public void CanonicalSourcePackage_IsAccepted()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("canonical-source"));

        result.Accepted.Should().BeTrue(result.Message);
    }
}
