using System.Security.Claims;
using QuranDashboard.Api.Authentication;

namespace QuranDashboard.Tests.Api.Access;

public sealed class RoleClaimsTransformationTests
{
    [Fact]
    public async Task AddsSingleRoleClaim_WhenResolverReturnsRole()
    {
        var transformation = new RoleClaimsTransformation(new FakeRoleResolver(roleName: "Owner"));
        var principal = AuthenticatedWithSub("logto-owner");

        var result = await transformation.TransformAsync(principal);

        result.FindAll(ClaimTypes.Role).Should().ContainSingle().Which.Value.Should().Be("Owner");
        result.IsInRole("Owner").Should().BeTrue();
    }

    public static TheoryData<string?> TokenBorneRoleClaimCases => new()
    {
        { null },
        { "SmuggledAdmin" },
    };

    [Theory]
    [MemberData(nameof(TokenBorneRoleClaimCases))]
    public async Task IsIdempotent_AddsDbBackedIdentityExactlyOnce_RegardlessOfTokenRoleClaim(string? tokenRoleClaim)
    {
        var resolver = new FakeRoleResolver(roleName: "Owner");
        var transformation = new RoleClaimsTransformation(resolver);
        var principal = AuthenticatedWithSub("logto-owner", tokenRoleClaim);

        var once = await transformation.TransformAsync(principal);
        var twice = await transformation.TransformAsync(once);

        resolver.Calls.Should().Be(1);
        MarkedIdentities(twice).Should().ContainSingle()
            .Which.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Owner");
        twice.IsInRole("Owner").Should().BeTrue();
    }

    [Fact]
    public async Task LeavesUnauthenticatedPrincipal_Untouched()
    {
        var resolver = new FakeRoleResolver(roleName: "Owner");
        var transformation = new RoleClaimsTransformation(resolver);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await transformation.TransformAsync(anonymous);

        result.FindFirst(ClaimTypes.Role).Should().BeNull();
        resolver.Calls.Should().Be(0);
    }

    [Fact]
    public async Task AddsNoClaim_WhenResolverReturnsNull()
    {
        var transformation = new RoleClaimsTransformation(new FakeRoleResolver(roleName: null));
        var principal = AuthenticatedWithSub("logto-pending");

        var result = await transformation.TransformAsync(principal);

        result.FindFirst(ClaimTypes.Role).Should().BeNull();
    }

    private static ClaimsPrincipal AuthenticatedWithSub(string sub, string? tokenRoleClaim = null)
    {
        var claims = new List<Claim> { new("sub", sub) };
        if (tokenRoleClaim is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, tokenRoleClaim));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    private static IEnumerable<ClaimsIdentity> MarkedIdentities(ClaimsPrincipal principal)
        => principal.Identities.Where(
            identity => identity.AuthenticationType == RoleClaimsTransformation.RoleClaimsAuthenticationType);

    private sealed class FakeRoleResolver(string? roleName) : IUserRoleResolver
    {
        public int Calls { get; private set; }

        public Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(roleName);
        }

        public void Evict(string logtoSub)
        {
        }
    }
}
