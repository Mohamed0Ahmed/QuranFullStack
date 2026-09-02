using System.Diagnostics;
using QuranDashboard.TestArtifacts;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

public sealed class PreviousReleaseMigrationUpgradeTests
{
    [Fact]
    public void AdoptionGate_VerifiesTheAdoptedReleaseEvidenceBeforeAnyDatabaseCanBeSelected()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["previous-release-upgrade", "--root", repositoryRoot],
            output,
            error);

        exitCode.Should().Be(0);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("state=verified")
            .And.Contain("authoritative-forward-migrations=0")
            .And.Contain("supplemental-forward-migrations=1");
    }

    [Fact]
    public void AdoptionGate_RejectsUnexpectedArgumentsAndDeclarationSchemas()
    {
        using var error = new StringWriter();

        PreviousReleaseMigrationUpgradeCommand.Execute(
            ["previous-release-upgrade", "unexpected"],
            TextWriter.Null,
            error).Should().Be(2);
        error.ToString().Should().Contain("Usage:");

        PreviousReleaseMigrationUpgradeCommand.Validate(new PreviousReleaseMigrationUpgradeDeclaration(
            "unexpected-schema",
            2,
            "adopted",
            null!,
            null!,
            null!,
            null!))
            .Should().Be("declaration-schema-or-version-is-invalid");
    }

    [Fact]
    public void SetupFailureEvidence_RetainsOnlyThePhaseAndExceptionType()
    {
        var evidence = PreviousReleaseMigrationUpgradeSetupFailureEvidence.Create(
            "database-startup",
            new InvalidOperationException("Password=synthetic-password"));

        evidence.ToSanitizedJson().Should().Contain("\"status\":\"failed\"")
            .And.Contain("\"phase\":\"database-startup\"")
            .And.Contain("\"detail\":\"unexpected-InvalidOperationException\"")
            .And.Contain("\"migrationState\":\"not-started\"")
            .And.NotContain("synthetic-password");
    }

    [Fact]
    public void HistoricalMigrationSourceParity_AllowsNewForwardMigrationsAndRejectsChangedOrDeletedHistoricalSources()
    {
        const string historicalMigration = "20260813153400_InitialBaseline";
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-previous-release-git-{Guid.NewGuid():N}");
        var migrations = Path.Combine(root, "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations");
        Directory.CreateDirectory(migrations);
        var sourcePath = Path.Combine(migrations, $"{historicalMigration}.cs");
        var designerPath = Path.Combine(migrations, $"{historicalMigration}.Designer.cs");
        File.WriteAllText(sourcePath, "historical source");
        File.WriteAllText(designerPath, "historical designer");
        try
        {
            RunGit(root, "init");
            RunGit(root, "add", ".");
            RunGit(root, "-c", "user.email=tests@example.test", "-c", "user.name=Tests", "commit", "-m", "historical migration");
            var commit = RunGit(root, "rev-parse", "HEAD").Trim();

            File.WriteAllText(Path.Combine(migrations, "20260826012918_AddQuranPhraseSearchIndex.cs"), "forward source");
            LocalGit.WorkingMigrationSourcesMatch(root, commit, [historicalMigration]).Should().BeTrue();

            File.AppendAllText(sourcePath, " changed");
            LocalGit.WorkingMigrationSourcesMatch(root, commit, [historicalMigration]).Should().BeFalse();

            File.WriteAllText(sourcePath, "historical source");
            File.Delete(designerPath);
            LocalGit.WorkingMigrationSourcesMatch(root, commit, [historicalMigration]).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SyntheticRehearsal_RetainsOnlyPhaseEvidenceWhenTheDisposableRestoreFails()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var plan = PreviousReleaseMigrationUpgradeCommand.VerifyAdoption(repositoryRoot);
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-previous-release-synthetic-{Guid.NewGuid():N}");
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(root);
        try
        {
            var artifactLock = CreateSyntheticLock(root);
            PreviousReleaseMigrationUpgradeEvidence? evidence = null;

            var action = () => PreviousReleaseMigrationUpgradeRehearsal.RunAsync(
                plan,
                artifactLock,
                repositoryRoot,
                staging,
                new SyntheticFetcher(root),
                new SyntheticDisposableDatabase(plan),
                _ => Task.CompletedTask,
                _ => Task.CompletedTask,
                captured => evidence = captured);

            await action.Should().ThrowAsync<PreviousReleaseMigrationUpgradeRehearsalException>()
                .WithMessage("*phase=restore*");
            evidence.Should().NotBeNull();
            evidence!.Status.Should().Be("failed");
            evidence.Phases.Select(phase => $"{phase.Name}:{phase.Status}").Should().Equal(
                "artifact:passed",
                "historical-schema:passed",
                "restore:failed");
            evidence.ToSanitizedJson().Should().NotContain("synthetic-password");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SyntheticRehearsal_RetainsOnlyPhaseEvidenceWhenArtifactStagingFails()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var plan = PreviousReleaseMigrationUpgradeCommand.VerifyAdoption(repositoryRoot);
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-previous-release-synthetic-{Guid.NewGuid():N}");
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(root);
        try
        {
            PreviousReleaseMigrationUpgradeEvidence? evidence = null;

            var action = () => PreviousReleaseMigrationUpgradeRehearsal.RunAsync(
                plan,
                CreateSyntheticLock(root),
                repositoryRoot,
                staging,
                new FailingSyntheticFetcher(),
                new SyntheticPassingDatabase(plan),
                _ => Task.CompletedTask,
                _ => Task.CompletedTask,
                captured => evidence = captured);

            await action.Should().ThrowAsync<PreviousReleaseMigrationUpgradeRehearsalException>()
                .WithMessage("*phase=artifact*");
            evidence.Should().NotBeNull();
            evidence!.Status.Should().Be("failed");
            evidence.Phases.Select(phase => $"{phase.Name}:{phase.Status}").Should().Equal("artifact:failed");
            evidence.ToSanitizedJson().Should().NotContain("synthetic-password");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SyntheticRehearsal_RecordsDeclaredMigrationsAndExactSanitizedSentinelResults()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var plan = PreviousReleaseMigrationUpgradeCommand.VerifyAdoption(repositoryRoot);
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-previous-release-synthetic-{Guid.NewGuid():N}");
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(root);
        try
        {
            var evidence = await PreviousReleaseMigrationUpgradeRehearsal.RunAsync(
                plan,
                CreateSyntheticLock(root),
                repositoryRoot,
                staging,
                new SyntheticFetcher(root),
                new SyntheticPassingDatabase(plan),
                _ => Task.CompletedTask,
                _ => Task.CompletedTask);

            var json = evidence.ToSanitizedJson();
            evidence.Status.Should().Be("passed");
            evidence.Phases.Should().OnlyContain(phase => phase.Status == "passed");
            json.Should().Contain("\"commit\":\"df07306b5a5ebe08ff205c0d2f6cd5a10af87f2d\"")
                .And.Contain("\"forwardMigrationIds\":[]")
                .And.Contain("\"commit\":\"08b161f4f41c390c8332cd1842e3bdec6c03e322\"")
                .And.Contain("\"forwardMigrationIds\":[\"20260826012918_AddQuranPhraseSearchIndex\"]")
                .And.Contain("\"expectedRows\":6236,\"actualRows\":6236")
                .And.Contain("\"expectedActiveBuild\":\"none\",\"actualRows\":1,\"actualActiveBuildRows\":0")
                .And.Contain("\"applicationBoot\":{\"expected\":\"succeeded\",\"actual\":\"succeeded\"}")
                .And.Contain("\"criticalReadSentinels\":{\"expected\":\"succeeded\",\"actual\":\"succeeded\"}")
                .And.NotContain("synthetic-password");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ArtifactTrustLock CreateSyntheticLock(string root)
    {
        var payloadPath = "synthetic/payload.dump";
        var manifestPath = "synthetic/manifest.json";
        var payload = Path.Combine(root, "payload.dump");
        File.WriteAllText(payload, "synthetic");
        var payloadSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(payload)));
        var migration = new ArtifactMigrationState("20260826012918_AddQuranPhraseSearchIndex", 6);
        var producer = new ArtifactProducer("synthetic", "1");
        var manifest = new TestArtifactManifest(
            1,
            "quran-canonical",
            "synthetic",
            migration,
            new ManifestPostgreSqlState("18.6"),
            producer,
            [new ArtifactManifestTable("quran_ayahs", 6236)],
            [],
            []);
        var manifestSource = Path.Combine(root, "manifest.json");
        File.WriteAllText(manifestSource, JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var manifestSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(manifestSource)));
        return new ArtifactTrustLock(
            "docs/testing/test-artifacts-lock.schema.json",
            1,
            [new LockedArtifact(
                "quran-canonical",
                "synthetic",
                ["scheduled"],
                [new LockedArtifactFile(payloadPath, "payload", new FileInfo(payload).Length, payloadSha), new LockedArtifactFile(manifestPath, "manifest", new FileInfo(manifestSource).Length, manifestSha)],
                manifestPath,
                migration,
                new ArtifactTableScope(true, false, false, false, false, ["quran_ayahs"]),
                new LockedPostgreSqlState("18.6", "sha256:synthetic"),
                producer,
                [],
                [],
                $"local://synthetic@sha256:{payloadSha}",
                new ArtifactRefresh("2026-09-01", "synthetic test", "test"))]);
    }

    private static string RunGit(string root, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, error);
        return output;
    }

    private sealed class SyntheticFetcher(string root) : IFullCanonicalArtifactFetcher
    {
        public Task FetchAsync(LockedArtifact artifact, string stagingRoot, CancellationToken cancellationToken = default)
        {
            foreach (var file in artifact.StagedFiles)
            {
                var source = Path.Combine(root, file.Role == "payload" ? "payload.dump" : "manifest.json");
                var destination = Path.Combine(stagingRoot, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FailingSyntheticFetcher : IFullCanonicalArtifactFetcher
    {
        public Task FetchAsync(LockedArtifact artifact, string stagingRoot, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Password=synthetic-password");
    }

    private sealed class SyntheticDisposableDatabase(PreviousReleaseMigrationUpgradePlan plan)
        : IPreviousReleaseMigrationUpgradeDatabase
    {
        private IReadOnlyList<string> applied = [];

        public Task MigrateToAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            applied = migrationId == plan.SupplementalRehearsalBaseline.Migration!.Head
                ? plan.SupplementalRehearsalBaseline.Migration.Inventory
                : plan.Artifact.Migration!.Inventory;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> AppliedMigrationsAsync(CancellationToken cancellationToken = default) => Task.FromResult(applied);

        public Task RestoreAsync(string payloadPath, IReadOnlyList<string> tables, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Password=synthetic-password");

        public Task<long> CountRowsAsync(string table, CancellationToken cancellationToken = default) => Task.FromResult(0L);

        public Task<PreviousReleasePhraseSearchActual> PhraseSearchStateAsync(PreviousReleasePhraseSearchExpectation expectation, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PreviousReleasePhraseSearchActual(1, 0));
    }

    private sealed class SyntheticPassingDatabase(PreviousReleaseMigrationUpgradePlan plan)
        : IPreviousReleaseMigrationUpgradeDatabase
    {
        private IReadOnlyList<string> applied = [];

        public Task MigrateToAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            applied = migrationId == plan.SupplementalRehearsalBaseline.Migration!.Head
                ? plan.SupplementalRehearsalBaseline.Migration.Inventory
                : plan.Artifact.Migration!.Inventory;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> AppliedMigrationsAsync(CancellationToken cancellationToken = default) => Task.FromResult(applied);

        public Task RestoreAsync(string payloadPath, IReadOnlyList<string> tables, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<long> CountRowsAsync(string table, CancellationToken cancellationToken = default)
        {
            var count = plan.Artifact.TableCounts!.Single(expected => expected.Name == table).Rows;
            return Task.FromResult(count);
        }

        public Task<PreviousReleasePhraseSearchActual> PhraseSearchStateAsync(PreviousReleasePhraseSearchExpectation expectation, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PreviousReleasePhraseSearchActual(1, 0));
    }

}
