using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Infrastructure.Security.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Permissions;

// FR-027/FR-028 / SC-007 (T052): grant/revoke serialization, stale-version, idempotent no-audit no-ops,
// permanent audit, and cache invalidation (real PG). Uses the assignable, non-baseline attribution.manage.
[Collection(nameof(AbwabDbCollection))]
public sealed class GrantRevokeTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Code = PermissionCatalogue.AttributionManage;
    private const string Role = "Admin";

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Grant_CreatesAssignment_IsPermanentlyAudited_AndInvalidatesCache()
    {
        var cache = new SecurityTestHarness.RecordingCache();

        var result = await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 0), cache);

        result.Audited.Should().BeTrue();
        cache.InvalidateCount.Should().Be(1);
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
        (await FindAsync()).Should().Be((true, 1L));
    }

    [Fact]
    public async Task ReGrant_IsIdempotent_NoAudit_NoCacheInvalidation()
    {
        await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 0));
        var cache = new SecurityTestHarness.RecordingCache();

        var result = await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 1), cache);

        result.Audited.Should().BeFalse();
        cache.InvalidateCount.Should().Be(0);
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
    }

    [Fact]
    public async Task Revoke_AfterGrant_IsAudited_AndBumpsVersion()
    {
        await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 0));

        var result = await SecurityTestHarness.RevokeAsync(fixture, Revoke(expectedVersion: 1));

        result.Audited.Should().BeTrue();
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(2);
        (await FindAsync()).Should().Be((false, 2L));
    }

    [Fact]
    public async Task ReRevoke_AndRevokeOfAbsent_AreIdempotentNoAudit()
    {
        var absent = await SecurityTestHarness.RevokeAsync(fixture, Revoke(expectedVersion: 0));
        absent.Audited.Should().BeFalse("revoking an assignment that was never granted is a no-op");

        await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 0));
        await SecurityTestHarness.RevokeAsync(fixture, Revoke(expectedVersion: 1));
        var reRevoke = await SecurityTestHarness.RevokeAsync(fixture, Revoke(expectedVersion: 2));

        reRevoke.Audited.Should().BeFalse();
    }

    [Fact]
    public async Task Grant_WithStaleVersion_IsRejected()
    {
        var act = () => SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion: 99));

        (await act.Should().ThrowAsync<PermissionAssignmentStaleException>())
            .Which.Code.Should().Be(AbwabConflictCodes.PermissionAssignmentStale);
    }

    [Fact]
    public async Task ConcurrentFirstGrants_AreSerialized_ToExactlyOneRealGrant()
    {
        var first = TryGrantAsync(expectedVersion: 0);
        var second = TryGrantAsync(expectedVersion: 0);
        var outcomes = await Task.WhenAll(first, second);

        outcomes.Count(audited => audited == true).Should().Be(1, "exactly one concurrent first-grant is the real grant");
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
        (await FindAsync()).Should().Be((true, 1L));
    }

    private async Task<bool?> TryGrantAsync(long expectedVersion)
    {
        try
        {
            return (await SecurityTestHarness.GrantAsync(fixture, Grant(expectedVersion))).Audited;
        }
        catch (PermissionAssignmentStaleException)
        {
            return null;
        }
    }

    private async Task<(bool Granted, long Version)?> FindAsync()
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        var assignment = await new PermissionAssignmentStore(db)
            .FindTrackedAsync(PermissionTargetKind.Role, Role, Code, CancellationToken.None);
        return assignment is null ? null : (assignment.IsGranted, assignment.Version);
    }

    private static GrantPermissionCommand Grant(long expectedVersion) =>
        new(PermissionTargetKind.Role, Role, Code, ExpectedTimelineGeneration.Of(0), expectedVersion, "actor");

    private static RevokePermissionCommand Revoke(long expectedVersion) =>
        new(PermissionTargetKind.Role, Role, Code, ExpectedTimelineGeneration.Of(0), expectedVersion, "actor");
}
