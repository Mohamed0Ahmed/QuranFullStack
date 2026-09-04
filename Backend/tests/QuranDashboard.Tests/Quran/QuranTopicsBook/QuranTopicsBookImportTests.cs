using System.Text.Json.Nodes;
using System.Text;
using QuranDashboard.DataImporter.Import.QuranTopicsBook;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.Http;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Quran.QuranTopicsBook;

[Collection(nameof(QuranTopicsBookImportTestCollection))]
public sealed class QuranTopicsBookImportTests(QuranTopicsBookImportTestFixture fixture)
{
    private static readonly string[] ExpectedTargetTables =
    [
        "abwab_sections",
        "abwab_doors",
        "abwab_door_aliases",
        "abwab_door_relations",
        "abwab_door_inclusions",
        "abwab_door_inclusion_unit_syncs",
        "abwab_templates",
        "abwab_template_nodes",
        "linking_confirmation_jobs",
        "linking_operations",
        "linking_prepared_affected_contributions",
        "linking_prepared_ayah_descriptions",
        "linking_prepared_ayah_words",
        "linking_prepared_ayahs",
        "linking_prepared_units",
        "linking_prepared_sources",
        "linking_prepared_preflights",
        "linking_source_contribution_units",
        "linking_source_contributions",
        "linking_unit_ayah_descriptions",
        "linking_unit_ayah_words",
        "linking_unit_ayahs",
        "linking_units",
        "linking_door_ayah_words",
        "linking_door_ayahs",
    ];

    private static readonly IReadOnlyDictionary<string, int> ExpectedImportedTargetCounts =
        ExpectedTargetTables.ToDictionary(
            table => table,
            table => table switch
            {
                "abwab_sections" => 1,
                "abwab_doors" => 2,
                "linking_operations" => 2,
                "linking_source_contribution_units" => 2,
                "linking_source_contributions" => 2,
                "linking_unit_ayahs" => 3,
                "linking_units" => 2,
                "linking_door_ayahs" => 3,
                _ => 0,
            },
            StringComparer.Ordinal);

    [Fact]
    public void TargetScope_IsTheLiteralTwentyFiveTableAbwabToLinkingClosure()
    {
        QuranTopicsBookContract.EmptyTargetTables.Should().Equal(ExpectedTargetTables);
    }

    [Fact]
    public async Task SourceReader_RejectsAChangedChecksumSidecarAfterValidation()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        var reader = new QuranTopicsBookSourceReader();
        var loaded = await reader.LoadAsync(package.SourcePath, CancellationToken.None);

        await QuranTopicsBookSyntheticPackageWriter.WriteChecksumSidecarAsync(
            package.SourcePath,
            new string('0', 64));

        (await reader.SourceUnchangedAsync(loaded, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Command_RejectsAnInvalidChecksumBeforeAnyTargetMutation()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        await QuranTopicsBookSyntheticPackageWriter.WriteChecksumSidecarAsync(
            package.SourcePath,
            new string('0', 64));

        var run = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId: 1);

        run.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("persisted").GetString().Should().Be("false");
        report.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().ContainSingle(error => error!.Contains("SHA-256", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SourceReader_RejectsAChecksumSidecarBoundToAnotherSourceFile()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        var sourceSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(package.SourcePath)));
        await QuranTopicsBookSyntheticPackageWriter.WriteChecksumSidecarAsync(
            package.SourcePath,
            sourceSha256,
            "another-quran-topics-book.json");
        var reader = new QuranTopicsBookSourceReader();

        var load = () => reader.LoadAsync(package.SourcePath, CancellationToken.None);

        var failure = await load.Should().ThrowAsync<QuranTopicsBookImportException>();
        failure.Which.Message.Should().Contain("checksum sidecar has an invalid contract");
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
    }

    [Fact]
    public async Task SourceReader_RejectsANonUtf8ChecksumSidecar()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        await File.WriteAllBytesAsync(package.SourcePath + ".sha256", [0xff]);
        var reader = new QuranTopicsBookSourceReader();

        var load = () => reader.LoadAsync(package.SourcePath, CancellationToken.None);

