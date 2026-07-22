using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Permissions;

[Collection(nameof(AbwabDbCollection))]
public sealed class AssignabilityTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(PermissionCatalogue.PermissionAdminister)]
    [InlineData(PermissionCatalogue.AuditRestore)]
    [InlineData(PermissionCatalogue.SafetyPointManage)]
    public async Task GrantingASystemOwnerOnlyCode_ToAnOrdinaryUser_IsRejected(string code)
    {
        var act = () => SecurityTestHarness.GrantAsync(fixture, new GrantPermissionCommand(
            PermissionTargetKind.Subject, "ordinary-user", code, ExpectedTimelineGeneration.Of(0), 0, "actor"));

        (await act.Should().ThrowAsync<PermissionBaselineLockedException>())
            .Which.Code.Should().Be(AbwabConflictCodes.PermissionBaselineLocked);

        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(0);
    }

    [Fact]
    public void EverySystemOwnerOnlyCode_IsMarkedNonAssignable()
    {
        foreach (var code in PermissionCatalogue.SystemOwnerOnlyCodes)
        {
            PermissionCatalogue.Find(code)!.IsAssignable.Should().BeFalse();
        }
    }
}
