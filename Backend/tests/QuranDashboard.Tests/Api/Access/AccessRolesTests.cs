using System.Net.Http.Headers;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessRolesTests(AccessTestFixture fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task Roles_AreSeeded_AsTheFixedSet_WithArabicDisplayNames()
    {
        var roles = await fixture.GetRolesAsync();

        roles.Select(r => (r.Id, r.Name, r.DisplayName)).Should().Equal(
            (1, RoleNames.Owner, "المالك"),
            (2, RoleNames.Admin, "المشرف"),
            (3, RoleNames.Editor, "المحرر"));
    }

    [Fact]
    public async Task OwnerEmail_FirstLogin_ProvisionsOwnerActive_AndMeReturnsRoleName()
    {
        await fixture.ResetAsync();
        var ownerRoleId = await OwnerRoleIdAsync();
        var token = TestJwtTokens.Mint(AccessTestFixture.OwnerSub);
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await DataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("roleName").GetString().Should().Be(RoleNames.Owner);
        data.GetProperty("roleId").GetInt32().Should().Be(ownerRoleId);

        var owner = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        owner!.Status.Should().Be(UserStatus.Active);
        owner.RoleId.Should().Be(ownerRoleId);
    }

    [Fact]
    public async Task OwnerEmail_SecondLogin_IsIdempotent_SingleRowUnchanged()
    {
        await fixture.ResetAsync();
        var token = TestJwtTokens.Mint(AccessTestFixture.OwnerSub);
        using var client = fixture.CreateApiClient();

        using var first = await GetMeAsync(client, token);
        var afterFirst = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        using var second = await GetMeAsync(client, token);
        var afterSecond = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await fixture.GetUsersAsync()).Should().ContainSingle();
        // The already-Owner/Active row is returned untouched on the repeat login (no re-write).
        afterSecond!.Id.Should().Be(afterFirst!.Id);
        afterSecond.RoleId.Should().Be(afterFirst.RoleId);
        afterSecond.Status.Should().Be(UserStatus.Active);
        afterSecond.UpdatedAtUtc.Should().Be(afterFirst.UpdatedAtUtc);
    }

    [Fact]
    public async Task ExistingOwnerEmailUser_PendingNoRole_IsUpgraded_AndCacheEvictedImmediately()
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

        // Prime the role cache to the negative result while the row is still Pending/no-role.
        (await ResolveRoleAsync(AccessTestFixture.OwnerSub)).Should().BeNull();

        var token = TestJwtTokens.Mint(AccessTestFixture.OwnerSub);
        using var client = fixture.CreateApiClient();
        using var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await DataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("roleName").GetString().Should().Be(RoleNames.Owner);

        var upgraded = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        upgraded!.Status.Should().Be(UserStatus.Active);
        upgraded.RoleId.Should().Be(await OwnerRoleIdAsync());

        // Eviction (not the TTL) makes the new role visible on the very next resolve.
        (await ResolveRoleAsync(AccessTestFixture.OwnerSub)).Should().Be(RoleNames.Owner);
    }

    private async Task<int> OwnerRoleIdAsync()
        => (await fixture.GetRolesAsync()).Single(r => r.Name == RoleNames.Owner).Id;

    private async Task<string?> ResolveRoleAsync(string sub)
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IUserRoleResolver>();
        return await resolver.GetActiveRoleNameAsync(sub, CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
}
