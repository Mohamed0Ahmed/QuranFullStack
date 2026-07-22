using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Permissions;

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

        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(2);
        (await SecurityTestHarness.ReadAuditHeadAsync(fixture)).Should().Be(0);
        (await SecurityTestHarness.ReadChangeSetCountAsync(fixture)).Should().Be(0);
    }

    [Fact]
    public async Task StaleGeneration_IsRejected_BeforeAnyMutation()
    {
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
        await SecurityTestHarness.SetBarrierStabilizingAsync(fixture);

        var act = () => SecurityTestHarness.GrantAsync(fixture, new GrantPermissionCommand(
            PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage,
            ExpectedTimelineGeneration.Of(0), 0, "actor"));

        (await act.Should().ThrowAsync<AbwabStabilizationActiveException>())
            .Which.Code.Should().Be(AbwabConflictCodes.StabilizationActive);

        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(0);
    }
}
