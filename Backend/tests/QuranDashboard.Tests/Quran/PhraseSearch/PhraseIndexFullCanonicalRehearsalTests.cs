using DotNet.Testcontainers.Configurations;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.DataImporter.Import.VerbRunners;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;
using QuranDashboard.Tests.Smoke.Data;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;
using QuranDashboard.TestArtifacts;

namespace QuranDashboard.Tests.Quran.PhraseSearch;

// This class is deliberately Release-only. It restores the locked Quran-only artifact once into a
// disposable postgres:18-alpine server, then derives isolated clones for production importer scenarios.
[Collection(nameof(PhraseIndexFullCanonicalRehearsalCollection))]
public sealed class PhraseIndexFullCanonicalRehearsalTests
{
    internal const string PostgreSqlImage = "postgres:18-alpine";
    internal const string DumpMountPath = "/dump";

    private static readonly PhraseIndexBuildExpectations FixtureExpectations =
        PhraseIndexBuildExpectations.Production with
        {
            ApprovedSourceFingerprint = "a11039dffb7d6b5dc84f6e302e4107d88fb08e8bc25713beb537c543e8543531",
            ExpectedSimpleVariantsLengthTwoPlus = 664_474,
            ExpectedTashkilVariantsLengthTwoPlus = 674_836,
            ExpectedTotalVariants = 1_375_387,
            ExpectedRepeatedVariants = 46_794,
            ExpectedRepeatedOccurrences = 144_530,
            ExpectedSimilarityEdges = 1_058_965,
            ExpectedThresholdCounts = new Dictionary<short, long>
            {
                [50] = 1_058_965,
                [60] = 218_193,
                [70] = 93_464,
                [80] = 30_757,
                [90] = 1_527,
            },
        };

