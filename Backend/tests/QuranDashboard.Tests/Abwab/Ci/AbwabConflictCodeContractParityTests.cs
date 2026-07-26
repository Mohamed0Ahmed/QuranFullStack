using System.Reflection;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab.Ci;

public sealed class AbwabConflictCodeContractParityTests
{
    private const string ConflictContractRelativePath = "Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab-conflict.ts";
    private const string FrontendAbwabFeatureRelativePath = "Frontend/quran-dashboard-ui/src/app/features/abwab";

    private static readonly string[] NewEndpointFamilyPrefixes = ["/api/abwab/relationships", "/api/abwab/templates"];

    private static readonly string[] CodesUsedBy030 =
    [
        AbwabConflictCodes.RelationshipDuplicate,
        AbwabConflictCodes.RelationshipCycle,
        AbwabConflictCodes.TemplateCycle,
        AbwabConflictCodes.TemplateRevisionStale,
        AbwabConflictCodes.RowStale,
        AbwabConflictCodes.TimelineGenerationStale,
        AbwabConflictCodes.TreeRevisionStale,
        AbwabConflictCodes.ManualProtection,
        AbwabConflictCodes.CategoryNameConflict,
        AbwabConflictCodes.CategoryUnavailable,
        AbwabConflictCodes.StabilizationActive,
    ];

    private static readonly Regex AnyAbwabCodePattern = new("abwab\\.[a-z_]+", RegexOptions.Compiled);
    private static readonly Regex UnionEntryPattern = new("'(abwab\\.[a-z_]+)'", RegexOptions.Compiled);
    private static readonly Regex MessageEntryPattern = new("'(abwab\\.[a-z_]+)':\\s*'([^']*)'", RegexOptions.Compiled);

    // ContractDriftTests already proves committed == live for the WHOLE surface, so what remains for
    // 030's families is that they are in that surface at all; the two together are §15.2 gate 4 for
    // the new endpoint families, without restating the equality.
    [Fact]
    public void BothNewEndpointFamilies_ArePresentInTheLiveApiSurface()
    {
        var live = ApiContractSources.ReadLiveEndpoints();

        foreach (var prefix in NewEndpointFamilyPrefixes)
        {
            live.Should().Contain(endpoint => endpoint.Contains(prefix, StringComparison.Ordinal),
                $"the '{prefix}' family is part of 030's delivered surface; if it stops being discoverable the "
                + "committed contract would drift green rather than failing");
        }
    }

    [Fact]
    public void Every030Code_IsCarriedByTheSharedFrontendConflictContract_WithAnArabicMessage()
    {
        var contract = File.ReadAllText(ApiContractSources.RepositoryPath(ConflictContractRelativePath));

        var union = ParseUnionCodes(contract);
        var messages = ParseMessages(contract);

        union.Should().Contain(CodesUsedBy030,
            "every abwab.* code 030's endpoints can return must be in the frontend code union, or the HTTP adapter "
            + "degrades it to an untyped error");
        messages.Keys.Should().BeEquivalentTo(union,
            "the union and the message map must cover each other exactly — a code with no message renders blank");

        var unusable = messages
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value) || !entry.Value.Any(IsArabicLetter))
            .Select(entry => entry.Key)
            .ToList();

        unusable.Should().BeEmpty(
            "this is an Arabic-first product: every conflict message the UI renders must be non-empty Arabic, "
            + "never blank and never left in English; offenders: " + string.Join(", ", unusable));
    }

    [Fact]
    public void TheFrontendCodeUnion_ContainsNoCodeTheBackendDoesNotDeclare()
    {
        var union = ParseUnionCodes(File.ReadAllText(ApiContractSources.RepositoryPath(ConflictContractRelativePath)));

        union.Should().BeSubsetOf(BackendDeclaredCodes(),
            "the frontend must not invent, rename, or remap an abwab.* code the backend never emits");
    }

    [Fact]
    public void NoFrontendAbwabSourceFile_UsesAnAbwabCodeOutsideTheSharedUnion()
    {
        var union = ParseUnionCodes(File.ReadAllText(ApiContractSources.RepositoryPath(ConflictContractRelativePath)));
        var featureRoot = ApiContractSources.RepositoryPath(FrontendAbwabFeatureRelativePath);

        var offenders = Directory
            .EnumerateFiles(featureRoot, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".ts", StringComparison.Ordinal) || file.EndsWith(".html", StringComparison.Ordinal))
            .SelectMany(file => AnyAbwabCodePattern
                .Matches(File.ReadAllText(file))
                .Select(match => (Path: Path.GetRelativePath(featureRoot, file), Code: match.Value)))
            .Where(hit => !union.Contains(hit.Code))
            .ToList();

        offenders.Should().BeEmpty(
            "the mock, HTTP adapter, facades, and UI must all branch on the same shared union — 0 invented, "
            + "0 renamed, 0 remapped; offenders: "
            + string.Join("; ", offenders.Select(o => $"{o.Path}:{o.Code}")));
    }

    private static bool IsArabicLetter(char value) => value is >= '؀' and <= 'ۿ';

    private static HashSet<string> ParseUnionCodes(string conflictContract)
    {
        var union = UnionEntryPattern
            .Matches(Block(conflictContract, "export const ABWAB_CONFLICT_CODES", "] as const;"))
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        union.Should().NotBeEmpty("the ABWAB_CONFLICT_CODES union must be parsable, otherwise this gate reads nothing");
        return union;
    }

    private static Dictionary<string, string> ParseMessages(string conflictContract)
    {
        var messages = MessageEntryPattern
            .Matches(Block(conflictContract, "export const ABWAB_CONFLICT_MESSAGES", "};"))
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value, StringComparer.Ordinal);

        messages.Should().NotBeEmpty("the ABWAB_CONFLICT_MESSAGES map must be parsable, otherwise this gate reads nothing");
        return messages;
    }

    private static string Block(string content, string startMarker, string endMarker)
    {
        var start = content.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"'{startMarker}' must exist in abwab-conflict.ts for this gate to read anything");

        var end = content.IndexOf(endMarker, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, $"'{startMarker}' must be terminated by '{endMarker}'");

        return content[start..end];
    }

    private static HashSet<string> BackendDeclaredCodes() =>
        typeof(AbwabConflictCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);
}
