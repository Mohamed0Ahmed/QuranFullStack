using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Npgsql;

namespace QuranDashboard.TestRuntime;

internal sealed record FullRehearsalSnapshot(
    string Database,
    long DatabaseOid,
    string EndpointKind,
    int PostgreSqlMajorVersion,
    bool InRecovery,
    bool CapabilityEnabled,
    bool ResetEnabled,
    bool RehearsalEnabled,
    string? RehearsalSubtype,
    string? CanonicalPipeline,
    string? CanonicalInputProvenance,
    string? ProtectedStateMarker,
    string? ComputedProtectedStateFingerprint,
    string? MarkerMigrationHead,
    string? DatabaseMigrationHead,
    DateTimeOffset? ProvisionedAtUtc,
    bool ExclusiveLockOwned);

internal sealed record FullRehearsalValidationResult(
    bool Succeeded,
    FullRehearsalReport Report,
    IReadOnlyList<ContractViolation> Violations);

internal static partial class FullRehearsalCapability
{
    internal const string ManualRefreshGuidance =
        "Manually refresh the Rehearsal Database through the canonical pipeline, restore its rehearsal markers, and rerun the explicitly selected full-data lane.";
    internal const string ManualProvisionGuidance =
        "Set ConnectionStrings__QuranDashboardRehearsal to an explicitly and manually provisioned local Rehearsal Database; TestRuntime will not create, clone, restore, refresh, or drop one automatically.";

    private static readonly HashSet<string> FullDataSubtypes = new(StringComparer.Ordinal)
    {
        "phrase-search-index-build",
        "recovery",
    };

    internal static bool IsApprovedSubtype(string? subtype) =>
        subtype is not null && FullDataSubtypes.Contains(subtype);

    internal static FullRehearsalValidationResult Validate(
        DatabaseContract contract,
        string expectedMigrationHead,
        string subtype,
        FullRehearsalSnapshot snapshot,
        DateTimeOffset utcNow,
        bool requireExclusiveLock,
        string mode = "inspect")
    {
        var violations = new List<ContractViolation>();
        var cleanupMode = mode.StartsWith("cleanup-", StringComparison.Ordinal);
        if (!IsApprovedSubtype(subtype)
            || !contract.RehearsalSubtypes.Contains(subtype, StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("rehearsal.subtype.not-approved"));
        }
        if (snapshot.PostgreSqlMajorVersion != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("rehearsal.postgres-version.mismatch"));
        }
        if (snapshot.InRecovery)
        {
            violations.Add(new ContractViolation("rehearsal.server.in-recovery"));
        }
        if (snapshot.CapabilityEnabled || snapshot.ResetEnabled)
        {
            violations.Add(new ContractViolation("rehearsal.target.authoritative-marker-present"));
        }
        if (!snapshot.RehearsalEnabled)
        {
            violations.Add(new ContractViolation("rehearsal.marker.missing"));
        }
        if (snapshot.RehearsalSubtype != subtype)
        {
            violations.Add(new ContractViolation("rehearsal.subtype.mismatch"));
        }
        if (snapshot.CanonicalPipeline != CapabilityRefresher.PipelineIdentity)
        {
            violations.Add(new ContractViolation("rehearsal.pipeline.mismatch"));
        }
        if (snapshot.CanonicalInputProvenance is null
            || !Sha256Pattern().IsMatch(snapshot.CanonicalInputProvenance))
        {
            violations.Add(new ContractViolation("rehearsal.provenance.invalid"));
        }
        if (!cleanupMode
            && (snapshot.ProtectedStateMarker is null
                || !Sha256Pattern().IsMatch(snapshot.ProtectedStateMarker)
                || snapshot.ComputedProtectedStateFingerprint is null
                || !string.Equals(
                    snapshot.ProtectedStateMarker,
                    snapshot.ComputedProtectedStateFingerprint,
                    StringComparison.OrdinalIgnoreCase)))
        {
            violations.Add(new ContractViolation("rehearsal.protected-state.mismatch"));
        }
        if (snapshot.MarkerMigrationHead != expectedMigrationHead
            || (!cleanupMode && snapshot.DatabaseMigrationHead != expectedMigrationHead))
        {
            violations.Add(new ContractViolation("rehearsal.migration.not-current"));
        }

        var fresh = snapshot.ProvisionedAtUtc is { } provisioned
                    && provisioned <= utcNow
                    && utcNow - provisioned <= TimeSpan.FromHours(contract.FullRehearsal.MaximumAgeHours);
        if (!fresh && !cleanupMode)
        {
            violations.Add(new ContractViolation("rehearsal.freshness.expired"));
        }
        if (requireExclusiveLock && !snapshot.ExclusiveLockOwned)
        {
            violations.Add(new ContractViolation("rehearsal.lock.not-owned"));
        }

