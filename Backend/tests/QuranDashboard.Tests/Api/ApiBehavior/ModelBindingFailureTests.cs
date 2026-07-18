using System.Net;
using QuranDashboard.Api.Common;

namespace QuranDashboard.Tests.Api.ApiBehavior;

/// <summary>
/// Asserts that a malformed typed query parameter (a non-integer value bound to an <c>int?</c> route
/// parameter) produces the shared <c>ApiResponse</c> failure envelope — not ASP.NET's default English
/// <c>ValidationProblemDetails</c> — via <c>ConfigureApiBehaviorOptions</c>.
/// </summary>
public sealed class ModelBindingFailureTests
{
    [Fact]
    public async Task MalformedTypedQueryParam_Returns400_WithApiResponseEnvelope()
    {
        using var factory = new ApiBehaviorTestFactory();
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync("/api/words/unique/tashkeel?page=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("isSuccess", "message", "data", "errors");
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.ValidationFailed);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);

        var errors = envelope.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToArray();
        errors.Should().NotBeEmpty();
        errors.Should().ContainSingle(error => error!.StartsWith("page:", StringComparison.Ordinal));
    }
}
