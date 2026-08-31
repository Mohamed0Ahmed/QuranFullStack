using QuranDashboard.TestArtifacts;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using System.Text;
using System.Text.Json.Nodes;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

[Collection(nameof(FullCanonicalArtifactProvisioningCollection))]
public sealed class FullCanonicalArtifactProvisioningTests(
    FullCanonicalArtifactProvisioningFixture fixture)
{
    [Fact]
    public async Task Provision_ScheduledArtifactFetchesAndRestoresOnce_ThenExecutionOnlyVerifiesSharedState()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var receipt = await FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await FullCanonicalArtifactProvisioner.VerifyProvisionedStateAsync(
            receipt,
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            database);

        fetcher.Calls.Should().Be(1);
        database.RestoreCalls.Should().Be(1);
        receipt.Artifacts.Should().ContainSingle()
            .Which.Tables.Should().ContainSingle(table => table.Name == "quran_provision_contract" && table.Rows == 2);
        (await database.CountRowsAsync(["quran_provision_contract"]))["quran_provision_contract"].Should().Be(2);
    }

    [Fact]
    public async Task Provision_LocalContentAddressedArtifactRootRestoresOnce_ThenExecutionOnlyVerifiesSharedState()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var artifactRoot = Path.Combine(repository.Root, ".pr-observation", "local-artifacts");
        try
        {
            repository.CopyToLocalArtifactRoot(artifactRoot);
            var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

            var receipt = await FullCanonicalArtifactProvisioner.ProvisionAsync(
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                new LocalFullCanonicalArtifactFetcher(artifactRoot),
                database);

            await FullCanonicalArtifactProvisioner.VerifyProvisionedStateAsync(
                receipt,
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                database);

            database.RestoreCalls.Should().Be(1);
            receipt.Artifacts.Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(artifactRoot))
            {
                Directory.Delete(artifactRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Provision_MissingLocalArtifactRootFailsClosedWithoutRepositoryFallbackOrRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var missingRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-missing-artifact-{Guid.NewGuid():N}");
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            new LocalFullCanonicalArtifactFetcher(missingRoot),
            database);

        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*QURAN_TEST_ARTIFACT_ROOT*");
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_LocalArtifactOutsideContentAddressedLocationFailsClosedWithoutRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var artifactRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-local-artifact-{Guid.NewGuid():N}");
        try
        {
            repository.CopyToLocalArtifactRoot(artifactRoot, useIncorrectContentAddress: true);
            var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

            var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                new LocalFullCanonicalArtifactFetcher(artifactRoot),
                database);

            await action.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*content-addressed artifact is missing*");
            database.RestoreCalls.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(artifactRoot))
            {
                Directory.Delete(artifactRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Provision_MismatchedFetchedPayloadFailsBeforeRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var fetcher = new SyntheticFetcher(repository, tamperPayload: true);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*artifact trust verification failed*");
        fetcher.Calls.Should().Be(1);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_RestoredTableCountMismatchFailsClosed()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(payloadRows: 1);
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*restored row count mismatch*quran_provision_contract*");
        fetcher.Calls.Should().Be(1);
        database.RestoreCalls.Should().Be(1);
    }

    [Fact]
    public async Task Provision_RestoredCanonicalSentinelMismatchFailsClosed()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(sentinelExpectedCount: 3);
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "release",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*canonical sentinel mismatch*synthetic-quran-sentinel*");
        fetcher.Calls.Should().Be(1);
        database.RestoreCalls.Should().Be(1);
    }

    [Fact]
    public async Task Provision_MissingScheduledArtifactFailsBeforeFetchOrRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(
            requiredLanes: ["critical"],
            includeRestoreContract: false);
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no locked full-canonical artifact*");
        fetcher.Calls.Should().Be(0);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_FullCanonicalArtifactWithNonQuranTableFailsBeforeFetchOrRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(extraTable: "unrelated_table");
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only Quran table scope*");
        fetcher.Calls.Should().Be(0);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_PopulatedTargetFailsAfterArtifactVerificationBeforeRestore()
    {
        await fixture.ResetAsync();
        await fixture.InsertProvisionedRowAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not empty*quran_provision_contract*");
        fetcher.Calls.Should().Be(1);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_FullCanonicalArtifactWithPrLaneFailsBeforeFetchOrRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(
            requiredLanes: ["scheduled", "critical"]);
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*scheduled or release lanes*");
        fetcher.Calls.Should().Be(0);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Provision_OverlappingArtifactsFailBeforeAnyFetchOrRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(duplicateTableOwner: true);
        var fetcher = new SyntheticFetcher(repository);
        var database = new SyntheticCanonicalDatabase(fixture.ConnectionString);

        var action = () => FullCanonicalArtifactProvisioner.ProvisionAsync(
            "scheduled",
            repository.Lock,
            repository.Root,
            repository.StagingRoot,
            fetcher,
            database);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*quran_provision_contract*more than one artifact*");
        fetcher.Calls.Should().Be(0);
        database.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task RecoveryRehearsal_RefusesUnconfirmedCaptureBeforeCreatingBackup()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var capture = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                confirmBackup: false,
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                new SyntheticCanonicalDatabase(fixture.ConnectionString));

            await capture.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*explicit operator intent*");
            File.Exists(backupPath).Should().BeFalse();
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_RefusesExistingAndRepositoryReachedBackupPathsBeforeCreatingBackup()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString);
        var existingPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        var symlink = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-link-{Guid.NewGuid():N}");
        File.WriteAllText(existingPath, "existing");
        try
        {
            var existing = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                true, "scheduled", repository.Lock, repository.Root, repository.StagingRoot, existingPath, source);
            await existing.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*new private file outside the repository worktree*");

            var inside = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                true, "scheduled", repository.Lock, repository.Root, repository.StagingRoot,
                Path.Combine(repository.Root, "new-backup.sql"), source);
            await inside.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*new private file outside the repository worktree*");

            Directory.CreateSymbolicLink(symlink, repository.Root);
            var throughLink = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                true, "scheduled", repository.Lock, repository.Root, repository.StagingRoot,
                Path.Combine(symlink, "new-backup.sql"), source);
            await throughLink.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*new private file outside the repository worktree*");

            source.BackupCalls.Should().Be(0);
        }
        finally
        {
            File.Delete(existingPath);
            if (Directory.Exists(symlink))
            {
                Directory.Delete(symlink);
            }
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_RefusesNonDisposableSourceBeforeCreatingBackup()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString, rejectDisposableSource: true);
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var capture = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                true, "scheduled", repository.Lock, repository.Root, repository.StagingRoot, backupPath, source);

            await capture.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*source is not disposable*");
            source.BackupCalls.Should().Be(0);
            File.Exists(backupPath).Should().BeFalse();
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_RefusesNonDisposableTargetBeforeRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString);
        repository.CopyToStaging(tamperPayload: false);
        await source.RestoreAsync(
            repository.Lock.Artifacts.Single(),
            Path.Combine(repository.StagingRoot, repository.Lock.Artifacts.Single().StagedFiles.Single(file => file.Role == "payload").Path));
        await using var targetLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(RecoveryRehearsal_RefusesNonDisposableTargetBeforeRestore));
        await CreateProvisionContractTableAsync(targetLease.ConnectionString);
        var target = new SyntheticCanonicalDatabase(targetLease.ConnectionString, rejectDisposableTarget: true);
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var backup = await FullCanonicalRecoveryRehearsal.CaptureAsync(
                true, "scheduled", repository.Lock, repository.Root, repository.StagingRoot, backupPath, source);
            var restore = () => FullCanonicalRecoveryRehearsal.RestoreAsync(
                "scheduled", repository.Lock, repository.Root, repository.StagingRoot, backupPath, backup, target);

            await restore.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*target is not disposable*");
            target.RestoreCalls.Should().Be(0);
            (await target.CountRowsAsync(["quran_provision_contract"]))["quran_provision_contract"].Should().Be(0);
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_CapturesExactBackupThenRestoresAndVerifiesIsolatedTarget()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString);
        repository.CopyToStaging(tamperPayload: false);
        await source.RestoreAsync(
            repository.Lock.Artifacts.Single(),
            Path.Combine(repository.StagingRoot, repository.Lock.Artifacts.Single().StagedFiles.Single(file => file.Role == "payload").Path));
        await using var targetLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(RecoveryRehearsal_CapturesExactBackupThenRestoresAndVerifiesIsolatedTarget));
        await CreateProvisionContractTableAsync(targetLease.ConnectionString);
        var target = new SyntheticCanonicalDatabase(targetLease.ConnectionString);
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var backup = await FullCanonicalRecoveryRehearsal.CaptureAsync(
                confirmBackup: true,
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                source);
            var receipt = await FullCanonicalRecoveryRehearsal.RestoreAsync(
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                backup,
                target);

            backup.FileName.Should().Be(Path.GetFileName(backupPath));
            backup.Size.Should().Be(new FileInfo(backupPath).Length);
            backup.Sha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(backupPath))));
            backup.Artifacts.Should().ContainSingle().Which.CriticalReads.Should().ContainSingle();
            receipt.Status.Should().Be("rehearsed");
            receipt.Classification.Should().Be("data-recovery");
            receipt.ApplicationRollback.Should().Be("application-rollback-not-requested");
            target.RestoreCalls.Should().Be(1);
            (await target.CountRowsAsync(["quran_provision_contract"]))["quran_provision_contract"].Should().Be(2);
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_RejectsCorruptedBackupBeforeRestore()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create();
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString);
        repository.CopyToStaging(tamperPayload: false);
        await source.RestoreAsync(
            repository.Lock.Artifacts.Single(),
            Path.Combine(repository.StagingRoot, repository.Lock.Artifacts.Single().StagedFiles.Single(file => file.Role == "payload").Path));
        await using var targetLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(RecoveryRehearsal_RejectsCorruptedBackupBeforeRestore));
        await CreateProvisionContractTableAsync(targetLease.ConnectionString);
        var target = new SyntheticCanonicalDatabase(targetLease.ConnectionString);
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var backup = await FullCanonicalRecoveryRehearsal.CaptureAsync(
                confirmBackup: true,
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                source);
            await File.AppendAllTextAsync(backupPath, "-- corrupted\n");

            var restore = () => FullCanonicalRecoveryRehearsal.RestoreAsync(
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                backup,
                target);

            await restore.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*integrity metadata does not match*before restore*");
            target.RestoreCalls.Should().Be(0);
            (await target.CountRowsAsync(["quran_provision_contract"]))["quran_provision_contract"].Should().Be(0);
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RecoveryRehearsal_RejectsCriticalReadMismatchBeforeCreatingBackup()
    {
        await fixture.ResetAsync();
        using var repository = SyntheticCanonicalArtifactRepository.Create(criticalReadSha256: new string('f', 64));
        var source = new SyntheticCanonicalDatabase(fixture.ConnectionString);
        repository.CopyToStaging(tamperPayload: false);
        await source.RestoreAsync(
            repository.Lock.Artifacts.Single(),
            Path.Combine(repository.StagingRoot, repository.Lock.Artifacts.Single().StagedFiles.Single(file => file.Role == "payload").Path));
        var backupPath = Path.Combine(Path.GetTempPath(), $"quran-dashboard-recovery-{Guid.NewGuid():N}.sql");
        try
        {
            var capture = () => FullCanonicalRecoveryRehearsal.CaptureAsync(
                confirmBackup: true,
                "scheduled",
                repository.Lock,
                repository.Root,
                repository.StagingRoot,
                backupPath,
                source);

            await capture.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*critical-read fingerprint*");
            File.Exists(backupPath).Should().BeFalse();
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public void ProcessDatabase_MalformedConnectionDoesNotExposeItsContents()
    {
        const string secret = "recognizable-fake-secret";
        var path = Path.Combine(Path.GetTempPath(), $"quran-dashboard-connection-{Guid.NewGuid():N}");
        File.WriteAllText(path, $"Host=127.0.0.1;Password={secret};broken");
        try
        {
            var construct = () => new ProcessFullCanonicalArtifactDatabase(path, "synthetic", "scheduled");

            construct.Should().Throw<InvalidDataException>()
                .Which.Message.Should().NotContain(secret);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("connection")]
    [InlineData("receipt")]
    public void CommandParse_RejectsPrivateStateInsideRepository(string location)
    {
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-command-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var outside = Path.Combine(Path.GetTempPath(), $"quran-dashboard-command-outside-{Guid.NewGuid():N}");
            var connection = location == "connection" ? Path.Combine(root, "private.connection") : outside;
            var receipt = location == "receipt" ? Path.Combine(root, "receipt.json") : outside;
            using var error = new StringWriter();

            var request = FullCanonicalArtifactProvisioningCommand.Parse(
                [
                    "verify-full-canonical",
                    "--run", "scheduled",
                    "--database-connection-file", connection,
                    "--database-container", "synthetic",
                    "--staging-root", outside,
                    "--receipt", receipt,
                    "--root", root,
                ],
                error);

            request.Should().BeNull();
            error.ToString().Should().Contain(location == "connection"
                ? "database connection file must stay outside"
                : "receipt must stay outside");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandParse_RequiresAndUsesLocalArtifactRoot()
    {
        var original = Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT");
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-artifact-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            Environment.SetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT", null);
            using var missingError = new StringWriter();
            var missing = FullCanonicalArtifactProvisioningCommand.Parse(
                ProvisioningArguments(root),
                missingError);

            missing.Should().BeNull();
            missingError.ToString().Should().Contain("QURAN_TEST_ARTIFACT_ROOT");

            Environment.SetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT", root);
            using var presentError = new StringWriter();
            var present = FullCanonicalArtifactProvisioningCommand.Parse(
                ProvisioningArguments(root),
                presentError);

            present.Should().NotBeNull();
            present!.ArtifactRoot.Should().Be(Path.GetFullPath(root));
            presentError.ToString().Should().BeEmpty();

            Environment.SetEnvironmentVariable(
                "QURAN_TEST_ARTIFACT_ROOT",
                Path.Combine(root, "repository"));
            using var repositoryError = new StringWriter();
            var repositoryRoot = FullCanonicalArtifactProvisioningCommand.Parse(
                ProvisioningArguments(root),
                repositoryError);

            repositoryRoot.Should().NotBeNull();
            repositoryRoot!.ArtifactRoot.Should().Be(Path.Combine(root, "repository"));
            repositoryError.ToString().Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT", original);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string[] ProvisioningArguments(string outsideRoot) =>
    [
        "provision-full-canonical",
        "--run", "scheduled",
        "--database-connection-file", Path.Combine(outsideRoot, "postgres.connection"),
        "--database-container", "synthetic",
        "--staging-root", Path.Combine(outsideRoot, "staging"),
        "--receipt", Path.Combine(outsideRoot, "receipt.json"),
        "--root", Path.Combine(outsideRoot, "repository"),
    ];

    private static async Task CreateProvisionContractTableAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "CREATE TABLE public.quran_provision_contract (id integer PRIMARY KEY, value text NOT NULL);",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public void CommandParse_RejectsStagingSymlinkIntoRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-command-{Guid.NewGuid():N}");
        var stagingLink = Path.Combine(Path.GetTempPath(), $"quran-dashboard-stage-link-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            Directory.CreateSymbolicLink(stagingLink, root);
            using var error = new StringWriter();

            var request = FullCanonicalArtifactProvisioningCommand.Parse(
                [
                    "verify-full-canonical",
                    "--run", "scheduled",
                    "--database-connection-file", Path.Combine(Path.GetTempPath(), "outside.connection"),
                    "--database-container", "synthetic",
                    "--staging-root", stagingLink,
                    "--receipt", Path.Combine(Path.GetTempPath(), "outside.receipt"),
                    "--root", root,
                ],
                error);

            request.Should().BeNull();
            error.ToString().Should().Contain("staging root must stay outside");
        }
        finally
        {
            if (Directory.Exists(stagingLink))
            {
                Directory.Delete(stagingLink);
            }
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandParse_RejectsConnectionFileBelowSymlinkedRepositoryAncestor()
    {
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-command-{Guid.NewGuid():N}");
        var outsideLink = Path.Combine(Path.GetTempPath(), $"quran-dashboard-outside-link-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "existing"));
        File.WriteAllText(Path.Combine(root, "existing", "private.connection"), "Host=127.0.0.1");
        try
        {
            Directory.CreateSymbolicLink(outsideLink, root);
            using var error = new StringWriter();

            var request = FullCanonicalArtifactProvisioningCommand.Parse(
                [
                    "verify-full-canonical",
                    "--run", "scheduled",
                    "--database-connection-file", Path.Combine(outsideLink, "existing", "private.connection"),
                    "--database-container", "synthetic",
                    "--staging-root", Path.Combine(Path.GetTempPath(), $"quran-dashboard-stage-{Guid.NewGuid():N}"),
                    "--receipt", Path.Combine(Path.GetTempPath(), $"quran-dashboard-receipt-{Guid.NewGuid():N}"),
                    "--root", root,
                ],
                error);

            request.Should().BeNull();
            error.ToString().Should().Contain("database connection file must stay outside");
        }
        finally
        {
            if (Directory.Exists(outsideLink))
            {
                Directory.Delete(outsideLink);
            }
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandParse_RejectsStagingDirectoryBelowSymlinkedRepositoryAncestor()
    {
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-command-{Guid.NewGuid():N}");
        var outsideLink = Path.Combine(Path.GetTempPath(), $"quran-dashboard-outside-link-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "existing", "staging"));
        try
        {
            Directory.CreateSymbolicLink(outsideLink, root);
            using var error = new StringWriter();

            var request = FullCanonicalArtifactProvisioningCommand.Parse(
                [
                    "verify-full-canonical",
                    "--run", "scheduled",
                    "--database-connection-file", Path.Combine(Path.GetTempPath(), $"quran-dashboard-connection-{Guid.NewGuid():N}"),
                    "--database-container", "synthetic",
                    "--staging-root", Path.Combine(outsideLink, "existing", "staging"),
                    "--receipt", Path.Combine(Path.GetTempPath(), $"quran-dashboard-receipt-{Guid.NewGuid():N}"),
                    "--root", root,
                ],
                error);

            request.Should().BeNull();
            error.ToString().Should().Contain("staging root must stay outside");
        }
        finally
        {
            if (Directory.Exists(outsideLink))
            {
                Directory.Delete(outsideLink);
            }
            Directory.Delete(root, recursive: true);
        }
    }
}

public sealed class FullCanonicalArtifactProvisioningFixture : IAsyncLifetime
{
    private PostgreSqlDatabaseLease? database;

    internal string ConnectionString => database?.ConnectionString
        ?? throw new InvalidOperationException("The provisioning database fixture has not been initialized.");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (database is not null)
        {
            await database.DisposeAsync();
            database = null;
        }
    }

    internal async Task ResetAsync()
    {
        if (database is not null)
        {
            await database.DisposeAsync();
        }

        database = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(FullCanonicalArtifactProvisioningFixture));
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "CREATE TABLE public.quran_provision_contract (id integer PRIMARY KEY, value text NOT NULL);",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    internal async Task InsertProvisionedRowAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO public.quran_provision_contract (id, value) VALUES (1, 'existing');",
            connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(FullCanonicalArtifactProvisioningCollection))]
public sealed class FullCanonicalArtifactProvisioningCollection
    : ICollectionFixture<FullCanonicalArtifactProvisioningFixture>;

internal sealed class SyntheticCanonicalArtifactRepository : IDisposable
{
    private const string ArtifactDirectory = "artifacts/full-canonical";
    private const string ManifestRelativePath = $"{ArtifactDirectory}/manifest.json";
    private const string PayloadRelativePath = $"{ArtifactDirectory}/full-canonical.sql";

    private readonly string sourceRoot;

    private SyntheticCanonicalArtifactRepository(string root, string sourceRoot, string stagingRoot, ArtifactTrustLock artifactLock)
    {
        Root = root;
        this.sourceRoot = sourceRoot;
        StagingRoot = stagingRoot;
        Lock = artifactLock;
    }

    internal string Root { get; }

    internal string StagingRoot { get; }

    internal ArtifactTrustLock Lock { get; }

    internal static SyntheticCanonicalArtifactRepository Create(
        int payloadRows = 2,
        long sentinelExpectedCount = 2,
        IReadOnlyList<string>? requiredLanes = null,
        bool includeRestoreContract = true,
        string? extraTable = null,
        bool duplicateTableOwner = false,
        string? criticalReadSha256 = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var root = Path.Combine(Path.GetTempPath(), $"quran-dashboard-full-canonical-lock-{suffix}");
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-full-canonical-source-{suffix}");
        var stagingRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-full-canonical-stage-{suffix}");
        Directory.CreateDirectory(Path.Combine(root, ArtifactDirectory));
        Directory.CreateDirectory(Path.Combine(sourceRoot, ArtifactDirectory));
        Directory.CreateDirectory(stagingRoot);
        CreateMigrationTree(root);

        var payload = string.Join(
            Environment.NewLine,
            Enumerable.Range(1, payloadRows)
                .Select(index => $"INSERT INTO public.quran_provision_contract (id, value) VALUES ({index}, 'row-{index}');"));
        File.WriteAllText(Path.Combine(sourceRoot, PayloadRelativePath), payload);
        var payloadHash = Sha256(Path.Combine(sourceRoot, PayloadRelativePath));
        var manifest = new JsonObject
        {
            ["contractVersion"] = 1,
            ["artifactId"] = "full-canonical",
            ["artifactVersion"] = "synthetic-1",
            ["migration"] = new JsonObject
            {
                ["head"] = "20260826012918_AddQuranPhraseSearchIndex",
                ["count"] = 6,
            },
            ["postgresql"] = new JsonObject { ["producerVersion"] = "16.0" },
            ["producer"] = new JsonObject
            {
                ["command"] = "synthetic-full-canonical-contract",
                ["version"] = "1",
            },
            ["tables"] = new JsonArray
            {
                new JsonObject { ["name"] = "quran_provision_contract", ["rows"] = 2 },
            },
            ["sources"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "synthetic-canonical-source",
                    ["version"] = "1",
                    ["sha256"] = new string('a', 64),
                    ["provenance"] = "Synthetic Testcontainers contract vector.",
                },
            },
            ["sentinels"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "synthetic-quran-sentinel",
                    ["expectedCount"] = sentinelExpectedCount,
                    ["oracleSha256"] = payloadHash,
                },
            },
        };
        if (includeRestoreContract)
        {
            manifest["restore"] = RestoreContract(sentinelExpectedCount, criticalReadSha256);
        }
        File.WriteAllText(Path.Combine(sourceRoot, ManifestRelativePath), manifest.ToJsonString());

        var artifactLock = new JsonObject
        {
            ["$schema"] = ArtifactTrustLock.SchemaPath,
            ["contractVersion"] = 1,
            ["artifacts"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "full-canonical",
                    ["version"] = "synthetic-1",
                    ["requiredLanes"] = new JsonArray((requiredLanes ?? ["scheduled", "release"])
                        .Select(lane => JsonValue.Create(lane)!)
                        .ToArray()),
                    ["stagedFiles"] = new JsonArray(
                    [
                        StagedFile(ManifestRelativePath, "manifest", sourceRoot),
                        StagedFile(PayloadRelativePath, "payload", sourceRoot),
                    ]),
                    ["manifestPath"] = ManifestRelativePath,
                    ["migration"] = new JsonObject
                    {
                        ["head"] = "20260826012918_AddQuranPhraseSearchIndex",
                        ["count"] = 6,
                    },
                    ["tableScope"] = new JsonObject
                    {
                        ["quran"] = true,
                        ["phraseSearch"] = false,
                        ["abwab"] = false,
                        ["access"] = false,
                        ["linking"] = false,
                        ["tables"] = extraTable is not null
                            ? new JsonArray("quran_provision_contract", extraTable)
                            : new JsonArray("quran_provision_contract"),
                    },
                    ["tableCounts"] = new JsonArray
                    {
                        new JsonObject { ["name"] = "quran_provision_contract", ["rows"] = 2 },
                    },
                    ["postgresql"] = new JsonObject
                    {
                        ["producerVersion"] = "16.0",
                        ["containerDigest"] = $"sha256:{new string('b', 64)}",
                    },
                    ["producer"] = new JsonObject
                    {
                        ["command"] = "synthetic-full-canonical-contract",
                        ["version"] = "1",
                    },
                    ["sources"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = "synthetic-canonical-source",
                            ["version"] = "1",
                            ["sha256"] = new string('a', 64),
                            ["provenance"] = "Synthetic Testcontainers contract vector.",
                        },
                    },
                    ["sentinels"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = "synthetic-quran-sentinel",
                            ["expectedCount"] = sentinelExpectedCount,
                            ["oracleSha256"] = payloadHash,
                        },
                    },
                    ["immutableStorageId"] = $"local://full-canonical@sha256:{Sha256(Path.Combine(sourceRoot, PayloadRelativePath))}",
                    ["refresh"] = new JsonObject
                    {
                        ["date"] = "2026-08-31",
                        ["reason"] = "Synthetic contract vector.",
                        ["ownerRole"] = "artifact-maintainer",
                    },
                },
            },
        };
        if (includeRestoreContract)
        {
            artifactLock["artifacts"]!.AsArray()[0]!["restore"] = RestoreContract(sentinelExpectedCount, criticalReadSha256);
        }
        if (duplicateTableOwner)
        {
            var duplicate = artifactLock["artifacts"]!.AsArray()[0]!.DeepClone().AsObject();
            duplicate["id"] = "full-canonical-second";
            duplicate["version"] = "synthetic-2";
            duplicate["immutableStorageId"] = $"local://full-canonical-second@sha256:{Sha256(Path.Combine(sourceRoot, PayloadRelativePath))}";
            duplicate["restore"]!["order"] = 2;
            artifactLock["artifacts"]!.AsArray().Add(duplicate);
        }
        File.WriteAllText(Path.Combine(root, ArtifactTrustLock.FileName), artifactLock.ToJsonString());

        return new SyntheticCanonicalArtifactRepository(
            root,
            sourceRoot,
            stagingRoot,
            ArtifactTrustLock.ReadFrom(Path.Combine(root, ArtifactTrustLock.FileName)));
    }

    internal void CopyToStaging(bool tamperPayload)
    {
        foreach (var file in Lock.Artifacts.Single().StagedFiles)
        {
            var destination = Path.Combine(StagingRoot, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(sourceRoot, file.Path), destination, overwrite: true);
            if (tamperPayload && file.Role == "payload")
            {
                var content = File.ReadAllText(destination);
                File.WriteAllText(destination, content.Replace("row-1", "row-x", StringComparison.Ordinal));
            }
        }
    }

    internal void CopyToLocalArtifactRoot(string artifactRoot, bool useIncorrectContentAddress = false)
    {
        var payload = Lock.Artifacts.Single().StagedFiles.Single(file => file.Role == "payload");
        var contentAddress = useIncorrectContentAddress ? new string('0', 64) : payload.Sha256;
        var contentDirectory = Path.Combine(artifactRoot, "sha256", contentAddress);
        Directory.CreateDirectory(contentDirectory);

        foreach (var file in Lock.Artifacts.Single().StagedFiles)
        {
            File.Copy(
                Path.Combine(sourceRoot, file.Path),
                Path.Combine(contentDirectory, Path.GetFileName(file.Path)));
        }
    }

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
        Directory.Delete(sourceRoot, recursive: true);
        Directory.Delete(StagingRoot, recursive: true);
    }

    private static JsonObject RestoreContract(long sentinelExpectedCount, string? criticalReadSha256 = null)
    {
        return new JsonObject
        {
            ["kind"] = "full-canonical",
            ["order"] = 1,
            ["sentinelTables"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "synthetic-quran-sentinel",
                    ["table"] = "quran_provision_contract",
                    ["expectedCount"] = sentinelExpectedCount,
                    ["criticalReadSha256"] = criticalReadSha256 ?? CriticalReadSha256(),
                },
            },
        };
    }

    private static JsonObject StagedFile(string path, string role, string sourceRoot)
    {
        var fullPath = Path.Combine(sourceRoot, path);
        return new JsonObject
        {
            ["path"] = path,
            ["role"] = role,
            ["size"] = new FileInfo(fullPath).Length,
            ["sha256"] = Sha256(fullPath),
        };
    }

    private static void CreateMigrationTree(string root)
    {
        var directory = Path.Combine(
            root,
            "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations");
        Directory.CreateDirectory(directory);
        foreach (var migration in new[]
                 {
                     "20260813153400_InitialBaseline.cs",
                     "20260814153559_M2DurablePreparedLinkingPreflight.cs",
                     "20260814212547_M3DurableLinkingConfirmationJobs.cs",
                     "20260815175846_AddUserDeviceSessions.cs",
                     "20260817163513_AddAbwabDoorInclusionSynchronization.cs",
                     "20260826012918_AddQuranPhraseSearchIndex.cs",
                 })
        {
            File.WriteAllText(Path.Combine(directory, migration), string.Empty);
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static string CriticalReadSha256()
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("1:row-1,2:row-2")));
    }
}

