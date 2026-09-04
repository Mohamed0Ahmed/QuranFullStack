using Microsoft.Extensions.Options;
using QuranDashboard.Api.Access;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke.Data;

namespace QuranDashboard.Tests.Smoke;

// These guard the composition the rest of the smoke tier assumes. If one fails, every later smoke
// result is meaningless — the host booted as something other than the API under Testing.
[Collection(nameof(MutableDatabaseCollection))]
public sealed class SmokeMutableBootGuardTests(AccessTestFixture fixture)
    : SmokeMutableWriterTest(fixture)
{
    [Fact]
    public void DbContextConnection_TargetsThePersistentCapability_NotTheDevelopmentDatabase()
    {
        using var scope = ApiServices.CreateScope();
        var effective = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>()
            .Database.GetConnectionString();

        // Fail-closed: base appsettings.json points at the developer's real local database; later smoke
        // phases reset data, so this suite must never inherit that connection string.
        var expected = new NpgsqlConnectionStringBuilder(Fixture.TargetConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(effective);
        actual.Host.Should().Be(expected.Host);
        actual.Port.Should().Be(expected.Port);
        actual.Database.Should().Be(expected.Database);
        actual.Username.Should().Be(expected.Username);
        actual.Options.Should().Contain("default_transaction_read_only=off");
        actual.ApplicationName.Should().Be("quran-dashboard-api-testing-mutable");

        ApiServices.GetRequiredService<IConfiguration>()
            .GetConnectionString("QuranDashboardDb").Should().Be(Fixture.TargetConnectionString);
    }

    [Fact]
    public void MutableSmokeHost_DisablesPermissionCatalogueStartupSynchronization()
    {
        ApiServices.GetRequiredService<IOptions<PermissionCatalogueStartupOptions>>().Value.Enabled
            .Should().BeFalse();
        ApiServices.GetRequiredService<DatabaseActivityPolicy>()
            .AllowPermissionCatalogueSynchronization.Should().BeFalse();
    }

}

[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeReadOnlyBootGuardTests(SmokeDataFixture fixture)
{
    [Fact]
    public async Task DashboardInfo_ReportsTestingEnvironment()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync("/api/dashboard/info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("data").GetProperty("environment").GetString()
            .Should().Be("Testing");
    }

    [Fact]
    public void RateLimiting_IsInheritedOff()
    {
        // Asserted rather than configured: base appsettings.json already ships Enabled=false, and the
        // partitioner short-circuits on it. Overriding it here would hide the day someone flips the
        // default and throttles the route sweep instead.
        fixture.ApiServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value.Enabled
            .Should().BeFalse();
    }

    [Fact]
    public async Task Health_Returns200_WithHealthyDatabaseAndPersistentCatalogue()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync("/api/health");

        // The controller answers 503 when unhealthy, so a 200 proves the persistent capability connection,
        // migrations, and System Catalogue checks all succeeded through the read-only host.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var data = envelope.GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("healthy");
        var checks = data.GetProperty("checks").EnumerateArray().ToArray();
        checks.Should().ContainSingle(check =>
            check.GetProperty("name").GetString() == "database"
            && check.GetProperty("status").GetString() == "healthy");
        checks.Should().ContainSingle(check =>
            check.GetProperty("name").GetString() == "permission_catalogue"
            && check.GetProperty("status").GetString() == "healthy");
    }
}
