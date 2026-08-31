using QuranDashboard.TestArtifacts;
using QuranDashboard.Tests.TestSupport.PostgreSql;
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
    public async Task Provision_PopulatedTargetFailsBeforeRestore()
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
        fetcher.Calls.Should().Be(0);
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
    private const string OracleRelativePath = $"{ArtifactDirectory}/oracle.json";
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
        bool duplicateTableOwner = false)
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
        File.WriteAllText(Path.Combine(sourceRoot, OracleRelativePath), "synthetic reviewed sentinel oracle\n");

        var oracleHash = Sha256(Path.Combine(sourceRoot, OracleRelativePath));
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
                    ["oracleSha256"] = oracleHash,
                },
            },
        };
        if (includeRestoreContract)
        {
            manifest["restore"] = RestoreContract(sentinelExpectedCount);
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
                        StagedFile(OracleRelativePath, "oracle", sourceRoot),
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
                            ["oracleSha256"] = oracleHash,
                        },
                    },
                    ["immutableStorageId"] = $"test://full-canonical@sha256:{new string('c', 64)}",
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
            artifactLock["artifacts"]!.AsArray()[0]!["restore"] = RestoreContract(sentinelExpectedCount);
        }
        if (duplicateTableOwner)
        {
            var duplicate = artifactLock["artifacts"]!.AsArray()[0]!.DeepClone().AsObject();
            duplicate["id"] = "full-canonical-second";
            duplicate["version"] = "synthetic-2";
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

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
        Directory.Delete(sourceRoot, recursive: true);
        Directory.Delete(StagingRoot, recursive: true);
    }

    private static JsonObject RestoreContract(long sentinelExpectedCount)
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

internal sealed class SyntheticCanonicalDatabase(string connectionString) : IFullCanonicalArtifactDatabase
{
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
        RestoreCalls++;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(File.ReadAllText(payloadPath), connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
}
