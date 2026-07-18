using System.Net;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.Common;

namespace QuranDashboard.Tests.Api.RateLimiting;

public sealed class RateLimitingIntegrationTests
{
    private const string DashboardPath = "/api/dashboard/info";
    private const string HealthPath = "/api/health";

    // Tiny permits + very long windows so no replenishment occurs mid-test (the inner
    // AutoReplenishment flag is forced false by the partition and is NOT a freeze lever).
    private static Dictionary<string, string?> TightLimits() => new()
    {
        ["RateLimiting:Enabled"] = "true",
        ["RateLimiting:ClientIpHeaderName"] = "X-Real-IP",
        ["RateLimiting:TokenLimit"] = "2",
        ["RateLimiting:TokensPerPeriod"] = "2",
        ["RateLimiting:ReplenishmentPeriodSeconds"] = "600",
        ["RateLimiting:QueueLimit"] = "0",
        ["RateLimiting:HealthPermitLimit"] = "2",
        ["RateLimiting:HealthWindowSeconds"] = "600",
    };

    [Fact]
    public async Task General_ThrottlesAfterBurst_WithEnvelopeAndRetryAfter()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var client = factory.CreateClientForIp("203.0.113.10");

        (await client.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.OK);

        var rejected = await client.GetAsync(DashboardPath);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        rejected.Headers.RetryAfter.Should().NotBeNull();

        var body = await rejected.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be(ApiMessages.TooManyRequests);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("errors").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task General_IsolatesPerIp()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var clientA = factory.CreateClientForIp("203.0.113.11");
        using var clientB = factory.CreateClientForIp("203.0.113.12");

        await clientA.GetAsync(DashboardPath);
        await clientA.GetAsync(DashboardPath);
        (await clientA.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        (await clientB.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Namespacing_GeneralExhaustion_DoesNotBlockHealth_ForSameIp()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var client = factory.CreateClientForIp("203.0.113.13");

        await client.GetAsync(DashboardPath);
        await client.GetAsync(DashboardPath);
        (await client.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        (await client.GetAsync(HealthPath)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Namespacing_HealthExhaustion_DoesNotBlockGeneral_ForSameIp()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var client = factory.CreateClientForIp("203.0.113.14");

        await client.GetAsync(HealthPath);
        await client.GetAsync(HealthPath);
        (await client.GetAsync(HealthPath)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        (await client.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ThrottlesAndIsolatesPerIp()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var clientA = factory.CreateClientForIp("203.0.113.15");
        using var clientB = factory.CreateClientForIp("203.0.113.16");

        await clientA.GetAsync(HealthPath);
        await clientA.GetAsync(HealthPath);
        (await clientA.GetAsync(HealthPath)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        (await clientB.GetAsync(HealthPath)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Options_AreNeverThrottled()
    {
        using var factory = new RateLimitingApiFactory(TightLimits());
        using var client = factory.CreateClientForIp("203.0.113.17");

        for (var i = 0; i < 6; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, DashboardPath);
            var response = await client.SendAsync(request);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task DevSwagger_IsNeverThrottled()
    {
        using var factory = new RateLimitingApiFactory(TightLimits(), environment: "Development");
        using var client = factory.CreateClientForIp("203.0.113.18");

        for (var i = 0; i < 6; i++)
        {
            var response = await client.GetAsync("/swagger/index.html");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task Disabled_NeverThrottles()
    {
        var overrides = TightLimits();
        overrides["RateLimiting:Enabled"] = "false";
        using var factory = new RateLimitingApiFactory(overrides);
        using var client = factory.CreateClientForIp("203.0.113.19");

        for (var i = 0; i < 6; i++)
        {
            (await client.GetAsync(DashboardPath)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public void InvalidConfig_ThrowsAtStartup()
    {
        var overrides = TightLimits();
        overrides["RateLimiting:TokenLimit"] = "0";
        using var factory = new RateLimitingApiFactory(overrides);

        var act = () => factory.CreateClientForIp("203.0.113.20");

        act.Should().Throw<OptionsValidationException>();
    }
}
