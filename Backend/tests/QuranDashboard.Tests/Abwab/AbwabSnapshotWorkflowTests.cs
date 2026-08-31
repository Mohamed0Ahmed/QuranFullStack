using System.Diagnostics;
using System.Text;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;
using QuranDashboard.DataImporter.Import.AbwabSnapshotImport;
using QuranDashboard.Tests.TestSupport.Http;
using QuranDashboard.Tests.TestSupport.Process;

namespace QuranDashboard.Tests.Abwab;

[Collection(nameof(AbwabSnapshotTestCollection))]
public sealed class AbwabSnapshotWorkflowTests(AbwabSnapshotTestFixture fixture)
{
    private static readonly IReadOnlyDictionary<string, int> ExpectedSourceCounts = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["abwab_sections"] = 1,
        ["abwab_doors"] = 3,
        ["abwab_door_aliases"] = 2,
        ["abwab_door_relations"] = 1,
        ["abwab_templates"] = 1,
        ["abwab_template_nodes"] = 3,
        ["abwab_door_inclusions"] = 1,
        ["abwab_door_inclusion_unit_syncs"] = 1,
    };

    private string? latestImportReportDirectory;

    [Fact]
    public async Task ExportAndImportSnapshot_ThroughCli_RestoresExactAuthoredRowsAndPublicReads()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();

        var sourceCounts = await fixture.ReadSourceCountsAsync();
        var sourceRows = await fixture.ReadSourceRowsAsync();
        sourceCounts.Should().BeEquivalentTo(ExpectedSourceCounts);
        var snapshotPath = await ExportSnapshotAsync();
        var sourcePackage = await new AbwabSnapshotSourceReader().LoadAsync(snapshotPath, CancellationToken.None);
        await AssertExportArtifactsAsync(snapshotPath, sourcePackage.Snapshot);

        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (string.Equals(table, AbwabSnapshotContract.ExcludedDerivedRowsTable, StringComparison.Ordinal))
            {
                sourceRows[table].Should().HaveCount(1);
                continue;
            }

            sourcePackage.Snapshot.Counts[table].Total.Should().Be(sourceCounts[table]);
            sourcePackage.Snapshot.Tables[table].Select(row => CompactJson(row.GetRawText())).Should().Equal(
                sourceRows[table].Select(CompactJson));
        }
        sourcePackage.Snapshot.Tables["abwab_door_aliases"].Select(row => row.GetProperty("id").GetInt32())
            .Should().Equal(501, 502);
        sourcePackage.Snapshot.Tables["abwab_template_nodes"].Select(row => row.GetProperty("id").GetInt32())
            .Should().Equal(401, 402, 403);

        var import = await ImportSnapshotAsync(snapshotPath);
        import.ExitCode.Should().Be(0);
        import.StandardOutput.Should().Contain("verdict=pass");
        import.StandardOutput.Should().Contain("persisted=true");

        var targetCounts = await fixture.ReadTargetCountsAsync();
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            var expectedCount = string.Equals(table, AbwabSnapshotContract.ExcludedDerivedRowsTable, StringComparison.Ordinal)
                ? 0
                : sourceCounts[table];
            targetCounts[table].Should().Be(expectedCount);
        }

        var targetRows = await fixture.ReadTargetRowsAsync();
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (string.Equals(table, AbwabSnapshotContract.ExcludedDerivedRowsTable, StringComparison.Ordinal))
            {
                targetRows[table].Should().BeEmpty();
                continue;
            }

            targetRows[table].Select(CompactJson).Should().Equal(sourceRows[table].Select(CompactJson));
        }

        await AssertPublicReadsAsync();
        await AssertImportReportAsync(sourcePackage);
    }

    [Fact]
    public async Task ImportSnapshot_WithChangedBytes_RefusesBeforeTargetMutation()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        await File.AppendAllTextAsync(snapshotPath, " ");

        var import = await ImportSnapshotAsync(snapshotPath);

        import.ExitCode.Should().Be(1);
        import.StandardError.Should().Contain("bytes do not match");
        (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
        await AssertImportReportAsync(expectedPersisted: "false", expectedVerdict: "fail");
    }

    [Fact]
    public async Task ImportSnapshot_WithLegacyFormatVersion_RefusesBeforeTargetMutation()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        await RewriteFormatVersionAndChecksumAsync(snapshotPath, 3);

        var import = await ImportSnapshotAsync(snapshotPath);

        import.ExitCode.Should().Be(1);
        import.StandardError.Should().Contain("format version 4");
        (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
        await AssertImportReportAsync(expectedPersisted: "false", expectedVerdict: "fail");
    }

    [Fact]
    public async Task ImportSnapshot_WithNonEmptyTarget_RefusesWithoutOverwritingAuthoredRows()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        await fixture.SeedTargetSentinelAsync();

        var import = await ImportSnapshotAsync(snapshotPath);

        import.ExitCode.Should().Be(1);
        import.StandardError.Should().Contain("all eight target tables to be empty");
        var targetCounts = await fixture.ReadTargetCountsAsync();
        targetCounts["abwab_sections"].Should().Be(1);
        targetCounts.Where(pair => pair.Key != "abwab_sections").Should().OnlyContain(pair => pair.Value == 0);
        await AssertImportReportAsync(expectedPersisted: "false", expectedVerdict: "fail");
    }

    [Fact]
    public async Task ImportSnapshot_WithTargetSchemaDrift_RefusesBeforeTargetMutation()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        await fixture.AddTargetSchemaDriftAsync();
        try
        {
            var import = await ImportSnapshotAsync(snapshotPath);

            import.ExitCode.Should().Be(1);
            import.StandardError.Should().Contain("schema does not exactly match");
            (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
            await AssertImportReportAsync(expectedPersisted: "false", expectedVerdict: "fail");
        }
        finally
        {
            await fixture.RemoveTargetSchemaDriftAsync();
        }
    }

    [Fact]
    public async Task ImportSnapshot_WithTargetBehindMigrationHead_RefusesBeforeTargetMutation()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        var migrationHead = await fixture.RemoveTargetMigrationHeadAsync();
        try
        {
            var import = await ImportSnapshotAsync(snapshotPath);

            import.ExitCode.Should().Be(1);
            import.StandardError.Should().Contain("not the compiled current head");
            (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
            await AssertImportReportAsync(expectedPersisted: "false", expectedVerdict: "fail");
        }
        finally
        {
            await fixture.RestoreTargetMigrationHeadAsync(migrationHead);
        }
    }

    [Fact]
    public async Task ImportSnapshot_WithUnconfirmedRemoteTarget_RefusesBeforeDatabaseImport()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        var package = await new AbwabSnapshotSourceReader().LoadAsync(snapshotPath, CancellationToken.None);
        var reportDirectory = fixture.CreateTemporaryDirectory("remote-import-report");
        latestImportReportDirectory = reportDirectory;

        var import = await RunImporterAsync(
            "Host=198.51.100.1;Port=1;Database=snapshot-test;Username=snapshot;Password=not-a-secret",
            "import-abwab-snapshot",
            "--source",
            snapshotPath,
            "--report-out",
            reportDirectory);

        import.ExitCode.Should().Be(1);
        import.StandardError.Should().Contain("Remote Abwab import is refused unless --allow-remote and --yes are supplied together.");
        import.StandardOutput.Should().NotContain("transaction=serializable/access-exclusive-fenced");
        (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
        await AssertImportReportAsync(package, expectedPersisted: "false", expectedVerdict: "fail", expectedTargetOpened: false);
    }

    [Theory]
    [InlineData("--allow-remote")]
    [InlineData("--yes")]
    public async Task ImportSnapshot_WithOnlyOneRemoteConfirmationFlag_IsRejected(string suppliedFlag)
    {
        var sourcePath = Path.Combine(fixture.CreateTemporaryDirectory("invalid-remote-confirmation"), "snapshot.json");
        var reportDirectory = fixture.CreateTemporaryDirectory("invalid-remote-confirmation-report");

        var import = await RunImporterAsync(
            "Host=198.51.100.1;Port=1;Database=snapshot-test;Username=snapshot;Password=not-a-secret",
            "import-abwab-snapshot",
            "--source",
            sourcePath,
            "--report-out",
            reportDirectory,
            suppliedFlag);

        import.ExitCode.Should().Be(1);
        import.StandardError.Should().Contain("--allow-remote and --yes must be supplied together.");
        Directory.EnumerateFileSystemEntries(reportDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WhenSourceBytesChangeBeforeCommit_RollsBackAllRows()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        var sourceReader = new AbwabSnapshotSourceReader();
        var package = await sourceReader.LoadAsync(snapshotPath, CancellationToken.None);
        await File.AppendAllTextAsync(snapshotPath, " ");
        var importer = new AbwabSnapshotImporter();
        var compiledMigrationHead = await fixture.ReadTargetMigrationHeadAsync();

        var act = async () => await importer.ImportAsync(
            fixture.TargetConnectionString,
            package,
            compiledMigrationHead,
            token => sourceReader.SourceUnchangedAsync(package, token),
            CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSnapshotImportException>()
            .WithMessage("*changed during import*");
        (await fixture.ReadTargetCountsAsync()).Values.Should().OnlyContain(count => count == 0);
    }

    [Fact]
    public async Task CommitReconciler_OnlyReportsKnownPersistenceAfterFreshDatabaseProof()
    {
        await fixture.ResetAsync();
        await fixture.SeedSyntheticAuthoredStateAsync();
        var snapshotPath = await ExportSnapshotAsync();
        var package = await new AbwabSnapshotSourceReader().LoadAsync(snapshotPath, CancellationToken.None);
        var compiledMigrationHead = await fixture.ReadTargetMigrationHeadAsync();
        var importer = new AbwabSnapshotImporter();
        var initialImport = await importer.ImportAsync(
            fixture.TargetConnectionString,
            package,
            compiledMigrationHead,
            _ => Task.FromResult(true),
            CancellationToken.None);
        initialImport.Persisted.Should().Be(AbwabSnapshotImportContract.PersistedTrue);

        var reconciler = new AbwabSnapshotCommitReconciler();
        var exact = await reconciler.ReconcileAsync(
            fixture.TargetConnectionString,
            package.Snapshot,
            compiledMigrationHead,
            new IOException("synthetic acknowledgement loss"));
        exact.Persisted.Should().Be(AbwabSnapshotImportContract.PersistedTrue);
        exact.Checks.Should().Contain("commit-ack-failure-reconciled-exact");

        await fixture.ResetAsync();
        var empty = await reconciler.ReconcileAsync(
            fixture.TargetConnectionString,
            package.Snapshot,
            compiledMigrationHead,
            new IOException("synthetic acknowledgement loss"));
        empty.Persisted.Should().Be(AbwabSnapshotImportContract.PersistedFalse);
        empty.Checks.Should().Contain("commit-reconciled-target-empty");

        await fixture.SeedTargetSentinelAsync();
        var mixed = await reconciler.ReconcileAsync(
            fixture.TargetConnectionString,
            package.Snapshot,
            compiledMigrationHead,
            new IOException("synthetic acknowledgement loss"));
        mixed.Persisted.Should().Be(AbwabSnapshotImportContract.PersistedUnknown);
        mixed.Checks.Should().Contain("commit-outcome-unknown");
    }

    private async Task<string> ExportSnapshotAsync()
    {
        var outputDirectory = fixture.CreateTemporaryDirectory("export");
        var export = await RunImporterAsync(
            fixture.SourceConnectionString,
            "export-abwab-snapshot",
            "--output-dir",
            outputDirectory);

        export.ExitCode.Should().Be(0);
        export.StandardOutput.Should().Contain("verdict=pass");
        return Directory.EnumerateFiles(outputDirectory, "abwab-snapshot-*.json")
            .Single(path => !path.EndsWith("-report.json", StringComparison.Ordinal));
    }

    private async Task<ProcessRunResult> ImportSnapshotAsync(string snapshotPath)
    {
        var reportDirectory = fixture.CreateTemporaryDirectory("import-report");
        latestImportReportDirectory = reportDirectory;
        return await RunImporterAsync(
            fixture.TargetConnectionString,
            "import-abwab-snapshot",
            "--source",
            snapshotPath,
            "--report-out",
            reportDirectory);
    }

    private static async Task<ProcessRunResult> RunImporterAsync(
        string connectionString,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(LocateImporterAssembly())
        };
        startInfo.ArgumentList.Add(LocateImporterAssembly());
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["ConnectionStrings__QuranDashboardDb"] = connectionString;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        var run = await ProcessExecution.RunAsync(startInfo);
        run.TimedOut.Should().BeFalse();
        return run;
    }

    private async Task AssertExportArtifactsAsync(string snapshotPath, AbwabSnapshotDocument snapshot)
    {
        snapshot.Format.Should().Be("quran-dashboard-abwab-snapshot");
        snapshot.FormatVersion.Should().Be(4);
        snapshot.Source.TransactionReadOnly.Should().BeTrue();
        snapshot.Scope.AbwabTables.Should().BeEquivalentTo(AbwabSnapshotContract.Tables, options => options.WithStrictOrdering());
        snapshot.Scope.LinkingRowsIncluded.Should().BeFalse();
        snapshot.Scope.LinkingSummaryIncluded.Should().BeFalse();
        snapshot.Tables["abwab_door_inclusion_unit_syncs"].Should().BeEmpty();
        snapshot.Counts["abwab_door_inclusion_unit_syncs"].Total.Should().Be(0);
        snapshot.Scope.SourceExcludedRowCounts["abwab_door_inclusion_unit_syncs"].Should().Be(1);
        snapshot.Source.Database.Should().NotBeNullOrWhiteSpace();
        snapshot.Source.ServerVersion.Should().NotBeNullOrWhiteSpace();
        snapshot.Source.MigrationHead.Should().NotBeNullOrWhiteSpace();

        var snapshotBytes = await File.ReadAllBytesAsync(snapshotPath);
        var checksumText = await File.ReadAllTextAsync($"{snapshotPath}.sha256");
        checksumText.Should().Be(
            $"{Convert.ToHexStringLower(SHA256.HashData(snapshotBytes))}  {Path.GetFileName(snapshotPath)}{Environment.NewLine}");

        var reportPath = snapshotPath.Replace(".json", "-report.json", StringComparison.Ordinal);
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        report.RootElement.GetProperty("verdict").GetString().Should().Be("pass");
        report.RootElement.GetProperty("persisted").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("snapshotPath").GetString().Should().Be(snapshotPath);
        report.RootElement.GetProperty("formatVersion").GetInt32().Should().Be(4);
        report.RootElement.GetProperty("snapshotSha256").GetString()
            .Should().Be(Convert.ToHexStringLower(SHA256.HashData(snapshotBytes)));
        report.RootElement.GetProperty("sourceExcludedRowCounts")
            .GetProperty("abwab_door_inclusion_unit_syncs").GetInt64().Should().Be(1);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            report.RootElement.GetProperty("counts").GetProperty(table).GetProperty("total").GetInt32()
                .Should().Be(snapshot.Counts[table].Total);
        }
        report.RootElement.GetProperty("checks").EnumerateArray().Select(check => check.GetString())
            .Should().Contain("transaction-repeatable-read-read-only");
        var markdownPath = snapshotPath.Replace(".json", "-report.md", StringComparison.Ordinal);
        var markdown = await File.ReadAllTextAsync(markdownPath);
        markdown.Should().Contain(Path.GetFileName(snapshotPath));
        markdown.Should().Contain(Convert.ToHexStringLower(SHA256.HashData(snapshotBytes)));
        markdown.Should().Contain("`abwab_door_inclusion_unit_syncs`: 1");
    }

    private async Task AssertImportReportAsync(
        AbwabSnapshotSourcePackage? package = null,
        string expectedPersisted = "true",
        string expectedVerdict = "pass",
        bool expectedTargetOpened = true)
    {
        var reportDirectory = latestImportReportDirectory
            ?? throw new InvalidOperationException("No import report directory was recorded.");
        var reportPath = Path.Combine(
            Directory.EnumerateDirectories(reportDirectory, "abwab-snapshot-import-*").Single(),
            "report.json");
        var reportJson = await File.ReadAllTextAsync(reportPath);
        using var report = JsonDocument.Parse(reportJson);
        report.RootElement.GetProperty("operation").GetString().Should().Be("import");
        report.RootElement.GetProperty("verdict").GetString().Should().Be(expectedVerdict);
        report.RootElement.GetProperty("persisted").GetString().Should().Be(expectedPersisted);
        var markdownPath = Path.ChangeExtension(reportPath, ".md");
        var markdown = await File.ReadAllTextAsync(markdownPath);
        markdown.Should().Contain("# Abwab Snapshot Import Report");
        markdown.Should().Contain($"- Persisted: `{expectedPersisted}`");

        if (package is null)
        {
            return;
        }

        report.RootElement.GetProperty("sourcePath").GetString().Should().Be(package.SourcePath);
        report.RootElement.GetProperty("sourceSha256").GetString().Should().Be(package.Sha256);
        report.RootElement.GetProperty("format").GetString().Should().Be(package.Snapshot.Format);
        report.RootElement.GetProperty("formatVersion").GetInt32().Should().Be(package.Snapshot.FormatVersion);
        report.RootElement.GetProperty("sourceMigrationHead").GetString().Should().Be(package.Snapshot.Source.MigrationHead);
        report.RootElement.GetProperty("sourceExcludedRowCounts")
            .GetProperty("abwab_door_inclusion_unit_syncs").GetInt64()
            .Should().Be(package.Snapshot.Scope.SourceExcludedRowCounts["abwab_door_inclusion_unit_syncs"]);
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            report.RootElement.GetProperty("counts").GetProperty(table).GetProperty("total").GetInt32()
                .Should().Be(package.Snapshot.Counts[table].Total);
        }

        if (expectedTargetOpened)
        {
            report.RootElement.GetProperty("targetMigrationHead").GetString().Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            report.RootElement.GetProperty("targetMigrationHead").ValueKind.Should().Be(JsonValueKind.Null);
            reportJson.Should().NotContain("not-a-secret");
        }

        markdown.Should().Contain(package.SourcePath);
        markdown.Should().Contain(package.Sha256);
    }

    private async Task AssertPublicReadsAsync()
    {
        using var client = fixture.CreateApiClient();

        using var treeResponse = await client.GetAsync("/api/abwab/tree");
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tree = await ApiEnvelope.ReadDataAsync(treeResponse);
        tree.GetProperty("version").GetDateTimeOffset().Should().Be(new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero));
        var section = tree.GetProperty("sections").EnumerateArray().Single();
        section.GetProperty("id").GetInt32().Should().Be(101);
        section.GetProperty("doorsInScopeCount").GetInt32().Should().Be(2);
        section.GetProperty("version").GetUInt32().Should().BeGreaterThan(0);
        var doors = tree.GetProperty("doors").EnumerateArray().ToDictionary(door => door.GetProperty("id").GetInt32());
        doors.Keys.Should().BeEquivalentTo([201, 202, 203]);
        doors[201].GetProperty("aliases").EnumerateArray().Select(alias => alias.GetString())
            .Should().Equal("synthetic-root-alias");
        doors[201].GetProperty("relationCount").GetInt32().Should().Be(1);
        doors[201].GetProperty("inclusionSourceCount").GetInt32().Should().Be(1);
        doors[201].GetProperty("linkCount").GetInt32().Should().Be(0);
        doors[202].GetProperty("inclusionConsumerCount").GetInt32().Should().Be(1);
        doors[203].GetProperty("isArchived").GetBoolean().Should().BeTrue();

        using var templatesResponse = await client.GetAsync("/api/abwab/templates/301");
        templatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await ApiEnvelope.ReadDataAsync(templatesResponse);
        template.GetProperty("name").GetString().Should().Be("Synthetic template root");
        template.GetProperty("nodes").EnumerateArray().Select(node => node.GetProperty("id").GetInt32())
            .Should().BeEquivalentTo([401, 402], options => options.WithStrictOrdering());

        using var relationsResponse = await client.GetAsync("/api/abwab/doors/201/relations");
        relationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var relation = (await ApiEnvelope.ReadDataAsync(relationsResponse)).EnumerateArray().Single();
        relation.GetProperty("id").GetInt32().Should().Be(601);
        relation.GetProperty("otherDoorId").GetInt32().Should().Be(202);
        relation.GetProperty("type").GetInt32().Should().Be(1);

        using var inclusionsResponse = await client.GetAsync("/api/abwab/doors/201/inclusions");
        inclusionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inclusion = (await ApiEnvelope.ReadDataAsync(inclusionsResponse)).GetProperty("sources").EnumerateArray().Single();
        inclusion.GetProperty("inclusionId").GetInt32().Should().Be(701);
        inclusion.GetProperty("doorId").GetInt32().Should().Be(202);
    }

    private static async Task RewriteFormatVersionAndChecksumAsync(string snapshotPath, int formatVersion)
    {
        var snapshot = JsonSerializer.Deserialize<AbwabSnapshotDocument>(
                await File.ReadAllBytesAsync(snapshotPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The generated snapshot is empty.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            snapshot with { FormatVersion = formatVersion },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        bytes = bytes.Concat([(byte)'\n']).ToArray();
        await File.WriteAllBytesAsync(snapshotPath, bytes);
        await File.WriteAllTextAsync(
            $"{snapshotPath}.sha256",
            $"{Convert.ToHexStringLower(SHA256.HashData(bytes))}  {Path.GetFileName(snapshotPath)}{Environment.NewLine}",
            new UTF8Encoding(false));
    }

    private static string CompactJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static string LocateImporterAssembly()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var assembly = Path.Combine(
                directory.FullName,
                "tools",
                "QuranDashboard.DataImporter",
                "bin",
                "Debug",
                "net10.0",
                "QuranDashboard.DataImporter.dll");
            if (File.Exists(assembly))
            {
                return assembly;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("The built QuranDashboard.DataImporter assembly was not found above the test output.");
    }
}
