using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using QuranDashboard.DataImporter;
using QuranDashboard.DataImporter.Import.VerbRunners;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.Process;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using DataImporterProgram = QuranDashboard.DataImporter.Program;

namespace QuranDashboard.Tests.Quran.QuranTopicsBook;

public sealed class QuranTopicsBookImportTestFixture : IAsyncLifetime
{
    private readonly List<string> tempDirectories = [];
    private string? scratchConnectionString;

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(QuranTopicsBookImportTestFixture),
            [DestructiveRehearsalSubtype.CanonicalImport, DestructiveRehearsalSubtype.CanonicalRebuild]);
    }

    public Task DisposeAsync()
    {
        scratchConnectionString = null;
        foreach (var directory in tempDirectories)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return Task.CompletedTask;
    }

    internal async Task<QuranTopicsBookTestDatabase> LeaseDatabaseAsync()
    {
        var connectionString = scratchConnectionString
            ?? throw new InvalidOperationException("QuranTopicsBookImportTestFixture not initialized.");
        await ResetDatabaseAsync(connectionString);
        return new QuranTopicsBookTestDatabase(connectionString, CreateTempDirectory());
    }

    private static async Task ResetDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            TRUNCATE
                abwab_sections,
                abwab_doors,
                abwab_door_aliases,
                abwab_door_relations,
                abwab_door_inclusions,
                abwab_door_inclusion_unit_syncs,
                abwab_templates,
                abwab_template_nodes,
                linking_confirmation_jobs,
                linking_operations,
                linking_prepared_affected_contributions,
                linking_prepared_ayah_descriptions,
                linking_prepared_ayah_words,
                linking_prepared_ayahs,
                linking_prepared_units,
                linking_prepared_sources,
                linking_prepared_preflights,
                linking_source_contribution_units,
                linking_source_contributions,
                linking_unit_ayah_descriptions,
                linking_unit_ayah_words,
                linking_unit_ayahs,
                linking_units,
                linking_door_ayah_words,
                linking_door_ayahs,
                quran_words_unique_simple,
                quran_words_unique_tashkeel,
                quran_words_ordered_simple,
                quran_words_ordered_tashkeel,
                quran_word_morphology_segments,
                quran_word_morphology,
                quran_stems,
                quran_lemmas,
                quran_roots,
                quran_pos_tags,
                quran_similar_ayah_links,
                quran_mutashabihat_occurrences,
                quran_mutashabihat_groups,
                quran_full_i3rab_ayah_entries,
                quran_full_i3rab_entries,
                quran_full_i3rab_sources,
                quran_translation_ayah_entries,
                quran_translation_sources,
                quran_tafsir_ayah_entries,
                quran_tafsir_entries,
                quran_tafsir_sources,
                quran_mushaf_lines,
                quran_words,
                quran_rubs,
                quran_hizbs,
                quran_juzs,
                quran_ayahs,
                quran_surahs,
                quran_mushaf_pages,
                users
            RESTART IDENTITY CASCADE;
            """,
            connection);
        await command.ExecuteNonQueryAsync();

        await using var dbContext = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(connectionString)
                .Options);
        foreach (var migrationId in dbContext.Database.GetMigrations())
        {
            await using var restoreCmd = new NpgsqlCommand(
                """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES (@migrationId, '10.0.8')
                ON CONFLICT ("MigrationId") DO NOTHING;
                """,
                connection);
            restoreCmd.Parameters.AddWithValue("migrationId", migrationId);
            await restoreCmd.ExecuteNonQueryAsync();
        }
    }

    internal QuranDashboardDbContext CreateDbContext(QuranTopicsBookTestDatabase database) =>
        new(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options);

    internal async Task SeedCanonicalMushafSliceAsync(QuranTopicsBookTestDatabase database)
    {
        var assembly = typeof(QuranTopicsBookImportTestFixture).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("mushaf-reader-seed.sql", StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var seedSql = await reader.ReadToEndAsync();

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(seedSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal async Task<int> CreateActiveOwnerAsync(QuranTopicsBookTestDatabase database)
    {
        await using var db = CreateDbContext(database);
        var ownerRoleId = await db.AccessRoles
            .Where(role => role.Name == RoleNames.Owner)
            .Select(role => role.Id)
            .SingleAsync();
        var owner = new User
        {
            LogtoSub = $"quran-topics-book-owner-{Guid.NewGuid():N}",
            Email = $"quran-topics-book-owner-{Guid.NewGuid():N}@example.test",
            NormalizedEmail = $"QURAN-TOPICS-BOOK-OWNER-{Guid.NewGuid():N}@EXAMPLE.TEST",
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.AccessUsers.Add(owner);
        await db.SaveChangesAsync();
        return owner.Id;
    }

    internal async Task<QuranTopicsBookCommandRun> RunCommandAsync(
        QuranTopicsBookTestDatabase database,
        string sourcePath,
        int actorUserId,
        bool validateOnly = false)
    {
        var reportDirectory = Path.Combine(database.TempDirectory, $"report-{Guid.NewGuid():N}");
        var arguments = new List<string>
        {
            "import-quran-topics-book",
            "--source",
            sourcePath,
            "--actor-user-id",
            actorUserId.ToString(),
            "--report-out",
            reportDirectory,
        };
        if (validateOnly)
        {
            arguments.Add("--validate-only");
        }

        var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["ConnectionStrings__QuranDashboardDb"] = database.ConnectionString,
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            },
            captureConsole: true);
        try
        {
            var exitCode = await DataImporterProgram.Main([.. arguments]);
            return new QuranTopicsBookCommandRun(exitCode, reportDirectory, processState.ConsoleOutput);
        }
        finally
        {
            processState.Dispose();
            if (processState.RestoreFailures.Count > 0)
            {
                throw new InvalidOperationException("The Quran topics import test did not restore process state.");
            }
        }
    }

    internal async Task<QuranTopicsBookCommandRun> RunWithAmbiguousCommitAsync(
        QuranTopicsBookTestDatabase database,
        string sourcePath,
        int actorUserId)
    {
        var reportDirectory = Path.Combine(database.TempDirectory, $"report-{Guid.NewGuid():N}");
        using var processState = ProcessGlobalStateScope.Enter(captureConsole: true);
        var exitCode = await ImportQuranTopicsBookRunner.RunAsync(
            [
                "--source",
                sourcePath,
                "--actor-user-id",
                actorUserId.ToString(),
                "--report-out",
                reportDirectory,
            ],
            () => CreateImporterHost(database, new ThrowAfterCommitAcknowledgementInterceptor()),
            () => { });
        return new QuranTopicsBookCommandRun(exitCode, reportDirectory, processState.ConsoleOutput);
    }

    internal async Task<IReadOnlyDictionary<string, int>> ReadTargetCountsAsync(
        QuranTopicsBookTestDatabase database,
        IReadOnlyList<string> tables)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT count(*)::integer FROM public.{table}",
                connection);
            counts.Add(table, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }

        return counts;
    }

    private string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"quran-topics-book-{Guid.NewGuid():N}");
        tempDirectories.Add(directory);
        return directory;
    }

    private IHost CreateImporterHost(
        QuranTopicsBookTestDatabase database,
        IInterceptor? transactionInterceptor = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = database.ConnectionString,
            })
            .Build();
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddApplication();
                services.AddInfrastructure(configuration);
                if (transactionInterceptor is not null)
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options =>
                        options.UseNpgsql(database.ConnectionString).AddInterceptors(transactionInterceptor));
                }
            })
            .Build();
    }

    private sealed class ThrowAfterCommitAcknowledgementInterceptor : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("simulated commit acknowledgement failure"));
    }
}

internal sealed class QuranTopicsBookTestDatabase(
    string connectionString,
    string tempDirectory) : IAsyncDisposable
{
    internal string ConnectionString => connectionString;

    internal string TempDirectory => tempDirectory;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed record QuranTopicsBookCommandRun(
    int ExitCode,
    string ReportDirectory,
    string ConsoleOutput);

[CollectionDefinition(nameof(QuranTopicsBookImportTestCollection), DisableParallelization = true)]
public sealed class QuranTopicsBookImportTestCollection
    : ICollectionFixture<QuranTopicsBookImportTestFixture>;
