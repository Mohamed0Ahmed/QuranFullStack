using QuranDashboard.Api.Abwab;
using QuranDashboard.Application.Abwab.Timeline;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-016 / SC-003 (T031): every Abwab mutation port/command and every actionable read MUST declare
// ExpectedTimelineGeneration; the foundation contract/source test fails if any omits it.
public sealed class GenerationContractCoverageTests
{
    [Fact]
    public void EveryProductionActionableRequest_CarriesTheGenerationContract()
    {
        var assemblies = new[]
        {
            typeof(AbwabGenerationContractInspector).Assembly, // Application
            typeof(AbwabConflictResponses).Assembly,           // Api
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
        // Non-vacuity: a writer that forgot the generation contract is reported.
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
