using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabConditionalReadTests(SmokeApiFixture fixture)
{
    public static TheoryData<string> ListPaths =>
    [
        "/api/abwab/tree",
        "/api/abwab/templates",
    ];

    public static TheoryData<string, string> NonMatchingHeaders => new()
    {
        { "\"__smoke_mismatch__\"", "mismatched" },
        { "not-an-etag", "malformed" },
        { "*", "wildcard" },
    };

    public static TheoryData<string, string, string> ListNonMatchingRequests => new()
    {
        { "/api/abwab/tree", "\"__smoke_mismatch__\"", "mismatched" },
        { "/api/abwab/tree", "not-an-etag", "malformed" },
        { "/api/abwab/tree", "*", "wildcard" },
        { "/api/abwab/templates", "\"__smoke_mismatch__\"", "mismatched" },
        { "/api/abwab/templates", "not-an-etag", "malformed" },
        { "/api/abwab/templates", "*", "wildcard" },
    };

    [Theory]
    [MemberData(nameof(ListPaths))]
    public async Task ListRead_WithAMatchingValidator_ReturnsBodiless304WithoutDatabaseCommands(string path)
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);
        using var first = await client.GetAsync(path);
        var etag = AssertValidatorHeaders(first);

        fixture.CommandCapture.Reset();
        using var request = ConditionalRequest(path, etag);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        response.Headers.ETag!.ToString().Should().Be(etag);
        response.Headers.CacheControl!.ToString().Should().Be("no-store");
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        fixture.CommandCapture.CommandTexts.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ListNonMatchingRequests))]
    public async Task ListRead_WithANonMatchingValidator_ReturnsFresh200(string path, string header, string _)
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var request = ConditionalRequest(path, header);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertValidatorHeaders(response);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }

    [Fact]
    public async Task TemplateDetail_WithAMatchingValidator_ReturnsBodiless304AfterTheResourceExists()
    {
        await fixture.ResetAsync();
        using var owner = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, _) = await SmokeAbwabApi.CreateTemplateAsync(owner, "__smoke_conditional_template__");
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);
        var path = $"/api/abwab/templates/{templateId}";
        using var first = await client.GetAsync(path);
        var etag = AssertValidatorHeaders(first);

        using var request = ConditionalRequest(path, etag);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        response.Headers.ETag!.ToString().Should().Be(etag);
        response.Headers.CacheControl!.ToString().Should().Be("no-store");
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NonMatchingHeaders))]
    public async Task TemplateDetail_WithANonMatchingValidator_ReturnsFresh200(string header, string _)
    {
        await fixture.ResetAsync();
        using var owner = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, _) = await SmokeAbwabApi.CreateTemplateAsync(owner, "__smoke_conditional_template__");
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var request = ConditionalRequest($"/api/abwab/templates/{templateId}", header);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertValidatorHeaders(response);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }

    private static string AssertValidatorHeaders(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Be("no-store");
        return response.Headers.ETag!.ToString();
    }

    private static HttpRequestMessage ConditionalRequest(string path, string header)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("If-None-Match", header).Should().BeTrue();
        return request;
    }
}
