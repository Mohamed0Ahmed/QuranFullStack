using DotNet.Testcontainers.Configurations;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.TestArtifacts;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

// This class is selected only by the explicit scheduled/release recovery lane. It never accepts an
// ambient connection string: both database servers are short-lived locked postgres:18 containers.
public sealed class FullCanonicalRecoveryRehearsalTests
{
    [Fact]
    public async Task QuranOnlyBackup_RestoresIntoSeparateDisposablePostgreSql18Target()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var privateRoot = Path.Combine(Path.GetTempPath(), $"quran-dashboard-full-canonical-recovery-{Guid.NewGuid():N}");
        var stagingRoot = Path.Combine(privateRoot, "staging");
        var backupRoot = Path.Combine(privateRoot, "backup");
        var backupPath = Path.Combine(backupRoot, "quran-canonical-recovery.dump");
        var artifactLock = ArtifactTrustLock.ReadFrom(Path.Combine(repositoryRoot, ArtifactTrustLock.FileName));
        var runKind = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_ARTIFACT_EXECUTION");
        var artifactRoot = Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT");
        var confirmBackup = string.Equals(
            Environment.GetEnvironmentVariable("QURAN_DASHBOARD_CONFIRM_FULL_CANONICAL_BACKUP"),
            "yes",
            StringComparison.Ordinal);
        var evidence = FullCanonicalRecoveryEvidence.Start();
        try
        {
            runKind.Should().BeOneOf("scheduled", "release");
            artifactRoot.Should().NotBeNullOrWhiteSpace();
            confirmBackup.Should().BeTrue("the release-only lane must receive explicit backup intent");
            ArtifactTrustLockValidator.Validate(artifactLock).Should().BeNull();
            var artifact = FullCanonicalArtifactProvisioner.SelectArtifacts(runKind!, artifactLock)
                .Should().ContainSingle().Subject;
            var ownedSequences = artifact.TableScope.OwnedSequences ?? [];
            ownedSequences.Should().NotBeEmpty();
            evidence = evidence with
            {
                LockedCriticalReads = artifact.Restore!.SentinelTables
                    .Select(sentinel => new FullCanonicalCriticalRead(sentinel.Id, sentinel.CriticalReadSha256!))
                    .ToArray(),
                LockedOracles = artifact.Sentinels,
            };

            CreatePrivateDirectory(privateRoot);
            Directory.CreateDirectory(stagingRoot);
            CreatePrivateDirectory(backupRoot);
            await new LocalFullCanonicalArtifactFetcher(artifactRoot!).FetchAsync(artifact, stagingRoot);
            ArtifactTrustVerifier.Verify(artifactLock, artifact, repositoryRoot, stagingRoot).State
                .Should().Be(ArtifactTrustState.Present);

            var image = $"postgres@{artifact.PostgreSql.ContainerDigest}";
            await using (var sourceServer = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
                             nameof(FullCanonicalRecoveryRehearsalTests),
                             image,
                             builder => builder
                                 .WithBindMount(stagingRoot, "/artifact", AccessMode.ReadOnly)
                                 .WithBindMount(backupRoot, "/backup", AccessMode.ReadWrite)))
            {
                var source = new ContainerRecoveryDatabase(
                    sourceServer,
                    image,
                    "/artifact",
                    "/backup",
                    ownedSequences);
                await MigrateAsync(sourceServer.ConnectionString);
                await source.RestoreArtifactAsync(artifact);
                evidence = evidence with
                {
                    Source = await source.DescribeAsync("source", artifact.Migration, artifact.PostgreSql),
                };

                var sourceCriticalReads = await source.ReadCriticalFingerprintsAsync(artifact.Restore!.SentinelTables);
                evidence = evidence with
                {
                    SourceCriticalReads = sourceCriticalReads.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray(),
                };
                Console.WriteLine($"full-canonical-recovery source-critical-read={sourceCriticalReads[artifact.Restore.SentinelTables.Single().Id]}");

                var backup = await FullCanonicalRecoveryRehearsal.CaptureAsync(
                    confirmBackup,
                    runKind!,
                    artifactLock,
                    repositoryRoot,
                    stagingRoot,
                    backupPath,
                    source);
                evidence = evidence with { Backup = backup };
            }

