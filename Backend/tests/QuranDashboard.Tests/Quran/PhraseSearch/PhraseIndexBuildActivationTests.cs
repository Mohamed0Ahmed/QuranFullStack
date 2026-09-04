using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.DataImporter;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Tests.TestSupport.Execution;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;
using DataImporterProgram = QuranDashboard.DataImporter.Program;
using System.Globalization;

namespace QuranDashboard.Tests.Quran.PhraseSearch;

[Collection(nameof(PhraseIndexBuildActivationCollection))]
public sealed class PhraseIndexBuildActivationTests(PhraseIndexBuildActivationFixture fixture)
{
    [Fact]
    public async Task MissingStorageProof_FailsBeforeSourceBootstrap_AndCreatesNoBuildAudit()
    {
        await using var database = await fixture.LeaseDatabaseAsync();

        var run = await fixture.RunBuildAsync(database);

        run.ExitCode.Should().Be(BuildPhraseIndexResult.FailureExitCode);
        run.Output.Should().Contain("outcome=Failed");
        (await fixture.ReadBuildCountAsync(database)).Should().Be(0);
        (await fixture.ReadActiveBuildIdAsync(database)).Should().BeNull();

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(run.ReportPath));
        report.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
        report.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
        report.RootElement.GetProperty("checks").EnumerateArray()
            .Should().Contain(check => check.GetProperty("id").GetString() == "DISK-STORAGE-PROOF"
                && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task MissingFoundation_FailsClosedBeforeBuildRowsExist()
    {
        await using var database = await fixture.LeaseDatabaseAsync();

        var run = await fixture.RunBuildAsync(
            database,
            verifiedStorageBytes: long.MaxValue,
            storageProofContract: PhraseIndexOptions.OperatorStorageProofContract);

        run.ExitCode.Should().Be(BuildPhraseIndexResult.FailureExitCode);
        (await fixture.ReadBuildCountAsync(database)).Should().Be(0);
        (await fixture.ReadActiveBuildIdAsync(database)).Should().BeNull();

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(run.ReportPath));
        report.RootElement.GetProperty("checks").EnumerateArray()
            .Should().Contain(check => check.GetProperty("id").GetString() == "SOURCE-READABLE-WORDS"
                && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task MissingWordIdentity_FailsClosedBeforeBuildRowsExist()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedMushafSliceWithMissingWordIdentityAsync(database);

        var run = await fixture.RunBuildAsync(
            database,
            verifiedStorageBytes: long.MaxValue,
            storageProofContract: PhraseIndexOptions.OperatorStorageProofContract);

        run.ExitCode.Should().Be(BuildPhraseIndexResult.FailureExitCode);
        (await fixture.ReadBuildCountAsync(database)).Should().Be(0);
        (await fixture.ReadActiveBuildIdAsync(database)).Should().BeNull();

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(run.ReportPath));
        report.RootElement.GetProperty("checks").EnumerateArray()
            .Should().Contain(check => check.GetProperty("id").GetString() == "SOURCE-IDENTITY-LINKS"
                && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task IncompleteBuild_IsRecoveredWithoutAnActivePointer_AndLeavesNoChildData()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var buildId = await fixture.SeedIncompleteBuildAsync(database);

        var run = await fixture.RunBuildAsync(
            database,
            verifiedStorageBytes: long.MaxValue,
            storageProofContract: PhraseIndexOptions.OperatorStorageProofContract);

        run.ExitCode.Should().Be(BuildPhraseIndexResult.FailureExitCode);
        (await fixture.ReadActiveBuildIdAsync(database)).Should().BeNull();
        (await fixture.ReadChildCountAsync(database, buildId)).Should().Be(0);
        (await fixture.ReadBuildStatusAsync(database, buildId)).Should().Be(PhraseIndexBuildStatus.Failed);

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(run.ReportPath));
        report.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
        report.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ExistingActiveBuild_RefusesSecondAndForceAttempts_WithoutReplacingIt()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var activeBuildId = await fixture.SeedValidatedBuildAsync(database);
        await fixture.ActivateSeededBuildAsync(database, activeBuildId);

        var second = await fixture.RunBuildAsync(database);
        var forced = await fixture.RunBuildAsync(database, force: true);

        second.ExitCode.Should().Be(BuildPhraseIndexResult.RefusedExitCode);
        second.Output.Should().Contain("outcome=Refused");
        forced.ExitCode.Should().Be(BuildPhraseIndexResult.RefusedExitCode);
        forced.Output.Should().Contain("does not support --force or replacement builds");
        (await fixture.ReadActiveBuildIdAsync(database)).Should().Be(activeBuildId);
        (await fixture.ReadBuildCountAsync(database)).Should().Be(1);
    }