    [Fact]
    public async Task FullCanonicalBuild_ActivatesExactlyOneGeneration_AndRetainsSanitizedLifecycleEvidence()
    {
        var runKind = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_ARTIFACT_EXECUTION");
        runKind.Should().BeOneOf("scheduled", "release");
        Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT").Should().NotBeNullOrWhiteSpace();

        var evidence = new PhraseIndexRehearsalEvidence(runKind!);
        var stagingRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-phrase-index-rehearsal-{Guid.NewGuid():N}");
        var phase = "setup";
        Exception? primaryFailure = null;
        try
        {
            // Hash, manifest scope, and migration head are verified before a database or container exists.
            var manifest = SmokeDumpGate.VerifyAndRead(18);
            evidence.RecordArtifact(manifest.Tables.Count);
            Directory.CreateDirectory(stagingRoot);

            var server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
                nameof(PhraseIndexFullCanonicalRehearsalTests),
                PostgreSqlImage,
                builder => builder.WithBindMount(SmokeDumpGate.DumpDirectory!, DumpMountPath, AccessMode.ReadOnly));
            Exception? serverFailure = null;
            try
            {
                await RestoreCanonicalStateAsync(server, evidence);
                var clones = await CanonicalCloneFactory.CreateAsync(server, evidence);
                Exception? cloneFailure = null;
                try
                {
                    phase = "successful-activation";
                    await RunSuccessfulActivationScenarioAsync(clones, stagingRoot, evidence);

                    phase = "post-staging-rollback";
                    await RunPostStagingRollbackScenarioAsync(clones, stagingRoot, evidence);

                    phase = "source-fingerprint-rejection";
                    await RunSourceFingerprintRejectionScenarioAsync(clones, stagingRoot, evidence);

                    evidence.RestoreCount.Should().Be(1);
                    evidence.CloneCount.Should().Be(3);
                    evidence.Status = "passed";
                }
                catch (Exception exception)
                {
                    cloneFailure = exception;
                    throw;
                }
                finally
                {
                    try
                    {
                        await clones.DisposeAsync();
                    }
                    catch (Exception exception) when (cloneFailure is not null)
                    {
                        Console.Error.WriteLine($"phrase-index-rehearsal clone-cleanup-failed={exception.GetType().Name}");
                    }
                }
            }
            catch (Exception exception)
            {
                serverFailure = exception;
                throw;
            }
            finally
            {
                try
                {
                    await server.DisposeAsync();
                }
                catch (Exception exception) when (serverFailure is not null)
                {
                    Console.Error.WriteLine($"phrase-index-rehearsal server-cleanup-failed={exception.GetType().Name}");
                }
            }
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            evidence.RecordFailure(phase, exception);
            throw;
        }
        finally
        {
            Exception? stagingCleanupFailure = null;
            try
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, recursive: true);
                }
            }
            catch (Exception exception) when (primaryFailure is not null)
            {
                Console.Error.WriteLine($"phrase-index-rehearsal staging-cleanup-failed={exception.GetType().Name}");
            }
            catch (Exception exception)
            {
                evidence.RecordFailure("staging-cleanup", exception);
                stagingCleanupFailure = exception;
            }

            try
            {
                RetainEvidence(evidence);
            }
            catch (Exception exception) when (primaryFailure is not null || stagingCleanupFailure is not null)
            {
                Console.Error.WriteLine($"phrase-index-rehearsal evidence-retention-failed={exception.GetType().Name}");
            }

            if (stagingCleanupFailure is not null)
            {
                ExceptionDispatchInfo.Capture(stagingCleanupFailure).Throw();
            }
        }
    }

    private static async Task RunSuccessfulActivationScenarioAsync(
        CanonicalCloneFactory clones,
        string stagingRoot,
        PhraseIndexRehearsalEvidence evidence)
    {
        var scenario = evidence.Scenario("successful-activation");
        scenario.Outcome = "running";
        var database = await clones.CreateAsync("successful-activation");
        Exception? scenarioFailure = null;
        var staged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hook = new PhraseIndexBuildLifecycleTestHook(async (_, ct) =>
        {
            staged.TrySetResult();
            await release.Task.WaitAsync(ct);
        });

        try
        {
            var buildTask = RunBuildAsync(database.ConnectionString, stagingRoot, "success", FixtureExpectations, hook);
            var completed = await Task.WhenAny(staged.Task, buildTask);
            if (completed == buildTask)
            {
                await buildTask;
            }

            (await ReadActiveBuildIdAsync(database.ConnectionString)).Should().BeNull();
            scenario.ActivePointerAbsentDuringStaging = true;
            release.TrySetResult();

            var success = await buildTask;
            success.ExitCode.Should().Be(BuildPhraseIndexResult.SuccessExitCode, BuildFailureDetail(success));
            success.Output.Should().Contain("outcome=Succeeded")
                .And.Contain("report_available=true")
                .And.Contain("report_linked=true");

            var successfulBuild = await ReadBuildAsync(database.ConnectionString);
            successfulBuild.Status.Should().Be(PhraseIndexBuildStatus.Active);
            successfulBuild.ExactReady.Should().BeTrue();
            successfulBuild.SimilarityReady.Should().BeTrue();
            successfulBuild.ActiveBuildId.Should().Be(successfulBuild.Id);
            successfulBuild.PreviousBuildId.Should().BeNull();
            successfulBuild.SourceFingerprint.Should().Be(FixtureExpectations.ApprovedSourceFingerprint);
            successfulBuild.StateFingerprint.Should().Be(successfulBuild.SourceFingerprint);
            successfulBuild.StateRevision.Should().Be(successfulBuild.SourceRevision);
            successfulBuild.ReportPath.Should().Be(success.ReportDirectory);

            using (var report = JsonDocument.Parse(await File.ReadAllTextAsync(success.ReportPath)))
            {
                report.RootElement.GetProperty("outcome").GetString().Should().Be("Succeeded");
                report.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
                report.RootElement.GetProperty("exactReady").GetBoolean().Should().BeTrue();
                report.RootElement.GetProperty("similarityReady").GetBoolean().Should().BeTrue();
                report.RootElement.GetProperty("activeBuildId").GetGuid().Should().Be(successfulBuild.Id);
                report.RootElement.GetProperty("sourceFingerprintBefore").GetString().Should().Be(successfulBuild.SourceFingerprint);
                report.RootElement.GetProperty("sourceFingerprintAtActivation").GetString().Should().Be(successfulBuild.SourceFingerprint);
                report.RootElement.GetProperty("sourceRevisionBefore").GetInt64().Should().Be(successfulBuild.SourceRevision);
                report.RootElement.GetProperty("sourceRevisionAtActivation").GetInt64().Should().Be(successfulBuild.SourceRevision);
                report.RootElement.GetProperty("checks").EnumerateArray().Should().OnlyContain(check => check.GetProperty("passed").GetBoolean());
                scenario.RecordReport(report.RootElement);
            }

            var capabilities = await ReadCapabilitiesAsync(database.ConnectionString);
            capabilities.ActiveBuildId.Should().Be(successfulBuild.Id);
            capabilities.ExactReady.Should().BeTrue();
            capabilities.SimilarityReady.Should().BeTrue();
            var read = await ReadOnePostBuildRepetitionAsync(database.ConnectionString, capabilities);
            read.ActiveBuildId.Should().Be(successfulBuild.Id);
            read.TotalCount.Should().BePositive();
            read.Items.Should().NotBeEmpty();
            scenario.RecordSuccess(success, read.TotalCount);

            var second = await RunBuildAsync(database.ConnectionString, stagingRoot, "second", FixtureExpectations);
            var forced = await RunBuildAsync(database.ConnectionString, stagingRoot, "force", FixtureExpectations, force: true);
            second.ExitCode.Should().Be(BuildPhraseIndexResult.RefusedExitCode);
            second.Output.Should().Contain("outcome=Refused");
            forced.ExitCode.Should().Be(BuildPhraseIndexResult.RefusedExitCode);
            forced.Output.Should().Contain("does not support --force or replacement builds");

            var unchangedBuild = await ReadBuildAsync(database.ConnectionString);
            var unchangedCapabilities = await ReadCapabilitiesAsync(database.ConnectionString);
            var unchangedRead = await ReadOnePostBuildRepetitionAsync(database.ConnectionString, unchangedCapabilities);
            unchangedBuild.Should().BeEquivalentTo(successfulBuild);
            unchangedCapabilities.Should().BeEquivalentTo(capabilities);
            unchangedRead.Should().BeEquivalentTo(read);
            (await ReadBuildCountAsync(database.ConnectionString)).Should().Be(1);
            scenario.SecondBuildRefused = true;
            scenario.ForceBuildRefused = true;
            scenario.ActiveBuildUnchangedAfterRefusals = true;
            scenario.CapabilitiesUnchangedAfterRefusals = true;
            scenario.RepresentativeReadUnchangedAfterRefusals = true;
            scenario.ChildDataUnchangedAfterRefusals = true;
        }
        catch (Exception exception)
        {
            scenarioFailure = exception;
            throw;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await database.DisposeAsync();
            }
            catch (Exception exception) when (scenarioFailure is not null)
            {
                Console.Error.WriteLine($"phrase-index-rehearsal successful-activation-cleanup-failed={exception.GetType().Name}");
            }
        }
    }

    private static async Task RunPostStagingRollbackScenarioAsync(
        CanonicalCloneFactory clones,
        string stagingRoot,
        PhraseIndexRehearsalEvidence evidence)
    {
        var scenario = evidence.Scenario("post-staging-rollback");
        scenario.Outcome = "running";
        var database = await clones.CreateAsync("post-staging-rollback");
        Exception? scenarioFailure = null;
        try
        {
            await InstallPostStagingFailureTriggerAsync(database.ConnectionString);

            var failed = await RunBuildAsync(database.ConnectionString, stagingRoot, "post-staging-failure", FixtureExpectations);
            failed.ExitCode.Should().Be(BuildPhraseIndexResult.FailureExitCode, failed.Output);
            failed.Output.Should().Contain("outcome=Failed");
            var failedBuild = await ReadBuildAsync(database.ConnectionString);
            failedBuild.Status.Should().Be(PhraseIndexBuildStatus.Failed);
            failedBuild.ActiveBuildId.Should().BeNull();
            failedBuild.Children.Should().BeEquivalentTo(PhraseIndexBuildChildCounts.Empty);
            using var report = JsonDocument.Parse(await File.ReadAllTextAsync(failed.ReportPath));
            report.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
            report.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
            scenario.RecordReport(report.RootElement);
            scenario.Outcome = "Failed";
            scenario.PostStagingRollbackClean = true;
        }
        catch (Exception exception)
        {
            scenarioFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                await database.DisposeAsync();
            }
            catch (Exception exception) when (scenarioFailure is not null)
            {
                Console.Error.WriteLine($"phrase-index-rehearsal post-staging-rollback-cleanup-failed={exception.GetType().Name}");
            }
        }
    }

    private static async Task RunSourceFingerprintRejectionScenarioAsync(
        CanonicalCloneFactory clones,
        string stagingRoot,
        PhraseIndexRehearsalEvidence evidence)
    {
        var scenario = evidence.Scenario("source-fingerprint-rejection");
        scenario.Outcome = "running";
        var database = await clones.CreateAsync("source-fingerprint-rejection");
        Exception? scenarioFailure = null;
        try
        {
            await SwapTwoSourceWordsAsync(database.ConnectionString);

            var rejected = await RunBuildAsync(database.ConnectionString, stagingRoot, "source-rejected");
            rejected.ExitCode.Should().Be(BuildPhraseIndexResult.SourceApprovalRequiredExitCode, rejected.Output);
            rejected.Output.Should().Contain("outcome=SourceApprovalRequired");
            (await ReadActiveBuildIdAsync(database.ConnectionString)).Should().BeNull();
            (await ReadBuildCountAsync(database.ConnectionString)).Should().Be(0);

            using var report = JsonDocument.Parse(await File.ReadAllTextAsync(rejected.ReportPath));
            report.RootElement.GetProperty("checks").EnumerateArray()
                .Where(check => check.GetProperty("id").GetString()!.StartsWith("SOURCE-", StringComparison.Ordinal)
                    && check.GetProperty("id").GetString() != "SOURCE-APPROVAL")
                .Should().OnlyContain(check => check.GetProperty("passed").GetBoolean());
            var approval = report.RootElement.GetProperty("checks").EnumerateArray()
                .Single(check => check.GetProperty("id").GetString() == "SOURCE-APPROVAL");
            approval.GetProperty("passed").GetBoolean().Should().BeFalse();
            approval.GetProperty("expected").GetString().Should().Be(
                $"v{PhraseIndexBuildExpectations.Production.ApprovedSourceFingerprintVersion}:{PhraseIndexBuildExpectations.Production.ApprovedSourceFingerprint}");
            scenario.RecordReport(report.RootElement);
            scenario.Outcome = "SourceApprovalRequired";
            scenario.SourceFingerprintRejected = true;
        }
        catch (Exception exception)
        {
            scenarioFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                await database.DisposeAsync();
            }
            catch (Exception exception) when (scenarioFailure is not null)
            {
                Console.Error.WriteLine($"phrase-index-rehearsal source-fingerprint-rejection-cleanup-failed={exception.GetType().Name}");
            }
        }
    }

    internal static async Task RestoreCanonicalStateAsync(ExclusivePostgreSqlLease server, PhraseIndexRehearsalEvidence evidence)
    {
        var started = Stopwatch.GetTimestamp();
        await using (var connection = new NpgsqlConnection(server.ConnectionString))
        {
            await connection.OpenAsync();
            await using var reset = new NpgsqlCommand("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", connection);
            await reset.ExecuteNonQueryAsync();
        }

        await using (var context = new QuranDashboardDbContext(new DbContextOptionsBuilder<QuranDashboardDbContext>()
                         .UseNpgsql(server.ConnectionString)
                         .Options))
        {
            await context.Database.MigrateAsync();
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder(server.ConnectionString);
        var restore = await server.ExecAsync([
            "pg_restore", "--exit-on-error", "--username", connectionBuilder.Username!, "--dbname", connectionBuilder.Database!,
            "--data-only", "--disable-triggers", "--jobs", "4", $"{DumpMountPath}/{SmokeDumpGate.DumpFileName}",
        ]);
        restore.ExitCode.Should().Be(0, restore.Stderr);
        evidence.RecordRestore(Stopwatch.GetElapsedTime(started));
    }

    internal static async Task<PhraseIndexBuildCommandRun> RunBuildAsync(
        string connectionString,
        string stagingRoot,
        string name,
        PhraseIndexBuildExpectations? expectations = null,
        PhraseIndexBuildLifecycleTestHook? testHook = null,
        bool force = false)
    {
        var reportRoot = Path.Combine(stagingRoot, name);
        var arguments = new List<string> { "--report-out", reportRoot };
        if (force)
        {
            arguments.Add("--force");
        }

        var scope = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["ConnectionStrings__QuranDashboardDb"] = connectionString,
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["PhraseSearch__VerifiedDatabaseFreeBytes"] = long.MaxValue.ToString(CultureInfo.InvariantCulture),
                ["PhraseSearch__DatabaseStorageProofContract"] = PhraseIndexOptions.OperatorStorageProofContract,
            },
            captureConsole: true);
        try
        {
            using var host = CreateImporterHost(connectionString, expectations, testHook);
            var exitCode = await BuildPhraseIndexRunner.RunAsync([.. arguments], () => host, static () => { });
            var reportDirectory = Directory.Exists(reportRoot)
                ? Directory.EnumerateDirectories(reportRoot).SingleOrDefault()
                : null;
            return new PhraseIndexBuildCommandRun(
                exitCode,
                scope.ConsoleOutput,
                reportDirectory ?? string.Empty,
                reportDirectory is null ? string.Empty : Path.Combine(reportDirectory, "phrase-index-build-report.json"));
        }
        finally
        {
            scope.Dispose();
            scope.RestoreFailures.Should().BeEmpty();
        }
    }

    internal static async Task SwapTwoSourceWordsAsync(string connectionString)
    {
        // Both replacement values come from the restored artifact. Linked identity IDs move with their word
        // values, preserving source integrity without writing any unique-identity value.
        const string sql = """
            CREATE TEMPORARY TABLE phrase_rehearsal_word_pair AS
              SELECT id, text_uthmani, word_key_imlaei_simple, unique_simple_word_id, unique_tashkeel_word_id
              FROM quran_words
              WHERE is_ayah_marker = false
              ORDER BY id DESC
              LIMIT 1;
            INSERT INTO phrase_rehearsal_word_pair (
              id, text_uthmani, word_key_imlaei_simple, unique_simple_word_id, unique_tashkeel_word_id)
            SELECT word.id, word.text_uthmani, word.word_key_imlaei_simple,
                   word.unique_simple_word_id, word.unique_tashkeel_word_id
            FROM quran_words AS word
            CROSS JOIN phrase_rehearsal_word_pair AS first_word
            WHERE word.is_ayah_marker = false
              AND (word.text_uthmani, word.word_key_imlaei_simple,
                   word.unique_simple_word_id, word.unique_tashkeel_word_id)
                  IS DISTINCT FROM (first_word.text_uthmani, first_word.word_key_imlaei_simple,
                                    first_word.unique_simple_word_id, first_word.unique_tashkeel_word_id)
            ORDER BY word.id DESC
            LIMIT 1;
            UPDATE quran_words AS target
            SET text_uthmani = pair.text_uthmani,
                word_key_imlaei_simple = pair.word_key_imlaei_simple,
                unique_simple_word_id = pair.unique_simple_word_id,
                unique_tashkeel_word_id = pair.unique_tashkeel_word_id
            FROM phrase_rehearsal_word_pair AS pair
            WHERE target.id IN (SELECT id FROM phrase_rehearsal_word_pair)
              AND pair.id <> target.id;
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = 30,
        };
        await command.ExecuteNonQueryAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM phrase_rehearsal_word_pair", connection);
        (await count.ExecuteScalarAsync()).Should().Be(2);
    }

    private static async Task InstallPostStagingFailureTriggerAsync(string connectionString)
    {
        const string sql = """
            CREATE FUNCTION reject_phrase_build_validation() RETURNS trigger AS $$
            BEGIN
              IF NEW.status = 2 THEN RAISE EXCEPTION 'phrase-rehearsal-post-staging-failure'; END IF;
              RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER reject_phrase_build_validation
            BEFORE UPDATE ON quran_phrase_index_builds
            FOR EACH ROW EXECUTE FUNCTION reject_phrase_build_validation();
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<PhraseIndexBuildDatabaseState> ReadBuildAsync(string connectionString)
    {
        const string sql = """
            SELECT build.id, build.status, build.exact_ready, build.similarity_ready, build.source_fingerprint,
                   build.source_revision, build.report_path, state.active_build_id, state.previous_build_id,
                   state.source_fingerprint, state.source_revision,
                   (SELECT count(*) FROM quran_phrase_search_tokens WHERE build_id = build.id),
                   (SELECT count(*) FROM quran_phrase_variants WHERE build_id = build.id),
                   (SELECT count(*) FROM quran_phrase_occurrences WHERE build_id = build.id),
                   (SELECT count(*) FROM quran_phrase_similarity_edges WHERE build_id = build.id),
                   (SELECT count(*) FROM quran_phrase_similarity_anchor_stats WHERE build_id = build.id)
            FROM quran_phrase_index_builds AS build
            CROSS JOIN quran_phrase_index_state AS state
            ORDER BY build.started_at_utc DESC
            LIMIT 1;
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new PhraseIndexBuildDatabaseState(
            reader.GetGuid(0),
            (PhraseIndexBuildStatus)reader.GetInt16(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetString(4),
            reader.GetInt64(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetGuid(7),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetInt64(10),
            new PhraseIndexBuildChildCounts(
                reader.GetInt64(11),
                reader.GetInt64(12),
                reader.GetInt64(13),
                reader.GetInt64(14),
                reader.GetInt64(15)));
    }

    internal static async Task<Guid?> ReadActiveBuildIdAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT active_build_id FROM quran_phrase_index_state WHERE id = 1", connection);
        return await command.ExecuteScalarAsync() is Guid buildId ? buildId : null;
    }

    internal static async Task<int> ReadBuildCountAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM quran_phrase_index_builds", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<PhraseSearchCapabilitiesResponse> ReadCapabilitiesAsync(string connectionString)
    {
        using var host = CreateReaderHost(connectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IPhraseRepetitionsReader>();
        return (await reader.GetCapabilitiesAsync(CancellationToken.None))
            .Should().BeOfType<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success>().Which.Value;
    }

    private static async Task<PhraseRepetitionsPageResponse> ReadOnePostBuildRepetitionAsync(
        string connectionString,
        PhraseSearchCapabilitiesResponse capabilities)
    {
        var mode = capabilities.Modes.First(candidate => candidate.RepeatedLengths.Count > 0);
        using var host = CreateReaderHost(connectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IPhraseRepetitionsReader>();
        return (await reader.GetRepetitionsAsync(
                Enum.Parse<PhraseTextMode>(mode.Mode, ignoreCase: true),
                mode.RepeatedLengths[0],
                [],
                PhraseRepetitionSort.OccurrencesDescending,
                1,
                1,
                CancellationToken.None))
            .Should().BeOfType<PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Success>().Which.Value;
    }

    private static IHost CreateImporterHost(
        string connectionString,
        PhraseIndexBuildExpectations? expectations,
        PhraseIndexBuildLifecycleTestHook? testHook)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString,
                ["PhraseSearch:VerifiedDatabaseFreeBytes"] = long.MaxValue.ToString(CultureInfo.InvariantCulture),
                ["PhraseSearch:DatabaseStorageProofContract"] = PhraseIndexOptions.OperatorStorageProofContract,
            })
            .Build();
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddApplication();
                services.AddInfrastructure(configuration);
                services.AddScoped(_ => expectations ?? PhraseIndexBuildExpectations.Production);
                services.AddScoped(_ => testHook ?? PhraseIndexBuildLifecycleTestHook.None);
            })
            .Build();
    }

    private static IHost CreateReaderHost(string connectionString)
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

    internal static void RetainEvidence(PhraseIndexRehearsalEvidence evidence)
    {
        var directory = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_PHRASE_INDEX_REHEARSAL_EVIDENCE_DIR")
            ?? Path.Combine(Path.GetTempPath(), "quran-dashboard-phrase-index-rehearsal-evidence");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(
            directory,
            $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-phrase-index-rehearsal-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Console.WriteLine("phrase-index-rehearsal evidence-written");
    }

    private static string BuildFailureDetail(PhraseIndexBuildCommandRun run)
    {
        if (string.IsNullOrWhiteSpace(run.ReportPath) || !File.Exists(run.ReportPath))
        {
            return run.Output;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(run.ReportPath));
        var failures = document.RootElement.GetProperty("checks").EnumerateArray()
            .Where(check => !check.GetProperty("passed").GetBoolean())
            .Select(check => check.GetProperty("id").GetString())
            .ToArray();
        return $"{run.Output}\nfailed_checks={string.Join(",", failures)}";
    }

    internal sealed record PhraseIndexBuildCommandRun(int ExitCode, string Output, string ReportDirectory, string ReportPath);

    internal sealed record PhraseIndexBuildDatabaseState(
        Guid Id,
        PhraseIndexBuildStatus Status,
        bool ExactReady,
        bool SimilarityReady,
        string SourceFingerprint,
        long SourceRevision,
        string? ReportPath,
        Guid? ActiveBuildId,
        Guid? PreviousBuildId,
        string? StateFingerprint,
        long StateRevision,
        PhraseIndexBuildChildCounts Children);

    internal sealed record PhraseIndexBuildChildCounts(
        long SearchTokens,
        long Variants,
        long Occurrences,
        long SimilarityEdges,
        long SimilarityAnchorStats)
    {
        internal static PhraseIndexBuildChildCounts Empty { get; } = new(0, 0, 0, 0, 0);
    }

    internal sealed class PhraseIndexRehearsalEvidence(string runKind)
    {
        private readonly Dictionary<string, PhraseIndexRehearsalScenarioEvidence> scenarios = new(StringComparer.Ordinal)
        {
            ["successful-activation"] = new("successful-activation"),
            ["post-staging-rollback"] = new("post-staging-rollback"),
            ["source-fingerprint-rejection"] = new("source-fingerprint-rejection"),
        };

        public string RunKind { get; } = runKind;
        public string Status { get; set; } = "failed";
        public int? CanonicalTableCount { get; private set; }
        public int? RestoreCount { get; private set; }
        public long? RestoreMilliseconds { get; private set; }
        public int? CloneCount { get; private set; }
        public long? CloneMilliseconds { get; private set; }
        public IReadOnlyList<PhraseIndexRehearsalScenarioEvidence> Scenarios => scenarios.Values.ToArray();
        public string? FailurePhase { get; private set; }
        public string? FailureType { get; private set; }

        public PhraseIndexRehearsalScenarioEvidence Scenario(string identifier) => scenarios[identifier];
        public void RecordArtifact(int tableCount) => CanonicalTableCount = tableCount;
        public void RecordRestore(TimeSpan duration)
        {
            RestoreCount = (RestoreCount ?? 0) + 1;
            RestoreMilliseconds = (RestoreMilliseconds ?? 0) + (long)duration.TotalMilliseconds;
        }

        public void RecordClone(TimeSpan duration)
        {
            CloneCount = (CloneCount ?? 0) + 1;
            CloneMilliseconds = (CloneMilliseconds ?? 0) + (long)duration.TotalMilliseconds;
        }

        public void RecordFailure(string phase, Exception exception)
        {
            Status = "failed";
            FailurePhase = phase;
            FailureType = exception.GetType().Name;
        }
    }

    internal sealed class PhraseIndexRehearsalScenarioEvidence(string identifier)
    {
        public string Identifier { get; } = identifier;
        public string Outcome { get; set; } = "not-run";
        public long? BuildDurationMilliseconds { get; private set; }
        public long? Variants { get; private set; }
        public long? Occurrences { get; private set; }
        public long? SimilarityEdges { get; private set; }
        public int? RepetitionCount { get; private set; }
        public bool? ActivePointerAbsentDuringStaging { get; set; }
        public bool? SecondBuildRefused { get; set; }
        public bool? ForceBuildRefused { get; set; }
        public bool? ActiveBuildUnchangedAfterRefusals { get; set; }
        public bool? CapabilitiesUnchangedAfterRefusals { get; set; }
        public bool? RepresentativeReadUnchangedAfterRefusals { get; set; }
        public bool? ChildDataUnchangedAfterRefusals { get; set; }
        public bool? SourceFingerprintRejected { get; set; }
        public bool? PostStagingRollbackClean { get; set; }
        public IReadOnlyList<PhraseIndexRehearsalCheckEvidence>? Checks { get; private set; }

        public void RecordSuccess(PhraseIndexBuildCommandRun run, int repetitions)
        {
            Outcome = "Succeeded";
            BuildDurationMilliseconds = ReadDuration(run.ReportPath);
            Variants = ReadTotal(run.ReportPath, "variants");
            Occurrences = ReadTotal(run.ReportPath, "occurrences");
            SimilarityEdges = ReadTotal(run.ReportPath, "similarityEdges");
            RepetitionCount = repetitions;
        }

        public void RecordReport(JsonElement report)
        {
            Checks = report.GetProperty("checks").EnumerateArray()
                .Select(check => new PhraseIndexRehearsalCheckEvidence(
                    check.GetProperty("id").GetString()!,
                    check.GetProperty("passed").GetBoolean()))
                .ToArray();
        }

        private static long ReadDuration(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("durationMilliseconds").GetInt64();
        }

        private static long ReadTotal(string path, string property)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("totals").GetProperty(property).GetInt64();
        }
    }

    internal sealed record PhraseIndexRehearsalCheckEvidence(string Id, bool Passed);

    private sealed class CanonicalCloneFactory : IAsyncDisposable
    {
        private readonly NpgsqlConnectionStringBuilder source;
        private readonly NpgsqlConnectionStringBuilder maintenance;
        private readonly PhraseIndexRehearsalEvidence evidence;
        private readonly List<string> cloneNames = [];

        private CanonicalCloneFactory(
            NpgsqlConnectionStringBuilder source,
            NpgsqlConnectionStringBuilder maintenance,
            PhraseIndexRehearsalEvidence evidence)
        {
            this.source = source;
            this.maintenance = maintenance;
            this.evidence = evidence;
        }

        internal static async Task<CanonicalCloneFactory> CreateAsync(
            ExclusivePostgreSqlLease server,
            PhraseIndexRehearsalEvidence evidence)
        {
            var restoredSource = new NpgsqlConnectionStringBuilder(server.ConnectionString);
            NpgsqlConnection.ClearPool(new NpgsqlConnection(restoredSource.ConnectionString));
            var source = new NpgsqlConnectionStringBuilder(restoredSource.ConnectionString) { Pooling = false };
            var maintenance = new NpgsqlConnectionStringBuilder(source.ConnectionString)
            {
                Database = "template1",
                Pooling = false,
            };
            var factory = new CanonicalCloneFactory(source, maintenance, evidence);
            await factory.ExecuteMaintenanceAsync(
                $"ALTER DATABASE {PostgreSqlDatabaseName.Quote(source.Database!)} WITH ALLOW_CONNECTIONS false IS_TEMPLATE true");
            return factory;
        }

        internal async Task<CanonicalClone> CreateAsync(string scenario)
        {
            var name = PostgreSqlDatabaseName.CreateForOwner($"phrase-index-{scenario}");
            var started = Stopwatch.GetTimestamp();
            await ExecuteMaintenanceAsync(
                $"CREATE DATABASE {PostgreSqlDatabaseName.Quote(name)} TEMPLATE {PostgreSqlDatabaseName.Quote(source.Database!)}");
            cloneNames.Add(name);
            evidence.RecordClone(Stopwatch.GetElapsedTime(started));
            var connection = new NpgsqlConnectionStringBuilder(source.ConnectionString)
            {
                Database = name,
                Pooling = false,
            }.ConnectionString;
            return new CanonicalClone(this, name, connection);
        }

        private async ValueTask ReleaseAsync(string name, string connectionString)
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(connectionString));
            await ExecuteMaintenanceAsync($"DROP DATABASE IF EXISTS {PostgreSqlDatabaseName.Quote(name)} WITH (FORCE)");
            cloneNames.Remove(name);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var cloneName in cloneNames.ToArray())
            {
                await ExecuteMaintenanceAsync($"DROP DATABASE IF EXISTS {PostgreSqlDatabaseName.Quote(cloneName)} WITH (FORCE)");
            }

            cloneNames.Clear();
            await ExecuteMaintenanceAsync(
                $"ALTER DATABASE {PostgreSqlDatabaseName.Quote(source.Database!)} WITH IS_TEMPLATE false ALLOW_CONNECTIONS true");
        }

        private async Task ExecuteMaintenanceAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection)
            {
                CommandTimeout = 120,
            };
            await command.ExecuteNonQueryAsync();
        }

        internal sealed class CanonicalClone : IAsyncDisposable
        {
            private readonly CanonicalCloneFactory factory;
            private readonly string name;

            internal CanonicalClone(CanonicalCloneFactory factory, string name, string connectionString)
            {
                this.factory = factory;
                this.name = name;
                ConnectionString = connectionString;
            }

            internal string ConnectionString { get; }

            public ValueTask DisposeAsync() => factory.ReleaseAsync(name, ConnectionString);
        }
    }
}

[CollectionDefinition(nameof(PhraseIndexFullCanonicalRehearsalCollection), DisableParallelization = true)]
public sealed class PhraseIndexFullCanonicalRehearsalCollection
    : ICollectionFixture<PhraseIndexFullCanonicalRehearsalFixture>;

// The collection, rather than the fixture, owns serialization. The test creates and disposes its
// exclusive server so a failed artifact prerequisite still occurs before any container starts.
public sealed class PhraseIndexFullCanonicalRehearsalFixture;
