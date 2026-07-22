using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Tests.Api.RateLimiting;

namespace QuranDashboard.Tests.Abwab.Permissions;

// FR-040 / SC-015 (T084): safe ENABLED rate-limiter defaults load; stricter named policies exist for
// permission-administration and operational owner-bootstrap; quotas are positive, bounded, and documented;
// bad quotas fail startup (the safe-429 envelope itself is the shared OnRejected writer, covered by the
// RateLimiting integration suite).
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

        // Stricter than the general limiter, and bootstrap the strictest of all.
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
