using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Permissions;

// FR-039 / SC-014 (T082): permission/owner writes produce permanent security-audit events but NEVER advance
// AbwabRevisionState.AuditHeadSequence and NEVER create a product ChangeSet / Restore-head — while STILL
// taking the barrier + AbwabRevisionState locks and carrying ExpectedTimelineGeneration (real PG).
[Collection(nameof(AbwabDbCollection))]
public sealed class SecurityAuditSeparationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Issuer = "https://logto.test";

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SecurityWrites_Append_ButNeverAdvanceAuditHead_OrCreateChangeSet()
    {
        await SecurityTestHarness.BootstrapOwnerAsync(fixture, new BootstrapSystemOwnerCommand(
            Issuer, "founder", Issuer, EmailVerified: true, AccountEnabled: true, ExpectedTimelineGeneration.Of(0), "actor"));

        await SecurityTestHarness.GrantAsync(fixture, new GrantPermissionCommand(
            PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage,
            ExpectedTimelineGeneration.Of(0), 0, "actor"));

        // Permanent security-audit events were written...
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(2);
        // ...but the product head never advanced and no product ChangeSet (Restore-head) was ever created.
        (await SecurityTestHarness.ReadAuditHeadAsync(fixture)).Should().Be(0);
        (await SecurityTestHarness.ReadChangeSetCountAsync(fixture)).Should().Be(0);
    }

    [Fact]
    public async Task StaleGeneration_IsRejected_BeforeAnyMutation()
    {
        // The revision state moved on: a security write carrying a stale expected generation must fail with
        // the exact 409 BEFORE any row is touched — proving it locked AbwabRevisionState and checked freshness.
        await SecurityTestHarness.SetGenerationAsync(fixture, 7);

        var act = () => SecurityTestHarness.GrantAsync(fixture, new GrantPermissionCommand(
            PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage,
            ExpectedTimelineGeneration.Of(0), 0, "actor"));

        (await act.Should().ThrowAsync<AbwabTimelineGenerationStaleException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TimelineGenerationStale);

        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(0);
    }

    [Fact]
    public async Task BarrierStabilizing_RefusesSecurityWrites()
    {
        // A non-Writable barrier is surfaced to the security surface as abwab.stabilization_active — proving
        // the write took and evaluated the AbwabWriteBarrier first.
        await SecurityTestHarness.SetBarrierStabilizingAsync(fixture);

        var act = () => SecurityTestHarness.GrantAsync(fixture, new GrantPermissionCommand(
            PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage,
            ExpectedTimelineGeneration.Of(0), 0, "actor"));

        (await act.Should().ThrowAsync<AbwabStabilizationActiveException>())
            .Which.Code.Should().Be(AbwabConflictCodes.StabilizationActive);

        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(0);
    }
}
