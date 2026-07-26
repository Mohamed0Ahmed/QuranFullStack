using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Smoke._Fixtures;

namespace QuranDashboard.Tests.Smoke.Guards;

[Collection(nameof(SmokeCollection))]
public sealed class SmokeHostGuardTests(SmokeApiFixture fixture)
{
    [Fact]
    public void Host_runs_in_Testing_environment()
        => fixture.InMemoryServices.GetRequiredService<IHostEnvironment>()
            .EnvironmentName.Should().Be("Testing");

    [Fact]
    public void Active_connection_string_is_the_test_container()
    {
        var config = fixture.InMemoryServices.GetRequiredService<IConfiguration>();
        var active = config.GetConnectionString("QuranDashboardDb");

        active.Should().Be(fixture.ConnectionString);
        active.Should().NotContain("rlwy.net").And.NotContain("railway");
    }

    [Fact]
    public async Task Authentication_scheme_inventory_is_exactly_Bearer()
    {
        var schemes = await fixture.InMemoryServices
            .GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();

        var scheme = schemes.Should().ContainSingle().Subject;
        scheme.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        scheme.HandlerType.Should().Be(typeof(JwtBearerHandler));
    }

    [Fact]
    public void DbContext_options_keep_the_production_write_guard_interceptor()
    {
        var options = fixture.InMemoryServices.GetRequiredService<DbContextOptions<QuranDashboardDbContext>>();
        var coreOptions = options.FindExtension<CoreOptionsExtension>();

        coreOptions.Should().NotBeNull();
        coreOptions!.Interceptors.Should().NotBeNull();
        coreOptions.Interceptors!.OfType<AbwabWriteGuardInterceptor>().Should().NotBeEmpty(
            "the smoke host must run the real persistence composition, including the write-kernel guard");
    }

    [Fact]
    public async Task Health_returns_200_through_in_memory_host()
        => (await fixture.InMemoryClient.GetAsync("api/health"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Health_returns_200_through_kestrel_host()
        => (await fixture.KestrelClient.GetAsync("api/health"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public void Kestrel_host_listens_on_real_loopback_http_port()
    {
        fixture.KestrelClient.BaseAddress!.Scheme.Should().Be("http");
        fixture.KestrelClient.BaseAddress.Port.Should().NotBe(0).And.NotBe(80);
    }
}
