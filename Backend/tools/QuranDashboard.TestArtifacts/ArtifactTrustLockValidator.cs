using System.Globalization;

namespace QuranDashboard.TestArtifacts;

internal static class ArtifactTrustLockValidator
{
    private static readonly IReadOnlySet<string> AllowedFileRoles = new HashSet<string>(
        ["manifest", "oracle", "payload"],
        StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> AccessTableNames = new HashSet<string>(
        [
            "access_audit_events",
            "permissions",
            "roles",
            "user_device_sessions",
            "user_permissions",
            "users",
        ],
        StringComparer.Ordinal);

    internal static string? Validate(ArtifactTrustLock artifactLock)
    {
        if (!string.Equals(
                artifactLock.Schema,
                ArtifactTrustLock.SchemaPath,
                StringComparison.Ordinal))
        {
            return $"$schema must be '{ArtifactTrustLock.SchemaPath}'";
        }

        if (artifactLock.ContractVersion != 1)
        {
            return $"unsupported lock contractVersion {artifactLock.ContractVersion}";
        }

        var duplicateArtifactId = DuplicateOf(
            artifactLock.Artifacts.Select(artifact => artifact.Id));
        if (duplicateArtifactId is not null)
        {
            return $"duplicate artifact id '{duplicateArtifactId}'";
        }

        foreach (var artifact in artifactLock.Artifacts)
        {
            var issue = ValidateArtifact(artifact);
            if (issue is not null)
            {
                return $"artifact '{artifact.Id}': {issue}";
            }
        }

        return null;
    }

    private static string? ValidateArtifact(LockedArtifact artifact)
    {
        if (!IsSafeToken(artifact.Id) || !IsSafeVersion(artifact.Version))
        {
            return "id or version is invalid";
        }

        if (artifact.RequiredLanes.Count == 0
            || artifact.RequiredLanes.Any(lane => !IsSafeToken(lane))
            || DuplicateOf(artifact.RequiredLanes) is not null)
        {
            return "requiredLanes must contain unique safe lane identifiers";
        }

        if (artifact.StagedFiles.Count == 0)
        {
            return "stagedFiles must not be empty";
        }

        var duplicatePath = DuplicateOf(artifact.StagedFiles.Select(file => file.Path));
        if (duplicatePath is not null)
        {
            return $"duplicate staged path '{duplicatePath}'";
        }

        foreach (var file in artifact.StagedFiles)
        {
            if (!IsSafeRelativePath(file.Path))
            {
                return $"staged path '{file.Path}' must be repository-relative without traversal";
            }

            if (!AllowedFileRoles.Contains(file.Role))
            {
                return $"staged file '{file.Path}' has unsupported role '{file.Role}'";
            }

            if (file.Size < 0 || !IsSha256(file.Sha256))
            {
                return $"staged file '{file.Path}' has an invalid size or sha256";
            }
        }

        var manifestFiles = artifact.StagedFiles
            .Where(file => file.Role == "manifest")
            .ToArray();
        if (manifestFiles.Length != 1
            || !string.Equals(
                manifestFiles[0].Path,
                artifact.ManifestPath,
                StringComparison.Ordinal))
        {
            return "manifestPath must identify the single staged manifest file";
        }

        if (!artifact.StagedFiles.Any(file => file.Role == "payload"))
        {
            return "stagedFiles must identify at least one payload file";
        }

        if (!IsMigrationId(artifact.Migration.Head) || artifact.Migration.Count <= 0)
        {
            return "migration head or count is invalid";
        }

        var invalidTable = artifact.TableScope.Tables
            .FirstOrDefault(table => !IsValidTableIdentifier(table));
        if (invalidTable is not null)
        {
            return $"invalid table identifier '{invalidTable}'";
        }

        if (artifact.TableScope.Tables.Count == 0
            || DuplicateOf(artifact.TableScope.Tables) is not null
            || !new[]
            {
                artifact.TableScope.Quran,
                artifact.TableScope.PhraseSearch,
                artifact.TableScope.Abwab,
                artifact.TableScope.Access,
                artifact.TableScope.Linking,
            }.Any(present => present))
        {
            return "tableScope must name unique tables and at least one present data family";
        }

        var scopeIssue = ValidateTableFamilyScope(artifact);
        if (scopeIssue is not null)
        {
            return scopeIssue;
        }

        if (string.IsNullOrWhiteSpace(artifact.PostgreSql.ProducerVersion)
            || !artifact.PostgreSql.ContainerDigest.StartsWith(
                "sha256:",
                StringComparison.Ordinal)
            || !IsSha256(artifact.PostgreSql.ContainerDigest["sha256:".Length..]))
        {
            return "PostgreSQL producer version or container digest is invalid";
        }

        if (string.IsNullOrWhiteSpace(artifact.Producer.Command)
            || !IsSafeVersion(artifact.Producer.Version))
        {
            return "producer command or version is invalid";
        }

        if (artifact.Sources.Count == 0
            || DuplicateOf(artifact.Sources.Select(source => source.Id)) is not null
            || artifact.Sources.Any(source =>
                !IsSafeToken(source.Id)
                || !IsSafeVersion(source.Version)
                || !IsSha256(source.Sha256)
                || string.IsNullOrWhiteSpace(source.Provenance)))
        {
            return "sources must carry unique identity, version, sha256, and provenance";
        }

        if (artifact.Sentinels.Count == 0
            || DuplicateOf(artifact.Sentinels.Select(sentinel => sentinel.Id)) is not null
            || artifact.Sentinels.Any(sentinel =>
                !IsSafeToken(sentinel.Id)
                || sentinel.ExpectedCount < 0
                || !IsSha256(sentinel.OracleSha256)))
        {
            return "sentinels must carry unique identity, non-negative counts, and oracle hashes";
        }

        if (!IsImmutableCredentialFreeStorageIdentity(artifact.ImmutableStorageId))
        {
            return "immutableStorageId must be a credential-free immutable logical identifier";
        }

        if (!DateOnly.TryParseExact(
                artifact.Refresh.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _)
            || string.IsNullOrWhiteSpace(artifact.Refresh.Reason)
            || !IsSafeToken(artifact.Refresh.OwnerRole))
        {
            return "refresh date, reason, or owner role is invalid";
        }

        if (artifact.PhraseSearch is not null
            && (!IsSha256(artifact.PhraseSearch.ManifestSha256)
                || string.IsNullOrWhiteSpace(artifact.PhraseSearch.SourceFingerprint)
                || artifact.PhraseSearch.ReadinessExpectation is not "available" and not "unavailable"))
        {
            return "PhraseSearch manifest hash, fingerprint, or readiness expectation is invalid";
        }

        var restoreIssue = ValidateRestoreContract(artifact);
        if (restoreIssue is not null)
        {
            return restoreIssue;
        }

        return null;
    }

    private static string? ValidateRestoreContract(LockedArtifact artifact)
    {
        var appliesOnlyToScheduledOrRelease = artifact.RequiredLanes.All(
            lane => lane is "scheduled" or "release");
        if (artifact.Restore is null)
        {
            return null;
        }

        if (artifact.Restore.Kind != "full-canonical" || artifact.Restore.Order < 0)
        {
            return "restore contract kind or order is invalid";
        }

        if (!artifact.TableScope.Quran
            || artifact.TableScope.PhraseSearch
            || artifact.TableScope.Abwab
            || artifact.TableScope.Access
            || artifact.TableScope.Linking
            || artifact.TableScope.Tables.Any(table => !IsQuranTable(table) || IsPhraseSearchTable(table))
            || !appliesOnlyToScheduledOrRelease)
        {
            return "full-canonical restore contracts require only Quran table scope and scheduled or release lanes";
        }

        var payloads = artifact.StagedFiles.Where(file => file.Role == "payload").ToArray();
        if (payloads.Length != 1)
        {
            return "full-canonical restore contracts require exactly one payload file";
        }

        var oracleFiles = artifact.StagedFiles.Where(file => file.Role == "oracle").ToArray();
        if (oracleFiles.Length != 1
            || artifact.Sentinels.Any(sentinel => sentinel.OracleSha256 != oracleFiles[0].Sha256))
        {
            return "full-canonical restore contracts require one staged oracle matching every sentinel hash";
        }

        if (artifact.Restore.SentinelTables.Count == 0
            || DuplicateOf(artifact.Restore.SentinelTables.Select(sentinel => sentinel.Id)) is not null)
        {
            return "restore sentinel tables must be non-empty and have unique identifiers";
        }

        var lockedSentinels = artifact.Sentinels.ToDictionary(sentinel => sentinel.Id, StringComparer.Ordinal);
        foreach (var sentinel in artifact.Restore.SentinelTables)
        {
            if (!IsSafeToken(sentinel.Id)
                || !IsValidTableIdentifier(sentinel.Table)
                || !artifact.TableScope.Tables.Contains(sentinel.Table, StringComparer.Ordinal)
                || !lockedSentinels.TryGetValue(sentinel.Id, out var locked)
                || sentinel.ExpectedCount < 0
                || sentinel.ExpectedCount != locked.ExpectedCount)
            {
                return "restore sentinel tables must map every locked sentinel to a scoped safe table and expected count";
            }

            if (!IsSha256(sentinel.CriticalReadSha256))
            {
                return "restore sentinel tables require a lowercase SHA-256 critical-read fingerprint";
            }
        }

        return artifact.Restore.SentinelTables.Count == lockedSentinels.Count
            ? null
            : "restore sentinel tables must map every locked sentinel exactly once";
    }

    private static string? ValidateTableFamilyScope(LockedArtifact artifact)
    {
        var tables = artifact.TableScope.Tables;
        var hasPhraseSearchTables = tables.Any(IsPhraseSearchTable);
        var actualFamilies = new ArtifactTableFamilies(
            Quran: tables.Any(table => IsQuranTable(table) && !IsPhraseSearchTable(table)),
            PhraseSearch: hasPhraseSearchTables,
            Abwab: tables.Any(table => table.StartsWith("abwab_", StringComparison.Ordinal)),
            Access: tables.Any(AccessTableNames.Contains),
            Linking: tables.Any(table => table.StartsWith("linking_", StringComparison.Ordinal)));
        var declaredFamilies = new ArtifactTableFamilies(
            artifact.TableScope.Quran,
            artifact.TableScope.PhraseSearch,
            artifact.TableScope.Abwab,
            artifact.TableScope.Access,
            artifact.TableScope.Linking);

        if (declaredFamilies != actualFamilies)
        {
            return "tableScope family flags do not match the declared tables";
        }

        if (artifact.TableScope.PhraseSearch != (artifact.PhraseSearch is not null))
        {
            return "PhraseSearch table scope must match the PhraseSearch trust metadata";
        }

        return null;
    }

    private static bool IsQuranTable(string table)
    {
        return table.StartsWith("quran_", StringComparison.Ordinal);
    }

    private static bool IsPhraseSearchTable(string table)
    {
        return table.StartsWith("quran_phrase_", StringComparison.Ordinal);
    }

    private static bool IsImmutableCredentialFreeStorageIdentity(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)
            || identity.Any(char.IsWhiteSpace)
            || identity.Contains('?')
            || identity.Contains('#'))
        {
            return false;
        }

