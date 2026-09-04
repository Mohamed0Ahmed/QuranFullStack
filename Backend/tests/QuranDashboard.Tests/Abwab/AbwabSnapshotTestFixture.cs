using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestRuntime;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Abwab;

public sealed class AbwabSnapshotTestFixture : IAsyncLifetime
{
    private const string SeedSyntheticAuthoredStateSql = """
        INSERT INTO public.abwab_sections
            (id, name, order_value, created_at, updated_at)
        VALUES
            (101, 'Synthetic snapshot section', 1, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z');

        INSERT INTO public.abwab_templates
            (id, created_at, updated_at)
        VALUES
            (301, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z');

        INSERT INTO public.abwab_doors
            (id, section_id, parent_id, name, description, representative_ayah_text,
             order_value, global_order_value, created_at, updated_at, deleted_at)
        VALUES
            (201, 101, NULL, 'Synthetic root door', 'Synthetic root description', 'Synthetic root cue',
             1, 1, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL),
            (202, 101, 201, 'Synthetic child door', 'Synthetic child description', 'Synthetic child cue',
             1, NULL, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL),
            (203, 101, NULL, 'Synthetic archived door', 'Synthetic archived description', 'Synthetic archived cue',
             2, NULL, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', '2026-01-03T03:04:05Z');

        INSERT INTO public.abwab_template_nodes
            (id, template_id, parent_node_id, name, description, representative_ayah_text, aliases,
             order_value, created_at, updated_at, deleted_at)
        VALUES
            (401, 301, NULL, 'Synthetic template root', 'Synthetic template description',
             'Synthetic template cue', ARRAY['synthetic-template-alias'], 1,
             '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL),
            (402, 301, 401, 'Synthetic template child', 'Synthetic template child description',
             'Synthetic template child cue', ARRAY['synthetic-template-child-alias'], 1,
             '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL),
            (403, 301, 401, 'Synthetic archived template child', 'Synthetic archived template child description',
             'Synthetic archived template child cue', ARRAY['synthetic-archived-template-alias'], 2,
             '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', '2026-01-03T03:04:05Z');

        INSERT INTO public.abwab_door_aliases
            (id, door_id, value, created_at, updated_at, deleted_at)
        VALUES
            (501, 201, 'synthetic-root-alias', '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL),
            (502, 203, 'synthetic-archived-alias', '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', '2026-01-03T03:04:05Z');

        INSERT INTO public.abwab_door_relations
            (id, door_a_id, door_b_id, relation_type, broader_door_id, created_at, updated_at, deleted_at)
        VALUES
            (601, 201, 202, 1, NULL, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL);

        INSERT INTO public.abwab_door_inclusions
            (id, target_door_id, source_door_id, created_at, updated_at, deleted_at)
        VALUES
            (701, 201, 202, '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z', NULL);

        INSERT INTO public.users
            (id, logto_sub, email, normalized_email, status, created_at, updated_at)
        VALUES
            (801, 'snapshot-test-user', 'snapshot-test@example.test', 'SNAPSHOT-TEST@EXAMPLE.TEST', 2,
             '2026-01-02T03:04:05Z', '2026-01-02T03:04:05Z');

        INSERT INTO public.linking_units
            (id, door_id, identity, identity_hash, is_grouped, created_at, created_by)
        VALUES
            (1001, 202, 'synthetic-source-unit', decode('01', 'hex'), FALSE,
             '2026-01-02T03:04:05Z', 801),
            (1002, 201, 'synthetic-target-unit', decode('02', 'hex'), FALSE,
             '2026-01-02T03:04:05Z', 801);

        INSERT INTO public.abwab_door_inclusion_unit_syncs
            (id, door_inclusion_id, source_unit_id, target_unit_id, state, source_fingerprint,
             created_at, created_by, updated_at, updated_by)
        VALUES
            (1101, 701, 1001, 1002, 'active', decode('0123456789ABCDEF', 'hex'),
             '2026-01-02T03:04:05Z', 801, '2026-01-02T03:04:05Z', 801);
        """;

