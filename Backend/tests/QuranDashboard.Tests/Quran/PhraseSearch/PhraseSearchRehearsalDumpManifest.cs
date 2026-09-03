namespace QuranDashboard.Tests.Quran.PhraseSearch;

// Only the fields the tier consumes are modelled; `name`, `createdUtc` and `migrationCount` are written
// by Backend/scripts/create-smoke-dump for an operator reading the file and are ignored here rather than
// carried as properties nothing asserts.
internal sealed record PhraseSearchRehearsalDumpManifest(
    string MigrationId,
    int MigrationCount,
    string DumpSha256,
    string PgDumpVersion,
    IReadOnlyDictionary<string, int> Tables)
{
    // Web defaults: the script writes camelCase, which is also what the API's own serializer emits.
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public static PhraseSearchRehearsalDumpManifest ReadFrom(string path) =>
        JsonSerializer.Deserialize<PhraseSearchRehearsalDumpManifest>(File.ReadAllText(path), ReadOptions)
        ?? throw new InvalidOperationException(
            $"Canonical rehearsal dump manifest is empty: {path}. Regenerate it with {PhraseSearchRehearsalDumpGate.RegenerateCommand}.");

    // A named lookup rather than raw indexing: a seeded expectation naming a table the manifest does not
    // carry is a wiring mistake in the catalog, and KeyNotFoundException never says which table.
    public int RowCount(string table) =>
        Tables.TryGetValue(table, out var rows)
            ? rows
            : throw new InvalidOperationException(
                $"Canonical rehearsal dump manifest carries no row count for '{table}'. Regenerate it with {PhraseSearchRehearsalDumpGate.RegenerateCommand}.");
}
