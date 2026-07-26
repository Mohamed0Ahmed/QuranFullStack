using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke._Fixtures;

// Restores the verified canonical Quran dump into a migrated container, then hosts the real API
// over it. Missing dump ⇒ every [QuranDumpFact] skips and this fixture never starts a container.
// A PRESENT but corrupt or stale dump fails loud — that is never a skip.
public sealed class QuranDataSmokeFixture : IAsyncLifetime
{
    // postgres:18, not the substrate's 16: the archive carries the dumping client's SET preamble
    // (e.g. transaction_timeout, a PG17+ GUC), so the restore server's major version must be >= the
    // pg_dump client's on the staged machine that produced the dump.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .Build();

    private DataSmokeWebApplicationFactory? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (QuranDumpGate.IsMissing)
        {
            return;
        }

        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(QuranDumpGate.ManifestPath));
        VerifyDumpChecksum(manifest);

        await _container.StartAsync();
        var connectionString = _container.GetConnectionString();
        await MigrateAsync(connectionString);
        await VerifyManifestMatchesMigratedSchemaAsync(manifest, connectionString);

        RunHostPgRestore(connectionString);

        await VerifyRestoredCountsAsync(manifest, connectionString);

        _factory = new DataSmokeWebApplicationFactory(connectionString);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private static void VerifyDumpChecksum(JsonDocument manifest)
    {
        var expected = manifest.RootElement.GetProperty("dumpSha256").GetString();
        using var stream = File.OpenRead(QuranDumpGate.DumpPath);
        var actual = Convert.ToHexStringLower(SHA256.HashData(stream));

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Canonical dump checksum mismatch (manifest {expected}, file {actual}) — the dump is corrupt; re-run Backend/scripts/create-smoke-dump.");
        }
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var services = new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(connectionString));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>().Database.MigrateAsync();
    }

    private static async Task VerifyManifestMatchesMigratedSchemaAsync(JsonDocument manifest, string connectionString)
    {
        var manifestMigration = manifest.RootElement.GetProperty("migrationId").GetString();
        var appliedMigration = await ScalarAsync<string>(
            connectionString, """SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1""");

        if (!string.Equals(manifestMigration, appliedMigration, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical dump is stale: manifest migration '{manifestMigration}' != migrated schema head '{appliedMigration}'. Re-run Backend/scripts/create-smoke-dump.");
        }
    }

    // pg_restore runs on the HOST against the container's mapped port: the host client tools wrote
    // the archive, and an older in-container pg_restore may reject a newer archive format.
    private static void RunHostPgRestore(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_restore",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("--data-only");
        startInfo.ArgumentList.Add("--disable-triggers");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add(builder.Host!);
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(builder.Port.ToString());
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(builder.Username!);
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(builder.Database!);
        startInfo.ArgumentList.Add(QuranDumpGate.DumpPath);
        startInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start pg_restore.");
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pg_restore exited with {process.ExitCode}: {stderr}");
        }
    }

    private static async Task VerifyRestoredCountsAsync(JsonDocument manifest, string connectionString)
    {
        foreach (var table in new[] { "quran_surahs", "quran_ayahs", "quran_words" })
        {
            var expected = manifest.RootElement.GetProperty("tables").GetProperty(table).GetInt64();
            var actual = await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM {table}");
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"Restored {table} has {actual} rows, manifest says {expected} — restore is incomplete.");
            }
        }
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed class DataSmokeWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => SmokeHostConfigurator.Configure(builder, connectionString);
    }
}

[CollectionDefinition(nameof(QuranDataSmokeCollection))]
public sealed class QuranDataSmokeCollection : ICollectionFixture<QuranDataSmokeFixture>;