    private static string TruncateSnapshotTablesSql =>
        $"TRUNCATE TABLE public.linking_units, public.users, {string.Join(", ", AbwabSnapshotContract.Tables.Select(table => $"public.\"{table}\""))} RESTART IDENTITY CASCADE";

    private string? sourceConnectionString;
    private string? targetConnectionString;
    private string? targetDatabaseName;
    private string? maintenanceConnectionString;
    private WebApplicationFactory<HealthController>? apiFactory;
    private string? temporaryDirectory;

    public string SourceConnectionString => sourceConnectionString
        ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    public string TargetConnectionString => targetConnectionString
        ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    public async Task InitializeAsync()
    {
        var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(TestRuntimeTestPaths.ContractPath);
        sourceConnectionString = scratch.ConnectionString;
        await MigrateAsync(sourceConnectionString);

        var source = new NpgsqlConnectionStringBuilder(sourceConnectionString);
        targetDatabaseName = PostgreSqlDatabaseName.CreateForOwner($"{nameof(AbwabSnapshotTestFixture)}Target");
        maintenanceConnectionString = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        }.ConnectionString;
        await ExecuteAsync(
            maintenanceConnectionString,
            $"CREATE DATABASE {PostgreSqlDatabaseName.Quote(targetDatabaseName)} TEMPLATE template0");
        targetConnectionString = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = targetDatabaseName,
        }.ConnectionString;
        await MigrateAsync(targetConnectionString);

        temporaryDirectory = Path.Combine(Path.GetTempPath(), $"abwab-snapshot-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        apiFactory = SmokeApiHost.Build(
            TargetConnectionString,
            new FakeExternalUserProfileSource(),
            new TestSqlCommandCapture());
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (apiFactory is not null)
            {
                await apiFactory.DisposeAsync();
            }
        }
        finally
        {
            if (targetConnectionString is not null)
            {
                NpgsqlConnection.ClearPool(new NpgsqlConnection(targetConnectionString));
            }

            if (maintenanceConnectionString is not null && targetDatabaseName is not null)
            {
                await ExecuteAsync(
                    maintenanceConnectionString,
                    $"""
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = {PostgreSqlLiteral(targetDatabaseName)}
                      AND pid <> pg_backend_pid()
                    """);
                await ExecuteAsync(
                    maintenanceConnectionString,
                    $"DROP DATABASE IF EXISTS {PostgreSqlDatabaseName.Quote(targetDatabaseName)}");
            }

            if (temporaryDirectory is not null)
            {
                try
                {
                    Directory.Delete(temporaryDirectory, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    public HttpClient CreateApiClient() => SmokeApiHost.CreateClient(
        apiFactory ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet."));

    public string CreateTemporaryDirectory(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var path = Path.Combine(
            temporaryDirectory ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet."),
            $"{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task ResetAsync()
    {
        await ExecuteAsync(SourceConnectionString, TruncateSnapshotTablesSql);
        await ExecuteAsync(TargetConnectionString, TruncateSnapshotTablesSql);
        InvalidateTargetAbwabCaches();
    }

    public Task SeedSyntheticAuthoredStateAsync() => ExecuteAsync(SourceConnectionString, SeedSyntheticAuthoredStateSql);

    public Task SeedTargetSentinelAsync() => ExecuteAsync(
        TargetConnectionString,
        """
        INSERT INTO public.abwab_sections (id, name, order_value, created_at, updated_at)
        VALUES (901, 'Existing target sentinel', 1, '2026-01-04T03:04:05Z', '2026-01-04T03:04:05Z')
        """);

    public Task AddTargetSchemaDriftAsync() => ExecuteAsync(
        TargetConnectionString,
        "ALTER TABLE public.abwab_sections ADD COLUMN snapshot_test_drift integer NULL");

    public Task RemoveTargetSchemaDriftAsync() => ExecuteAsync(
        TargetConnectionString,
        "ALTER TABLE public.abwab_sections DROP COLUMN IF EXISTS snapshot_test_drift");

    public async Task<MigrationHistoryEntry> RemoveTargetMigrationHeadAsync()
    {
        await using var connection = new NpgsqlConnection(TargetConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var read = new NpgsqlCommand(
            "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1",
            connection,
            transaction);
        await using var reader = await read.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("The migrated disposable target has no migration history.");
        }

        var entry = new MigrationHistoryEntry(reader.GetString(0), reader.GetString(1));
        await reader.DisposeAsync();

        await using var delete = new NpgsqlCommand(
            "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @migrationId",
            connection,
            transaction);
        delete.Parameters.AddWithValue("migrationId", entry.MigrationId);
        await delete.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return entry;
    }

    public async Task RestoreTargetMigrationHeadAsync(MigrationHistoryEntry entry)
    {
        await using var connection = new NpgsqlConnection(TargetConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migrationId, @productVersion)",
            connection);
        command.Parameters.AddWithValue("migrationId", entry.MigrationId);
        command.Parameters.AddWithValue("productVersion", entry.ProductVersion);
        await command.ExecuteNonQueryAsync();
    }

    public Task<IReadOnlyDictionary<string, int>> ReadSourceCountsAsync() => ReadCountsAsync(SourceConnectionString);

    public Task<IReadOnlyDictionary<string, int>> ReadTargetCountsAsync() => ReadCountsAsync(TargetConnectionString);

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadSourceRowsAsync() => ReadRowsAsync(SourceConnectionString);

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadTargetRowsAsync() => ReadRowsAsync(TargetConnectionString);

    private static async Task<IReadOnlyDictionary<string, int>> ReadCountsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            string.Join(
                " UNION ALL ",
                AbwabSnapshotContract.Tables.Select(table =>
                    $"SELECT '{table}', count(*)::int FROM public.\"{table}\"")),
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            counts.Add(reader.GetString(0), reader.GetInt32(1));
        }

        return counts;
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadRowsAsync(string connectionString)
    {
        var rowsByTable = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var sql = $"SELECT to_jsonb(row_data)::text FROM public.\"{table}\" AS row_data ORDER BY id";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.GetString(0));
            }

            rowsByTable.Add(table, rows);
        }

        return rowsByTable;
    }

    public async Task<string> ReadTargetMigrationHeadAsync()
    {
        await using var connection = new NpgsqlConnection(TargetConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1",
            connection);
        return await command.ExecuteScalarAsync() as string
            ?? throw new InvalidOperationException("The migrated disposable target has no migration history.");
    }

    private static async Task MigrateAsync(string connectionString)
    {
        await using var dbContext = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(connectionString)
                .Options);
        await dbContext.Database.MigrateAsync();
    }

    private static string PostgreSqlLiteral(string value) => $"'{value.Replace("'", "''")}'";

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private void InvalidateTargetAbwabCaches()
    {
        using var scope = (apiFactory ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet."))
            .Services
            .CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IAbwabCacheInvalidator>();
        cache.InvalidateTree();
        cache.InvalidateTemplates();
    }
}

public sealed record MigrationHistoryEntry(string MigrationId, string ProductVersion);

[CollectionDefinition(nameof(AbwabSnapshotTestCollection))]
public sealed class AbwabSnapshotTestCollection : ICollectionFixture<AbwabSnapshotTestFixture>;
