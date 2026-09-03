using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class PermissionCatalogueSynchronizerTests(LegacyAccessTestFixture fixture)
{
    [Fact]
    public async Task SynchronizeAsync_InsertsAllKnownCodesAndIsIdempotent()
    {
        await fixture.ResetAsync();

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
    public async Task SynchronizeAsync_ReportsUnknownAndRetiredCodesFromThePersistedStateWithoutChangingThem()
    {
        await fixture.ResetAsync();
        await SynchronizeAsync();
        const string retiredCode = AbwabPermissions.Sections.Delete;

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var updateDb = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var known = await updateDb.AccessPermissions.SingleAsync(permission =>
                permission.Code == AbwabPermissions.Doors.Create);
            known.UpdateMetadata("تغيير مؤقت", "Temporary", 99);
            updateDb.AccessPermissions.Add(new Permission("future.example", "مستقبلي", "Future", 100));
            await updateDb.SaveChangesAsync();
            await updateDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE permissions SET retired_at = {DateTimeOffset.UtcNow} WHERE code = {retiredCode};");
        }

        var result = await SynchronizeAsync();

        result.UpdatedCodes.Should().Equal(AbwabPermissions.Doors.Create);
        result.UnknownDatabaseCodes.Should().Equal("future.example");
        result.RetiredCanonicalCodes.Should().Equal(retiredCode);

        await using var readScope = fixture.QueryServices.CreateAsyncScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await readDb.AccessPermissions.AsNoTracking()
            .Select(permission => permission.Code)
            .ToListAsync())
            .Should().Contain("future.example");
        (await readDb.AccessPermissions.AsNoTracking()
            .Where(permission => permission.RetiredAtUtc != null)
            .Select(permission => permission.Code)
            .ToListAsync())
            .Should().Equal(retiredCode);
    }

    private async Task<PermissionCatalogueSyncResult> SynchronizeAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }
}
