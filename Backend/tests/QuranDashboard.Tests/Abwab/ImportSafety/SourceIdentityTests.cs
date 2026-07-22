using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

// US2 / FR-008: pinned canonical source identity + stable-ID verification. A package whose declared
// identity does not match the pinned canonical id (wrong identity), whose stable IDs are not stable
// (duplicated / non-monotonic), or that carries no identity manifest at all, must be refused.
public sealed class SourceIdentityTests
{
    private static string FixtureDirectory(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Abwab", "ImportSafety", "_fixtures", name);

    [Fact]
    public void WrongSourceIdentity_IsRefused()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("wrong-identity-source"));

        result.Accepted.Should().BeFalse("a package declaring a non-canonical identity must be refused");
        result.Status.Should().Be(SourceIdentityStatus.WrongIdentity);
    }

    [Fact]
    public void UnstableStableIds_AreRefused()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("unstable-id-source"));

        result.Accepted.Should().BeFalse("non-monotonic / duplicated stable IDs must be refused");
        result.Status.Should().Be(SourceIdentityStatus.UnstableIds);
    }

    [Fact]
    public void MissingIdentityManifest_IsRefused()
    {
        var verifier = new CanonicalQuranSourceVerifier();

        var result = verifier.Verify(FixtureDirectory("no-manifest-source"));

        result.Accepted.Should().BeFalse("an unverifiable package (no identity manifest) must be refused");
        result.Status.Should().Be(SourceIdentityStatus.MissingManifest);
    }
}
