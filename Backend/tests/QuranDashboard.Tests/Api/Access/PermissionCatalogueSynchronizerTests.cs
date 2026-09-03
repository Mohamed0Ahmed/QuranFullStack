using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessScratchRehearsalCollection))]
public sealed class PermissionCatalogueSynchronizerTests(AccessMigrationTestFixture fixture)
{
    [Fact]
    public async Task SynchronizeAsync_InsertsAllKnownCodesAndIsIdempotent()
    {
        await using var database = await fixture.CreateMigratedDatabaseAsync();

        var first = await SynchronizeAsync(database.ConnectionString);

        first.AddedCodes.Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
        first.UpdatedCodes.Should().BeEmpty();
        first.UnknownDatabaseCodes.Should().BeEmpty();

        var second = await SynchronizeAsync(database.ConnectionString);

        second.AddedCodes.Should().BeEmpty();
        second.UpdatedCodes.Should().BeEmpty();
        second.UnknownDatabaseCodes.Should().BeEmpty();
        await using var readDb = CreateDbContext(database.ConnectionString);
        (await readDb.AccessPermissions.AsNoTracking().OrderBy(permission => permission.DisplayOrder).ToListAsync())
            .Select(permission => permission.Code)
            .Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
    }

    [Fact]
    public async Task SynchronizeAsync_ReportsUnknownAndRetiredCodesFromThePersistedStateWithoutChangingThem()
    {
        await using var database = await fixture.CreateMigratedDatabaseAsync();
        await SynchronizeAsync(database.ConnectionString);
        const string retiredCode = AbwabPermissions.Sections.Delete;
        await using (var updateDb = CreateDbContext(database.ConnectionString))
        {
            var known = await updateDb.AccessPermissions.SingleAsync(permission =>
                permission.Code == AbwabPermissions.Doors.Create);
            known.UpdateMetadata("تغيير مؤقت", "Temporary", 99);
            updateDb.AccessPermissions.Add(new Permission("future.example", "مستقبلي", "Future", 100));
            await updateDb.SaveChangesAsync();
            await updateDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE permissions SET retired_at = {DateTimeOffset.UtcNow} WHERE code = {retiredCode};");
        }

        var result = await SynchronizeAsync(database.ConnectionString);

        result.UpdatedCodes.Should().Equal(AbwabPermissions.Doors.Create);
        result.UnknownDatabaseCodes.Should().Equal("future.example");
        result.RetiredCanonicalCodes.Should().Equal(retiredCode);
        await using var readDb = CreateDbContext(database.ConnectionString);
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

    private static async Task<PermissionCatalogueSyncResult> SynchronizeAsync(string connectionString)
    {
        await using var db = CreateDbContext(connectionString);
        return await new PermissionCatalogueSynchronizer(db).SynchronizeAsync(CancellationToken.None);
    }

    private static QuranDashboardDbContext CreateDbContext(string connectionString) => new(
        new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options);
}