        var failure = await load.Should().ThrowAsync<QuranTopicsBookImportException>();
        failure.Which.Message.Should().Contain("checksum sidecar is not valid UTF-8");
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
    }

    [Theory]
    [InlineData("format", "format must be")]
    [InlineData("provenance", "source provenance")]
    [InlineData("policy", "import policy")]
    [InlineData("hierarchy", "missing or cross-section parent")]
    [InlineData("door-identity", "door keys must be unique")]
    [InlineData("verse-key", "invalid verseKey")]
    [InlineData("repeated-verse", "repeats verseKey")]
    public async Task SourceReader_RejectsInvalidFormatV1ContractsBeforeAnyTargetMutation(
        string invalidContract,
        string expectedError)
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(
            database.TempDirectory,
            document => ApplyInvalidContract(document, invalidContract));
        var reader = new QuranTopicsBookSourceReader();

        var load = () => reader.LoadAsync(package.SourcePath, CancellationToken.None);

        var failure = await load.Should().ThrowAsync<QuranTopicsBookImportException>();
        failure.Which.Message.Should().Contain(expectedError);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
    }

    [Fact]
    public async Task Command_RejectsAnInactiveOrNonOwnerActorBeforeAnyTargetMutation()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);

        var run = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId: 1);

        run.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().ContainSingle(error => error!.Contains("active Owner", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Command_RejectsAMissingCanonicalVerseWithoutPartialTopicsOrProjections()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var actorUserId = await fixture.CreateActiveOwnerAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(
            database.TempDirectory,
            document => RootDoor(document)["ayahGroups"]!.AsArray()[0]!.AsObject()["verseKeys"] =
                new JsonArray("114:6"));

        var run = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId);

        run.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().ContainSingle(error => error!.Contains("missing 1 requested verse keys", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Command_RefusesANonEmptyTargetAndValidateOnlyLeavesItUntouched()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var actorUserId = await fixture.CreateActiveOwnerAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        await using (var db = fixture.CreateDbContext(database))
        {
            db.AbwabSections.Add(new AbwabSection
            {
                Name = "Existing section",
                OrderValue = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = actorUserId,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedBy = actorUserId,
            });
            await db.SaveChangesAsync();
        }

        var before = await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables);
        var importRun = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId);
        var validateOnlyRun = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId, validateOnly: true);

        importRun.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables)).Should().Equal(before);
        using (var importReport = await ReadReportAsync(importRun.ReportDirectory))
        {
            importReport.RootElement.GetProperty("errors").EnumerateArray()
                .Select(error => error.GetString())
                .Should().ContainSingle(error => error!.Contains("target tables to be empty", StringComparison.Ordinal));
        }

        validateOnlyRun.ExitCode.Should().Be(0);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables)).Should().Equal(before);
        using var validateOnlyReport = await ReadReportAsync(validateOnlyRun.ReportDirectory);
        validateOnlyReport.RootElement.GetProperty("verdict").GetString().Should().Be("pass");
        validateOnlyReport.RootElement.GetProperty("persisted").GetString().Should().Be("false");
        validateOnlyReport.RootElement.GetProperty("warnings").EnumerateArray()
            .Select(warning => warning.GetString())
            .Should().ContainSingle(warning => warning!.StartsWith("TARGET-NOT-EMPTY:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Command_RejectsATargetBehindTheCompiledMigrationHeadBeforeMutation()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        await using (var connection = new NpgsqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                DELETE FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = (
                    SELECT "MigrationId"
                    FROM "__EFMigrationsHistory"
                    ORDER BY "MigrationId" DESC
                    LIMIT 1)
                """,
                connection);
            (await command.ExecuteNonQueryAsync()).Should().Be(1);
        }

        var run = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId: 1);

        run.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().ContainSingle(error => error!.Contains("migration head", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_RollsBackAllTopicsAndProjectionsWhenTheChecksumChangesBeforeCommit()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var actorUserId = await fixture.CreateActiveOwnerAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);
        var reader = new QuranTopicsBookSourceReader();
        var loaded = await reader.LoadAsync(package.SourcePath, CancellationToken.None);
        await using var db = fixture.CreateDbContext(database);
        var importer = new QuranTopicsBookImporter(db);

        var import = () => importer.ImportAsync(
            loaded,
            actorUserId,
            validateOnly: false,
            async cancellationToken =>
            {
                await QuranTopicsBookSyntheticPackageWriter.WriteChecksumSidecarAsync(
                    package.SourcePath,
                    new string('0', 64));
                return await reader.SourceUnchangedAsync(loaded, cancellationToken);
            },
            CancellationToken.None);

        var failure = await import.Should().ThrowAsync<QuranTopicsBookImportException>();
        failure.Which.Message.Should().Contain("source or checksum changed");
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Values.Should().OnlyContain(count => count == 0);
    }

    [Fact]
    public async Task Command_FailsClosedAndReportsUnknownPersistenceAfterAnAmbiguousCommitAcknowledgement()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var actorUserId = await fixture.CreateActiveOwnerAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);

        var run = await fixture.RunWithAmbiguousCommitAsync(database, package.SourcePath, actorUserId);

        run.ExitCode.Should().Be(1);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Should().Equal(ExpectedImportedTargetCounts);
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("verdict").GetString().Should().Be("fail");
        report.RootElement.GetProperty("persisted").GetString().Should().Be("unknown");
        report.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().ContainSingle(error => error!.Contains("commit acknowledgement was ambiguous", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Command_ImportsExactHierarchyManualContributionsAndFullAyahProjections()
    {
        await using var database = await fixture.LeaseDatabaseAsync();
        await fixture.SeedCanonicalMushafSliceAsync(database);
        var actorUserId = await fixture.CreateActiveOwnerAsync(database);
        var package = await QuranTopicsBookSyntheticPackageWriter.WriteAsync(database.TempDirectory);

        var run = await fixture.RunCommandAsync(database, package.SourcePath, actorUserId);

        run.ExitCode.Should().Be(0);
        (await fixture.ReadTargetCountsAsync(database, ExpectedTargetTables))
            .Should().Equal(ExpectedImportedTargetCounts);
        var doorIds = await AssertPersistedStateAsync(database, actorUserId);
        await AssertAuditEvidenceAsync(run, package.SourcePath, actorUserId);
        await AssertPublicProjectionsAsync(database, doorIds.RootDoorId, doorIds.ChildDoorId);
    }

    private async Task<(int RootDoorId, int ChildDoorId)> AssertPersistedStateAsync(
        QuranTopicsBookTestDatabase database,
        int actorUserId)
    {
        await using var db = fixture.CreateDbContext(database);
        var sections = await db.AbwabSections.AsNoTracking()
            .OrderBy(section => section.OrderValue)
            .Select(section => new { section.Id, section.Name, section.OrderValue, section.CreatedBy, section.UpdatedBy })
            .ToListAsync();
        sections.Should().ContainSingle();
        sections[0].Name.Should().Be("Synthetic section");
        sections[0].OrderValue.Should().Be(1);
        sections[0].CreatedBy.Should().Be(actorUserId);
        sections[0].UpdatedBy.Should().Be(actorUserId);

        var doors = await db.AbwabDoors.AsNoTracking()
            .OrderBy(door => door.Id)
            .Select(door => new
            {
                door.Id,
                door.SectionId,
                door.ParentId,
                door.Name,
                door.OrderValue,
                door.GlobalOrderValue,
                door.CreatedBy,
                door.UpdatedBy,
            })
            .ToListAsync();
        doors.Should().HaveCount(2);
        var root = doors.Single(door => door.Name == "Synthetic root");
        var child = doors.Single(door => door.Name == "Synthetic child");
        root.SectionId.Should().Be(sections[0].Id);
        root.ParentId.Should().BeNull();
        root.OrderValue.Should().Be(1);
        root.GlobalOrderValue.Should().Be(1);
        root.CreatedBy.Should().Be(actorUserId);
        root.UpdatedBy.Should().Be(actorUserId);
        child.SectionId.Should().Be(sections[0].Id);
        child.ParentId.Should().Be(root.Id);
        child.OrderValue.Should().Be(1);
        child.GlobalOrderValue.Should().BeNull();
        child.CreatedBy.Should().Be(actorUserId);
        child.UpdatedBy.Should().Be(actorUserId);

        var contributions = await db.LinkingSourceContributions.AsNoTracking()
            .OrderBy(contribution => contribution.DoorId)
            .Select(contribution => new
            {
                contribution.DoorId,
                contribution.OrderValue,
                contribution.ContributionMode,
                contribution.SourceKind,
                contribution.Label,
                contribution.ScopeJson,
                contribution.ResolvedAyahCount,
                contribution.CreatedBy,
                contribution.UpdatedBy,
            })
            .ToListAsync();
        contributions.Should().HaveCount(2);
        contributions.Should().OnlyContain(contribution =>
            contribution.SourceKind == LinkingSourceKind.ManualMushafAyahs
            && contribution.OrderValue == 1
            && contribution.CreatedBy == actorUserId
            && contribution.UpdatedBy == actorUserId);
        var rootContribution = contributions.Single(contribution => contribution.DoorId == root.Id);
        rootContribution.OrderValue.Should().Be(1);
        rootContribution.ContributionMode.Should().Be(LinkingContributionMode.ManualSingle);
        rootContribution.Label.Should().Be("Synthetic root");
        rootContribution.ResolvedAyahCount.Should().Be(1);
        using (var scope = JsonDocument.Parse(rootContribution.ScopeJson))
        {
            scope.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
            scope.RootElement.GetProperty("contextKey").GetString().Should().Be(
                "book:synthetic-quran-topics-source.pdf:door:section-01.door-01:reference:1");
        }

        var childContribution = contributions.Single(contribution => contribution.DoorId == child.Id);
        childContribution.OrderValue.Should().Be(1);
        childContribution.ContributionMode.Should().Be(LinkingContributionMode.ManualGrouped);
        childContribution.Label.Should().Be("Synthetic child");
        childContribution.ResolvedAyahCount.Should().Be(2);
        using (var scope = JsonDocument.Parse(childContribution.ScopeJson))
        {
            scope.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
            scope.RootElement.GetProperty("contextKey").GetString().Should().Be(
                "book:synthetic-quran-topics-source.pdf:door:section-01.door-02:reference:1");
        }

        var units = await db.LinkingUnits.AsNoTracking()
            .OrderBy(unit => unit.DoorId)
            .Select(unit => new { unit.Id, unit.DoorId, unit.IsGrouped, unit.CreatedBy })
            .ToListAsync();
        units.Should().HaveCount(2);
        units.Single(unit => unit.DoorId == root.Id).Should().BeEquivalentTo(new
        {
            Id = units.Single(unit => unit.DoorId == root.Id).Id,
            DoorId = root.Id,
            IsGrouped = false,
            CreatedBy = actorUserId,
        });
        units.Single(unit => unit.DoorId == child.Id).IsGrouped.Should().BeTrue();
        (await db.LinkingSourceContributionUnits.CountAsync()).Should().Be(2);
        (await db.LinkingUnitAyahs.CountAsync()).Should().Be(3);
        (await db.LinkingUnitAyahWords.CountAsync()).Should().Be(0);
        (await db.LinkingDoorAyahWords.CountAsync()).Should().Be(0);

        var unitVerseKeys = await (
                from unitAyah in db.LinkingUnitAyahs.AsNoTracking()
                join unit in db.LinkingUnits.AsNoTracking() on unitAyah.UnitId equals unit.Id
                join ayah in db.QuranAyahs.AsNoTracking() on unitAyah.AyahId equals ayah.Id
                orderby unit.DoorId, unitAyah.OrderValue
                select new { unit.DoorId, unitAyah.OrderValue, ayah.VerseKey })
            .ToListAsync();
        unitVerseKeys.Should().Equal(
            new { DoorId = root.Id, OrderValue = 1, VerseKey = "1:1" },
            new { DoorId = child.Id, OrderValue = 1, VerseKey = "1:2" },
            new { DoorId = child.Id, OrderValue = 2, VerseKey = "1:3" });

        var contributionUnitIdentities = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                join unit in db.LinkingUnits.AsNoTracking() on mapping.UnitId equals unit.Id
                orderby contribution.DoorId
                select new
                {
                    contribution.DoorId,
                    contribution.SourceIdentity,
                    contribution.SourceIdentityHash,
                    unit.Identity,
                    unit.IdentityHash,
                })
            .ToListAsync();
        contributionUnitIdentities.Should().HaveCount(2);
        contributionUnitIdentities.Select(identity => new
            {
                identity.DoorId,
                identity.SourceIdentity,
                SourceIdentityHash = Convert.ToHexStringLower(identity.SourceIdentityHash),
                identity.Identity,
                IdentityHash = Convert.ToHexStringLower(identity.IdentityHash),
            })
            .Should().Equal(
                new
                {
                    DoorId = root.Id,
                    SourceIdentity = "manual-mushaf-ayahs|context|book%3Asynthetic-quran-topics-source.pdf%3Adoor%3Asection-01.door-01%3Areference%3A1|1%3A1|link-mode:manual_single",
                    SourceIdentityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("manual-mushaf-ayahs|context|book%3Asynthetic-quran-topics-source.pdf%3Adoor%3Asection-01.door-01%3Areference%3A1|1%3A1|link-mode:manual_single"))),
                    Identity = "independent|11",
                    IdentityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("independent|11"))),
                },
                new
                {
                    DoorId = child.Id,
                    SourceIdentity = "manual-mushaf-ayahs|context|book%3Asynthetic-quran-topics-source.pdf%3Adoor%3Asection-01.door-02%3Areference%3A1|1%3A2|1%3A3|link-mode:manual_grouped",
                    SourceIdentityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("manual-mushaf-ayahs|context|book%3Asynthetic-quran-topics-source.pdf%3Adoor%3Asection-01.door-02%3Areference%3A1|1%3A2|1%3A3|link-mode:manual_grouped"))),
                    Identity = "grouped|12|13",
                    IdentityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("grouped|12|13"))),
                });

        var projectionVerseKeys = await (
                from projection in db.LinkingDoorAyahs.AsNoTracking()
                join ayah in db.QuranAyahs.AsNoTracking() on projection.AyahId equals ayah.Id
                orderby projection.DoorId, ayah.SurahNumber, ayah.AyahNumber
                select new { projection.DoorId, ayah.VerseKey, projection.CreatedBy })
            .ToListAsync();
        projectionVerseKeys.Should().Equal(
            new { DoorId = root.Id, VerseKey = "1:1", CreatedBy = actorUserId },
            new { DoorId = child.Id, VerseKey = "1:2", CreatedBy = actorUserId },
            new { DoorId = child.Id, VerseKey = "1:3", CreatedBy = actorUserId });

        var operations = await db.LinkingOperations.AsNoTracking()
            .OrderBy(operation => operation.DoorId)
            .Select(operation => new
            {
                operation.DoorId,
                operation.ActorUserId,
                operation.SourceCount,
                operation.AyahCount,
                operation.OutcomeJson,
            })
            .ToListAsync();
        operations.Should().HaveCount(2);
        operations.Should().OnlyContain(operation => operation.ActorUserId == actorUserId && operation.SourceCount == 1);
        operations.Single(operation => operation.DoorId == root.Id).AyahCount.Should().Be(1);
        operations.Single(operation => operation.DoorId == child.Id).AyahCount.Should().Be(2);
        foreach (var operation in operations)
        {
            using var outcome = JsonDocument.Parse(operation.OutcomeJson);
            outcome.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
            outcome.RootElement.GetProperty("kind").GetString().Should().Be("quran_topics_book_import");
        }

        return (root.Id, child.Id);
    }

    private static async Task AssertAuditEvidenceAsync(
        QuranTopicsBookCommandRun run,
        string sourcePath,
        int actorUserId)
    {
        var expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(sourcePath)));
        using var report = await ReadReportAsync(run.ReportDirectory);
        report.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        report.RootElement.GetProperty("sourcePath").GetString().Should().Be(sourcePath);
        report.RootElement.GetProperty("sourceSha256").GetString().Should().Be(expectedSha256);
        report.RootElement.GetProperty("actorUserId").GetInt32().Should().Be(actorUserId);
        report.RootElement.GetProperty("validateOnly").GetBoolean().Should().BeFalse();
        report.RootElement.GetProperty("verdict").GetString().Should().Be("pass");
        report.RootElement.GetProperty("persisted").GetString().Should().Be("true");
        report.RootElement.GetProperty("metrics").GetProperty("sectionCount").GetInt32().Should().Be(1);
        report.RootElement.GetProperty("metrics").GetProperty("doorCount").GetInt32().Should().Be(2);
        report.RootElement.GetProperty("metrics").GetProperty("ayahGroupCount").GetInt32().Should().Be(2);
        report.RootElement.GetProperty("metrics").GetProperty("groupedRangeCount").GetInt32().Should().Be(1);
        report.RootElement.GetProperty("metrics").GetProperty("ayahReferenceCount").GetInt32().Should().Be(3);
        report.RootElement.GetProperty("checks").EnumerateArray()
            .Select(check => check.GetString())
            .Should().Contain(["source-provenance", "all-verse-keys-resolved", "target-tables-empty", "persisted-counts-exact", "source-unchanged-before-commit"]);
        report.RootElement.GetProperty("warnings").GetArrayLength().Should().Be(0);
        report.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
    }

    private static async Task AssertPublicProjectionsAsync(
        QuranTopicsBookTestDatabase database,
        int rootDoorId,
        int childDoorId)
    {
        await using var factory = SmokeApiHost.Build(
            database.ConnectionString,
            new FakeExternalUserProfileSource(),
            new TestSqlCommandCapture());
        using var client = SmokeApiHost.CreateClient(factory);

        using var rootResponse = await client.GetAsync($"/api/abwab/doors/{rootDoorId}/links/snapshot");
        rootResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rootSnapshot = await ApiEnvelope.ReadDataAsync(rootResponse);
        rootSnapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        rootSnapshot.GetProperty("records")[0].GetProperty("isGrouped").GetBoolean().Should().BeFalse();
        rootSnapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");

        using var childResponse = await client.GetAsync($"/api/abwab/doors/{childDoorId}/links/snapshot");
        childResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var childSnapshot = await ApiEnvelope.ReadDataAsync(childResponse);
        childSnapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        childSnapshot.GetProperty("records")[0].GetProperty("isGrouped").GetBoolean().Should().BeTrue();
        childSnapshot.GetProperty("records")[0].GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("selectedWordIds").GetArrayLength())
            .Should().Equal(0, 0);
        childSnapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:2", "1:3");

        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:3/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("verseKey").GetString().Should().Be("1:3");
        projection.GetProperty("doorIds").EnumerateArray()
            .Select(doorId => doorId.GetInt32())
            .Should().Equal(childDoorId);
    }

    private static async Task<JsonDocument> ReadReportAsync(string reportDirectory)
    {
        var path = Directory.EnumerateFiles(reportDirectory, "quran-topics-book-import-*.json")
            .Should().ContainSingle().Subject;
        return JsonDocument.Parse(await File.ReadAllTextAsync(path));
    }

    private static void ApplyInvalidContract(JsonObject document, string invalidContract)
    {
        switch (invalidContract)
        {
            case "format":
                document["format"] = "unsupported-quran-topics-book-format";
                return;
            case "provenance":
                document["source"]!.AsObject()["sha256"] = "not-a-sha256";
                return;
            case "policy":
                document["policy"]!.AsObject()["parentAyahPolicy"] = "inherit_descendants";
                return;
            case "hierarchy":
                ChildDoor(document)["parentKey"] = "missing-door";
                return;
            case "door-identity":
                ChildDoor(document)["key"] = RootDoor(document)["key"]!.GetValue<string>();
                return;
            case "verse-key":
                RootDoor(document)["ayahGroups"]!.AsArray()[0]!.AsObject()["verseKeys"] =
                    new JsonArray("not-a-verse-key");
                return;
            case "repeated-verse":
                RootDoor(document)["ayahGroups"]!.AsArray()[0]!.AsObject()["verseKeys"] =
                    new JsonArray("1:1", "1:1");
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidContract), invalidContract, "Unknown contract.");
        }
    }

    private static JsonObject RootDoor(JsonObject document) =>
        document["sections"]!.AsArray()[0]!.AsObject()["doors"]!.AsArray()[0]!.AsObject();

    private static JsonObject ChildDoor(JsonObject document) =>
        document["sections"]!.AsArray()[0]!.AsObject()["doors"]!.AsArray()[1]!.AsObject();
}
