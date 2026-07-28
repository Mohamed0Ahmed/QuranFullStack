namespace QuranDashboard.Tests.Smoke.Data;

// Absent and stale are opposite verdicts on purpose. A machine that never generated the artifact is an
// ordinary machine, so the whole tier skips. A dump that is present but no longer matches this tree is
// not ordinary: running it would exercise the reads against data the schema no longer describes and
// report green, so it throws. A stale dump quietly skipping is the single failure this gate exists to
// make impossible.
internal static class SmokeDumpGate
{
    public const string DumpFileName = "quran-canonical.dump";
    public const string RegenerateCommand = "Backend/scripts/create-smoke-dump --yes";

    // bin/<configuration>/<tfm>/ up to the repository root, the same walk CanonicalImportSourceTestGate
    // uses to reach the gitignored resources/ tree.
    public static string DumpDirectory { get; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..",
        "resources", "db-dumps", "quran-canonical"));

    public static string DumpFile => Path.Combine(DumpDirectory, DumpFileName);

    public static string ManifestFile => Path.Combine(DumpDirectory, "manifest.json");

    public static bool IsAbsent => !File.Exists(DumpFile) || !File.Exists(ManifestFile);

    public static string AbsentReason =>
        $"Canonical smoke dump is absent ({DumpFile}). Generate it with {RegenerateCommand}.";

    // Called before the container is started, so a stale artifact costs a hash rather than a restore.
    public static SmokeDumpManifest VerifyAndRead(int restoreImageMajorVersion)
    {
        var manifest = SmokeDumpManifest.ReadFrom(ManifestFile);

        var actualSha256 = ComputeSha256(DumpFile);
        if (!string.Equals(actualSha256, manifest.DumpSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump does not match its manifest. {ManifestFile} records sha256 " +
                $"{manifest.DumpSha256}, but {DumpFile} hashes to {actualSha256} — the dump is corrupt or " +
                $"the two were written separately. Regenerate both with {RegenerateCommand}.");
        }

        var treeHead = TreeMigrationHead();
        if (!string.Equals(manifest.MigrationId, treeHead, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical smoke dump is stale. {ManifestFile} was taken at migration " +
                $"'{manifest.MigrationId}', but this tree's head migration is '{treeHead}', so the data no " +
                $"longer fits the schema the fixture migrates to. Regenerate it with {RegenerateCommand}.");
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
    private static string TreeMigrationHead()
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql("Host=smoke-dump-gate")
            .Options;

        using var context = new QuranDashboardDbContext(options);
        return context.Database.GetMigrations().Last();
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class SmokeDumpFactAttribute : FactAttribute
{
    public SmokeDumpFactAttribute()
    {
        if (SmokeDumpGate.IsAbsent)
        {
            Skip = SmokeDumpGate.AbsentReason;
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class SmokeDumpTheoryAttribute : TheoryAttribute
{
    public SmokeDumpTheoryAttribute()
    {
        if (SmokeDumpGate.IsAbsent)
        {
            Skip = SmokeDumpGate.AbsentReason;
        }
    }
}
