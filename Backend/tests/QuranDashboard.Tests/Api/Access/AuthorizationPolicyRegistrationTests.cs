using Microsoft.AspNetCore.Authorization;
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
    public static TheoryData<string> LegacyRolePolicyNames =>
    [
        "Owner",
        "Admin",
        "Editor",
    ];

    [Theory]
    [MemberData(nameof(LegacyRolePolicyNames))]
    public async Task LegacyRolePolicy_IsNotRegistered(string policyName)
    {
        var provider = fixture.ApiServices.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync(policyName);

        policy.Should().BeNull();
    }

    [Fact]
    public void NoFallbackPolicy_IsConfigured()
    {
        var options = fixture.ApiServices.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        options.FallbackPolicy.Should().BeNull();
    }

    [Fact]
    public void AuthorizationCore_UsesTheControlledResultHandler_WithoutClaimsTransformation()
    {
        var resultHandler = fixture.ApiServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        resultHandler.GetType().FullName.Should().Be(
            "QuranDashboard.Api.Authorization.ApiAuthorizationMiddlewareResultHandler");
        fixture.ApiServices.GetServices<IClaimsTransformation>()
            .Should().ContainSingle()
            .Which.GetType().FullName.Should().Be(
                "Microsoft.AspNetCore.Authentication.NoopClaimsTransformation");
        fixture.ApiServices.GetRequiredService<UnsafeEndpointMetadataValidator>().Should().NotBeNull();
    }

    [Fact]
    public void UnsafeEndpointValidator_AcceptsTheLiveClassifiedRoutes_WhenExplicitlyInvoked()
    {
        var endpoints = fixture.ApiServices.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .ToArray();

        var validator = fixture.ApiServices.GetRequiredService<UnsafeEndpointMetadataValidator>();
        var validate = () => validator.Validate(endpoints);

        endpoints.Should().NotBeEmpty();
        validate.Should().NotThrow();
    }
}
