using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.PhraseSearch;

public sealed class PhraseSearchComputePipelineTests
{
    private const string QueryResolutionPath =
        "/api/quran/phrase-search/query-resolutions?mode=simple&q64=eA";
    private const int ComputePermitLimit = 4;
    private const int ComputeQueueLimit = 8;
    private const string ComputeTimeoutMessage = "استغرق حساب نتائج العبارة وقتًا أطول من المسموح";
    private const string ComputeTimeoutCode = "phrase_compute_timeout";

    [Fact]
    public async Task ComputePolicy_QueuesRequestsAndRejectsTheOverflowWithoutConcurrentExecution()
    {
        var queryReader = new BlockingPhraseQueryResolutionReader(ComputePermitLimit);
        using var factory = new PhraseSearchApiFactory(
            new ImmediatePhraseSearchReader(),
            queryReader);
        using var client = factory.CreatePhraseSearchClient();

        var permitted = Enumerable.Range(0, ComputePermitLimit)
            .Select(_ => client.GetAsync(QueryResolutionPath))
            .ToArray();
        await queryReader.WaitUntilConcurrencyThresholdReachedAsync();
        var queuedOrRejected = Enumerable.Range(0, ComputeQueueLimit + 1)
            .Select(_ => client.GetAsync(QueryResolutionPath))
            .ToArray();
        var rejected = await await Task.WhenAny(queuedOrRejected).WaitAsync(TimeSpan.FromSeconds(5));

        rejected.StatusCode.Should().Be(
            HttpStatusCode.TooManyRequests,
            "the named compute policy must cap execution at {0} permits and {1} queued requests; "
            + "the reader observed {2} invocations with {3} concurrent reads",
            ComputePermitLimit,
            ComputeQueueLimit,
            queryReader.InvocationCount,
            queryReader.MaximumConcurrentRequests);
        rejected.Headers.RetryAfter.Should().NotBeNull();
        var rejectionEnvelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(rejected);
        rejectionEnvelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);

        queryReader.Release();
        var completed = await Task.WhenAll(permitted.Concat(queuedOrRejected));
        completed.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(1);
        foreach (var response in completed)
        {
            using (response)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
            }
        }

        queryReader.InvocationCount.Should().Be(ComputePermitLimit + ComputeQueueLimit);
        queryReader.MaximumConcurrentRequests.Should().Be(ComputePermitLimit);
    }

    [Fact]
    public async Task ComputePolicy_TimeoutReturnsTheDocumentedUnavailableEnvelopeAndCancelsTheRead()
    {
        var queryReader = new BlockingPhraseQueryResolutionReader();
        using var factory = new PhraseSearchApiFactory(
            new ImmediatePhraseSearchReader(),
            queryReader);
        using var client = factory.CreatePhraseSearchClient();

        var pending = client.GetAsync(QueryResolutionPath);
        await queryReader.WaitUntilConcurrencyThresholdReachedAsync();
        using var timedOut = await pending;

        timedOut.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(timedOut);
        envelope.GetProperty("message").GetString().Should().Be(ComputeTimeoutMessage);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        envelope.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().Equal(ComputeTimeoutCode);
        await queryReader.WaitUntilCancellationIsObservedAsync();
    }

}
