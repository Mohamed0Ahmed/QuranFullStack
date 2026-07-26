namespace QuranDashboard.Tests.Smoke._Support;

internal static class ApiEnvelopeAssertions
{
    // Only 401 (OnChallenge writer), 400 (invalid-model-state factory), and in-body controller
    // failures carry the ApiResponse envelope; bare policy 403s have an empty body — never
    // call this on those.
    public static async Task AssertFailureEnvelopeAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        using var envelope = JsonDocument.Parse(body);

        envelope.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