internal sealed class SyntheticFetcher(
    SyntheticCanonicalArtifactRepository repository,
    bool tamperPayload = false) : IFullCanonicalArtifactFetcher
{
    internal int Calls { get; private set; }

    public Task FetchAsync(
        LockedArtifact artifact,
        string stagingRoot,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        repository.CopyToStaging(tamperPayload);
        return Task.CompletedTask;
    }
}

internal sealed class SyntheticCanonicalDatabase(
    string connectionString,
    bool rejectDisposableSource = false,
    bool rejectDisposableTarget = false) : IFullCanonicalRecoveryDatabase
{
    internal int BackupCalls { get; private set; }

    internal int RestoreCalls { get; private set; }

    public Task AssertPostgreSqlCompatibilityAsync(
        LockedPostgreSqlState expected,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task AssertMigrationAsync(
        ArtifactMigrationState expected,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT count(*)::text || '|' || max(\"MigrationId\") FROM public.\"__EFMigrationsHistory\";",
            connection);
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))!.Split('|');
        result.Should().Equal(expected.Count.ToString(), expected.Head);
    }

    public async Task AssertRestoreTargetIsEmptyAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default)
    {
        var rows = await CountRowsAsync(tables, cancellationToken);
        var populated = rows.FirstOrDefault(entry => entry.Value != 0);
        if (!string.IsNullOrEmpty(populated.Key))
        {
            throw new InvalidOperationException($"The provisioner-owned PostgreSQL target is not empty at '{populated.Key}'.");
        }
    }

    public async Task RestoreAsync(
        LockedArtifact artifact,
        string payloadPath,
        CancellationToken cancellationToken = default)
    {
        await RestorePayloadAsync(payloadPath, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, long>> CountRowsAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var rows = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM public.\"{table}\";", connection);
            rows[table] = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        }

        return rows;
    }

    public async Task CreateBackupAsync(
        IReadOnlyList<string> tables,
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        BackupCalls++;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, value FROM public.quran_provision_contract ORDER BY id;",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add($"INSERT INTO public.quran_provision_contract (id, value) VALUES ({reader.GetInt32(0)}, '{reader.GetString(1)}');");
        }

        await File.WriteAllTextAsync(backupPath, string.Join(Environment.NewLine, rows), cancellationToken);
    }

    public Task RestoreBackupAsync(
        IReadOnlyList<string> tables,
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        return RestorePayloadAsync(backupPath, cancellationToken);
    }

    public Task AssertDisposableRecoveryTargetAsync(CancellationToken cancellationToken = default)
    {
        return rejectDisposableTarget
            ? Task.FromException(new InvalidOperationException("The recovery target is not disposable."))
            : Task.CompletedTask;
    }

    public Task AssertDisposableRecoverySourceAsync(CancellationToken cancellationToken = default)
    {
        return rejectDisposableSource
            ? Task.FromException(new InvalidOperationException("The recovery source is not disposable."))
            : Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadCriticalFingerprintsAsync(
        IReadOnlyList<ArtifactRestoreSentinel> sentinels,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT string_agg(id::text || ':' || value, ',' ORDER BY id) FROM public.quran_provision_contract;",
            connection);
        var value = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        return sentinels.ToDictionary(sentinel => sentinel.Id, _ => fingerprint, StringComparer.Ordinal);
    }

    private async Task RestorePayloadAsync(string payloadPath, CancellationToken cancellationToken)
    {
        RestoreCalls++;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(File.ReadAllText(payloadPath), connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
