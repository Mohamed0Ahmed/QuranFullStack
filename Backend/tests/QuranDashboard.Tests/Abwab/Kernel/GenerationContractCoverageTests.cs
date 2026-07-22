using QuranDashboard.Api.Abwab;
using QuranDashboard.Application.Abwab.Timeline;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

public sealed class GenerationContractCoverageTests
{
    [Fact]
    public void EveryProductionActionableRequest_CarriesTheGenerationContract()
    {
        var assemblies = new[]
        {
            typeof(AbwabGenerationContractInspector).Assembly,
            typeof(AbwabConflictResponses).Assembly,
        };

        var candidates = AbwabGenerationContractInspector.DiscoverActionableRequests(assemblies);
        var missing = AbwabGenerationContractInspector.FindRequestsMissingGeneration(candidates);

        missing.Should().BeEmpty(
            "every Abwab writer/actionable read MUST carry ExpectedTimelineGeneration; missing on: "
            + string.Join(", ", missing.Select(type => type.FullName)));
    }

    [Fact]
    public void Inspector_Flags_ARequest_ThatOmitsGeneration()
    {
        var missing = AbwabGenerationContractInspector.FindRequestsMissingGeneration(
            [typeof(FixtureWriterMissingGeneration)]);

        missing.Should().ContainSingle().Which.Should().Be(typeof(FixtureWriterMissingGeneration));
    }

    [Fact]
    public void Inspector_Accepts_CompliantCommandsAndReads()
    {
        var missing = AbwabGenerationContractInspector.FindRequestsMissingGeneration(
            [typeof(FixtureCompliantMutationCommand), typeof(FixtureCompliantActionableRead)]);

        missing.Should().BeEmpty();
    }
}
