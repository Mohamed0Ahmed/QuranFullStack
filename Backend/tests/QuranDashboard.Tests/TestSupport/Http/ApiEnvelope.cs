namespace QuranDashboard.Tests.TestSupport.Http;

// Every API response is the same ApiResponse envelope (Backend/.architecture/API_GUIDELINES.md), so its
// shape is spelled out once here rather than re-copied into each suite that asserts it.
internal static class ApiEnvelope
{
    public static readonly string[] PropertyNames = ["isSuccess", "message", "data", "errors"];

    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }

    public static async Task AssertFailureEnvelopeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedMessage)
    {
        response.StatusCode.Should().Be(expectedStatus);

        // Read the header into a local first: the regression this guards is a bare framework rejection,
        // which carries no Content-Type at all. Dereferencing it would throw an NRE, and `?.` chained
        // straight into Should() would skip the assertion silently; both hide the failure being hunted.
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PropertyNames);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(expectedMessage);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        var errors = envelope.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);
        errors.GetArrayLength().Should().Be(0);
    }
}
