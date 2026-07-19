using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Integration tests for <see cref="CachedUserRoleResolver"/> against the real Postgres container and a
/// real <see cref="MemoryCache"/> (no mocked persistence — correctness of the query and the caching is
/// the point). They pin the resolution contract (a role name only for an Active user with a role; null
/// otherwise) and the caching contract (a result — including a negative one — is cached, and
/// <see cref="CachedUserRoleResolver.Evict"/> makes a subsequent database change visible immediately).
/// </summary>
[Collection(nameof(AccessCollection))]
public sealed class CachedUserRoleResolverTests(AccessTestFixture fixture)
{
    public static TheoryData<UserStatus, bool, string?> RoleResolutionCases => new()
    {
        { UserStatus.Active, true, RoleNames.Owner },   // Active + role → the role name
        { UserStatus.Pending, true, null },             // not Active → null
        { UserStatus.Disabled, true, null },            // not Active → null
        { UserStatus.Active, false, null },             // Active but no role → null
    };

    [Theory]
    [MemberData(nameof(RoleResolutionCases))]
    public async Task GetActiveRoleName_ReflectsStatusAndRole(UserStatus status, bool hasRole, string? expected)
    {
        await fixture.ResetAsync();
        const string sub = "resolve-state";
        await SeedUserAsync(sub, status, hasRole ? await OwnerRoleIdAsync() : null);

        await using var db = NewDb();
        using var cache = NewCache();
        var resolver = new CachedUserRoleResolver(cache, db);

        (await resolver.GetActiveRoleNameAsync(sub, CancellationToken.None)).Should().Be(expected);
    }

    [Fact]
    public async Task GetActiveRoleName_ReturnsNull_WhenSubjectHasNoUser()
    {
        await fixture.ResetAsync();

        await using var db = NewDb();
        using var cache = NewCache();
        var resolver = new CachedUserRoleResolver(cache, db);

        (await resolver.GetActiveRoleNameAsync("ghost", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task CachesNegativeResult_ThenEvictReflectsDatabaseImmediately()
    {
        await fixture.ResetAsync();
        var ownerRoleId = await OwnerRoleIdAsync();
        const string sub = "cache-then-evict";
        await SeedUserAsync(sub, UserStatus.Pending, roleId: null);

        await using var db = NewDb();
        using var cache = NewCache();
        var resolver = new CachedUserRoleResolver(cache, db);

        (await resolver.GetActiveRoleNameAsync(sub, CancellationToken.None)).Should().BeNull();

        // Promote the row to Owner/Active directly in the database, bypassing the resolver's cache.
        await using (var mutate = NewDb())
        {
            var user = await mutate.AccessUsers.SingleAsync(u => u.LogtoSub == sub);
            user.RoleId = ownerRoleId;
            user.Status = UserStatus.Active;
            await mutate.SaveChangesAsync();
        }

        (await resolver.GetActiveRoleNameAsync(sub, CancellationToken.None))
            .Should().BeNull("the negative result is cached and no eviction happened");

        resolver.Evict(sub);

        (await resolver.GetActiveRoleNameAsync(sub, CancellationToken.None))
            .Should().Be(RoleNames.Owner, "eviction forces a fresh read of the promoted row");
    }

    private async Task<int> OwnerRoleIdAsync()
        => (await fixture.GetRolesAsync()).Single(r => r.Name == RoleNames.Owner).Id;

    private async Task SeedUserAsync(string sub, UserStatus status, int? roleId)
    {
        var now = DateTimeOffset.UtcNow;
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            RoleId = roleId,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private QuranDashboardDbContext NewDb()
        => new(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options);

    private static MemoryCache NewCache() => new(new MemoryCacheOptions());
}
