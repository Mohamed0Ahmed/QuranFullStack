using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;
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

    [Theory]
    [InlineData("structural-creation")]
    [InlineData("only")]
    [InlineData("all-except")]
    [InlineData("unknown-ayah")]
    [InlineData("empty-resolution")]
    [InlineData("ayah-corruption")]
    [InlineData("word-order")]
    [InlineData("word-subset")]
    [InlineData("word-corruption")]
    public void LinkingSelection_UsesCanonicalMembershipAndOrderingRules(string scenario)
    {
        static PhraseLinkingAyahSelection Create(
            PhraseLinkingAyahSelectionMode mode,
            IReadOnlyList<int> ayahIds)
        {
            PhraseLinkingAyahSelection.TryCreate(mode, ayahIds, out var selection).Should().BeTrue();
            return selection!;
        }

        switch (scenario)
        {
            case "structural-creation":
                PhraseLinkingAyahSelection.TryCreate(
                    PhraseLinkingAyahSelectionMode.Only,
                    [0],
                    out _).Should().BeFalse();
                PhraseLinkingAyahSelection.TryCreate(
                    PhraseLinkingAyahSelectionMode.AllExcept,
                    [2, 2],
                    out _).Should().BeFalse();
                PhraseLinkingAyahSelection.TryCreate(
                    (PhraseLinkingAyahSelectionMode)byte.MaxValue,
                    [],
                    out _).Should().BeFalse();

                var submitted = new[] { 3, 1 };
                var immutable = Create(PhraseLinkingAyahSelectionMode.Only, submitted);
                submitted[0] = 9;
                immutable.OverrideAyahIds.Should().Equal(3, 1);
                break;

            case "only":
                PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.Only, [30, 10]),
                        [10, 20, 30])
                    .Should().Equal(10, 30);
                break;

            case "all-except":
                PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.AllExcept, [20]),
                        [10, 20, 30])
                    .Should().Equal(10, 30);
                break;

            case "unknown-ayah":
                PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.Only, [40]),
                        [10, 20, 30])
                    .Should().BeNull();
                break;

            case "empty-resolution":
                PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.Only, []),
                        [10, 20])
                    .Should().BeNull();
                PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.AllExcept, [10, 20]),
                        [10, 20])
                    .Should().BeNull();
                break;

            case "ayah-corruption":
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.AllExcept, []),
                        [10, 10]))
                    .Should().Throw<InvalidDataException>();
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.ResolveAyahIds(
                        Create(PhraseLinkingAyahSelectionMode.AllExcept, []),
                        [0, 10]))
                    .Should().Throw<InvalidDataException>();
                break;

            case "word-order":
                PhraseLinkingSelectionResolver.OrderSelectedWords([11, 12, 13], [13, 11])
                    .Should().Equal(11, 13);
                break;

            case "word-subset":
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.OrderSelectedWords([11, 12], []))
                    .Should().Throw<InvalidDataException>();
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.OrderSelectedWords([11, 12], [13]))
                    .Should().Throw<InvalidDataException>();
                break;

            case "word-corruption":
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.OrderSelectedWords([11, 11], [11]))
                    .Should().Throw<InvalidDataException>();
                FluentActions.Invoking(() => PhraseLinkingSelectionResolver.OrderSelectedWords([0, 11], [11]))
                    .Should().Throw<InvalidDataException>();
                break;

            default:
                throw new InvalidOperationException($"Unknown test scenario '{scenario}'.");
        }
    }

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
