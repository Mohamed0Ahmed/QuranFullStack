using QuranDashboard.TestArtifacts;

namespace QuranDashboard.Tests.Quran.PhraseSearch;

// Absent and stale are opposite verdicts on purpose. A machine that never generated the artifact is an
// ordinary machine, so the whole tier skips. A dump that is present but no longer matches this tree is
// not ordinary: running it would exercise the reads against data the schema no longer describes and
// report green, so it throws. A stale dump quietly skipping is the single failure this gate exists to
// make impossible.
internal static class PhraseSearchRehearsalDumpGate
{
    public const string DumpFileName = "quran-canonical.dump";
    public const string RegenerateCommand = "Backend/scripts/create-smoke-dump --yes";
    private const long DumpSize = 372143834;
    private const string DumpSha256 = "3d4038d561a2b4b048e72c05f0cc472b2b1bcf0f2af0d09d0c054cff38e9b29d";
    private const string ManifestSha256 = "3b2d15dbc30d8dbe5010f1d373e6a33b8e089902b6347ebb6f561f18874bec3e";

    // The canonical dump never resolves from a checkout-relative convenience path. The explicit local
    // root makes the approved content identity the only local source the smoke tier can consume.
    public static string? ArtifactRoot => Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT");

    public static string? DumpDirectory => string.IsNullOrWhiteSpace(ArtifactRoot)
        ? null
        : Path.Combine(Path.GetFullPath(ArtifactRoot), "sha256", DumpSha256);

    public static string? DumpFile => DumpDirectory is null ? null : Path.Combine(DumpDirectory, DumpFileName);

    public static string? ManifestFile => DumpDirectory is null ? null : Path.Combine(DumpDirectory, "manifest.json");

    public static string? ProvisioningReceiptFile => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_FULL_CANONICAL_RECEIPT");

    public static string? ProvisionedConnectionFile => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_FULL_CANONICAL_CONNECTION_FILE");

    public static string? ProvisionedStagingRoot => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_FULL_CANONICAL_STAGING_ROOT");

    public static string? ProvisionedDatabaseContainer => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_FULL_CANONICAL_DATABASE_CONTAINER");

    public static string? ProvisionedRunKind => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_FULL_CANONICAL_RUN");

    public static string ArtifactExecution => Environment.GetEnvironmentVariable(
        "QURAN_DASHBOARD_ARTIFACT_EXECUTION") ?? "local";

    public static bool RequiresProvisionedFullCanonicalState => ArtifactExecution is "scheduled" or "release";

    public static bool UsesProvisionedFullCanonicalState =>
        RequiresProvisionedFullCanonicalState || !string.IsNullOrWhiteSpace(ProvisioningReceiptFile);

    public static bool IsAbsent => !UsesProvisionedFullCanonicalState
        && (DumpFile is null || ManifestFile is null || !File.Exists(DumpFile) || !File.Exists(ManifestFile));

    public static string AbsentReason =>
        "Canonical smoke dump is absent from QURAN_TEST_ARTIFACT_ROOT. " +
        $"Place the approved content at sha256/{DumpSha256}/ and set that environment variable.";

    // Called before the container is started, so a stale artifact costs a hash rather than a restore.
    public static PhraseSearchRehearsalDumpManifest VerifyAndRead(int restoreImageMajorVersion)
    {
        if (DumpFile is null || ManifestFile is null)
        {
            throw new InvalidOperationException(AbsentReason);
        }

        var actualSize = new FileInfo(DumpFile).Length;
        if (actualSize != DumpSize)
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump size mismatch. Expected {DumpSize}, actual {actualSize}.");
        }

        var manifestSha256 = ComputeSha256(ManifestFile);
        if (!string.Equals(manifestSha256, ManifestSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical smoke manifest does not match the approved identity. Expected {ManifestSha256}, actual {manifestSha256}.");
        }

        var manifest = PhraseSearchRehearsalDumpManifest.ReadFrom(ManifestFile);

        var actualSha256 = ComputeSha256(DumpFile);
        if (!string.Equals(actualSha256, DumpSha256, StringComparison.Ordinal)
            || !string.Equals(manifest.DumpSha256, DumpSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump does not match the approved identity. Manifest {manifest.DumpSha256}, " +
                $"dump {actualSha256}, expected {DumpSha256}.");
        }

        var treeMigrations = TreeMigrations();
        if (!string.Equals(manifest.MigrationId, treeMigrations[^1], StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump is stale. {ManifestFile} was taken at migration " +
                $"'{manifest.MigrationId}', but this tree's head migration is '{treeMigrations[^1]}', so the data no " +
                $"longer fits the schema the fixture migrates to. Regenerate it with {RegenerateCommand}.");
        }

        if (manifest.MigrationCount != treeMigrations.Count)
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump migration count is stale. {ManifestFile} records {manifest.MigrationCount}, " +
                $"but this tree has {treeMigrations.Count} migrations.");
        }

        // The upper bound is the one that bites. pg_dump already refuses a server newer than itself, so a
        // too-old producer cannot happen; an operator who moves the local server to 19 gets an archive the
        // restore image cannot read, and without this would discover it only after a two-minute dump, a
        // container start and a mid-restore failure.
        var pgDumpMajorVersion = MajorVersionOf(manifest.PgDumpVersion);
        if (pgDumpMajorVersion > restoreImageMajorVersion)
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump was written by pg_dump {pgDumpMajorVersion}, which " +
                $"postgres:{restoreImageMajorVersion}-alpine cannot restore — pg_restore rejects an archive " +
                $"from a newer pg_dump. Either move this fixture to a postgres:{pgDumpMajorVersion}-alpine " +
                $"image, or regenerate the dump from a PostgreSQL {restoreImageMajorVersion} server with " +
                $"{RegenerateCommand}.");
        }

        if (manifest.Tables.Count != 32
            || manifest.Tables.Any(table => !ArtifactTrustLockValidator.IsValidTableIdentifier(table.Key)
                || !table.Key.StartsWith("quran_", StringComparison.Ordinal)
                || table.Key.StartsWith("quran_phrase_", StringComparison.Ordinal)
                || table.Value < 0))
        {
            throw new InvalidOperationException(
                "Canonical smoke manifest table scope is not the approved 32-table Quran-only state.");
        }

        return manifest;
    }

    private static int MajorVersionOf(string pgDumpVersion)
    {
        if (!int.TryParse(pgDumpVersion.Split('.')[0], out var major))
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump manifest records an unreadable pgDumpVersion '{pgDumpVersion}'. Regenerate it with {RegenerateCommand}.");
        }

        return major;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    // Read from the Infrastructure assembly's migrations, not from a database: the check has to answer
    // before any container exists. The connection string is never opened — GetMigrations only reads the
    // migrations assembly.
    private static IReadOnlyList<string> TreeMigrations()
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql("Host=smoke-dump-gate")
            .Options;

        using var context = new QuranDashboardDbContext(options);
        return context.Database.GetMigrations().ToArray();
    }
}
