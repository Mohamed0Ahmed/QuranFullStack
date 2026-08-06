using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessCollectionResetContractTests(AccessTestFixture fixture)
{
    private const string ProbePermissionCode = "reset.contract_probe";

    [Fact]
    public async Task ResetAsync_EmptiesEveryTableTheCollectionMutates_AndKeepsTheSeededRoles()
    {
        await fixture.ResetAsync();
        var actorId = await fixture.InsertPersonaAsync("Owner");
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");
        await DirtyEveryMutatedTableAsync(actorId, targetId);

        await AssertRowCountsAsync(users: 2, permissions: 1, userPermissions: 1, auditEvents: 1);

        await fixture.ResetAsync();

        await AssertRowCountsAsync(users: 0, permissions: 0, userPermissions: 0, auditEvents: 0);
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessRoles.AsNoTracking().Select(role => role.Name).ToListAsync())
            .Should().Equal(RoleNames.Owner, RoleNames.Admin, RoleNames.Editor);
    }

    [Fact]
    public async Task ResetAsync_RestartsTheIdentityOfEveryTableItTruncates()
    {
        await fixture.ResetAsync();
        var actorId = await fixture.InsertPersonaAsync("Owner");
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");
        await DirtyEveryMutatedTableAsync(actorId, targetId);

        await fixture.ResetAsync();
        var restartedUserId = await fixture.InsertPersonaAsync("Owner");

        restartedUserId.Should().Be(1);
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        db.AccessPermissions.Add(
            new Permission(ProbePermissionCode, "فحص إعادة الضبط", "Reset contract probe", 900));
        await db.SaveChangesAsync();
        (await db.AccessPermissions.AsNoTracking().Select(permission => permission.Id).SingleAsync())
            .Should().Be(1);
    }

    private async Task DirtyEveryMutatedTableAsync(int actorId, int targetId)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permission = new Permission(
            ProbePermissionCode,
            "فحص إعادة الضبط",
            "Reset contract probe",
            900);
        db.AccessPermissions.Add(permission);
        await db.SaveChangesAsync();

        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = targetId,
            PermissionId = permission.Id,
            GrantedByUserId = actorId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        db.AccessAuditEvents.Add(new AccessAuditEvent(
            DateTimeOffset.UtcNow,
            AccessAuditActionType.PermissionGranted,
            AccessAuditActorType.User,
            actorId,
            targetId,
            "{\"sub\":\"actor\"}",
            "{\"sub\":\"target\"}",
            permission.Code,
            "{}",
            $"{{\"permission\":\"{ProbePermissionCode}\"}}",
            "reset contract",
            new AccessAuditMetadata(1)));
        await db.SaveChangesAsync();
    }

    private async Task AssertRowCountsAsync(
        int users,
        int permissions,
        int userPermissions,
        int auditEvents)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await db.AccessUsers.CountAsync()).Should().Be(users);
        (await db.AccessPermissions.CountAsync()).Should().Be(permissions);
        (await db.AccessUserPermissions.CountAsync()).Should().Be(userPermissions);
        (await db.AccessAuditEvents.CountAsync()).Should().Be(auditEvents);
    }
}