    [Fact]
    public async Task ActivatedBuild_ReportAndRuntimeCapabilitiesAgreeOnTheOnlyReadyBuild()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var buildId = await fixture.SeedValidatedBuildAsync(database);

        await fixture.ActivateSeededBuildAsync(database, buildId);
        var reportPath = await fixture.WriteActivationReportAsync(database, buildId);
        var capabilities = await fixture.ReadCapabilitiesAsync(database);

        capabilities.ActiveBuildId.Should().Be(buildId);
        capabilities.ExactReady.Should().BeTrue();
        capabilities.SimilarityReady.Should().BeTrue();
        (await fixture.ReadActiveBuildIdAsync(database)).Should().Be(buildId);
        (await fixture.ReadBuildCountAsync(database)).Should().Be(1);

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        report.RootElement.GetProperty("outcome").GetString().Should().Be("Succeeded");
        report.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("exactReady").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("similarityReady").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("activeBuildId").GetGuid().Should().Be(capabilities.ActiveBuildId);
    }
}

public sealed class PhraseIndexBuildActivationFixture : IAsyncLifetime
{
    private const string SyntheticFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private readonly List<string> temporaryDirectories = [];
    private string? scratchConnectionString;

    public async Task InitializeAsync()
    {
        scratchConnectionString = await MigratedScratchDatabase.ResolveAndMigrateAsync(
            nameof(PhraseIndexBuildActivationFixture),
            DestructiveRehearsalSubtype.PhraseSearchIndexBuild);
    }