        if (Uri.TryCreate(identity, UriKind.Absolute, out var uri)
            && !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var lower = identity.ToLowerInvariant();
        if (new[] { "credential", "password", "secret", "signature", "token=" }
            .Any(lower.Contains))
        {
            return false;
        }

        var versionSeparator = identity.LastIndexOf('@');
        if (versionSeparator <= 0 || versionSeparator == identity.Length - 1)
        {
            return false;
        }

        var immutableVersion = identity[(versionSeparator + 1)..];
        return immutableVersion.StartsWith("sha256:", StringComparison.Ordinal)
            ? IsSha256(immutableVersion["sha256:".Length..])
            : immutableVersion.StartsWith("version:", StringComparison.Ordinal)
              && IsSafeVersion(immutableVersion["version:".Length..]);
    }

    private static bool IsSafeRelativePath(string path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && !Path.IsPathRooted(path)
            && !path.Contains('\\')
            && path.Split('/').All(segment => segment is not "" and not "." and not "..");
    }

    private static bool IsSafeToken(string value)
    {
        return value.Length is >= 1 and <= 100
            && value[0] is >= 'a' and <= 'z'
            && value.Skip(1).All(character =>
                character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character is '-' or '_' or '.');
    }

    private static bool IsSafeVersion(string value)
    {
        return value.Length is >= 1 and <= 100
            && char.IsAsciiLetterOrDigit(value[0])
            && value.Skip(1).All(character =>
                character is >= 'a' and <= 'z'
                || character is >= 'A' and <= 'Z'
                || character is >= '0' and <= '9'
                || character is '-' or '_' or '.');
    }

    internal static bool IsMigrationId(string value)
    {
        return value.Length > 15
            && value[14] == '_'
            && value.Take(14).All(char.IsAsciiDigit)
            && value.Skip(15).All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
    }

    internal static bool IsValidTableIdentifier(string identifier)
    {
        if (identifier.Length is < 1 or > 63 || identifier[0] is < 'a' or > 'z')
        {
            return false;
        }

        return identifier.Skip(1).All(character =>
            character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character == '_');
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64
            && value.All(character =>
                character is >= '0' and <= '9'
                || character is >= 'a' and <= 'f');
    }

    private static string? DuplicateOf(IEnumerable<string> values)
    {
        return values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
    }
}

internal sealed record ArtifactTableFamilies(
    bool Quran,
    bool PhraseSearch,
    bool Abwab,
    bool Access,
    bool Linking);