        var ordered = violations
            .Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();
        var guidance = ordered.Length == 0 ? [] : new[] { ManualRefreshGuidance };
        return new FullRehearsalValidationResult(
            ordered.Length == 0,
            new FullRehearsalReport(
                mode,
                snapshot.Database,
                subtype,
                ordered.Length == 0
                    ? cleanupMode ? "cleanup-ready" : "ready"
                    : cleanupMode ? "cleanup-refused" : "refresh-required",
                snapshot.CanonicalPipeline,
                snapshot.CanonicalInputProvenance,
                snapshot.ProtectedStateMarker,
                snapshot.ComputedProtectedStateFingerprint,
                snapshot.MarkerMigrationHead,
                snapshot.DatabaseMigrationHead,
                snapshot.ProvisionedAtUtc?.ToUniversalTime().ToString("O"),
                fresh,
                snapshot.ExclusiveLockOwned,
                Removed: false,
                guidance,
                DumpFilesRetained: 0),
            ordered);
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}

internal static class FullRehearsalTargetValidator
{
    internal static InspectionTargetValidation Validate(string connectionString, DatabaseContract contract)
    {
        NpgsqlConnectionStringBuilder connection;
        try
        {
            connection = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return Invalid("rehearsal.target.connection-string.invalid");
        }

        var violations = new List<ContractViolation>();
        var database = connection.Database;
        if (string.IsNullOrWhiteSpace(database))
        {
            violations.Add(new ContractViolation("rehearsal.target.database-missing"));
        }
        else if (database == contract.Targets.DevelopmentDatabase)
        {
            violations.Add(new ContractViolation("rehearsal.target.development-database"));
        }
        else if (database == contract.Targets.TestDatabase)
        {
            violations.Add(new ContractViolation("rehearsal.target.test-database"));
        }
        else if (database.StartsWith(contract.Targets.ScratchPrefix, StringComparison.Ordinal)
                 || database.StartsWith(contract.Targets.RefreshPrefix, StringComparison.Ordinal)
                 || database is "postgres" or "template0" or "template1")
        {
            violations.Add(new ContractViolation("rehearsal.target.reserved-database"));
        }

        var endpointKind = InspectionTargetValidator.LocalEndpointKind(connection.Host);
        if (endpointKind is null)
        {
            violations.Add(new ContractViolation("rehearsal.target.remote"));
        }

        return new InspectionTargetValidation(
            violations.Count == 0,
            string.IsNullOrWhiteSpace(database) ? null : database,
            endpointKind,
            violations.Count == 0 ? connection : null,
            violations);
    }

    private static InspectionTargetValidation Invalid(string code) => new(
        false,
        null,
        null,
        null,
        [new ContractViolation(code)]);
}

internal static class FullRehearsalRecoveryPayload
{
    internal static async Task<FullRehearsalRecoveryEvidence> FinalizeAsync(
        string payloadPath,
        string sourceProtectedStateFingerprint,
        bool rehearsalSucceeded,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(payloadPath))
        {
            throw new InvalidOperationException("The recovery rehearsal backup payload does not exist.");
        }
        if (sourceProtectedStateFingerprint.Length != 64
            || !sourceProtectedStateFingerprint.All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException("Recovery evidence requires the source Protected State fingerprint.");
        }

        string hash;
        await using (var stream = new FileStream(
                         payloadPath,
                         FileMode.Open,
                         FileAccess.Read,
                         FileShare.Read,
                         81920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
        }
        if (rehearsalSucceeded)
        {
            File.Delete(payloadPath);
        }
        return new FullRehearsalRecoveryEvidence(
            hash,
            sourceProtectedStateFingerprint.ToLowerInvariant(),
            rehearsalSucceeded && !File.Exists(payloadPath));
    }
}

internal sealed record FullRehearsalCleanupAuthorization(
    bool Authorized,
    IReadOnlyList<ContractViolation> Violations);

internal static class FullRehearsalCleanup
{
    internal static FullRehearsalCleanupAuthorization Authorize(
        FullRehearsalValidationResult validation,
        string displayedDatabase,
        string? confirmedDatabase,
        bool explicitlyConfirmed)
    {
        var violations = new List<ContractViolation>();
        if (!validation.Succeeded)
        {
            violations.AddRange(validation.Violations);
        }
        if (!explicitlyConfirmed)
        {
            violations.Add(new ContractViolation("rehearsal.cleanup.explicit-confirmation-required"));
        }
        if (!string.Equals(displayedDatabase, confirmedDatabase, StringComparison.Ordinal))
        {
            violations.Add(new ContractViolation("rehearsal.cleanup.confirmation-mismatch"));
        }
        var ordered = violations
            .Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();
        return new FullRehearsalCleanupAuthorization(ordered.Length == 0, ordered);
    }
}
