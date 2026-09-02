namespace QuranDashboard.Tests.Api.PhraseSearch;

public sealed class PhraseSearchConditionalRequestTests
{
    [Fact]
    public async Task SupportedRepetitionsEndpoint_ReusesItsValidatorWithinOneApiProcess()
    {
        var repetitionsReader = new ImmediatePhraseSearchReader();
        using var factory = new PhraseSearchApiFactory(
            repetitionsReader,
            new ImmediatePhraseQueryResolutionReader());
        using var client = factory.CreatePhraseSearchClient();

        using var initial = await client.GetAsync(
            "/api/quran/phrase-search/repetitions?mode=simple&length=2");
        initial.StatusCode.Should().Be(HttpStatusCode.OK);
        var validator = initial.Headers.ETag?.Tag;
        validator.Should().NotBeNullOrWhiteSpace();
        repetitionsReader.RepetitionsReadCount.Should().Be(1);

        using var conditionalRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/quran/phrase-search/repetitions?mode=simple&length=2");
        conditionalRequest.Headers.TryAddWithoutValidation("If-None-Match", validator).Should().BeTrue();
        using var notModified = await client.SendAsync(conditionalRequest);

        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
        notModified.Headers.ETag?.Tag.Should().Be(validator);
        repetitionsReader.RepetitionsReadCount.Should().Be(1);
    }
}
