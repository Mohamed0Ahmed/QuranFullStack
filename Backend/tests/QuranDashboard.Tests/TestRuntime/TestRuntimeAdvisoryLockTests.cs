using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeAdministrationCollection))]
public sealed class TestRuntimeAdvisoryLockTests(TestRuntimeAdministrationFixture fixture)
{
    private const long CommittedLockKey = 1500000001L;

    [Fact]
    public async Task CanonicalImporterChild_RequiresTheExpectedExclusiveKeeperIdentity()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            "refresh-child",
            "capability-refresh",
            TimeSpan.FromSeconds(2));
        acquisition.Lease.Should().NotBeNull();
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["QURAN_DASHBOARD_TEST_RUNTIME_GUARD"] = "exclusive-v1",
            ["QURAN_DASHBOARD_TEST_RUN_ID"] = "refresh-child",
            ["QURAN_DASHBOARD_TEST_LOCK_COMMAND"] = "capability-refresh",
            ["QURAN_DASHBOARD_TEST_LOCK_KEY"] = contract.AdvisoryLock.Key.ToString(),
        };
        using var error = new StringWriter();

        await using (acquisition.Lease!)
        {
            (await CanonicalMaintenanceGuard.VerifyIfRequiredAsync(
                fixture.ConnectionString,
                error,
                name => environment.GetValueOrDefault(name))).Should().BeTrue();

            var withoutGuard = new Dictionary<string, string>(environment, StringComparer.Ordinal);
            withoutGuard.Remove("QURAN_DASHBOARD_TEST_RUNTIME_GUARD");
            (await CanonicalMaintenanceGuard.VerifyIfRequiredAsync(
                fixture.ConnectionString,
                error,
                name => withoutGuard.GetValueOrDefault(name))).Should().BeFalse();
        }

        (await CanonicalMaintenanceGuard.VerifyIfRequiredAsync(
            fixture.ConnectionString,
            error,
            name => environment.GetValueOrDefault(name))).Should().BeFalse();
        error.ToString().Should().NotContain(fixture.CredentialSentinel);
    }

    [Fact]
    public async Task SharedKeepers_CoexistAndExcludeAnExclusiveContenderWithHolderDiagnostics()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var first = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Shared,
            "shared-reader-one",
            "guarded-reader",
            TimeSpan.FromSeconds(2));
        first.Lease.Should().NotBeNull();
        await using var firstLease = first.Lease!;
        var second = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Shared,
            "shared-reader-two",
            "guarded-reader",
            TimeSpan.FromSeconds(2));
        second.Lease.Should().NotBeNull();
        await using var secondLease = second.Lease!;

        var exclusive = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            "waiting-writer",
            "mutable-writer",
            TimeSpan.FromMilliseconds(150));

        exclusive.Lease.Should().BeNull();
        exclusive.Report.Status.Should().Be("timeout");
        exclusive.Report.Key.Should().Be(CommittedLockKey);
        exclusive.Report.WaitMilliseconds.Should().BeGreaterThanOrEqualTo(100);
        exclusive.Report.Holders.Select(holder => holder.RunId)
            .Should().BeEquivalentTo("shared-reader-one", "shared-reader-two");
        exclusive.Report.Holders.Should().OnlyContain(holder => holder.Mode == "shared");
        JsonSerializer.Serialize(exclusive.Report).Should().NotContain(fixture.CredentialSentinel);
    }

    [Fact]
    public async Task ExclusiveKeeper_ExcludesAllContendersAndOwnershipRequiresTheExpectedRun()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var exclusive = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            "exclusive-owner",
            "capability-admin",
            TimeSpan.FromSeconds(2));
        exclusive.Lease.Should().NotBeNull();
        await using var lease = exclusive.Lease!;
        await using var verifier = new NpgsqlConnection(fixture.ConnectionString);
        await verifier.OpenAsync();

        var verified = await AdvisoryLockProtocol.VerifyOwnershipAsync(
            verifier,
            contract.AdvisoryLock.Key,
            "exclusive-owner",
            "capability-admin",
            AdvisoryLockMode.Exclusive);
        var wrongRun = await AdvisoryLockProtocol.VerifyOwnershipAsync(
            verifier,
            contract.AdvisoryLock.Key,
            "different-run",
            "capability-admin",
            AdvisoryLockMode.Exclusive);
        var shared = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Shared,
            "waiting-reader",
            "guarded-reader",
            TimeSpan.FromMilliseconds(100));
        var anotherExclusive = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            "waiting-writer",
            "mutable-writer",
            TimeSpan.FromMilliseconds(100));

        verified.Should().BeTrue();
        wrongRun.Should().BeFalse();
        shared.Lease.Should().BeNull();
        anotherExclusive.Lease.Should().BeNull();
    }

    [Fact]
    public async Task ExclusiveOwnershipOfAnotherKey_DoesNotAuthorizeTheCommittedGlobalLock()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var unrelated = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key + 1,
            AdvisoryLockMode.Exclusive,
            "unrelated-owner",
            "capability-admin",
            TimeSpan.FromSeconds(2));
        unrelated.Lease.Should().NotBeNull();
        await using var unrelatedLease = unrelated.Lease!;
        await using var verifier = new NpgsqlConnection(fixture.ConnectionString);
        await verifier.OpenAsync();

        var verified = await AdvisoryLockProtocol.VerifyOwnershipAsync(
            verifier,
            contract.AdvisoryLock.Key,
            "unrelated-owner",
            "capability-admin",
            AdvisoryLockMode.Exclusive);

        verified.Should().BeFalse();
    }

    [Fact]
    public async Task KeeperConnection_UsesTheConfiguredTimeoutAndObservableRunMetadata()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Shared,
            "metadata-run",
            "reader-command");
        acquisition.Lease.Should().NotBeNull();
        await using var lease = acquisition.Lease!;
        await using var observer = new NpgsqlConnection(fixture.ConnectionString);
        await observer.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT application_name FROM pg_catalog.pg_stat_activity WHERE pid = @pid",
            observer);
        command.Parameters.AddWithValue("pid", lease.Ownership.KeeperProcessId);

        var applicationName = (string?)await command.ExecuteScalarAsync();

        acquisition.Report.TimeoutMilliseconds.Should().Be(900_000);
        applicationName.Should().Contain("metadata-run").And.Contain("reader-command");
    }

    [Fact]
    public async Task CatalogueReconciliation_VerifiesGlobalExclusiveOwnershipBeforeItsNarrowerLock()
    {
        var contract = DatabaseContractReader.Read(ContractPath);
        var global = await AdvisoryLockProtocol.AcquireAsync(
            fixture.ConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Exclusive,
            "catalogue-order",
            "catalogue-reconcile",
            TimeSpan.FromSeconds(2));
        global.Lease.Should().NotBeNull();
        await using var globalLease = global.Lease!;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var ownsGlobalLock = await AdvisoryLockProtocol.VerifyOwnershipAsync(
            connection,
            contract.AdvisoryLock.Key,
            "catalogue-order",
            "catalogue-reconcile",
            AdvisoryLockMode.Exclusive);
        ownsGlobalLock.Should().BeTrue();

        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        var result = await new PermissionCatalogueSynchronizer(context).SynchronizeAsync(CancellationToken.None);
        result.RetiredCanonicalCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task TerminatingKeeperProcess_ReleasesTheSessionLock()
    {
        var startInfo = new ProcessStartInfo(
            "dotnet",
            $"\"{TestRuntimeAssemblyPath}\" lock hold --mode exclusive --run-id process-owner --command crash-probe --timeout-seconds 5")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment[TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable] = fixture.ConnectionString;
        using var process = Process.Start(startInfo);
        process.Should().NotBeNull();
        try
        {
            var acquiredLine = await process!.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            if (acquiredLine is null)
            {
                acquiredLine.Should().NotBeNull(await process.StandardError.ReadToEndAsync());
            }

            using var acquired = JsonDocument.Parse(acquiredLine!);
            acquired.RootElement.GetProperty("advisoryLock").GetProperty("status").GetString()
                .Should().Be("acquired");

            process.Kill();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            var contract = DatabaseContractReader.Read(ContractPath);
            var successor = await AdvisoryLockProtocol.AcquireAsync(
                fixture.ConnectionString,
                contract.AdvisoryLock.Key,
                AdvisoryLockMode.Exclusive,
                "successor",
                "mutable-writer",
                TimeSpan.FromSeconds(5));
            successor.Lease.Should().NotBeNull();
            await using var successorLease = successor.Lease!;
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
    }

    private static string ContractPath => TestRuntimeTestPaths.ContractPath;

    private static string TestRuntimeAssemblyPath => TestRuntimeTestPaths.AssemblyPath;
}
