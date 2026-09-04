using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.TestArtifacts;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

// This class is selected only by the scheduled/release previous-release-upgrade lane. It validates Git
// adoption before acquiring its exclusive PostgreSQL 18 server, then uses only the pinned local artifact.
public sealed class PreviousReleaseMigrationUpgradeRehearsalTests
{
    [Fact]
    public async Task SupplementalFiveToSixUpgrade_RestoresCanonicalData_ThenBootsAndServesReadSentinels()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var stagingRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-previous-release-upgrade-{Guid.NewGuid():N}");
        var setupPhase = "execution-contract";
        var rehearsalStarted = false;
        WebApplicationFactory<HealthController>? application = null;
        try
        {
            var runKind = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_ARTIFACT_EXECUTION");
            runKind.Should().BeOneOf("scheduled", "release");
            var artifactRoot = Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT");
            artifactRoot.Should().NotBeNullOrWhiteSpace();

            setupPhase = "adoption";
            // All release and local-Git evidence is checked before this test creates its disposable target.
            var plan = PreviousReleaseMigrationUpgradeCommand.VerifyAdoption(repositoryRoot);
            var artifactLock = ArtifactTrustLock.ReadFrom(Path.Combine(repositoryRoot, ArtifactTrustLock.FileName));

            setupPhase = "private-staging";
            Directory.CreateDirectory(stagingRoot);

            setupPhase = "database-startup";
            await using var server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
                nameof(PreviousReleaseMigrationUpgradeRehearsalTests),
                "postgres:18-alpine",
                builder => builder.WithBindMount(stagingRoot, "/artifact", AccessMode.ReadOnly));
            var database = new UpgradeDatabase(server, stagingRoot);
            rehearsalStarted = true;
            var evidence = await PreviousReleaseMigrationUpgradeRehearsal.RunAsync(
                plan,
                artifactLock,
                repositoryRoot,
                stagingRoot,
                new LocalFullCanonicalArtifactFetcher(artifactRoot!),
                database,
                async _ =>
                {
                    application = SmokeApiHost.Build(
                        server.ConnectionString,
                        new FakeExternalUserProfileSource(),
                        new TestSqlCommandCapture());
                    using var client = SmokeApiHost.CreateClient(application);
                    (await client.GetAsync("/api/health")).StatusCode.Should().Be(HttpStatusCode.OK);
                },
                async _ =>
                {
                    using var client = SmokeApiHost.CreateClient(application!);
                    (await client.GetAsync("/api/mushaf/pages/1")).StatusCode.Should().Be(HttpStatusCode.OK);
                    (await client.GetAsync("/api/quran/phrase-search/capabilities")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
                },
                RetainEvidence);

            evidence.Status.Should().Be("passed");
            evidence.Phases.Should().OnlyContain(phase => phase.Status == "passed");
        }
        catch (Exception exception) when (!rehearsalStarted)
        {
            RetainSetupEvidence(PreviousReleaseMigrationUpgradeSetupFailureEvidence.Create(setupPhase, exception));
            throw;
        }
        finally
        {
            application?.Dispose();
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    private static void RetainSetupEvidence(PreviousReleaseMigrationUpgradeSetupFailureEvidence evidence) =>
        RetainSanitizedJson("setup", evidence.ToSanitizedJson());

    private static void RetainEvidence(PreviousReleaseMigrationUpgradeEvidence evidence)
        => RetainSanitizedJson("rehearsal", evidence.ToSanitizedJson());

    private static void RetainSanitizedJson(string kind, string json)
    {
        var root = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_PREVIOUS_RELEASE_EVIDENCE_DIR")
            ?? Path.Combine(Path.GetTempPath(), "quran-dashboard-previous-release-upgrade-evidence");
        Directory.CreateDirectory(root);
        var evidencePath = Path.Combine(root, $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{kind}-{Guid.NewGuid():N}.json");
        File.WriteAllText(evidencePath, json);
        Console.WriteLine($"previous-release-upgrade evidence={json}");
    }

    private sealed class UpgradeDatabase(ExclusivePostgreSqlLease server, string stagingRoot)
        : IPreviousReleaseMigrationUpgradeDatabase
    {
        public async Task MigrateToAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync(migrationId, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> AppliedMigrationsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = CreateContext();
            return (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        }

        public async Task RestoreAsync(string payloadPath, IReadOnlyList<string> tables, CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnectionStringBuilder(server.ConnectionString);
            var relativePayload = Path.GetRelativePath(stagingRoot, payloadPath);
            if (relativePayload.StartsWith("..", StringComparison.Ordinal))
            {
                throw new PreviousReleaseMigrationUpgradePhaseFailureException("payload-outside-private-staging");
            }
            if (tables.Any(table => !ArtifactTrustLockValidator.IsValidTableIdentifier(table)))
            {
                throw new PreviousReleaseMigrationUpgradePhaseFailureException("invalid-locked-table-scope");
            }

            var command = new List<string>
            {
                "pg_restore",
                "--exit-on-error",
                "--no-owner",
                "--no-privileges",
                "--schema",
                "public",
                "--username",
                connection.Username!,
                "--dbname",
                connection.Database!,
                "--data-only",
                "--disable-triggers",
                "--jobs",
                "4",
            };
            // This custom archive matches table selectors by relation name. The separate schema filter
            // keeps those validated names constrained to the public schema.
            command.AddRange(tables.Select(table => $"--table={table}"));
            command.Add($"/artifact/{relativePayload}");
            var result = await server.ExecAsync(command, cancellationToken);
            if (result.ExitCode != 0)
            {
                throw new PreviousReleaseMigrationUpgradePhaseFailureException("pg-restore-rejected-payload");
            }
        }

        public async Task<long> CountRowsAsync(string table, CancellationToken cancellationToken = default)
        {
            if (!ArtifactTrustLockValidator.IsValidTableIdentifier(table))
            {
                throw new InvalidOperationException("The rehearsal requested an invalid table identifier.");
            }

            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM public.\"{table}\";", connection);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        }

        public async Task<PreviousReleasePhraseSearchActual> PhraseSearchStateAsync(
            PreviousReleasePhraseSearchExpectation expectation,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                $"SELECT count(*) FILTER (WHERE active_build_id IS NOT NULL), count(*) FROM public.\"{expectation.StateTable}\";",
                connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            (await reader.ReadAsync(cancellationToken)).Should().BeTrue();
            return new PreviousReleasePhraseSearchActual(reader.GetInt64(1), reader.GetInt64(0));
        }

        private QuranDashboardDbContext CreateContext() => new(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(server.ConnectionString)
            .Options);
    }

}
