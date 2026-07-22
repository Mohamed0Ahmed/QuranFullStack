using Microsoft.AspNetCore.Mvc.ApiExplorer;
using QuranDashboard.Api.Extensions;

namespace QuranDashboard.Tests.Abwab.Ci;

// FR-002 / SC (§15) contract-drift gate.
//
// The authoritative, byte-exact drift gate is the CI mechanism `Backend/scripts/check-api-contract`
// (export-swagger via the Swashbuckle CLI -> npm generate:api -> npm docs:api -> `git diff --exit-code`).
// The recorded baseline lives in git: `Frontend/quran-dashboard-ui/openapi/swagger.json` plus the
// generated frontend models and `docs/api-reference/`. That mechanism regenerates the contract from
// the real API and fails the build on ANY drift; reproducing the Swashbuckle CLI's exact byte
// serialization inside xUnit would be fragile, so it is owned by the CI job (T011), not this test.
//
// This test is the load-bearing in-process companion: it derives the live endpoint set directly from
// the API's own ApiExplorer (the same source Swashbuckle builds the document from) and asserts the
// committed swagger baseline describes EXACTLY that set of {method path} pairs. Adding, removing, or
// re-routing an endpoint without regenerating the committed spec makes this go RED, independently of
// the CI git-diff. It compares the endpoint SET (not formatting), so it is stable against cosmetic
// serialization differences while still catching real contract changes.
public sealed class ContractDriftTests
{
    private const string BaselineRelativePath = "Frontend/quran-dashboard-ui/openapi/swagger.json";

    [Fact]
    public void CommittedSwaggerBaseline_ExistsAndIsWellFormed()
    {
        var baseline = ReadBaselineDocument();

        baseline.RootElement.TryGetProperty("openapi", out var version).Should().BeTrue(
            "the committed contract must be an OpenAPI document");
        version.GetString().Should().StartWith("3.", "the pipeline targets OpenAPI 3.x");

        ReadCommittedEndpoints(baseline).Should().NotBeEmpty(
            "the committed swagger baseline must describe the API surface");
        baseline.RootElement.GetProperty("components").GetProperty("schemas")
            .EnumerateObject().Should().NotBeEmpty("the committed contract must carry DTO schemas");
    }

    [Fact]
    public void CommittedSwaggerBaseline_MatchesLiveApiEndpointSet()
    {
        var liveEndpoints = ReadLiveEndpoints();
        liveEndpoints.Should().NotBeEmpty(
            "the ApiExplorer must expose the real controller endpoints, otherwise this gate proves nothing");

        var committedEndpoints = ReadCommittedEndpoints(ReadBaselineDocument());

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

    // The live surface, straight from the API's ApiExplorer (the same feed Swashbuckle serializes).
    private static HashSet<string> ReadLiveEndpoints()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers()
            .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);
        services.AddRouting();

        using var provider = services.BuildServiceProvider();
        var apiExplorer = provider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        return apiExplorer.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Select(description => Endpoint(
                description.HttpMethod ?? "GET",
                "/" + (description.RelativePath ?? string.Empty)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ReadCommittedEndpoints(JsonDocument document)
    {
        var endpoints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                endpoints.Add(Endpoint(operation.Name, path.Name));
            }
        }

        return endpoints;
    }

    private static string Endpoint(string method, string path) =>
        $"{method.ToUpperInvariant()} {path.TrimEnd('/')}";

    private static JsonDocument ReadBaselineDocument()
    {
        var baselinePath = Path.Combine(ResolveRepositoryRoot(), BaselineRelativePath);
        File.Exists(baselinePath).Should().BeTrue(
            $"the committed API contract baseline must exist at {BaselineRelativePath}");
        return JsonDocument.Parse(File.ReadAllText(baselinePath));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, BaselineRelativePath)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not resolve the repository root (searched upward from {AppContext.BaseDirectory} for {BaselineRelativePath}).");
    }
}
