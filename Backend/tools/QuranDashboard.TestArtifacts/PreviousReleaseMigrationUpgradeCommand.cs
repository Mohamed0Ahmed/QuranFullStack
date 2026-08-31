using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

// This is deliberately read-only. A release upgrade cannot start until its source release and data are
// independently adopted; guessing either would turn a disposable rehearsal into fabricated evidence.
internal static class PreviousReleaseMigrationUpgradeCommand
{
    private const string DeclarationRelativePath = "docs/testing/previous-release-migration-upgrade.json";
    private const string DeclarationSchema = "docs/testing/previous-release-migration-upgrade.schema.json";
    private static readonly string[] RequiredBlockers =
    [
        "authoritative-previous-release-ref",
        "approved-prior-schema-representative-artifact",
    ];

    internal static int Execute(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        var repositoryRoot = Parse(args, error);
        if (repositoryRoot is null)
        {
            return 2;
        }

        try
        {
            var declaration = StrictJson.Read<PreviousReleaseMigrationUpgradeDeclaration>(
                Path.Combine(repositoryRoot, DeclarationRelativePath),
                "Previous-release migration upgrade declaration");
            var issue = Validate(declaration);
            if (issue is not null)
            {
                output.WriteLine($"previous-release-upgrade state=mismatched detail={issue}");
                return 1;
            }

            output.WriteLine(
                "previous-release-upgrade state=blocked " +
                "detail=missing-authoritative-previous-release-ref-and-approved-prior-schema-representative-artifact");
            return 1;
        }
        catch (Exception exception) when (exception is IOException
            or JsonException
            or InvalidDataException
            or UnauthorizedAccessException)
        {
            output.WriteLine("previous-release-upgrade state=mismatched detail=declaration-unreadable");
            return 1;
        }
    }

    private static string? Parse(IReadOnlyList<string> args, TextWriter error)
    {
        if (args.Count is not 1 and not 3
            || (args.Count == 3 && (args[1] != "--root" || string.IsNullOrWhiteSpace(args[2]))))
        {
            error.WriteLine("Usage: test-artifacts previous-release-upgrade [--root REPOSITORY_ROOT]");
            return null;
        }

        return Path.GetFullPath(args.Count == 3 ? args[2] : Directory.GetCurrentDirectory());
    }

    internal static string? Validate(PreviousReleaseMigrationUpgradeDeclaration declaration)
    {
        if (declaration.Schema != DeclarationSchema
            || declaration.ContractVersion != 1
            || declaration.Status != "blocked")
        {
            return "declaration-must-remain-blocked-until-release-evidence-is-adopted";
        }

        return declaration.Blockers.Order(StringComparer.Ordinal).SequenceEqual(
            RequiredBlockers.Order(StringComparer.Ordinal),
            StringComparer.Ordinal)
            ? null
            : "declaration-must-name-both-required-adoption-blockers";
    }
}

internal sealed record PreviousReleaseMigrationUpgradeDeclaration(
    [property: System.Text.Json.Serialization.JsonPropertyName("$schema")] string Schema,
    int ContractVersion,
    string Status,
    IReadOnlyList<string> Blockers);
