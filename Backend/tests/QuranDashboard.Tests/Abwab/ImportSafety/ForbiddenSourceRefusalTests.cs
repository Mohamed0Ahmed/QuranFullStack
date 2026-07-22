using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

// US2 / FR-008: a forbidden (un-pinned) source package must be refused by the canonical-source
// verifier, proven against ACTUAL on-disk fixtures (not a mocked file reader). Source-safe: the
// fixtures carry only identity metadata, no Quran text.
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

    // Non-vacuous companion: the verifier must ACCEPT a correctly pinned canonical package, otherwise
    // the refusal above could pass by refusing everything.
    [Fact]
    public void CanonicalSourcePackage_IsAccepted()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("canonical-source"));

        result.Accepted.Should().BeTrue(result.Message);
    }
}