    public Task DisposeAsync()
    {
        scratchConnectionString = null;
        foreach (var directory in temporaryDirectories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    internal async Task<PhraseIndexBuildActivationDatabase> LeaseDatabaseAsync()
    {
        var connectionString = scratchConnectionString
            ?? throw new InvalidOperationException("PhraseIndexBuildActivationFixture not initialized.");
        await ResetDatabaseAsync(connectionString);
        var reportRoot = Path.Combine(Path.GetTempPath(), $"phrase-index-activation-{Guid.NewGuid():N}");
        temporaryDirectories.Add(reportRoot);
        return new PhraseIndexBuildActivationDatabase(connectionString, reportRoot);
    }

    private static async Task ResetDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            TRUNCATE
                quran_phrase_search_tokens,
                quran_phrase_index_builds,
                quran_words,
                quran_ayahs,
                quran_surahs,
                quran_mushaf_pages
            RESTART IDENTITY CASCADE;

            INSERT INTO quran_phrase_index_state (
                id,
                source_revision,
                source_fingerprint,
                active_build_id,
                previous_build_id,
                is_stale,
                stale_reason,
                updated_at_utc)
            VALUES (
                1,
                0,
                NULL,
                NULL,
                NULL,
                FALSE,
                NULL,
                CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE
            SET source_revision = 0,
                source_fingerprint = NULL,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = FALSE,
                stale_reason = NULL,
                updated_at_utc = CURRENT_TIMESTAMP;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    internal async Task<PhraseIndexBuildCommandRun> RunBuildAsync(
        PhraseIndexBuildActivationDatabase database,
        bool force = false,
        long? verifiedStorageBytes = null,
        string? storageProofContract = null)
    {
        var reportRoot = Path.Combine(database.ReportRoot, $"run-{Guid.NewGuid():N}");
        var arguments = new List<string> { "build-phrase-index", "--report-out", reportRoot };
        if (force)
        {
            arguments.Add("--force");
        }

        var scope = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["ConnectionStrings__QuranDashboardDb"] = database.ConnectionString,
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["PhraseSearch__VerifiedDatabaseFreeBytes"] = verifiedStorageBytes?.ToString(CultureInfo.InvariantCulture),
                ["PhraseSearch__DatabaseStorageProofContract"] = storageProofContract,
            },
            captureConsole: true);
        try
        {
            var exitCode = await DataImporterProgram.Main([.. arguments]);
            var reportPath = Directory.Exists(reportRoot)
                ? Path.Combine(
                    Directory.EnumerateDirectories(reportRoot).Single(),
                    "phrase-index-build-report.json")
                : string.Empty;
            return new PhraseIndexBuildCommandRun(exitCode, scope.ConsoleOutput, reportPath);
        }
        finally
        {
            scope.Dispose();
            scope.RestoreFailures.Should().BeEmpty();
        }
    }

    internal async Task<Guid> SeedIncompleteBuildAsync(PhraseIndexBuildActivationDatabase database)
    {
        var buildId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE quran_phrase_index_state
            SET source_revision = 1,
                source_fingerprint = @fingerprint,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = FALSE,
                stale_reason = NULL
            WHERE id = 1;
            INSERT INTO quran_phrase_index_builds (
                id, status, format_version, exact_ready, similarity_ready, builder_version,
                source_revision, source_fingerprint, started_at_utc, search_token_count,
                variant_count, occurrence_count, similarity_edge_count, similarity_anchor_stat_count)
            VALUES (@build_id, 1, 2, FALSE, FALSE, 'synthetic-lifecycle-contract',
                1, @fingerprint, CURRENT_TIMESTAMP, 0, 0, 0, 0, 0);
            INSERT INTO quran_phrase_search_tokens (build_id, mode, id, search_text, exact_token_ids)
            VALUES (@build_id, 1, 1, 'synthetic-token', ARRAY[1]);
            """,
            connection);
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("fingerprint", SyntheticFingerprint);
        await command.ExecuteNonQueryAsync();
        return buildId;
    }

    internal async Task SeedMushafSliceWithMissingWordIdentityAsync(PhraseIndexBuildActivationDatabase database)
    {
        var resourceName = typeof(PhraseIndexBuildActivationFixture).Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("mushaf-reader-seed.sql", StringComparison.Ordinal));
        await using var stream = typeof(PhraseIndexBuildActivationFixture).Assembly
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using (var seed = new NpgsqlCommand(await reader.ReadToEndAsync(), connection))
        {
            await seed.ExecuteNonQueryAsync();
        }

        await using var removeIdentity = new NpgsqlCommand(
            "UPDATE quran_words SET unique_simple_word_id = NULL WHERE id = 1001",
            connection);
        await removeIdentity.ExecuteNonQueryAsync();
    }

    internal async Task<Guid> SeedValidatedBuildAsync(PhraseIndexBuildActivationDatabase database)
    {
        var buildId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE quran_phrase_index_state
            SET source_revision = 1,
                source_fingerprint = @fingerprint,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = FALSE,
                stale_reason = NULL
            WHERE id = 1;
            INSERT INTO quran_phrase_index_builds (
                id, status, format_version, exact_ready, similarity_ready, builder_version,
                source_revision, source_fingerprint, started_at_utc, validated_at_utc,
                search_token_count, variant_count, occurrence_count, similarity_edge_count,
                similarity_anchor_stat_count, validation_verdict)
            VALUES (@build_id, 2, 2, TRUE, TRUE, 'synthetic-lifecycle-contract',
                1, @fingerprint, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,
                0, 0, 0, 0, 0, 'pass');
            """,
            connection);
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("fingerprint", SyntheticFingerprint);
        await command.ExecuteNonQueryAsync();
        return buildId;
    }

