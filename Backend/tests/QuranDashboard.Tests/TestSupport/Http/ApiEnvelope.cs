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

    // The envelope shape every response shares: the four properties, the media type, a message, and an
    // isSuccess that agrees with the status class — a 200 carrying isSuccess=false, or a 404 carrying
    // true, is a contract break no status-code assertion can see. Callers that own an exact message pin
    // it themselves; the route sweep asserts 48 different messages and pins only that one is present.
    // Returns the parsed envelope, detached from the JsonDocument, so a caller adding its own assertions
    // does not read and parse the same body a second time.
    public static async Task<JsonElement> AssertEnvelopeMatchesStatusAsync(HttpResponseMessage response)
    {
        // Read the header into a local first: the regression this guards is a bare framework rejection,
        // which carries no Content-Type at all. Dereferencing it would throw an NRE, and `?.` chained
        // straight into Should() would skip the assertion silently; both hide the failure being hunted.
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PropertyNames);
        envelope.GetProperty("isSuccess").GetBoolean().Should().Be(response.IsSuccessStatusCode);
        envelope.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();

        return envelope.Clone();
    }

    public static async Task AssertFailureEnvelopeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedMessage)
    {
        // Status first, so the shared check below reads isSuccess against a status already proven to be
        // the expected failure one rather than against whatever came back.
        response.StatusCode.Should().Be(expectedStatus);
        var envelope = await AssertEnvelopeMatchesStatusAsync(response);

        // Re-pinned rather than left to the shared check: that one asserts isSuccess against the status
        // class, which only means "false" while every caller passes a failure status. This method's name
        // promises a failure envelope whatever expectedStatus is.
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(expectedMessage);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        var errors = envelope.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);
        errors.GetArrayLength().Should().Be(0);
    }
}
