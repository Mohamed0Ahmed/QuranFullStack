using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Tests.Api.RateLimiting;

namespace QuranDashboard.Tests.Abwab.Permissions;

public sealed class RateLimiterDefaultsTests
{
    [Fact]
    public void NamedPolicyQuotas_ArePositiveBoundedAndStricterThanGeneral()
    {
        var options = new RateLimitingOptions();

        options.PermissionAdminPermitLimit.Should().BeInRange(1, 1000);
        options.PermissionAdminWindowSeconds.Should().BeInRange(1, 3600);
        options.OwnerBootstrapPermitLimit.Should().BeInRange(1, 1000);
        options.OwnerBootstrapWindowSeconds.Should().BeInRange(1, 3600);

        options.PermissionAdminPermitLimit.Should().BeLessThan(options.TokenLimit);
        options.OwnerBootstrapPermitLimit.Should().BeLessThan(options.PermissionAdminPermitLimit);
    }

    [Fact]
    public void NamedPolicyNames_ArePresentAndDistinct()
    {
        RateLimitPolicyNames.PermissionAdmin.Should().Be("permission-admin");
        RateLimitPolicyNames.OwnerBootstrap.Should().Be("owner-bootstrap");
        RateLimitPolicyNames.PermissionAdmin.Should().NotBe(RateLimitPolicyNames.OwnerBootstrap);
    }

    [Fact]
    public async Task SafeDefaults_BootTheApi_AndServeRequests()
    {
        using var factory = new RateLimitingApiFactory(new Dictionary<string, string?>());
        using var client = factory.CreateClientForIp("203.0.113.50");

        var response = await client.GetAsync("/api/dashboard/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void InvalidNamedPolicyQuota_FailsStartup()
    {
        using var factory = new RateLimitingApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:PermissionAdminPermitLimit"] = "0",
        });

        var act = () => factory.CreateClientForIp("203.0.113.51");

        act.Should().Throw<Exception>("a non-positive named-policy quota must fail options validation at startup");
    }
}
