using System.Reflection;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab._Guards;

public sealed class AbwabConflictCodeDriftTests
{
    private const string MasterPlanRelativePath = "docs/feature-abwab-management/MASTER_PLAN.md";
    private const string Section11StartMarker = "conflict catalogue is exact";
    private const string Section11EndMarker = "Malformed field/domain input uses HTTP 400";
    private const int MinimumKnownSection11CatalogueSize = 41;

    private static readonly Regex AnyAbwabCodePattern = new("abwab\\.[a-z_]+", RegexOptions.Compiled);
    private static readonly Regex RelationshipOrTemplateCodePattern = new("abwab\\.[a-z_]*(relationship|template)[a-z_]*", RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> NewlyDeclaredCodesByConstantName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RelationshipDuplicate"] = "abwab.relationship_duplicate",
        ["RelationshipCycle"] = "abwab.relationship_cycle",
        ["TemplateCycle"] = "abwab.template_cycle",
        ["TemplateRevisionStale"] = "abwab.template_revision_stale",
    };

    private static readonly IReadOnlyDictionary<string, string> ReusedCodesByConstantName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RowStale"] = "abwab.row_stale",
        ["TimelineGenerationStale"] = "abwab.timeline_generation_stale",
        ["TreeRevisionStale"] = "abwab.tree_revision_stale",
        ["ManualProtection"] = "abwab.manual_protection",
        ["CategoryNameConflict"] = "abwab.category_name_conflict",
        ["CategoryUnavailable"] = "abwab.category_unavailable",
        ["StabilizationActive"] = "abwab.stabilization_active",
    };

    private static readonly string[] LeafSourceRoots =
    [
        Path.Combine("domain", "QuranDashboard.Domain", "Abwab", "Relationships"),
        Path.Combine("domain", "QuranDashboard.Domain", "Abwab", "Templates"),
        Path.Combine("application", "QuranDashboard.Application.Abstractions", "Abwab", "Relationships"),
        Path.Combine("application", "QuranDashboard.Application.Abstractions", "Abwab", "Templates"),
        Path.Combine("application", "QuranDashboard.Application", "Abwab", "Relationships"),
        Path.Combine("application", "QuranDashboard.Application", "Abwab", "Templates"),
        Path.Combine("api", "QuranDashboard.Api", "Abwab", "Relationships"),
        Path.Combine("api", "QuranDashboard.Api", "Abwab", "Templates"),
    ];

    private static readonly string[] SharedDeclarationPaths =
    [
        Path.Combine("application", "QuranDashboard.Application.Abstractions", "Abwab", "AbwabConflictCodes.cs"),
        Path.Combine("api", "QuranDashboard.Api", "Abwab", "AbwabConflictResponses.cs"),
        Path.Combine("api", "QuranDashboard.Api", "Common", "ApiMessages.cs"),
        Path.Combine("infrastructure", "QuranDashboard.Infrastructure", "Persistence", "Configurations", "Abwab"),
        Path.Combine("infrastructure", "QuranDashboard.Infrastructure", "Persistence", "Reads", "Abwab"),
        Path.Combine("infrastructure", "QuranDashboard.Infrastructure", "Abwab", "Restore"),
    ];

    [Fact]
    public void AllExpectedScanRoots_ExistOnDisk()
    {
        var backendRoot = BackendRoot();
        Directory.Exists(backendRoot).Should().BeTrue(
            $"BackendRoot() must resolve to a real directory (got '{backendRoot}') — a wrong root would silently scan nothing and pass green");

        var missing = LeafSourceRoots.Concat(SharedDeclarationPaths)
            .Select(root => Path.Combine(backendRoot, root))
            .Where(resolved => !File.Exists(resolved) && !Directory.Exists(resolved))
            .ToList();

        missing.Should().BeEmpty(
            "every declared 030 scan root must exist so this drift guard actually visits it rather than silently skipping it; missing: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void NewlyDeclaredCodes_AreGenuineMasterPlanSection11Members()
    {
        NewlyDeclaredCodesByConstantName.Values.Should().BeSubsetOf(
            ParseMasterPlanSection11Codes(),
            "each of the four codes 030 declares must be a genuine §11 member, not an addition");
    }

    [Fact]
    public void ReusedCodes_AreGenuineMasterPlanSection11Members()
    {
        ReusedCodesByConstantName.Values.Should().BeSubsetOf(
            ParseMasterPlanSection11Codes(),
            "each code 030 reuses as-is must be a genuine §11 member");
    }

    [Fact]
    public void ReusedCodes_StillMatchTheirFrozenSection11ValueInAbwabConflictCodes()
    {
        var declared = DeclaredAbwabConflictCodesByName();

        declared.Keys.Should().Contain(ReusedCodesByConstantName.Keys, "030 reuses each of these AbwabConflictCodes constants as-is");

        var actual = ReusedCodesByConstantName.Keys
            .Where(declared.ContainsKey)
            .ToDictionary(name => name, name => declared[name], StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(ReusedCodesByConstantName,
            "every reused AbwabConflictCodes constant must keep its frozen §11 value, never renamed/remapped");
    }

    [Fact]
    public void NewlyDeclaredCodes_MatchTheirFrozenSection11Value_OnceDeclared()
    {
        var declared = DeclaredAbwabConflictCodesByName();

        var expected = NewlyDeclaredCodesByConstantName
            .Where(pair => declared.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        var actual = expected.Keys.ToDictionary(name => name, name => declared[name], StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(expected,
            "each declared AbwabConflictCodes constant among the four new 030 codes must equal its exact frozen "
            + "§11 string, never renamed/remapped (vacuous per-name until T027/T062 declare it)");
    }

    [Fact]
    public void No030OwnedSourceFile_UsesAnAbwabDotStarStringOutsideTheAllowedSet()
    {
        var allowed = NewlyDeclaredCodesByConstantName.Values
            .Concat(ReusedCodesByConstantName.Values)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = ScanLiterals(LeafSourceRoots, AnyAbwabCodePattern)
            .Where(hit => !allowed.Contains(hit.Code))
            .ToList();

        offenders.Should().BeEmpty(
            "030-owned folders must use only 030's allowed abwab.* codes — 0 invented, 0 renamed, 0 remapped, "
            + "0 borrowed from another domain (vacuous until 030 production files exist); offenders: "
            + string.Join("; ", offenders.Select(o => $"{o.RelativePath}:{o.Code}")));
    }

    [Fact]
    public void SharedDeclarationFiles_UseOnlyTheFrozen030RelationshipOrTemplateCodeSpelling()
    {
        var offenders = ScanLiterals(SharedDeclarationPaths, RelationshipOrTemplateCodePattern)
            .Where(hit => !NewlyDeclaredCodesByConstantName.Values.Contains(hit.Code))
            .ToList();

        offenders.Should().BeEmpty(
            "030's relationship_/template_ conflict codes, wherever declared or mapped across the shared "
            + "AbwabConflictCodes/AbwabConflictResponses/ApiMessages/infrastructure files, must spell exactly "
            + "one of the four frozen §11 strings — 0 renamed/remapped; offenders: "
            + string.Join("; ", offenders.Select(o => $"{o.RelativePath}:{o.Code}")));
    }

    private static Dictionary<string, string> DeclaredAbwabConflictCodesByName() =>
        typeof(AbwabConflictCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!, StringComparer.Ordinal);

    private static List<string> ParseMasterPlanSection11Codes()
    {
        var path = Path.Combine(RepositoryRoot(), MasterPlanRelativePath);
        File.Exists(path).Should().BeTrue($"the Master Plan must exist at '{MasterPlanRelativePath}' for this drift guard to have ground truth");

        var content = File.ReadAllText(path);
        var startIndex = content.IndexOf(Section11StartMarker, StringComparison.Ordinal);
        var endIndex = content.IndexOf(Section11EndMarker, StringComparison.Ordinal);

        (startIndex >= 0 && endIndex > startIndex).Should().BeTrue(
            "the §11 HTTP 409 conflict catalogue markers must be present and ordered in the Master Plan — "
            + "its structure may have changed and this parser needs updating");

        var codes = Regex.Matches(content[startIndex..endIndex], "`(abwab\\.[a-z_]+)`")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        codes.Count.Should().BeGreaterThanOrEqualTo(MinimumKnownSection11CatalogueSize,
            "the parsed §11 catalogue must yield its full known size, otherwise this parser is silently reading the wrong section");

        return codes;
    }

    private static List<(string RelativePath, string Code)> ScanLiterals(IReadOnlyList<string> roots, Regex pattern)
    {
        var backendRoot = BackendRoot();
        var hits = new List<(string, string)>();

        foreach (var root in roots)
        {
            var resolved = Path.Combine(backendRoot, root);
            var files = File.Exists(resolved)
                ? [resolved]
                : Directory.EnumerateFiles(resolved, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                foreach (Match match in pattern.Matches(content))
                {
                    hits.Add((Path.GetRelativePath(backendRoot, file), match.Value));
                }
            }
        }

        return hits;
    }

    private static string BackendRoot() => Path.Combine(RepositoryRoot(), "Backend");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, MasterPlanRelativePath)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not resolve the repository root (searched upward from {AppContext.BaseDirectory} for {MasterPlanRelativePath}).");
    }
}
