namespace QuranDashboard.Tests.Abwab.Ci;

public sealed class ContractDriftTests
{
    [Fact]
    public void CommittedSwaggerBaseline_ExistsAndIsWellFormed()
    {
        using var baseline = ApiContractSources.ReadGeneratedContract();

        baseline.RootElement.TryGetProperty("openapi", out var version).Should().BeTrue(
            "the committed contract must be an OpenAPI document");
        version.GetString().Should().StartWith("3.", "the pipeline targets OpenAPI 3.x");

        ApiContractSources.ReadCommittedEndpoints(baseline).Should().NotBeEmpty(
            "the committed swagger baseline must describe the API surface");
        baseline.RootElement.GetProperty("components").GetProperty("schemas")
            .EnumerateObject().Should().NotBeEmpty("the committed contract must carry DTO schemas");
    }

    [Fact]
    public void CommittedSwaggerBaseline_MatchesLiveApiEndpointSet()
    {
        var liveEndpoints = ApiContractSources.ReadLiveEndpoints();
        liveEndpoints.Should().NotBeEmpty(
            "the ApiExplorer must expose the real controller endpoints, otherwise this gate proves nothing");

        using var baseline = ApiContractSources.ReadGeneratedContract();
        var committedEndpoints = ApiContractSources.ReadCommittedEndpoints(baseline);

        var missingFromBaseline = liveEndpoints.Except(committedEndpoints).OrderBy(x => x).ToList();
        var staleInBaseline = committedEndpoints.Except(liveEndpoints).OrderBy(x => x).ToList();

        missingFromBaseline.Should().BeEmpty(
            "the committed contract is stale — these live endpoints are not in swagger.json; "
            + "run Backend/scripts/check-api-contract and commit the regenerated spec: "
            + string.Join(", ", missingFromBaseline));
        staleInBaseline.Should().BeEmpty(
            "the committed contract is stale — these swagger.json endpoints no longer exist on the API; "
            + "run Backend/scripts/check-api-contract and commit the regenerated spec: "
            + string.Join(", ", staleInBaseline));
    }
}
