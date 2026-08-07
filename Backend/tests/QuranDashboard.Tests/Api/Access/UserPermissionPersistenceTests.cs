using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class UserPermissionPersistenceTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task UserPermission_RejectsDuplicateGrantAndInvalidForeignKey()
    {
        await fixture.ResetAsync();
        using (var scope = fixture.ApiServices.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        var actorId = await fixture.InsertPersonaAsync("Owner");
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");
        int permissionId;
        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            permissionId = await db.AccessPermissions
                .Where(permission => permission.Code == AbwabPermissions.Doors.Create)
                .Select(permission => permission.Id)
                .SingleAsync();
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = targetId,
                PermissionId = permissionId,
                GrantedByUserId = actorId,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = targetId,
                PermissionId = permissionId,
                GrantedByUserId = actorId,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
            var duplicate = () => db.SaveChangesAsync();
            var duplicateException = await duplicate.Should().ThrowAsync<DbUpdateException>();
            duplicateException.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = targetId,
                PermissionId = int.MaxValue,
                GrantedByUserId = actorId,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
            var invalidForeignKey = () => db.SaveChangesAsync();
            var foreignKeyException = await invalidForeignKey.Should().ThrowAsync<DbUpdateException>();
            foreignKeyException.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23503");
        }
    }
}
