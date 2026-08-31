namespace QuranDashboard.Tests.Smoke.Data;

// Only the fields the tier consumes are modelled; `name`, `createdUtc` and `migrationCount` are written
// by Backend/scripts/create-smoke-dump for an operator reading the file and are ignored here rather than
// carried as properties nothing asserts.
internal sealed record SmokeDumpManifest(
    string MigrationId,
    string DumpSha256,
    string PgDumpVersion,
    IReadOnlyDictionary<string, int> Tables)
{
    // Web defaults: the script writes camelCase, which is also what the API's own serializer emits.
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public static SmokeDumpManifest ReadFrom(string path) =>
        JsonSerializer.Deserialize<SmokeDumpManifest>(File.ReadAllText(path), ReadOptions)
        ?? throw new InvalidOperationException(
            $"Canonical smoke dump manifest is empty: {path}. Regenerate it with {SmokeDumpGate.RegenerateCommand}.");

    // A named lookup rather than raw indexing: a seeded expectation naming a table the manifest does not
    // carry is a wiring mistake in the catalog, and KeyNotFoundException never says which table.
    public int RowCount(string table) =>
        Tables.TryGetValue(table, out var rows)
            ? rows
            : throw new InvalidOperationException(
                $"Canonical smoke dump manifest carries no row count for '{table}'. Either the SmokeRouteCatalog entry names the wrong table, or the manifest predates it — regenerate it with {SmokeDumpGate.RegenerateCommand}.");
}

// The scheduled/release provisioner receipt deliberately has no dump path or credentials. Smoke reads
// only the already checked table counts, so local dump metadata never leaks into the shared-state path.
internal sealed record SmokeRestoredDataManifest(IReadOnlyDictionary<string, int> Tables)
{
    public int RowCount(string table) =>
        Tables.TryGetValue(table, out var rows)
            ? rows
            : throw new InvalidOperationException(
                $"The provisioned canonical receipt carries no row count for '{table}'.");
}