    internal async Task ActivateSeededBuildAsync(PhraseIndexBuildActivationDatabase database, Guid buildId)
    {
        using var host = CreateHost(database.ConnectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var activator = scope.ServiceProvider.GetRequiredService<PhraseIndexActivator>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();

        var activation = await activator.ActivateAsync(connection, buildId, 1, SyntheticFingerprint, CancellationToken.None);

        activation.Activated.Should().BeTrue();
        activation.ActiveBuildId.Should().Be(buildId);
    }

    internal async Task<string> WriteActivationReportAsync(PhraseIndexBuildActivationDatabase database, Guid buildId)
    {
        using var host = CreateHost(database.ConnectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<PhraseIndexBuildReportWriter>();
        var reportDirectory = Path.Combine(database.ReportRoot, "activated-report");
        await writer.WriteAsync(
            new PhraseIndexBuildReport(
                buildId,
                "2",
                "synthetic-lifecycle-contract",
                "Active",
                "Succeeded",
                true,
                true,
                true,
                true,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                0,
                0,
                1,
                SyntheticFingerprint,
                1,
                SyntheticFingerprint,
                buildId,
                PhraseIndexBuildTotals.Empty,
                new PhraseDiskPreflight(0, 0, 0, 0, 0, 0, 0, "synthetic-test", true, true),
                [],
                [],
                [],
                []),
            reportDirectory,
            CancellationToken.None);
        return Path.Combine(reportDirectory, "phrase-index-build-report.json");
    }

    internal async Task<PhraseSearchCapabilitiesResponse> ReadCapabilitiesAsync(PhraseIndexBuildActivationDatabase database)
    {
        using var host = CreateHost(database.ConnectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IPhraseRepetitionsReader>();
        var result = await reader.GetCapabilitiesAsync(CancellationToken.None);
        return result.Should().BeOfType<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success>()
            .Which.Value;
    }

    internal async Task<int> ReadBuildCountAsync(PhraseIndexBuildActivationDatabase database)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM quran_phrase_index_builds", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    internal async Task<Guid?> ReadActiveBuildIdAsync(PhraseIndexBuildActivationDatabase database)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT active_build_id FROM quran_phrase_index_state WHERE id = 1",
            connection);
        var value = await command.ExecuteScalarAsync();
        return value switch
        {
            null or DBNull => null,
            Guid buildId => buildId,
            _ => throw new InvalidOperationException("PhraseSearch state has an invalid active build ID."),
        };
    }

    internal async Task<int> ReadChildCountAsync(PhraseIndexBuildActivationDatabase database, Guid buildId)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM quran_phrase_search_tokens WHERE build_id = @build_id",
            connection);
        command.Parameters.AddWithValue("build_id", buildId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    internal async Task<PhraseIndexBuildStatus> ReadBuildStatusAsync(PhraseIndexBuildActivationDatabase database, Guid buildId)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT status FROM quran_phrase_index_builds WHERE id = @build_id",
            connection);
        command.Parameters.AddWithValue("build_id", buildId);
        return (PhraseIndexBuildStatus)(await command.ExecuteScalarAsync() is short status
            ? status
            : throw new InvalidOperationException("The seeded PhraseSearch build status is missing."));
    }

    private static IHost CreateHost(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString,
            })
            .Build();
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddApplication();
                services.AddInfrastructure(configuration);
            })
            .Build();
    }
}

internal sealed class PhraseIndexBuildActivationDatabase(string connectionString, string reportRoot) : IAsyncDisposable
{
    internal string ConnectionString => connectionString;

    internal string ReportRoot => reportRoot;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed record PhraseIndexBuildCommandRun(int ExitCode, string Output, string ReportPath);

[CollectionDefinition(nameof(PhraseIndexBuildActivationCollection), DisableParallelization = true)]
public sealed class PhraseIndexBuildActivationCollection
    : ICollectionFixture<PhraseIndexBuildActivationFixture>;
