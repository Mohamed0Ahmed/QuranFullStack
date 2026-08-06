using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AuthorizationStateResolverTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task ResolveAsync_ReturnsOneScopedSnapshot_ForAnActiveNonOwnerWithAnExactGrant()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-state-active-user";
        await SynchronizePermissionsAsync();
        var userId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = "authorization-state-active-user@example.test",
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await GrantAsync(userId, AbwabPermissions.Doors.Create);

        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IAuthorizationStateResolver>();

        var first = resolver.ResolveAsync(sub, CancellationToken.None);
        var second = resolver.ResolveAsync(sub, CancellationToken.None);

        first.Should().BeSameAs(second);

        var state = await first;
        state.Should().NotBeNull();
        state!.UserId.Should().Be(userId);
        state.Status.Should().Be(UserStatus.Active);
        state.IsOwner.Should().BeFalse();
        state.PermissionCodes.Should().Equal(AbwabPermissions.Doors.Create);
    }

    [Fact]
    public async Task ResolveAsync_RejectsASecondSubject_InTheSameScope()
    {
        await fixture.ResetAsync();
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IAuthorizationStateResolver>();

        var firstState = await resolver.ResolveAsync("authorization-state-first-subject", CancellationToken.None);

        firstState.Should().BeNull();
        (await fixture.GetUsersAsync()).Should().BeEmpty();

        var resolveSecondSubject = () => resolver.ResolveAsync(
            "authorization-state-second-subject",
            CancellationToken.None);

        await resolveSecondSubject.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(UserStatus.Active, true)]
    [InlineData(UserStatus.Disabled, false)]
    public async Task ResolveAsync_ExcludesDirectGrantsUnlessTheUserIsActiveAndNonOwner(
        UserStatus status,
        bool owner)
    {
        await fixture.ResetAsync();
        const string sub = "authorization-state-no-direct-grants";
        await SynchronizePermissionsAsync();
        var roleId = owner
            ? (await fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id
            : (int?)null;
        var userId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = "authorization-state-no-direct-grants@example.test",
            RoleId = roleId,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await GrantAsync(userId, AbwabPermissions.Doors.Create);

        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var state = await scope.ServiceProvider.GetRequiredService<IAuthorizationStateResolver>()
            .ResolveAsync(sub, CancellationToken.None);

        state.Should().NotBeNull();
        state!.IsOwner.Should().Be(owner);
        state.PermissionCodes.Should().BeEmpty();
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private async Task GrantAsync(int userId, string permissionCode)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionId = await db.AccessPermissions
            .Where(permission => permission.Code == permissionCode)
            .Select(permission => permission.Id)
            .SingleAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            GrantedByUserId = userId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
