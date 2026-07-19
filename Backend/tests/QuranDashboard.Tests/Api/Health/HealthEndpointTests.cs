using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace QuranDashboard.Tests.Api.Health;

/// <summary>
/// Behavioural tests for <c>GET /api/health</c> covering all three overall statuses: <c>healthy</c> and
/// <c>degraded</c> must both answer <c>200</c> (existing behaviour), while <c>unhealthy</c> must answer
/// <c>503</c> so infra/Railway probes keyed on HTTP status correctly detect a dead dependency — the
/// response body still carries the full <c>data</c> (status + per-check detail) either way.
/// </summary>
public sealed class HealthEndpointTests
{
    private const string HealthPath = "/api/health";

    [Fact]
    public async Task Healthy_Returns200_WithHealthyEnvelopeAndChecks()
    {
        using var factory = new HealthApiFactory(HealthStatus.Healthy);
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(HealthPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.HealthOk);

        var data = envelope.GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("healthy");
        var checks = data.GetProperty("checks").EnumerateArray().ToArray();
        checks.Should().ContainSingle(check => check.GetProperty("name").GetString() == "database");
        checks.Single().GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task Degraded_Returns200_WithDegradedEnvelope()
    {
        using var factory = new HealthApiFactory(HealthStatus.Degraded);
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(HealthPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.HealthDegraded);

        var data = envelope.GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("degraded");
        data.GetProperty("checks").EnumerateArray()
            .Should().ContainSingle(check => check.GetProperty("name").GetString() == "database")
            .Which.GetProperty("status").GetString().Should().Be("degraded");
    }

    [Fact]
    public async Task Unhealthy_Returns503_WithUnhealthyEnvelopeAndChecksStillPresent()
    {
        using var factory = new HealthApiFactory(HealthStatus.Unhealthy);
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(HealthPath);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.HealthUnhealthy);

        // The 503 body must still carry data (status + checks) so probes/consumers get detail —
        // ApiResponse.Fail alone carries no data, which is exactly why the envelope is built inline.
        var data = envelope.GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("unhealthy");
        data.GetProperty("checks").EnumerateArray()
            .Should().ContainSingle(check => check.GetProperty("name").GetString() == "database")
            .Which.GetProperty("status").GetString().Should().Be("unhealthy");
    }
}
