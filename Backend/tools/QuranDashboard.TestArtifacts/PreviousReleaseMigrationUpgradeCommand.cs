using System.Diagnostics;
using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

// Adoption is intentionally database-free: a bad release declaration or Git inventory must be rejected
// before a caller can create, select, or mutate a rehearsal target.
internal static class PreviousReleaseMigrationUpgradeCommand
{
    private const string DeclarationRelativePath = "docs/testing/previous-release-migration-upgrade.json";
    private const string DeclarationSchema = "docs/testing/previous-release-migration-upgrade.schema.json";
    private const string AuthoritativeSha = "df07306b5a5ebe08ff205c0d2f6cd5a10af87f2d";
    private const string SupplementalSha = "08b161f4f41c390c8332cd1842e3bdec6c03e322";
    private static readonly string[] FiveMigrations =
    [
        "20260813153400_InitialBaseline",
        "20260814153559_M2DurablePreparedLinkingPreflight",
        "20260814212547_M3DurableLinkingConfirmationJobs",
        "20260815175846_AddUserDeviceSessions",
        "20260817163513_AddAbwabDoorInclusionSynchronization",
    ];
    private static readonly string[] SixMigrations = [.. FiveMigrations, "20260826012918_AddQuranPhraseSearchIndex"];

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

            VerifyLocalGit(repositoryRoot, declaration);
            VerifyLockedArtifact(repositoryRoot, declaration);
            output.WriteLine(
                "previous-release-upgrade state=verified " +
                "authoritative-forward-migrations=0 supplemental-forward-migrations=1");
            return 0;
        }
        catch (PreviousReleaseVerificationException exception)
        {
            output.WriteLine($"previous-release-upgrade state=mismatched detail={exception.Detail}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException
            or JsonException
            or InvalidDataException
            or UnauthorizedAccessException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            output.WriteLine("previous-release-upgrade state=mismatched detail=declaration-or-local-verification-unreadable");
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
        if (declaration.Schema != DeclarationSchema || declaration.ContractVersion != 2 || declaration.Status != "adopted")
        {
            return "declaration-schema-or-version-is-invalid";
        }

        if (!ReleaseMatches(
                declaration.AuthoritativePreviousRelease,
                AuthoritativeSha,
                "authoritative-previous-release",
                "6158870536",
                "17506058851",
                "2026-08-29T18:38:39Z",
                SixMigrations)
            || !ReleaseMatches(
                declaration.SupplementalRehearsalBaseline,
                SupplementalSha,
                "supplemental-historical-rehearsal-baseline",
                "6074244346",
                "17279084675",
                "2026-08-25T00:50:52Z",
                FiveMigrations))
        {
            return "release-declaration-does-not-match-adopted-evidence";
        }

        if (declaration.Artifact is null
            || declaration.Artifact.Id != "quran-canonical"
            || declaration.Artifact.PayloadSha256 != "3d4038d561a2b4b048e72c05f0cc472b2b1bcf0f2af0d09d0c054cff38e9b29d"
            || declaration.Artifact.PayloadSize != 372143834
            || declaration.Artifact.ManifestSha256 != "3b2d15dbc30d8dbe5010f1d373e6a33b8e089902b6347ebb6f561f18874bec3e"
            || declaration.Artifact.ManifestSize != 1363
            || !MigrationMatches(declaration.Artifact.Migration, SixMigrations)
            || declaration.Artifact.TableScope is null
            || !declaration.Artifact.TableScope.SequenceEqual(RequiredQuranTables, StringComparer.Ordinal)
            || declaration.Artifact.TableCounts is null
            || declaration.Artifact.TableCounts.Count != RequiredQuranTables.Length
            || declaration.Artifact.TableCounts.Any(count => !RequiredQuranTables.Contains(count.Name, StringComparer.Ordinal) || count.Rows < 0))
        {
            return "representative-artifact-declaration-does-not-match-adoption";
        }

        if (declaration.Expectations is null
            || !declaration.Expectations.AuthoritativeForwardMigrationIds.SequenceEqual([], StringComparer.Ordinal)
            || !declaration.Expectations.SupplementalForwardMigrationIds.SequenceEqual([SixMigrations[^1]], StringComparer.Ordinal)
            || declaration.Expectations.PreUpgradeSentinel is not { Table: "quran_ayahs", ExpectedCount: 6236 }
            || declaration.Expectations.PostUpgradeSentinel is not { Table: "quran_ayahs", ExpectedCount: 6236 }
            || declaration.Expectations.PhraseSearch is not { StateTable: "quran_phrase_index_state", ExpectedRows: 1, ActiveBuild: "none" })
        {
            return "rehearsal-expectations-do-not-match-adoption";
        }

        return null;
    }

    internal static PreviousReleaseMigrationUpgradePlan VerifyAdoption(string repositoryRoot)
    {
        var declaration = StrictJson.Read<PreviousReleaseMigrationUpgradeDeclaration>(
            Path.Combine(repositoryRoot, DeclarationRelativePath),
            "Previous-release migration upgrade declaration");
        var issue = Validate(declaration);
        if (issue is not null)
        {
            throw new PreviousReleaseVerificationException(issue);
        }

        VerifyLocalGit(repositoryRoot, declaration);
        VerifyLockedArtifact(repositoryRoot, declaration);
        return new PreviousReleaseMigrationUpgradePlan(
            declaration.AuthoritativePreviousRelease!,
            declaration.SupplementalRehearsalBaseline!,
            declaration.Artifact!,
            declaration.Expectations!);
    }

    private static void VerifyLocalGit(string repositoryRoot, PreviousReleaseMigrationUpgradeDeclaration declaration)
    {
        VerifyGitInventory(repositoryRoot, declaration.AuthoritativePreviousRelease!, SixMigrations);
        VerifyGitInventory(repositoryRoot, declaration.SupplementalRehearsalBaseline!, FiveMigrations);
        VerifyWorkingMigrationSources(repositoryRoot, declaration.AuthoritativePreviousRelease!);
        VerifyWorkingMigrationSources(repositoryRoot, declaration.SupplementalRehearsalBaseline!);
        var current = RepositoryMigrationInventory.Read(repositoryRoot);
        if (!current.SequenceEqual(SixMigrations, StringComparer.Ordinal))
        {
            throw new PreviousReleaseVerificationException("current-migration-inventory-does-not-match-declared-head");
        }
    }

    private static void VerifyWorkingMigrationSources(string repositoryRoot, PreviousReleaseReference release)
    {
        if (!LocalGit.WorkingMigrationSourcesMatch(repositoryRoot, release.Sha, release.Migration!.Inventory))
        {
            throw new PreviousReleaseVerificationException("working-historical-migration-sources-do-not-match-declared-release");
        }
    }

    private static void VerifyGitInventory(string repositoryRoot, PreviousReleaseReference release, IReadOnlyList<string> expected)
    {
        if (!LocalGit.CommitExists(repositoryRoot, release.Sha))
        {
            throw new PreviousReleaseVerificationException("declared-git-commit-is-unavailable");
        }

        var inventory = LocalGit.MigrationInventory(repositoryRoot, release.Sha);
        if (!inventory.SequenceEqual(expected, StringComparer.Ordinal)
            || !inventory.SequenceEqual(release.Migration!.Inventory, StringComparer.Ordinal))
        {
            throw new PreviousReleaseVerificationException("declared-git-migration-inventory-mismatched");
        }
    }

    private static void VerifyLockedArtifact(string repositoryRoot, PreviousReleaseMigrationUpgradeDeclaration declaration)
    {
        var artifactLock = ArtifactTrustLock.ReadFrom(Path.Combine(repositoryRoot, ArtifactTrustLock.FileName));
        var issue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (issue is not null)
        {
            throw new PreviousReleaseVerificationException("artifact-lock-is-invalid");
        }

        var artifact = artifactLock.Artifacts.SingleOrDefault(candidate => candidate.Id == declaration.Artifact!.Id);
        if (artifact is null
            || artifact.Migration.Head != declaration.Artifact!.Migration!.Head
            || artifact.Migration.Count != declaration.Artifact.Migration.Count
            || !artifact.TableScope.Tables.SequenceEqual(declaration.Artifact.TableScope!, StringComparer.Ordinal)
            || artifact.TableCounts is null
            || !artifact.TableCounts.SequenceEqual(declaration.Artifact.TableCounts!)
            || artifact.StagedFiles.SingleOrDefault(file => file.Role == "payload") is not { } payload
            || payload.Sha256 != declaration.Artifact.PayloadSha256
            || payload.Size != declaration.Artifact.PayloadSize
            || artifact.StagedFiles.SingleOrDefault(file => file.Role == "manifest") is not { } manifest
            || manifest.Sha256 != declaration.Artifact.ManifestSha256
            || manifest.Size != declaration.Artifact.ManifestSize)
        {
            throw new PreviousReleaseVerificationException("representative-artifact-lock-mismatched");
        }
    }

    private static bool ReleaseMatches(
        PreviousReleaseReference? release,
        string sha,
        string role,
        string deploymentId,
        string statusId,
        string completedAtUtc,
        IReadOnlyList<string> migrations)
    {
        return release is not null
            && release.Sha == sha
            && release.Role == role
            && release.Deployment is { Id: var id, StatusId: var status, State: "success", CompletedAtUtc: var completed }
            && id == deploymentId
            && status == statusId
            && completed == completedAtUtc
            && MigrationMatches(release.Migration, migrations);
    }

    private static bool MigrationMatches(PreviousReleaseMigration? migration, IReadOnlyList<string> inventory) =>
        migration is not null
        && migration.Count == inventory.Count
        && migration.Head == inventory[^1]
        && migration.Inventory.SequenceEqual(inventory, StringComparer.Ordinal);

    private static readonly string[] RequiredQuranTables =
    [
        "quran_ayahs", "quran_full_i3rab_ayah_entries", "quran_full_i3rab_entries", "quran_full_i3rab_sources", "quran_hizbs", "quran_i3rab_rules", "quran_juzs", "quran_lemma_analyses", "quran_lemmas", "quran_mushaf_lines", "quran_mushaf_pages", "quran_mutashabihat_groups", "quran_mutashabihat_occurrences", "quran_pos_tags", "quran_roots", "quran_rubs", "quran_sajdas", "quran_similar_ayah_links", "quran_stems", "quran_surahs", "quran_tafsir_ayah_entries", "quran_tafsir_entries", "quran_tafsir_sources", "quran_translation_ayah_entries", "quran_translation_sources", "quran_word_morphology", "quran_word_morphology_segments", "quran_words", "quran_words_ordered_simple", "quran_words_ordered_tashkeel", "quran_words_unique_simple", "quran_words_unique_tashkeel",
    ];
}

