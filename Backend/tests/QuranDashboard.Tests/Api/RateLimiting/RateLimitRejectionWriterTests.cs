using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.RateLimiting;

namespace QuranDashboard.Tests.Api.RateLimiting;

public sealed class RateLimitRejectionWriterTests
{
    private static RateLimitRejectionWriter CreateWriter(RateLimitingOptions? options = null) =>
        new(Options.Create(options ?? new RateLimitingOptions
        {
            ReplenishmentPeriodSeconds = 15,
            HealthWindowSeconds = 60,
        }));

    [Fact]
    public async Task WriteAsync_Writes429_WithEnvelope_AndRetryAfterFromMetadata()
    {
        var writer = CreateWriter();
        var context = CreateHttpContext("/api/dashboard/info");
        var onRejected = new OnRejectedContext
        {
            HttpContext = context,
            Lease = new StubLease(TimeSpan.FromSeconds(12)),
        };

        await writer.WriteAsync(onRejected, CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.ContentType.Should().StartWith("application/json");
        context.Response.Headers.RetryAfter.ToString().Should().Be("12");

        var body = await ReadBody(context);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be(ApiMessages.TooManyRequests);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("errors").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_UsesGeneralPeriodFallback_WhenMetadataMissing_OnNonHealthPath()
    {
        var writer = CreateWriter(new RateLimitingOptions { ReplenishmentPeriodSeconds = 20, HealthWindowSeconds = 90 });
        var context = CreateHttpContext("/api/dashboard/info");
        var onRejected = new OnRejectedContext { HttpContext = context, Lease = new StubLease(null) };

        await writer.WriteAsync(onRejected, CancellationToken.None);

        context.Response.Headers.RetryAfter.ToString().Should().Be("20");
    }

    [Fact]
    public async Task WriteAsync_UsesHealthWindowFallback_WhenMetadataMissing_OnHealthPath()
    {
        var writer = CreateWriter(new RateLimitingOptions { ReplenishmentPeriodSeconds = 20, HealthWindowSeconds = 90 });
        var context = CreateHttpContext("/api/health");
        var onRejected = new OnRejectedContext { HttpContext = context, Lease = new StubLease(null) };

        await writer.WriteAsync(onRejected, CancellationToken.None);

        context.Response.Headers.RetryAfter.ToString().Should().Be("90");
    }

    [Fact]
    public async Task WriteAsync_ShortCircuits_WhenResponseHasStarted()
    {
        var writer = CreateWriter();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var onRejected = new OnRejectedContext
        {
            HttpContext = context,
            Lease = new StubLease(TimeSpan.FromSeconds(5)),
        };

        await writer.WriteAsync(onRejected, CancellationToken.None);

        context.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class StubLease(TimeSpan? retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames =>
            retryAfter is null ? [] : [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (retryAfter is not null && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter.Value;
                return true;
            }

            metadata = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
        }
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
