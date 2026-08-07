using System.Net.Http.Headers;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessRolesTests(AccessTestFixture fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task Roles_SeedOnlyOwner_WithArabicDisplayName()
    {
        var roles = await fixture.GetRolesAsync();

        roles.Select(r => (r.Id, r.Name, r.DisplayName)).Should().Equal(
            (1, RoleNames.Owner, "المالك"));
    }

    [Fact]
    public async Task OwnerEmail_FirstLogin_ProvisionsOwnerActive_AndMeReturnsOwnerAccessContract()
    {
        await fixture.ResetAsync();
        var ownerRoleId = await OwnerRoleIdAsync();
        var token = OwnerToken();
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        data.GetProperty("permissions").GetArrayLength().Should().Be(0);

        var owner = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        owner!.Status.Should().Be(UserStatus.Active);
        owner.RoleId.Should().Be(ownerRoleId);
    }

    [Fact]
    public async Task OwnerEmail_SecondLogin_IsIdempotent_SingleRowUnchanged()
    {
        await fixture.ResetAsync();
        var token = OwnerToken();
        using var client = fixture.CreateApiClient();

        using var first = await GetMeAsync(client, token);
        var afterFirst = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        using var second = await GetMeAsync(client, token);
        var afterSecond = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await fixture.GetUsersAsync()).Should().ContainSingle();
        afterSecond!.Id.Should().Be(afterFirst!.Id);
        afterSecond.RoleId.Should().Be(afterFirst.RoleId);
        afterSecond.Status.Should().Be(UserStatus.Active);
        afterSecond.UpdatedAtUtc.Should().Be(afterFirst.UpdatedAtUtc);
    }

    [Fact]
    public async Task ExistingOwnerEmailUser_PendingNoRole_IsUpgraded()
    {
        await fixture.ResetAsync();
        var now = DateTimeOffset.UtcNow;
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = AccessTestFixture.OwnerSub,
            Email = AccessTestFixture.OwnerEmail,
            DisplayName = "Pending Owner",
            RoleId = null,
            Status = UserStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        var token = OwnerToken();
        using var client = fixture.CreateApiClient();
        using var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");

        var upgraded = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        upgraded!.Status.Should().Be(UserStatus.Active);
        upgraded.RoleId.Should().Be(await OwnerRoleIdAsync());

    }

    [Fact]
    public async Task ActiveNonOwner_MeReturnsOnlyOrderedDirectPermissionCodes()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string sub = "access-me-direct-permissions";
        var userId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await AddGrantsAsync(userId, ownerId, [AbwabPermissions.Sections.Edit, AbwabPermissions.Doors.Create]);
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, TestJwtTokens.Mint(sub));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("isOwner").GetBoolean().Should().BeFalse();
        data.GetProperty("permissions").EnumerateArray().Select(value => value.GetString())
            .Should().Equal(AbwabPermissions.Doors.Create, AbwabPermissions.Sections.Edit);
    }

    [Fact]
    public async Task DisabledOwner_MeRetainsOwnerIdentityButReturnsNoPermissions()
    {
        await fixture.ResetAsync();
        const string sub = "smoke-disabled-owner";
        await fixture.InsertPersonaAsync("DisabledOwner");
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, TestJwtTokens.Mint(sub));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("disabled");
        data.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        data.GetProperty("permissions").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PersonaFixture_CanSeedAStatusAndRoleWithoutGrantTables()
    {
        await fixture.ResetAsync();

        var userId = await fixture.InsertPersonaAsync("DisabledOwner");
        var user = await fixture.GetUserBySubAsync("smoke-disabled-owner");

        user!.Id.Should().Be(userId);
        user.Status.Should().Be(UserStatus.Disabled);
        user.RoleId.Should().Be(await OwnerRoleIdAsync());
    }

    private async Task<int> OwnerRoleIdAsync()
        => (await fixture.GetRolesAsync()).Single(r => r.Name == RoleNames.Owner).Id;

    private async Task<int> SeedActiveOwnerAsync()
    {
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = AccessTestFixture.OwnerSub,
            Email = AccessTestFixture.OwnerEmail,
            RoleId = await OwnerRoleIdAsync(),
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private async Task AddGrantsAsync(int targetUserId, int actorUserId, IReadOnlyList<string> permissionCodes)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionIds = await db.AccessPermissions
            .Where(permission => permissionCodes.Contains(permission.Code))
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);
        foreach (var permissionCode in permissionCodes)
        {
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = targetUserId,
                PermissionId = permissionIds[permissionCode],
                GrantedByUserId = actorUserId,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static string OwnerToken() => TestJwtTokens.Mint(
        AccessTestFixture.OwnerSub,
        additionalClaims: new Dictionary<string, object>
        {
            ["email"] = AccessTestFixture.OwnerEmail,
            ["email_verified"] = true,
        });
}