            await using (var targetServer = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
                             nameof(FullCanonicalRecoveryRehearsalTests),
                             image,
                             builder => builder.WithBindMount(backupRoot, "/backup", AccessMode.ReadOnly)))
            {
                var target = new ContainerRecoveryDatabase(
                    targetServer,
                    image,
                    artifactMount: null,
                    backupMount: "/backup",
                    ownedSequences);
                await MigrateAsync(targetServer.ConnectionString);
                var targetDescriptor = await target.DescribeAsync("target", artifact.Migration, artifact.PostgreSql);
                evidence.Source!.ServerInstanceId.Should().NotBe(targetDescriptor.ServerInstanceId);
                var receipt = await FullCanonicalRecoveryRehearsal.RestoreAsync(
                    runKind!,
                    artifactLock,
                    repositoryRoot,
                    stagingRoot,
                    backupPath,
                    evidence.Backup!,
                    target);
                evidence = evidence with
                {
                    Target = targetDescriptor,
                    Receipt = receipt,
                    TargetCriticalReads = (await target.ReadCriticalFingerprintsAsync(artifact.Restore!.SentinelTables))
                        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                        .ToArray(),
                    TargetSequences = await target.ReadSequenceStatesAsync(artifact.TableScope.Tables, ownedSequences),
                };
            }

            evidence.Receipt!.Status.Should().Be("rehearsed");
            evidence.Receipt.Classification.Should().Be("data-recovery");
            evidence.Receipt.ApplicationRollback.Should().Be("application-rollback-not-requested");
            evidence.SourceCriticalReads.Should().Equal(evidence.TargetCriticalReads);
            evidence.Backup!.SequenceReconciliations.Select(result => result.Reconciled)
                .Should().Equal(evidence.TargetSequences);
            evidence.Backup.SequenceReconciliations.Should().OnlyContain(result =>
                !result.Reconciled.HighWaterMark.HasValue
                || result.Reconciled.NextValue > result.Reconciled.HighWaterMark.Value);
            evidence.Source!.PostgreSqlVersion.StartsWith("18.", StringComparison.Ordinal).Should().BeTrue();
            evidence.Target!.PostgreSqlVersion.StartsWith("18.", StringComparison.Ordinal).Should().BeTrue();
            evidence = evidence with { Status = "passed", DurationMilliseconds = evidence.Stopwatch.ElapsedMilliseconds };
        }
        catch
        {
            evidence = evidence with { Status = "failed", DurationMilliseconds = evidence.Stopwatch.ElapsedMilliseconds };
            throw;
        }
        finally
        {
            try
            {
                RetainEvidence(evidence);
            }
            finally
            {
                if (Directory.Exists(privateRoot))
                {
                    Directory.Delete(privateRoot, recursive: true);
                }
            }
        }
    }

    private static async Task MigrateAsync(string connectionString)
    {
        await using var context = new QuranDashboardDbContext(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options);
        await context.Database.MigrateAsync();
    }

    private static void CreatePrivateDirectory(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The private recovery backup lane requires Linux filesystem permissions.");
        }

        Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static void RetainEvidence(FullCanonicalRecoveryEvidence evidence)
    {
        var root = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_FULL_CANONICAL_RECOVERY_EVIDENCE_DIR")
            ?? Path.Combine(Path.GetTempPath(), "quran-dashboard-full-canonical-recovery-evidence");
        Directory.CreateDirectory(root);
        var json = evidence.ToSanitizedJson();
        File.WriteAllText(Path.Combine(root, $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-rehearsal-{Guid.NewGuid():N}.json"), json);
        Console.WriteLine($"full-canonical-recovery evidence={json}");
    }

    private sealed class ContainerRecoveryDatabase(
        ExclusivePostgreSqlLease server,
        string expectedImage,
        string? artifactMount,
        string backupMount,
        IReadOnlyList<ArtifactOwnedSequence> lockedOwnedSequences) : IFullCanonicalRecoveryDatabase
    {
        public async Task AssertPostgreSqlCompatibilityAsync(
            LockedPostgreSqlState expected,
            CancellationToken cancellationToken = default)
        {
            server.Image.Should().Be(expectedImage);
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SHOW server_version;", connection);
            Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))!
                .StartsWith("18.", StringComparison.Ordinal).Should().BeTrue();
        }

        public async Task AssertMigrationAsync(
            ArtifactMigrationState expected,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT count(*)::text || '|' || max(\"MigrationId\") FROM public.\"__EFMigrationsHistory\";",
                connection);
            Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))!.Split('|')
                .Should().Equal(expected.Count.ToString(CultureInfo.InvariantCulture), expected.Head);
        }

        public async Task<FullCanonicalRecoveryDatabaseDescriptor> DescribeAsync(
            string role,
            ArtifactMigrationState expectedMigration,
            LockedPostgreSqlState expectedPostgreSql,
            CancellationToken cancellationToken = default)
        {
            server.Image.Should().Be($"postgres@{expectedPostgreSql.ContainerDigest}");
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var versionCommand = new NpgsqlCommand("SHOW server_version;", connection);
            var version = Convert.ToString(await versionCommand.ExecuteScalarAsync(cancellationToken))!;
            version.StartsWith("18.", StringComparison.Ordinal).Should().BeTrue();
            await using var migrationCommand = new NpgsqlCommand(
                "SELECT count(*)::text || '|' || max(\"MigrationId\") FROM public.\"__EFMigrationsHistory\";",
                connection);
            var migration = Convert.ToString(await migrationCommand.ExecuteScalarAsync(cancellationToken))!.Split('|');
            migration.Should().Equal(expectedMigration.Count.ToString(CultureInfo.InvariantCulture), expectedMigration.Head);
            return new FullCanonicalRecoveryDatabaseDescriptor(
                role,
                server.ServerInstanceId.ToString("N"),
                expectedPostgreSql.ContainerDigest,
                version,
                migration[1],
                int.Parse(migration[0], CultureInfo.InvariantCulture));
        }

        public async Task AssertRestoreTargetIsEmptyAsync(
            IReadOnlyList<string> tables,
            CancellationToken cancellationToken = default)
        {
            (await CountRowsAsync(tables, cancellationToken)).Values.Should().OnlyContain(count => count == 0);
        }

        public Task RestoreAsync(LockedArtifact artifact, string payloadPath, CancellationToken cancellationToken = default) =>
            RestoreArtifactAsync(artifact, cancellationToken);

        public Task AssertDisposableRecoverySourceAsync(CancellationToken cancellationToken = default)
        {
            server.Image.Should().Be(expectedImage);
            return Task.CompletedTask;
        }

        public Task AssertDisposableRecoveryTargetAsync(CancellationToken cancellationToken = default)
        {
            server.Image.Should().Be(expectedImage);
            return Task.CompletedTask;
        }

        public async Task RestoreArtifactAsync(LockedArtifact artifact, CancellationToken cancellationToken = default)
        {
            if (artifactMount is null)
            {
                throw new InvalidOperationException("The isolated target has no artifact mount.");
            }

            var payload = artifact.StagedFiles.Single(file => file.Role == "payload");
            await RestoreAsync(
                $"{artifactMount}/{payload.Path}",
                artifact.TableScope.Tables,
                (artifact.TableScope.OwnedSequences ?? []).Select(sequence => sequence.Name).ToArray(),
                cancellationToken);
        }

        public async Task<IReadOnlyList<FullCanonicalSequenceReconciliation>> ReconcileOwnedSequencesAsync(
            IReadOnlyList<string> tables,
            IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
            CancellationToken cancellationToken = default)
        {
            ownedSequences.Should().Equal(lockedOwnedSequences);
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return await ReconcileOwnedSequencesInTransactionAsync(
                connection,
                tables,
                ownedSequences,
                cancellationToken);
        }

        public async Task<IReadOnlyList<FullCanonicalSequenceState>> ReadSequenceStatesAsync(
            IReadOnlyList<string> tables,
            IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
            CancellationToken cancellationToken = default)
        {
            ownedSequences.Should().Equal(lockedOwnedSequences);
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return await ReadOwnedSequenceStatesAsync(
                connection,
                transaction: null,
                tables,
                ownedSequences,
                cancellationToken);
        }

        public async Task CreateBackupAsync(
            IReadOnlyList<string> tables,
            IReadOnlyList<string> sequences,
            string backupPath,
            CancellationToken cancellationToken = default)
        {
            sequences.Should().Equal(lockedOwnedSequences.Select(sequence => sequence.Name));
            var relativePath = PrivateRelativePath(backupPath);
            var command = new List<string>
            {
                "pg_dump", "--format=custom", "--data-only", "--no-owner", "--no-privileges",
                "--username", "postgres", "--dbname", "postgres", "--schema", "public",
                "--file", $"{backupMount}/{relativePath}",
            };
            command.AddRange(tables.Select(table => $"--table={table}"));
            command.AddRange(sequences.Select(sequence => $"--table={sequence}"));
            (await server.ExecAsync(command, cancellationToken)).ExitCode.Should().Be(0);
            AssertPrivateBackupFile(backupPath);
            await AssertBackupScopeAsync(tables, sequences, relativePath, cancellationToken);
        }

        public Task RestoreBackupAsync(
            IReadOnlyList<string> tables,
            IReadOnlyList<string> sequences,
            string backupPath,
            CancellationToken cancellationToken = default) =>
            RestoreAsync($"{backupMount}/{PrivateRelativePath(backupPath)}", tables, sequences, cancellationToken);

        public async Task<IReadOnlyDictionary<string, long>> CountRowsAsync(
            IReadOnlyList<string> tables,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            var counts = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                ArtifactTrustLockValidator.IsValidTableIdentifier(table).Should().BeTrue();
                await using var command = new NpgsqlCommand($"SELECT count(*) FROM public.\"{table}\";", connection);
                counts[table] = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }
            return counts;
        }

        public async Task<IReadOnlyDictionary<string, string>> ReadCriticalFingerprintsAsync(
            IReadOnlyList<ArtifactRestoreSentinel> sentinels,
            CancellationToken cancellationToken = default)
        {
            if (sentinels.Any(sentinel => sentinel.Id != "quran-canonical.ayahs-count" || sentinel.Table != "quran_ayahs"))
            {
                throw new InvalidOperationException("The recovery critical-read procedure is not defined for this sentinel.");
            }

            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT string_agg(id::text || '|' || verse_key || '|' || text_uthmani, E'\\n' ORDER BY id) FROM public.\"quran_ayahs\" WHERE surah_number = 1;",
                connection);
            var serialization = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
            if (string.IsNullOrEmpty(serialization))
            {
                throw new InvalidOperationException("The recovery critical-read query returned no canonical ayahs.");
            }

            var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{serialization}\n")));
            return sentinels.ToDictionary(sentinel => sentinel.Id, _ => fingerprint, StringComparer.Ordinal);
        }

        private async Task RestoreAsync(
            string archivePath,
            IReadOnlyList<string> tables,
            IReadOnlyList<string> sequences,
            CancellationToken cancellationToken)
        {
            var command = new List<string>
            {
                "pg_restore", "--data-only", "--disable-triggers", "--exit-on-error", "--no-owner", "--no-privileges",
                "--username", "postgres", "--dbname", "postgres", "--schema", "public",
            };
            command.AddRange(tables.Select(table => $"--table={table}"));
            command.AddRange(sequences.Select(sequence => $"--table={sequence}"));
            command.Add(archivePath);
            (await server.ExecAsync(command, cancellationToken)).ExitCode.Should().Be(0);
        }

        private async Task AssertBackupScopeAsync(
            IReadOnlyList<string> tables,
            IReadOnlyList<string> sequences,
            string relativePath,
            CancellationToken cancellationToken)
        {
            var result = await server.ExecAsync(["pg_restore", "--list", $"{backupMount}/{relativePath}"], cancellationToken);
            result.ExitCode.Should().Be(0);
            AssertArchiveTableOfContentsScope(tables, sequences, result.Stdout);
        }

        private static void AssertPrivateBackupFile(string backupPath)
        {
            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException("The private recovery backup lane requires Linux filesystem permissions.");
            }

            File.GetUnixFileMode(backupPath).Should().NotHaveFlag(
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
        }

        private static string PrivateRelativePath(string backupPath)
        {
            var relativePath = Path.GetRelativePath(Path.GetDirectoryName(backupPath)!, backupPath);
            if (relativePath != Path.GetFileName(backupPath))
            {
                throw new InvalidOperationException("The recovery backup is outside its private mount.");
            }
            return relativePath;
        }
    }

    internal static async Task<IReadOnlyList<FullCanonicalSequenceReconciliation>> ReconcileOwnedSequencesInTransactionAsync(
        NpgsqlConnection connection,
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
        CancellationToken cancellationToken = default)
    {
        ValidateSequenceScopeIdentifiers(tables, ownedSequences);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var table in tables.Order(StringComparer.Ordinal))
        {
            await using var tableLock = new NpgsqlCommand(
                $"LOCK TABLE public.\"{table}\" IN ACCESS EXCLUSIVE MODE;",
                connection,
                transaction);
            await tableLock.ExecuteNonQueryAsync(cancellationToken);
        }

        await AssertExactOwnedSequenceScopeAsync(connection, transaction, tables, ownedSequences, cancellationToken);
        var results = new List<FullCanonicalSequenceReconciliation>();
        foreach (var ownership in ownedSequences)
        {
            var original = await ReadSequenceStateAsync(connection, transaction, ownership, cancellationToken);
            if (original.IncrementBy <= 0)
            {
                throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' must have a positive increment.");
            }

            long restartWith;
            try
            {
                restartWith = original.HighWaterMark is long highWaterMark
                    ? checked(highWaterMark + original.IncrementBy)
                    : await ReadSequenceStartAsync(connection, transaction, ownership.Name, cancellationToken);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' safe restart value overflows bigint.", exception);
            }

            // ALTER SEQUENCE RESTART is transactional in PostgreSQL; unlike setval, it rolls back if a
            // later ownership or bounds check fails before this transaction commits.
            await using (var reconcile = new NpgsqlCommand(
                $"ALTER SEQUENCE public.\"{ownership.Name}\" RESTART WITH {restartWith.ToString(CultureInfo.InvariantCulture)};",
                connection,
                transaction))
            {
                await reconcile.ExecuteNonQueryAsync(cancellationToken);
            }

            var reconciled = await ReadSequenceStateAsync(connection, transaction, ownership, cancellationToken);
            if (reconciled.HighWaterMark != original.HighWaterMark
                || reconciled.IncrementBy <= 0
                || reconciled.HighWaterMark is long reconciledHighWaterMark
                    && reconciled.NextValue <= reconciledHighWaterMark)
            {
                throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' could not be reconciled safely.");
            }
            results.Add(new FullCanonicalSequenceReconciliation(original, reconciled));
        }

        await transaction.CommitAsync(cancellationToken);
        return results;
    }

    internal static async Task<IReadOnlyList<FullCanonicalSequenceState>> ReadOwnedSequenceStatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> ownedSequences,
        CancellationToken cancellationToken = default)
    {
        ValidateSequenceScopeIdentifiers(tables, ownedSequences);
        await AssertExactOwnedSequenceScopeAsync(connection, transaction, tables, ownedSequences, cancellationToken);
        var states = new List<FullCanonicalSequenceState>();
        foreach (var ownership in ownedSequences)
        {
            states.Add(await ReadSequenceStateAsync(connection, transaction, ownership, cancellationToken));
        }
        return states;
    }

    private static void ValidateSequenceScopeIdentifiers(
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> ownedSequences)
    {
        if (tables.Count == 0
            || tables.Any(table => !ArtifactTrustLockValidator.IsValidTableIdentifier(table))
            || ownedSequences.Any(ownership =>
                !ArtifactTrustLockValidator.IsValidTableIdentifier(ownership.Name)
                || !ArtifactTrustLockValidator.IsValidTableIdentifier(ownership.Table)
                || !ArtifactTrustLockValidator.IsValidTableIdentifier(ownership.Column)))
        {
            throw new InvalidOperationException("The recovery sequence ownership scope contains an unsafe identifier.");
        }
    }

    private static async Task AssertExactOwnedSequenceScopeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        IReadOnlyList<string> tables,
        IReadOnlyList<ArtifactOwnedSequence> expected,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sequence_schema.nspname, sequence.relname, owned_table.relname, owned_column.attname
            FROM pg_catalog.pg_class AS sequence
            JOIN pg_catalog.pg_namespace AS sequence_schema ON sequence_schema.oid = sequence.relnamespace
            JOIN pg_catalog.pg_depend AS dependency
              ON dependency.objid = sequence.oid
             AND dependency.deptype IN ('a', 'i')
            JOIN pg_catalog.pg_class AS owned_table ON owned_table.oid = dependency.refobjid
            JOIN pg_catalog.pg_namespace AS table_schema ON table_schema.oid = owned_table.relnamespace
            JOIN pg_catalog.pg_attribute AS owned_column
              ON owned_column.attrelid = owned_table.oid
             AND owned_column.attnum = dependency.refobjsubid
            WHERE sequence.relkind = 'S'
              AND table_schema.nspname = 'public'
              AND owned_table.relname = ANY(@tables)
            ORDER BY sequence_schema.nspname, sequence.relname, owned_table.relname, owned_column.attname;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tables", tables.ToArray());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var actual = new List<ArtifactOwnedSequence>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!string.Equals(reader.GetString(0), "public", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A scoped recovery table owns a sequence outside the locked public schema.");
            }
            actual.Add(new ArtifactOwnedSequence(reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        static IOrderedEnumerable<ArtifactOwnedSequence> Ordered(IEnumerable<ArtifactOwnedSequence> sequences) =>
            sequences.OrderBy(sequence => sequence.Name, StringComparer.Ordinal)
                .ThenBy(sequence => sequence.Table, StringComparer.Ordinal)
                .ThenBy(sequence => sequence.Column, StringComparer.Ordinal);

        if (!Ordered(actual).SequenceEqual(Ordered(expected)))
        {
            throw new InvalidOperationException("The database owned-sequence set does not exactly match the locked scoped-table contract.");
        }
    }

    private static async Task<FullCanonicalSequenceState> ReadSequenceStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        ArtifactOwnedSequence ownership,
        CancellationToken cancellationToken)
    {
        long lastValue;
        bool isCalled;
        await using (var state = new NpgsqlCommand(
            $"SELECT last_value, is_called FROM public.\"{ownership.Name}\";",
            connection,
            transaction))
        await using (var reader = await state.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' did not produce a state row.");
            }
            lastValue = reader.GetInt64(0);
            isCalled = reader.GetBoolean(1);
            if (await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' produced more than one state row.");
            }
        }

        long? highWaterMark;
        await using (var maximum = new NpgsqlCommand(
            $"SELECT max(\"{ownership.Column}\") FROM public.\"{ownership.Table}\";",
            connection,
            transaction))
        {
            var value = await maximum.ExecuteScalarAsync(cancellationToken);
            highWaterMark = value is null or DBNull
                ? null
                : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        var incrementBy = await ReadSequenceMetadataValueAsync(
            connection,
            transaction,
            ownership.Name,
            "seqincrement",
            cancellationToken);
        long nextValue;
        try
        {
            nextValue = isCalled ? checked(lastValue + incrementBy) : lastValue;
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException($"The recovery sequence '{ownership.Name}' next value overflows bigint.", exception);
        }

        return new FullCanonicalSequenceState(
            ownership,
            highWaterMark,
            lastValue,
            isCalled,
            incrementBy,
            nextValue);
    }

    private static Task<long> ReadSequenceStartAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sequence,
        CancellationToken cancellationToken) =>
        ReadSequenceMetadataValueAsync(connection, transaction, sequence, "seqstart", cancellationToken);

    private static async Task<long> ReadSequenceMetadataValueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sequence,
        string column,
        CancellationToken cancellationToken)
    {
        if (column is not "seqstart" and not "seqincrement")
        {
            throw new InvalidOperationException("The recovery sequence metadata column is not allowlisted.");
        }
        await using var command = new NpgsqlCommand(
            $"SELECT {column} FROM pg_catalog.pg_sequence WHERE seqrelid = CAST(@sequence AS regclass);",
            connection,
            transaction);
        command.Parameters.AddWithValue("sequence", $"public.\"{sequence}\"");
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    internal static void AssertArchiveTableOfContentsScope(
        IReadOnlyList<string> tables,
        IReadOnlyList<string> ownedSequences,
        string toc)
    {
        var actualTables = new List<string>();
        var actualSequences = new List<string>();
        foreach (var line in toc.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith(';'))
            {
                continue;
            }

            var separator = line.IndexOf(';');
            if (separator < 0)
            {
                throw new InvalidOperationException("The recovery backup TOC entry is unreadable.");
            }

            var parts = line[(separator + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                throw new InvalidOperationException("The recovery backup TOC entry is unreadable.");
            }

            var kind = parts.Length > 3 && (parts[2], parts[3]) is ("TABLE", "DATA") or ("SEQUENCE", "SET") or ("BLOB", "DATA")
                ? $"{parts[2]} {parts[3]}"
                : parts[2];
            var nameOffset = kind.Contains(' ') ? 4 : 3;
            if (kind == "TABLE DATA")
            {
                if (parts.Length <= nameOffset + 1 || parts[nameOffset] != "public")
                {
                    throw new InvalidOperationException("The recovery backup contains an out-of-scope table data entry.");
                }
                actualTables.Add(parts[nameOffset + 1]);
                continue;
            }

            if (kind == "SEQUENCE SET")
            {
                if (parts.Length <= nameOffset + 1
                    || parts[nameOffset] != "public"
                    || !ownedSequences.Contains(parts[nameOffset + 1], StringComparer.Ordinal))
                {
                    throw new InvalidOperationException("The recovery backup contains an unapproved sequence value entry.");
                }
                actualSequences.Add(parts[nameOffset + 1]);
                continue;
            }

            if (kind is "BLOB" or "BLOB DATA")
            {
                throw new InvalidOperationException("The recovery backup contains an unapproved blob data entry.");
            }

            if (kind is not "ACL" and not "COMMENT" and not "CONSTRAINT" and not "DEFAULT" and not "EXTENSION"
                and not "FUNCTION" and not "INDEX" and not "RULE" and not "SCHEMA" and not "SEQUENCE"
                and not "TABLE" and not "TRIGGER" and not "TYPE" and not "VIEW")
            {
                throw new InvalidOperationException("The recovery backup contains an unapproved data-bearing TOC entry.");
            }
        }

        actualTables.Order(StringComparer.Ordinal).Should().Equal(tables.Order(StringComparer.Ordinal));
        if (!actualSequences.Order(StringComparer.Ordinal).SequenceEqual(ownedSequences.Order(StringComparer.Ordinal)))
        {
            throw new InvalidOperationException("The recovery backup sequence value entries do not exactly match the locked scope.");
        }
    }

    private sealed record FullCanonicalRecoveryEvidence(
        string Status,
        string Classification,
        string ApplicationRollback,
        IReadOnlyList<FullCanonicalCriticalRead> LockedCriticalReads,
        IReadOnlyList<ArtifactSentinel> LockedOracles,
        FullCanonicalRecoveryBackup? Backup,
        FullCanonicalRecoveryReceipt? Receipt,
        FullCanonicalRecoveryDatabaseDescriptor? Source,
        FullCanonicalRecoveryDatabaseDescriptor? Target,
        IReadOnlyList<KeyValuePair<string, string>> SourceCriticalReads,
        IReadOnlyList<KeyValuePair<string, string>> TargetCriticalReads,
        IReadOnlyList<FullCanonicalSequenceState> TargetSequences,
        long DurationMilliseconds,
        Stopwatch Stopwatch)
    {
        internal static FullCanonicalRecoveryEvidence Start() =>
            new(
                "running",
                "data-recovery",
                "application-rollback-not-requested",
                [],
                [],
                null,
                null,
                null,
                null,
                [],
                [],
                [],
                0,
                Stopwatch.StartNew());

        internal string ToSanitizedJson() => JsonSerializer.Serialize(
            new
            {
                Status,
                Classification,
                ApplicationRollback,
                LockedCriticalReads,
                LockedOracles,
                Backup = Backup is null
                    ? null
                    : new
                    {
                        Backup.FileName,
                        Backup.Size,
                        Backup.Sha256,
                        Backup.RepositoryMigration,
                        Backup.Tables,
                        Backup.OwnedSequences,
                        Backup.SequenceReconciliations,
                        Artifacts = Backup.Artifacts.Select(artifact => new
                        {
                            artifact.Id,
                            artifact.ImmutableStorageId,
                            artifact.Tables,
                            artifact.Sentinels,
                            artifact.StagedFiles,
                            Sources = artifact.Sources.Select(source => new
                            {
                                source.Id,
                                source.Version,
                                source.Sha256,
                                source.Provenance,
                            }),
                            artifact.CriticalReads,
                            artifact.Sequences,
                        }),
                    },
                Receipt = Receipt is null
                    ? null
                    : new { Receipt.Status, Receipt.Classification, Receipt.ApplicationRollback },
                Source,
                Target,
                SourceCriticalReads,
                TargetCriticalReads,
                TargetSequences,
                DurationMilliseconds,
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed record FullCanonicalRecoveryDatabaseDescriptor(
        string Role,
        string ServerInstanceId,
        string ImageDigest,
        string PostgreSqlVersion,
        string MigrationHead,
        int MigrationCount);
}
