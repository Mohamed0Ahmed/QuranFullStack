using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.PhraseSearch;

[Collection(nameof(PhraseSearchApiCollection))]
public sealed class PhraseSearchStateTests(PhraseSearchStateFixture fixture) : IAsyncLifetime
{
    private const string IndexUnavailableMessage = "فهرس البحث في العبارات غير متاح حاليًا";
    private const string IndexChangedMessage = "تغير فهرس البحث، أعد اختيار النتيجة";
    private const string IndexUnavailableCode = "phrase_index_unavailable";
    private const string IndexChangedCode = "phrase_index_changed";

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MissingActiveState_CapabilitiesReturnsTheDocumentedUnavailableEnvelope()
    {
        await fixture.ResetToMissingActiveStateAsync();
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync("/api/quran/phrase-search/capabilities");

        await AssertPhraseFailureAsync(
            response,
            HttpStatusCode.ServiceUnavailable,
            IndexUnavailableMessage,
            IndexUnavailableCode);
    }

    [Fact]
    public async Task StaleActiveState_QueryResolutionReturnsTheDocumentedUnavailableEnvelope()
    {
        await fixture.CreateActiveBuildAsync(stale: true);
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync(
            "/api/quran/phrase-search/query-resolutions?mode=simple&q64=eA");

        await AssertPhraseFailureAsync(
            response,
            HttpStatusCode.ServiceUnavailable,
            IndexUnavailableMessage,
            IndexUnavailableCode);
    }

    [Fact]
    public async Task StaleBuildReference_OccurrencesReturnsConflictInsteadOfResolvingAgainstTheActiveBuild()
    {
        var activeBuildId = await fixture.CreateActiveBuildAsync(stale: false);
        var staleBuildId = Guid.NewGuid();
        staleBuildId.Should().NotBe(activeBuildId);
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync(
            $"/api/quran/phrase-search/repetitions/{staleBuildId:D}/1/occurrences");

        await AssertPhraseFailureAsync(
            response,
            HttpStatusCode.Conflict,
            IndexChangedMessage,
            IndexChangedCode);
    }

    private static async Task AssertPhraseFailureAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedMessage,
        string expectedErrorCode)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("message").GetString().Should().Be(expectedMessage);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        envelope.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().Equal(expectedErrorCode);
    }
}