internal sealed record PreviousReleaseMigrationUpgradeDeclaration(
    [property: System.Text.Json.Serialization.JsonPropertyName("$schema")] string Schema,
    int ContractVersion,
    string Status,
    PreviousReleaseReference? AuthoritativePreviousRelease,
    PreviousReleaseReference? SupplementalRehearsalBaseline,
    PreviousReleaseArtifact? Artifact,
    PreviousReleaseExpectations? Expectations);

internal sealed record PreviousReleaseReference(
    string Sha,
    string Role,
    PreviousReleaseDeployment? Deployment,
    PreviousReleaseMigration? Migration);

internal sealed record PreviousReleaseDeployment(string Id, string StatusId, string State, string CompletedAtUtc);
internal sealed record PreviousReleaseMigration(string Head, int Count, IReadOnlyList<string> Inventory);
internal sealed record PreviousReleaseArtifact(
    string Id,
    string PayloadSha256,
    long PayloadSize,
    string ManifestSha256,
    long ManifestSize,
    PreviousReleaseMigration? Migration,
    IReadOnlyList<string>? TableScope,
    IReadOnlyList<ArtifactManifestTable>? TableCounts);
internal sealed record PreviousReleaseExpectations(
    IReadOnlyList<string> AuthoritativeForwardMigrationIds,
    IReadOnlyList<string> SupplementalForwardMigrationIds,
    PreviousReleaseTableSentinel? PreUpgradeSentinel,
    PreviousReleaseTableSentinel? PostUpgradeSentinel,
    PreviousReleasePhraseSearchExpectation? PhraseSearch);
