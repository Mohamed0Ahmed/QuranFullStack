using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.RateLimiting;

namespace QuranDashboard.Tests.Api.RateLimiting;

public sealed class ClientIpResolverTests
{
    private static ClientIpResolver CreateResolver(string headerName = "X-Real-IP") =>
        new(Options.Create(new RateLimitingOptions { ClientIpHeaderName = headerName }));

    [Fact]
    public void Resolve_ReturnsHeaderIp_WhenConfiguredHeaderPresent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "203.0.113.7";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        resolver.Resolve(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Resolve_FallsBackToRemoteIp_WhenHeaderAbsent()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        resolver.Resolve(context).Should().Be("10.0.0.5");
    }

    [Fact]
    public void Resolve_FallsBackToRemoteIp_WhenHeaderMalformed()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "not-an-ip";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.9");

        resolver.Resolve(context).Should().Be("10.0.0.9");
    }

    [Fact]
    public void Resolve_HonorsCustomHeaderName()
    {
        var resolver = CreateResolver("CF-Connecting-IP");
        var context = new DefaultHttpContext();
        context.Request.Headers["CF-Connecting-IP"] = "198.51.100.23";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        resolver.Resolve(context).Should().Be("198.51.100.23");
    }

    [Fact]
    public void Resolve_ParsesIpv6_AndTrimsWhitespace()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "  2001:db8::1  ";

        resolver.Resolve(context).Should().Be(IPAddress.Parse("2001:db8::1").ToString());
    }

    [Fact]
    public void Resolve_ReturnsUnknown_WhenNoHeaderAndNoRemoteIp()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();

        resolver.Resolve(context).Should().Be(ClientIpResolver.UnknownClient);
    }
}
