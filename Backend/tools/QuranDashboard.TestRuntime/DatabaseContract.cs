using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuranDashboard.TestRuntime;

internal sealed class DatabaseContract
{
    public required int ContractVersion { get; init; }
    public required int CapabilityMetadataVersion { get; init; }
    public required int PostgresMajorVersion { get; init; }
    public required DatabaseDataClasses DataClasses { get; init; }
    public required LinkingDataBaseline LinkingDataBaseline { get; init; }
    public required SystemCatalogueContract SystemCatalogue { get; init; }
    public required TestDatabaseRoles Roles { get; init; }
    public required TestDatabaseMarkers Markers { get; init; }
    public required TestDatabaseTargets Targets { get; init; }
    public required AdvisoryLockContract AdvisoryLock { get; init; }
    public required string[] RehearsalSubtypes { get; init; }

    public IEnumerable<(string DataClass, string Table)> ApplicationTables()
    {
        return DataClasses.CanonicalQuranData.Select(table => ("canonicalQuranData", table))
            .Concat(DataClasses.SystemCatalogue.Select(table => ("systemCatalogue", table)))
            .Concat(DataClasses.MutableApplicationState.Select(table => ("mutableApplicationState", table)));
    }

    public IEnumerable<(string DataClass, string Table)> AllTables()
    {
        return ApplicationTables()
            .Concat(DataClasses.SchemaState.Select(table => ("schemaState", table)));
    }
}

internal sealed class DatabaseDataClasses
{
    public required string[] CanonicalQuranData { get; init; }
    public required string[] SystemCatalogue { get; init; }
    public required string[] MutableApplicationState { get; init; }
    public required string[] SchemaState { get; init; }
}

internal sealed class LinkingDataBaseline
{
    public required string Table { get; init; }
    public required long Id { get; init; }
    public required long Generation { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }
}

internal sealed class SystemCatalogueContract
{
    public required OwnerRoleContract OwnerRole { get; init; }
}

internal sealed class OwnerRoleContract
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
}

internal sealed class TestDatabaseRoles
{
    public required string Reader { get; init; }
    public required string Application { get; init; }
    public required string Resetter { get; init; }
    public required string ScratchAdministrator { get; init; }

    public IReadOnlyDictionary<string, string> AsDictionary() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["reader"] = Reader,
        ["application"] = Application,
        ["resetter"] = Resetter,
        ["scratchAdministrator"] = ScratchAdministrator,
    };
}

internal sealed class TestDatabaseMarkers
{
    public required string CapabilityEnabled { get; init; }
    public required string ResetEnabled { get; init; }
    public required string ContractVersion { get; init; }
    public required string CapabilityMetadataVersion { get; init; }
    public required string CanonicalPipeline { get; init; }
    public required string CanonicalInputProvenance { get; init; }
    public required string CanonicalQuranFingerprint { get; init; }
    public required string SystemCatalogueFingerprint { get; init; }
    public required string ProtectedStateFingerprint { get; init; }
    public required string MigrationHead { get; init; }
    public required string RefreshedAtUtc { get; init; }
    public required string RehearsalEnabled { get; init; }
    public required string RehearsalSubtype { get; init; }

    public IReadOnlyDictionary<string, string> AsDictionary() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["capabilityEnabled"] = CapabilityEnabled,
        ["resetEnabled"] = ResetEnabled,
        ["contractVersion"] = ContractVersion,
        ["capabilityMetadataVersion"] = CapabilityMetadataVersion,
        ["canonicalPipeline"] = CanonicalPipeline,
        ["canonicalInputProvenance"] = CanonicalInputProvenance,
        ["canonicalQuranFingerprint"] = CanonicalQuranFingerprint,
        ["systemCatalogueFingerprint"] = SystemCatalogueFingerprint,
        ["protectedStateFingerprint"] = ProtectedStateFingerprint,
        ["migrationHead"] = MigrationHead,
        ["refreshedAtUtc"] = RefreshedAtUtc,
        ["rehearsalEnabled"] = RehearsalEnabled,
        ["rehearsalSubtype"] = RehearsalSubtype,
    };
}

internal sealed class TestDatabaseTargets
{
    public required string DevelopmentDatabase { get; init; }
    public required string TestDatabase { get; init; }
    public required string ScratchPrefix { get; init; }
    public required string RefreshPrefix { get; init; }
    public required string[] AllowedDatabaseTargets { get; init; }
}

internal sealed class AdvisoryLockContract
{
    public required long Key { get; init; }
}

internal static class DatabaseContractReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    internal static DatabaseContract Read(string path)
    {
        using var stream = File.OpenRead(path);
        var contract = JsonSerializer.Deserialize<DatabaseContract>(stream, JsonOptions)
            ?? throw new JsonException("The database contract was empty.");
        if (contract.DataClasses is null
            || contract.DataClasses.CanonicalQuranData is null
            || contract.DataClasses.SystemCatalogue is null
            || contract.DataClasses.MutableApplicationState is null
            || contract.DataClasses.SchemaState is null
            || contract.LinkingDataBaseline is null
            || contract.SystemCatalogue?.OwnerRole is null
            || contract.Roles is null
            || contract.Markers is null
            || contract.Targets is null
            || contract.Targets.AllowedDatabaseTargets is null
            || contract.AdvisoryLock is null
            || contract.RehearsalSubtypes is null)
        {
            throw new JsonException("The database contract contains a null required value.");
        }

        return contract;
    }
}