internal sealed record PreviousReleaseTableSentinel(string Table, long ExpectedCount);
internal sealed record PreviousReleasePhraseSearchExpectation(string StateTable, long ExpectedRows, string ActiveBuild);
internal sealed record PreviousReleaseMigrationUpgradePlan(
    PreviousReleaseReference AuthoritativePreviousRelease,
    PreviousReleaseReference SupplementalRehearsalBaseline,
    PreviousReleaseArtifact Artifact,
    PreviousReleaseExpectations Expectations);
internal sealed class PreviousReleaseVerificationException(string detail) : InvalidOperationException
{
    internal string Detail { get; } = detail;
}

internal static class RepositoryMigrationInventory
{
    internal static IReadOnlyList<string> Read(string repositoryRoot) => Directory
        .EnumerateFiles(Path.Combine(repositoryRoot, "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations"), "*.cs")
        .Select(Path.GetFileNameWithoutExtension)
        .Where(name => name is not null && ArtifactTrustLockValidator.IsMigrationId(name))
        .Order(StringComparer.Ordinal)
        .Cast<string>()
        .ToArray();
}

internal static class LocalGit
{
    private const string MigrationsPath = "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations";

    internal static bool CommitExists(string repositoryRoot, string sha) => Run(repositoryRoot, ["cat-file", "-e", $"{sha}^{{commit}}"], out _);

    internal static IReadOnlyList<string> MigrationInventory(string repositoryRoot, string sha)
    {
        if (!Run(repositoryRoot, ["ls-tree", "-r", "--name-only", sha, "--", MigrationsPath], out var output))
        {
            throw new PreviousReleaseVerificationException("local-git-migration-inventory-unreadable");
        }

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && ArtifactTrustLockValidator.IsMigrationId(name))
            .Order(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
    }

    internal static bool WorkingMigrationSourcesMatch(
        string repositoryRoot,
        string sha,
        IReadOnlyList<string> migrationIds)
    {
        var paths = migrationIds
            .SelectMany(migrationId => new[]
            {
                $"{MigrationsPath}/{migrationId}.cs",
                $"{MigrationsPath}/{migrationId}.Designer.cs",
            })
            .ToArray();

        return paths.Length != 0
            && paths.All(path => Run(repositoryRoot, ["cat-file", "-e", $"{sha}:{path}"], out _))
            && Run(repositoryRoot, ["diff", "--quiet", sha, "--", .. paths], out _);
    }

    private static bool Run(string repositoryRoot, IReadOnlyList<string> arguments, out string output)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new PreviousReleaseVerificationException("local-git-is-unavailable");
        output = process.StandardOutput.ReadToEnd();
        _ = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
