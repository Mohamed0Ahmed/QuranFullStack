using System.Security.Claims;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Unit tests for <see cref="RoleClaimsTransformation"/> with a fake <see cref="IUserRoleResolver"/> (the
/// only real boundary). They pin the transformation's contract: it adds exactly one
/// <see cref="ClaimTypes.Role"/> claim on a dedicated identity when a role resolves; its idempotency keys
/// off that marked identity — so a repeat invocation adds nothing, and a token-borne
/// <see cref="ClaimTypes.Role"/> claim neither short-circuits the database role load nor is mistaken for the
/// transformation's own output; it leaves an unauthenticated principal untouched; and it adds no claim when
/// no role resolves.
/// </summary>
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

    // Whether or not the token itself carries a ClaimTypes.Role claim, idempotency must key off the marked
    // identity: the database role load runs exactly once and exactly one marked identity is added.
    public static TheoryData<string?> TokenBorneRoleClaimCases => new()
    {
        { null },              // clean token: no role claim of its own
        { "SmuggledAdmin" },   // token smuggles a ClaimTypes.Role claim (MapInboundClaims = false)
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

        // The marked identity short-circuits the second pass, so the resolver is consulted exactly once and
        // exactly one database-backed identity carrying the resolved role is present — the token-borne claim
        // never short-circuits the load.
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
