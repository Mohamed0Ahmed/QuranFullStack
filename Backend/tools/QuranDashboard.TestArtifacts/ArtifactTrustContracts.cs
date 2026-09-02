using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuranDashboard.TestArtifacts;

internal sealed record ArtifactTrustLock(
    [property: JsonPropertyName("$schema")] string Schema,
    int ContractVersion,
    IReadOnlyList<LockedArtifact> Artifacts)
{
    internal const string FileName = "test-artifacts.lock.json";
    internal const string SchemaPath = "docs/testing/test-artifacts-lock.schema.json";

    internal static ArtifactTrustLock ReadFrom(string path)
    {
        return StrictJson.Read<ArtifactTrustLock>(path, "Artifact lock");
    }
}

internal sealed record LockedArtifact(
    string Id,
    string Version,
    IReadOnlyList<string> RequiredLanes,
    IReadOnlyList<LockedArtifactFile> StagedFiles,
    string ManifestPath,
    ArtifactMigrationState Migration,
    ArtifactTableScope TableScope,
    [property: JsonPropertyName("postgresql")] LockedPostgreSqlState PostgreSql,
    ArtifactProducer Producer,
    IReadOnlyList<ArtifactSource> Sources,
    IReadOnlyList<ArtifactSentinel> Sentinels,
    string ImmutableStorageId,
    ArtifactRefresh Refresh,
    IReadOnlyList<ArtifactManifestTable>? TableCounts = null,
    LockedPhraseSearchState? PhraseSearch = null,
    ArtifactRestoreContract? Restore = null);

internal sealed record LockedArtifactFile(
    string Path,
    string Role,
    long Size,
    string Sha256);

internal sealed record ArtifactMigrationState(string Head, int Count);

internal sealed record ArtifactTableScope(
    bool Quran,
    bool PhraseSearch,
    bool Abwab,
    bool Access,
    bool Linking,
    IReadOnlyList<string> Tables,
    IReadOnlyList<ArtifactOwnedSequence>? OwnedSequences = null);

internal sealed record ArtifactOwnedSequence(
    string Name,
    string Table,
    string Column);

internal sealed record LockedPostgreSqlState(
    string ProducerVersion,
    string ContainerDigest);

internal sealed record ManifestPostgreSqlState(string ProducerVersion);

internal sealed record ArtifactProducer(string Command, string Version);

internal sealed record ArtifactSource(
    string Id,
    string Version,
    string Sha256,
    string Provenance);

internal sealed record ArtifactSentinel(
    string Id,
    long ExpectedCount,
    string OracleSha256);

internal sealed record ArtifactRefresh(
    string Date,
    string Reason,
    string OwnerRole);

internal sealed record LockedPhraseSearchState(
    string ManifestSha256,
    string SourceFingerprint,
    string ReadinessExpectation);

internal sealed record ArtifactRestoreContract(
    string Kind,
    int Order,
    IReadOnlyList<ArtifactRestoreSentinel> SentinelTables);

internal sealed record ArtifactRestoreSentinel(
    string Id,
    string Table,
    long ExpectedCount,
    string? CriticalReadSha256 = null);

internal sealed record TestArtifactManifest(
    int ContractVersion,
    string ArtifactId,
    string ArtifactVersion,
    ArtifactMigrationState Migration,
    [property: JsonPropertyName("postgresql")] ManifestPostgreSqlState PostgreSql,
    ArtifactProducer Producer,
    IReadOnlyList<ArtifactManifestTable> Tables,
    IReadOnlyList<ArtifactSource> Sources,
    IReadOnlyList<ArtifactSentinel> Sentinels,
    ManifestPhraseSearchState? PhraseSearch = null,
    ArtifactRestoreContract? Restore = null)
{
    internal static TestArtifactManifest ReadFrom(string path)
    {
        return StrictJson.Read<TestArtifactManifest>(path, "Artifact manifest");
    }
}

internal sealed record ArtifactManifestTable(string Name, long Rows);

internal sealed record CanonicalDumpManifest(
    string Name,
    DateTimeOffset CreatedUtc,
    string MigrationId,
    int MigrationCount,
    string DumpSha256,
    string PgDumpVersion,
    IReadOnlyDictionary<string, long> Tables)
{
    internal static CanonicalDumpManifest ReadFrom(string path) =>
        StrictJson.Read<CanonicalDumpManifest>(path, "Canonical dump manifest");

    internal IReadOnlyList<ArtifactManifestTable> TableCounts() => Tables
        .Select(table => new ArtifactManifestTable(table.Key, table.Value))
        .ToArray();
}

internal static class ArtifactManifestReader
{
    internal static IReadOnlyList<ArtifactManifestTable> ReadTables(LockedArtifact artifact, string path) =>
        artifact.Restore is null
            ? TestArtifactManifest.ReadFrom(path).Tables
            : ReadFullCanonicalTables(path);

    private static IReadOnlyList<ArtifactManifestTable> ReadFullCanonicalTables(string path)
    {
        try
        {
            return CanonicalDumpManifest.ReadFrom(path).TableCounts();
        }
        catch (JsonException)
        {
            return TestArtifactManifest.ReadFrom(path).Tables;
        }
    }
}

internal sealed record ManifestPhraseSearchState(
    string SourceFingerprint,
    string Readiness,
    string ActiveBuildId);

internal static class StrictJson
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    internal static T Read<T>(string path, string documentName)
    {
        var json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);
        RejectDuplicateProperties(document.RootElement, documentName, "$");

        return JsonSerializer.Deserialize<T>(json, ReadOptions)
            ?? throw new InvalidDataException($"{documentName} is empty: {path}.");
    }

    private static void RejectDuplicateProperties(
        JsonElement element,
        string documentName,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException(
                        $"{documentName} contains duplicate property '{property.Name}' at {path}.");
                }

                RejectDuplicateProperties(
                    property.Value,
                    documentName,
                    $"{path}.{property.Name}");
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item, documentName, $"{path}[{index}]");
                index++;
            }
        }
    }
}
