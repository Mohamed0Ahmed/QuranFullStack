using QuranDashboard.TestArtifacts;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

public sealed class PreviousReleaseMigrationUpgradeTests
{
    [Fact]
    public void AdoptionGate_BlocksBeforeAnyDatabaseCanBeSelected()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["previous-release-upgrade", "--root", repositoryRoot],
            output,
            error);

        exitCode.Should().Be(1);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("state=blocked")
            .And.Contain("missing-authoritative-previous-release-ref-and-approved-prior-schema-representative-artifact");
    }

    [Fact]
    public void AdoptionGate_RejectsUnexpectedArgumentsAndDeclarationSchemas()
    {
        using var error = new StringWriter();

        PreviousReleaseMigrationUpgradeCommand.Execute(
            ["previous-release-upgrade", "unexpected"],
            TextWriter.Null,
            error).Should().Be(2);
        error.ToString().Should().Contain("Usage:");

        PreviousReleaseMigrationUpgradeCommand.Validate(new PreviousReleaseMigrationUpgradeDeclaration(
            "unexpected-schema",
            1,
            "blocked",
            ["authoritative-previous-release-ref", "approved-prior-schema-representative-artifact"]))
            .Should().Be("declaration-must-remain-blocked-until-release-evidence-is-adopted");
    }

}
