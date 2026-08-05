using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class PermissionCatalogueSynchronizerTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task SynchronizeAsync_InsertsAllKnownCodesAndIsIdempotent()
    {
        await ClearPermissionsAsync();

        PermissionCatalogueSyncResult first;
        using (var scope = fixture.ApiServices.CreateScope())
        {
            first = await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        first.AddedCodes.Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
        first.UpdatedCodes.Should().BeEmpty();
        first.UnknownDatabaseCodes.Should().BeEmpty();

        PermissionCatalogueSyncResult second;
        using (var scope = fixture.ApiServices.CreateScope())
        {
            second = await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        second.AddedCodes.Should().BeEmpty();
        second.UpdatedCodes.Should().BeEmpty();
        second.UnknownDatabaseCodes.Should().BeEmpty();

        await using var readScope = fixture.QueryServices.CreateAsyncScope();
        var db = readScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessPermissions.AsNoTracking().OrderBy(permission => permission.DisplayOrder).ToListAsync())
            .Select(permission => permission.Code)
            .Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
    }

    [Fact]
    public async Task SynchronizeAsync_UpdatesMetadataAndReportsUnknownCodesWithoutDeletingThem()
    {
        await ClearPermissionsAsync();
        using (var scope = fixture.ApiServices.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var updateDb = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var known = await updateDb.AccessPermissions.SingleAsync(permission =>
                permission.Code == AbwabPermissions.Doors.Create);
            known.UpdateMetadata("تغيير مؤقت", "Temporary", 99);
            updateDb.AccessPermissions.Add(new Permission("future.example", "مستقبلي", "Future", 100));
            await updateDb.SaveChangesAsync();
        }

        PermissionCatalogueSyncResult result;
        using (var scope = fixture.ApiServices.CreateScope())
        {
            result = await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        result.UpdatedCodes.Should().ContainSingle().Which.Should().Be(AbwabPermissions.Doors.Create);
        result.UnknownDatabaseCodes.Should().ContainSingle().Which.Should().Be("future.example");

        await using var readScope = fixture.QueryServices.CreateAsyncScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await readDb.AccessPermissions.AsNoTracking().SingleAsync(permission => permission.Code == "future.example"))
            .Should().NotBeNull();
    }

    private async Task ClearPermissionsAsync()
    {
        await fixture.ResetAsync();
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE user_permissions, access_audit_events, permissions RESTART IDENTITY CASCADE;");
    }
}
