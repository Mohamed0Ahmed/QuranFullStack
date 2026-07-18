using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.Authentication;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Verifies the Phase-2 authorization posture at the DI level: one named role policy per
/// <c>AuthorizationPolicyNames</c> value is registered (each requiring an authenticated caller in that
/// role), and there is NO global fallback policy. The policies exist only in DI here — no endpoint
/// carries one — which is asserted behaviourally by the public-route tests elsewhere in this suite.
/// </summary>
[Collection(nameof(AccessCollection))]
public sealed class AuthorizationPolicyRegistrationTests(AccessTestFixture fixture)
{
    public static TheoryData<string> PolicyNames =>
    [
        AuthorizationPolicyNames.Owner,
        AuthorizationPolicyNames.Admin,
        AuthorizationPolicyNames.Editor,
    ];

    [Theory]
    [MemberData(nameof(PolicyNames))]
    public async Task NamedRolePolicy_IsResolvable_AndRequiresThatRole(string policyName)
    {
        var provider = fixture.ApiServices.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync(policyName);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().Contain(r => r is DenyAnonymousAuthorizationRequirement);
        policy.Requirements.OfType<RolesAuthorizationRequirement>()
            .SelectMany(requirement => requirement.AllowedRoles)
            .Should().ContainSingle().Which.Should().Be(policyName);
    }

    [Fact]
    public void NoFallbackPolicy_IsConfigured()
    {
        var options = fixture.ApiServices.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        options.FallbackPolicy.Should().BeNull();
    }
}
