using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Api.Authentication;

namespace QuranDashboard.Tests.Api.Access;

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

    [Fact]
    public void AuthorizationCore_UsesTheControlledResultHandler_WithoutRoleClaimTransformation()
    {
        var resultHandler = fixture.ApiServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        resultHandler.GetType().FullName.Should().Be(
            "QuranDashboard.Api.Authorization.ApiAuthorizationMiddlewareResultHandler");
        fixture.ApiServices.GetServices<IClaimsTransformation>()
            .Should().NotContain(transformation => transformation is RoleClaimsTransformation);
        fixture.ApiServices.GetService<UnsafeEndpointMetadataValidator>().Should().BeNull();
    }

    [Fact]
    public void UnsafeEndpointValidator_RejectsTheCurrentUnclassifiedWrites_WhenExplicitlyInvoked()
    {
        var endpoints = fixture.ApiServices.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .ToArray();

        var validate = () => new UnsafeEndpointMetadataValidator().Validate(endpoints);

        endpoints.Should().NotBeEmpty();
        validate.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("has no permission or Owner classification");
    }
}
