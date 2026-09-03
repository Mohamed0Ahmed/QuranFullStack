using FluentAssertions;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeFullRehearsalTests
{
    [Fact]
    public void Validate_AcceptsOnlyAFreshMarkedNonAuthoritativeTargetWithMatchingProtectedState()
    {
        var now = new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedMigration = DatabaseContractValidator.Validate(contract).ExpectedMigrations.Last();
        var snapshot = ValidSnapshot(contract, expectedMigration, now.AddHours(-2));

        var result = FullRehearsalCapability.Validate(
            contract,
            expectedMigration,
            "phrase-search-index-build",
            snapshot,
            now,
            requireExclusiveLock: true);

        result.Succeeded.Should().BeTrue();
        result.Violations.Should().BeEmpty();
        result.Report.Database.Should().Be("quran_rehearsal_phrase_index");
        result.Report.Fresh.Should().BeTrue();
        result.Report.ExclusiveLockOwned.Should().BeTrue();
        result.Report.DumpFilesRetained.Should().Be(0);
    }

    [Theory]
    [InlineData("quran_dashboard", "rehearsal.target.development-database")]
    [InlineData("quran_dashboard_test", "rehearsal.target.test-database")]
    [InlineData("quran_test_scratch_abc", "rehearsal.target.reserved-database")]
    [InlineData("quran_dashboard_test_refresh_abc", "rehearsal.target.reserved-database")]
    public void TargetValidation_RejectsAuthoritativeAndRuntimeOwnedDatabases(string database, string code)
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);

        var result = FullRehearsalTargetValidator.Validate(
            $"Host=localhost;Database={database};Username=operator;Password=do-not-report",
            contract);

        result.IsValid.Should().BeFalse();
        result.Violations.Select(violation => violation.Code).Should().Contain(code);
        result.Violations.Select(violation => violation.Subject).Should().NotContain("do-not-report");
    }

    [Fact]
    public void Validate_ReportsEveryStaleOrMismatchedCapabilityFieldWithManualRefreshGuidance()
    {
        var now = new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedMigration = DatabaseContractValidator.Validate(contract).ExpectedMigrations.Last();
        var snapshot = ValidSnapshot(contract, expectedMigration, now.AddHours(-169)) with
        {
            RehearsalSubtype = "recovery",
            CanonicalPipeline = "unknown-pipeline",
            CanonicalInputProvenance = "not-a-fingerprint",
            ProtectedStateMarker = new string('a', 64),
            ComputedProtectedStateFingerprint = new string('b', 64),
            MarkerMigrationHead = "old-migration",
            DatabaseMigrationHead = "old-migration",
            ExclusiveLockOwned = false,
        };

        var result = FullRehearsalCapability.Validate(
            contract,
            expectedMigration,
            "phrase-search-index-build",
            snapshot,
            now,
            requireExclusiveLock: true);

        result.Succeeded.Should().BeFalse();
        result.Violations.Select(violation => violation.Code).Should().Contain([
            "rehearsal.subtype.mismatch",
            "rehearsal.pipeline.mismatch",
            "rehearsal.provenance.invalid",
            "rehearsal.protected-state.mismatch",
            "rehearsal.migration.not-current",
            "rehearsal.freshness.expired",
            "rehearsal.lock.not-owned",
        ]);
        result.Report.Guidance.Should().ContainSingle()
            .Which.Should().Contain("Manually refresh the Rehearsal Database");
    }

    [Fact]
    public async Task RecoveryPayloadEvidence_RemovesThePayloadOnlyAfterSuccessfulCompletion()
    {
        var directory = Directory.CreateTempSubdirectory("qdb-rehearsal-recovery-");
        var successfulPayload = Path.Combine(directory.FullName, "successful.dump");
        var failedPayload = Path.Combine(directory.FullName, "failed.dump");
        await File.WriteAllTextAsync(successfulPayload, "successful backup");
        await File.WriteAllTextAsync(failedPayload, "failed backup");
        try
        {
            var successful = await FullRehearsalRecoveryPayload.FinalizeAsync(
                successfulPayload,
                new string('c', 64),
                rehearsalSucceeded: true);
            var failed = await FullRehearsalRecoveryPayload.FinalizeAsync(
                failedPayload,
                new string('d', 64),
                rehearsalSucceeded: false);

            successful.PayloadSha256.Should().MatchRegex("^[a-f0-9]{64}$");
            successful.SourceProtectedStateFingerprint.Should().Be(new string('c', 64));
            successful.PayloadRemoved.Should().BeTrue();
            File.Exists(successfulPayload).Should().BeFalse();
            failed.PayloadRemoved.Should().BeFalse();
            File.Exists(failedPayload).Should().BeTrue();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void CleanupAuthorization_RequiresAValidCapabilityExactDisplayedTargetAndExplicitConfirmation()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedMigration = DatabaseContractValidator.Validate(contract).ExpectedMigrations.Last();
        var snapshot = ValidSnapshot(contract, expectedMigration, DateTimeOffset.UtcNow.AddHours(-1));
        var valid = FullRehearsalCapability.Validate(
            contract,
            expectedMigration,
            "phrase-search-index-build",
            snapshot,
            DateTimeOffset.UtcNow,
            requireExclusiveLock: true,
            mode: "cleanup-apply");

        FullRehearsalCleanup.Authorize(
                valid,
                snapshot.Database,
                snapshot.Database,
                explicitlyConfirmed: true)
            .Authorized.Should().BeTrue();
        FullRehearsalCleanup.Authorize(
                valid,
                snapshot.Database,
                "another_database",
                explicitlyConfirmed: true)
            .Violations.Select(violation => violation.Code)
            .Should().Contain("rehearsal.cleanup.confirmation-mismatch");
        FullRehearsalCleanup.Authorize(
                valid,
                snapshot.Database,
                snapshot.Database,
                explicitlyConfirmed: false)
            .Violations.Select(violation => violation.Code)
            .Should().Contain("rehearsal.cleanup.explicit-confirmation-required");
    }

    [Fact]
    public void CleanupValidation_AcceptsTheMarkedTargetAfterAFailedRehearsalMutatesProtectedState()
    {
        var now = new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var expectedMigration = DatabaseContractValidator.Validate(contract).ExpectedMigrations.Last();
        var failedState = ValidSnapshot(contract, expectedMigration, now.AddDays(-30)) with
        {
            ProtectedStateMarker = new string('a', 64),
            ComputedProtectedStateFingerprint = new string('b', 64),
            DatabaseMigrationHead = "partially-restored-migration",
        };

        var result = FullRehearsalCapability.Validate(
            contract,
            expectedMigration,
            "phrase-search-index-build",
            failedState,
            now,
            requireExclusiveLock: true,
            mode: "cleanup-apply");

        result.Succeeded.Should().BeTrue();
        result.Report.CapabilityState.Should().Be("cleanup-ready");
        result.Report.Fresh.Should().BeFalse();
        result.Report.ProtectedStateFingerprint.Should().NotBe(
            result.Report.ComputedProtectedStateFingerprint);
    }

    private static FullRehearsalSnapshot ValidSnapshot(
        DatabaseContract contract,
        string expectedMigration,
        DateTimeOffset provisionedAtUtc) => new(
        "quran_rehearsal_phrase_index",
        168,
        "loopback",
        contract.PostgresMajorVersion,
        InRecovery: false,
        CapabilityEnabled: false,
        ResetEnabled: false,
        RehearsalEnabled: true,
        RehearsalSubtype: "phrase-search-index-build",
        CanonicalPipeline: CapabilityRefresher.PipelineIdentity,
        CanonicalInputProvenance: new string('1', 64),
        ProtectedStateMarker: new string('2', 64),
        ComputedProtectedStateFingerprint: new string('2', 64),
        MarkerMigrationHead: expectedMigration,
        DatabaseMigrationHead: expectedMigration,
        ProvisionedAtUtc: provisionedAtUtc,
        ExclusiveLockOwned: true);
}
