using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Tests.Api.PhraseSearch;

internal sealed class PhraseSearchApiFactory(
    IPhraseRepetitionsReader repetitionsReader,
    IPhraseQueryResolutionReader resolutionReader) : WebApplicationFactory<HealthController>
{
    public HttpClient CreatePhraseSearchClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Port=5432;Database=phrase_search_api_tests;Username=none;Password=none",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["Access:PermissionCatalogueStartupSync:Enabled"] = "false",
                ["OwnerBootstrap:Emails:0"] = "phrase-search-tests@example.test",
                ["RateLimiting:Enabled"] = "false",
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPhraseRepetitionsReader>();
            services.AddSingleton(repetitionsReader);
            services.RemoveAll<IPhraseQueryResolutionReader>();
            services.AddSingleton(resolutionReader);
        });
    }
}

internal sealed class ImmediatePhraseSearchReader : IPhraseRepetitionsReader
{
    public static readonly Guid ActiveBuildId = new("e3f8d58c-7651-4edc-9ca0-8dbef6987ab5");
    private int repetitionsReadCount;

    public int RepetitionsReadCount => Volatile.Read(ref repetitionsReadCount);

    public Task<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>> GetCapabilitiesAsync(
        CancellationToken cancellationToken) => Task.FromResult<
        PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>>(
        new PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success(
            new PhraseSearchCapabilitiesResponse(
                ActiveBuildId,
                ExactReady: true,
                SimilarityReady: true,
                DefaultMode: "simple",
                DefaultRepetitionLength: 2,
                DefaultRepetitionSort: "occurrences",
                DefaultPageSize: 25,
                MaximumPageSize: 100,
                MaximumRepetitionPageSize: 100,
                MaximumRepetitionOccurrencePageSize: 100,
                MinimumSimilarityPercent: 50,
                SimilarityThresholds: [50],
                Modes:
                [
                    new PhraseTextModeCapabilitiesDto(
                        "simple",
                        SupportedLengths: [2],
                        RepeatedLengths: [2],
                        SimilarityLengths: [2],
                        MaximumSupportedLength: 2,
                        MaximumRepeatedLength: 2,
                        MaximumSimilarityLength: 2),
                ])));

    public Task<PhraseSearchReadResult<PhraseRepetitionsPageResponse>> GetRepetitionsAsync(
        PhraseTextMode mode,
        short wordCount,
        IReadOnlyList<string> searchTerms,
        PhraseRepetitionSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref repetitionsReadCount);
        return Task.FromResult<PhraseSearchReadResult<PhraseRepetitionsPageResponse>>(
            new PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Success(
                new PhraseRepetitionsPageResponse(
                    ActiveBuildId,
                    "simple",
                    2,
                    "occurrences",
                    1,
                    25,
                    0,
                    [])));
    }

    public Task<PhraseSearchReadResult<PhraseOccurrencePageResponse>> GetOccurrencesAsync(
        Guid expectedBuildId,
        long variantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) => Task.FromResult<PhraseSearchReadResult<PhraseOccurrencePageResponse>>(
        new PhraseSearchReadResult<PhraseOccurrencePageResponse>.NotFound());
}

internal sealed class ImmediatePhraseQueryResolutionReader : IPhraseQueryResolutionReader
{
    public Task<PhraseQueryResolutionReadResult> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken) => Task.FromResult<PhraseQueryResolutionReadResult>(
        new PhraseQueryResolutionReadResult.Success(
            new PhraseQueryResolutionResponse(
                ImmediatePhraseSearchReader.ActiveBuildId,
                "simple",
                PhraseResolutionStatuses.Unresolved,
                [])));
}

internal sealed class BlockingPhraseQueryResolutionReader : IPhraseQueryResolutionReader
{
    private readonly int concurrencyThreshold;
    private readonly TaskCompletionSource concurrencyThresholdReached = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource cancellationObserved = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private int activeRequests;
    private int invocationCount;
    private int maximumConcurrentRequests;

    public BlockingPhraseQueryResolutionReader(int concurrencyThreshold = 1)
    {
        this.concurrencyThreshold = concurrencyThreshold;
    }

    public int InvocationCount => Volatile.Read(ref invocationCount);

    public int MaximumConcurrentRequests => Volatile.Read(ref maximumConcurrentRequests);

    public Task WaitUntilConcurrencyThresholdReachedAsync() => concurrencyThresholdReached.Task.WaitAsync(
        TimeSpan.FromSeconds(5));

    public Task WaitUntilCancellationIsObservedAsync() => cancellationObserved.Task.WaitAsync(
        TimeSpan.FromSeconds(5));

    public void Release() => release.TrySetResult();

    public async Task<PhraseQueryResolutionReadResult> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken)
    {
        var active = Interlocked.Increment(ref activeRequests);
        UpdateMaximumConcurrentRequests(active);
        Interlocked.Increment(ref invocationCount);
        if (active >= concurrencyThreshold)
        {
            concurrencyThresholdReached.TrySetResult();
        }
        try
        {
            await release.Task.WaitAsync(cancellationToken);
            return new PhraseQueryResolutionReadResult.Success(
                new PhraseQueryResolutionResponse(
                    ImmediatePhraseSearchReader.ActiveBuildId,
                    "simple",
                    PhraseResolutionStatuses.Unresolved,
                    []));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationObserved.TrySetResult();
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref activeRequests);
        }
    }

    private void UpdateMaximumConcurrentRequests(int active)
    {
        var observed = Volatile.Read(ref maximumConcurrentRequests);
        while (active > observed)
        {
            var previous = Interlocked.CompareExchange(
                ref maximumConcurrentRequests,
                active,
                observed);
            if (previous == observed)
            {
                return;
            }

            observed = previous;
        }
    }
}
