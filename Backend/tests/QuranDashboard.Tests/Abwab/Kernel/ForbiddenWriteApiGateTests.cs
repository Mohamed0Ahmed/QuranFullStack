namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-038 / SC-013 (T081): a source/architecture gate that fails the build when an Abwab writer namespace
// references a forbidden write/bypass API. DISTINCT from the SavingChanges interceptor-skip guard (that is
// a runtime interceptor; this is a build-time source scan). A reviewed allowlist entry requires an owner
// and a reason.
public sealed class ForbiddenWriteApiGateTests
{
    private static readonly string[] ForbiddenApis =
    [
        "ExecuteUpdate",
        "ExecuteDelete",
        "ExecuteSqlRaw",
        "ExecuteSqlInterpolated",
        "DbCommand",
        "NpgsqlConnection",
        "NpgsqlCommand",
        "BeginBinaryImport",
        "BeginBinaryExport",
    ];

    // Narrow reviewed allowlist: non-product, non-revertible infrastructure only. Each entry MUST carry an
    // owner and a reason. Empty in 028.
    private static readonly IReadOnlyList<AllowlistEntry> Allowlist = [];

    private static readonly string[] AbwabSourceRoots =
    [
        Path.Combine("domain", "QuranDashboard.Domain", "Abwab"),
        Path.Combine("application", "QuranDashboard.Application", "Abwab"),
        Path.Combine("application", "QuranDashboard.Application.Abstractions", "Abwab"),
        Path.Combine("infrastructure", "QuranDashboard.Infrastructure", "Abwab"),
        Path.Combine("api", "QuranDashboard.Api", "Abwab"),
    ];

    [Fact]
    public void NoAbwabWriterSource_ReferencesAForbiddenWriteApi()
    {
        var offenders = ScanForbiddenReferences()
            .Where(hit => !IsAllowlisted(hit))
            .ToList();

        offenders.Should().BeEmpty(
            "Abwab writer source MUST NOT reference a forbidden write/bypass API (§6.1 layer 2); offenders: "
            + string.Join("; ", offenders.Select(o => $"{o.RelativePath}:{o.Api}")));
    }

    [Fact]
    public void Scanner_Detects_AForbiddenReference()
    {
        // Non-vacuity: the detector must actually flag a forbidden token, otherwise the gate proves nothing.
        const string snippet = "var rows = context.Widgets.ExecuteDelete();";
        var hits = ForbiddenApis.Where(api => snippet.Contains(api, StringComparison.Ordinal)).ToList();

        hits.Should().Contain("ExecuteDelete");
    }

    [Fact]
    public void Allowlist_EntriesAreWellFormed()
    {
        foreach (var entry in Allowlist)
        {
            entry.Owner.Should().NotBeNullOrWhiteSpace("an allowlist entry must name an owner");
            entry.Reason.Should().NotBeNullOrWhiteSpace("an allowlist entry must state a reason");
        }
    }

    private static List<(string RelativePath, string Api)> ScanForbiddenReferences()
    {
        var backendRoot = BackendRoot();
        var hits = new List<(string, string)>();

        foreach (var root in AbwabSourceRoots)
        {
            var directory = Path.Combine(backendRoot, root);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                foreach (var api in ForbiddenApis.Where(api => content.Contains(api, StringComparison.Ordinal)))
                {
                    hits.Add((Path.GetRelativePath(backendRoot, file), api));
                }
            }
        }

        return hits;
    }

    private static bool IsAllowlisted((string RelativePath, string Api) hit) =>
        Allowlist.Any(entry =>
            string.Equals(entry.RelativePath, hit.RelativePath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entry.Api, hit.Api, StringComparison.Ordinal));

    private static string BackendRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "Backend"));

    private sealed record AllowlistEntry(string RelativePath, string Api, string Owner, string Reason);
}
