using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemAyahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMissingSurahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

/// <summary>
/// Consolidated Phase 11 (T115) logging audit across all fourteen Feature 016
/// handlers. Each handler must emit one structured log entry per call, with safe
/// fields only (IDs/counts/booleans/paging/sort/measured duration) and no Quran,
/// lemma, stem, root, Buckwalter, word, raw-search, SQL, or payload text.
/// </summary>
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class MorphologyExplorersLoggingTests(MorphologyExplorersTestFixture fixture)
{
    private const int LemmaId = 500;
    private const int StemId = 602;
    private const int UnknownId = 999_999;
    private const string SearchText = "كلمة";

    private static readonly string[] SafeSearchLeakFields =
    [
        "search", "searchText", "query", "q", "term", "rawSearch", "text"
    ];

    [Fact]
    public async Task GetLemmasPage_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(new GetLemmasPageQuery(null, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Lemmas");
        entry.GetValue<string>("operation").Should().Be("GetLemmasPage");
        entry.GetValue<string>("sort").Should().Be(LemmaSortKeys.MushafOrder);
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
    }

    [Fact]
    public async Task GetLemmasPage_with_search_logs_hasSearch_without_raw_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(new GetLemmasPageQuery(SearchText, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.FieldNames().Should().NotContain(SafeSearchLeakFields);
        AssertNoText(entry, SearchText);
    }

    [Theory]
    [MemberData(nameof(LemmasPageWarningCases))]
    public async Task GetLemmasPage_logs_one_warning_for_controlled_refusals(
        GetLemmasPageQuery query,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        AssertNoText(entry, SearchText);
    }

    [Fact]
    public async Task GetLemmaSummary_logs_success_with_safe_counts_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(new GetLemmaSummaryQuery(LemmaId), CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        var entry = SingleEntryFor<GetLemmaSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "lemmaId", "typeDistributionCount", "occurrencesCount", "ayahsCount", "surahsCount", "stemsCount"]);
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
        entry.GetValue<int>("typeDistributionCount").Should().Be(summary.TypeDistribution.Count);
        entry.GetValue<int>("occurrencesCount").Should().Be(summary.OccurrencesCount);
        entry.GetValue<int>("stemsCount").Should().Be(summary.StemsCount);
        AssertNoText(entry, summary.LemmaText);
    }

    [Theory]
    [MemberData(nameof(LemmaSummaryWarningCases))]
    public async Task GetLemmaSummary_logs_one_warning_for_controlled_failures(
        GetLemmaSummaryQuery query,
        LogLevel expectedLevel,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaSummaryHandler>(expectedLevel);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<int>("lemmaId").Should().Be(query.Id);
    }

    [Fact]
    public async Task GetLemmaAyahs_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        await handler.HandleAsync(new GetLemmaAyahsQuery(LemmaId, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaAyahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "lemmaId", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("feature").Should().Be("Lemmas");
        entry.GetValue<string>("operation").Should().Be("GetLemmaAyahs");
        entry.GetValue<string>("view").Should().Be("ayahs");
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
    }

    [Theory]
    [MemberData(nameof(LemmaAyahsWarningCases))]
    public async Task GetLemmaAyahs_logs_one_warning_for_controlled_failures(
        GetLemmaAyahsQuery query,
        string? expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaAyahsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        if (expectedReason is not null)
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }
        entry.GetValue<int>("lemmaId").Should().Be(query.Id);
    }

    [Fact]
    public async Task GetLemmaWords_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        await handler.HandleAsync(
            new GetLemmaWordsQuery(LemmaId, LemmaWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaWordsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "lemmaId", "subView", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("words");
        entry.GetValue<string>("subView").Should().Be(LemmaWordKindKeys.Simple);
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
    }

    [Fact]
    public async Task GetLemmaWords_invalid_kind_logs_warning_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        await handler.HandleAsync(new GetLemmaWordsQuery(LemmaId, "not-a-kind", 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaWordsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "reason", "view", "lemmaId", "pageNumber", "pageSize"]);
        entry.GetValue<string>("reason").Should().Be("invalidKind");
    }

    [Fact]
    public async Task GetLemmaMentionedSurahs_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMentionedSurahsHandler>();

        await handler.HandleAsync(new GetLemmaMentionedSurahsQuery(LemmaId), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaMentionedSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "lemmaId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("surahs");
        entry.GetValue<string>("subView").Should().Be("mentioned");
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
    }

    [Fact]
    public async Task GetLemmaMissingSurahs_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaMissingSurahsHandler>();

        await handler.HandleAsync(new GetLemmaMissingSurahsQuery(LemmaId), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaMissingSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "lemmaId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("surahs");
        entry.GetValue<string>("subView").Should().Be("missing");
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
    }

    [Fact]
    public async Task GetLemmaStems_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaStemsHandler>();

        await handler.HandleAsync(new GetLemmaStemsQuery(LemmaId), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaStemsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "lemmaId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("feature").Should().Be("Lemmas");
        entry.GetValue<string>("operation").Should().Be("GetLemmaStems");
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
    }

    [Fact]
    public async Task GetStemsPage_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(new GetStemsPageQuery(null, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Stems");
        entry.GetValue<string>("operation").Should().Be("GetStemsPage");
        entry.GetValue<string>("sort").Should().Be(StemSortKeys.MushafOrder);
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
    }

    [Fact]
    public async Task GetStemsPage_with_search_logs_hasSearch_without_raw_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(new GetStemsPageQuery(SearchText, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.FieldNames().Should().NotContain(SafeSearchLeakFields);
        AssertNoText(entry, SearchText);
    }

    [Theory]
    [MemberData(nameof(StemsPageWarningCases))]
    public async Task GetStemsPage_logs_one_warning_for_controlled_refusals(
        GetStemsPageQuery query,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        AssertNoText(entry, SearchText);
    }

    [Fact]
    public async Task GetStemSummary_logs_success_with_safe_counts_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(new GetStemSummaryQuery(StemId), CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary;

        var entry = SingleEntryFor<GetStemSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "stemId", "dominantLemmaId", "dominantRootId", "dominantTypeCode", "otherTypesCount", "occurrencesCount", "ayahsCount", "surahsCount", "simpleWordsCount", "tashkeelWordsCount"]);
        entry.GetValue<int>("stemId").Should().Be(StemId);
        entry.GetValue<int>("occurrencesCount").Should().Be(summary.OccurrencesCount);
        AssertNoText(entry, summary.StemText);
    }

    [Theory]
    [MemberData(nameof(StemSummaryWarningCases))]
    public async Task GetStemSummary_logs_one_warning_for_controlled_failures(
        GetStemSummaryQuery query,
        LogLevel expectedLevel,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetStemSummaryHandler>(expectedLevel);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<int>("stemId").Should().Be(query.Id);
    }

    [Fact]
    public async Task GetStemAyahs_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        await handler.HandleAsync(new GetStemAyahsQuery(StemId, 1, 50, null), CancellationToken.None);

        var entry = SingleEntryFor<GetStemAyahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "stemId", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("feature").Should().Be("Stems");
        entry.GetValue<string>("operation").Should().Be("GetStemAyahs");
        entry.GetValue<string>("view").Should().Be("ayahs");
        entry.GetValue<int>("stemId").Should().Be(StemId);
    }

    [Theory]
    [MemberData(nameof(StemAyahsWarningCases))]
    public async Task GetStemAyahs_logs_one_warning_for_controlled_failures(
        GetStemAyahsQuery query,
        string? expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetStemAyahsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        if (expectedReason is not null)
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }
        entry.GetValue<int>("stemId").Should().Be(query.Id);
    }

    [Fact]
    public async Task GetStemWords_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        await handler.HandleAsync(
            new GetStemWordsQuery(StemId, StemWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        var entry = SingleEntryFor<GetStemWordsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "stemId", "subView", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("words");
        entry.GetValue<string>("subView").Should().Be(StemWordKindKeys.Simple);
        entry.GetValue<int>("stemId").Should().Be(StemId);
    }

    [Fact]
    public async Task GetStemWords_invalid_kind_logs_warning_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        await handler.HandleAsync(new GetStemWordsQuery(StemId, "not-a-kind", 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetStemWordsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "reason", "view", "stemId", "pageNumber", "pageSize"]);
        entry.GetValue<string>("reason").Should().Be("invalidKind");
    }

    [Fact]
    public async Task GetStemMentionedSurahs_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMentionedSurahsHandler>();

        await handler.HandleAsync(new GetStemMentionedSurahsQuery(StemId), CancellationToken.None);

        var entry = SingleEntryFor<GetStemMentionedSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "stemId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("surahs");
        entry.GetValue<string>("subView").Should().Be("mentioned");
        entry.GetValue<int>("stemId").Should().Be(StemId);
    }

    [Fact]
    public async Task GetStemMissingSurahs_logs_success_with_subView_and_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemMissingSurahsHandler>();

        await handler.HandleAsync(new GetStemMissingSurahsQuery(StemId), CancellationToken.None);

        var entry = SingleEntryFor<GetStemMissingSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "stemId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("surahs");
        entry.GetValue<string>("subView").Should().Be("missing");
        entry.GetValue<int>("stemId").Should().Be(StemId);
    }

    [Fact]
    public async Task GetStemLemmas_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemLemmasHandler>();

        await handler.HandleAsync(new GetStemLemmasQuery(StemId), CancellationToken.None);

        var entry = SingleEntryFor<GetStemLemmasHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "stemId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("feature").Should().Be("Stems");
        entry.GetValue<string>("operation").Should().Be("GetStemLemmas");
        entry.GetValue<int>("stemId").Should().Be(StemId);
    }

    [Fact]
    public async Task No_handler_logs_quran_lemma_stem_or_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var sp = scope.ServiceProvider;

        var lemmaSummaryOutcome = await sp.GetRequiredService<GetLemmaSummaryHandler>()
            .HandleAsync(new GetLemmaSummaryQuery(LemmaId), CancellationToken.None);
        var lemmaText = lemmaSummaryOutcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary.LemmaText;

        var stemSummaryOutcome = await sp.GetRequiredService<GetStemSummaryHandler>()
            .HandleAsync(new GetStemSummaryQuery(StemId), CancellationToken.None);
        var stemText = stemSummaryOutcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary.StemText;

        await sp.GetRequiredService<GetLemmasPageHandler>()
            .HandleAsync(new GetLemmasPageQuery(SearchText, null, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetStemsPageHandler>()
            .HandleAsync(new GetStemsPageQuery(SearchText, null, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetLemmaAyahsHandler>()
            .HandleAsync(new GetLemmaAyahsQuery(LemmaId, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetLemmaWordsHandler>()
            .HandleAsync(new GetLemmaWordsQuery(LemmaId, LemmaWordKindKeys.Tashkeel, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetLemmaMentionedSurahsHandler>()
            .HandleAsync(new GetLemmaMentionedSurahsQuery(LemmaId), CancellationToken.None);
        await sp.GetRequiredService<GetLemmaMissingSurahsHandler>()
            .HandleAsync(new GetLemmaMissingSurahsQuery(LemmaId), CancellationToken.None);
        await sp.GetRequiredService<GetLemmaStemsHandler>()
            .HandleAsync(new GetLemmaStemsQuery(LemmaId), CancellationToken.None);
        await sp.GetRequiredService<GetStemAyahsHandler>()
            .HandleAsync(new GetStemAyahsQuery(StemId, 1, 50, null), CancellationToken.None);
        await sp.GetRequiredService<GetStemWordsHandler>()
            .HandleAsync(new GetStemWordsQuery(StemId, StemWordKindKeys.Tashkeel, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetStemMentionedSurahsHandler>()
            .HandleAsync(new GetStemMentionedSurahsQuery(StemId), CancellationToken.None);
        await sp.GetRequiredService<GetStemMissingSurahsHandler>()
            .HandleAsync(new GetStemMissingSurahsQuery(StemId), CancellationToken.None);
        await sp.GetRequiredService<GetStemLemmasHandler>()
            .HandleAsync(new GetStemLemmasQuery(StemId), CancellationToken.None);

        lemmaText.Should().NotBeNullOrWhiteSpace();
        stemText.Should().NotBeNullOrWhiteSpace();
        foreach (var entry in fixture.LoggingProvider.Entries)
        {
            AssertNoText(entry, lemmaText);
            AssertNoText(entry, stemText);
            AssertNoText(entry, SearchText);
        }
    }

    public static TheoryData<GetLemmasPageQuery, string, string[]> LemmasPageWarningCases => new()
    {
        {
            new GetLemmasPageQuery(SearchText, "bogus", 1, 50), "invalidSort",
            ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"]
        },
        {
            new GetLemmasPageQuery(SearchText, null, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "sort", "pageNumber", "pageSize", "hasSearch"]
        },
    };

    public static TheoryData<GetLemmaSummaryQuery, LogLevel, string[]> LemmaSummaryWarningCases => new()
    {
        { new GetLemmaSummaryQuery(0), LogLevel.Warning, ["feature", "operation", "reason", "lemmaId"] },
        { new GetLemmaSummaryQuery(UnknownId), LogLevel.Warning, ["feature", "operation", "lemmaId"] },
    };

    public static TheoryData<GetLemmaAyahsQuery, string?, string[]> LemmaAyahsWarningCases => new()
    {
        {
            new GetLemmaAyahsQuery(0, 1, 50), "invalidId",
            ["feature", "operation", "reason", "view", "lemmaId", "pageNumber", "pageSize"]
        },
        {
            new GetLemmaAyahsQuery(LemmaId, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "view", "lemmaId", "pageNumber", "pageSize"]
        },
        {
            new GetLemmaAyahsQuery(UnknownId, 1, 50), null,
            ["feature", "operation", "view", "lemmaId", "pageNumber", "pageSize", "elapsedMs"]
        },
    };

    public static TheoryData<GetStemsPageQuery, string, string[]> StemsPageWarningCases => new()
    {
        {
            new GetStemsPageQuery(SearchText, "bogus", 1, 50), "invalidSort",
            ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"]
        },
        {
            new GetStemsPageQuery(SearchText, null, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "sort", "pageNumber", "pageSize", "hasSearch"]
        },
    };

    public static TheoryData<GetStemSummaryQuery, LogLevel, string[]> StemSummaryWarningCases => new()
    {
        { new GetStemSummaryQuery(0), LogLevel.Warning, ["feature", "operation", "reason", "stemId"] },
        { new GetStemSummaryQuery(UnknownId), LogLevel.Warning, ["feature", "operation", "stemId"] },
    };

    public static TheoryData<GetStemAyahsQuery, string?, string[]> StemAyahsWarningCases => new()
    {
        {
            new GetStemAyahsQuery(0, 1, 50, null), "invalidId",
            ["feature", "operation", "reason", "view", "stemId", "pageNumber", "pageSize"]
        },
        {
            new GetStemAyahsQuery(StemId, 0, 50, null), "invalidPaging",
            ["feature", "operation", "reason", "view", "stemId", "pageNumber", "pageSize"]
        },
        {
            new GetStemAyahsQuery(UnknownId, 1, 50, null), null,
            ["feature", "operation", "view", "stemId", "pageNumber", "pageSize", "elapsedMs"]
        },
    };

    private RecordingLoggerProvider.LogEntry SingleEntryFor<THandler>(LogLevel expectedLevel)
    {
        var categoryName = typeof(THandler).FullName;
        var entry = fixture.LoggingProvider.Entries
            .Where(e => e.CategoryName == categoryName)
            .Should()
            .ContainSingle()
            .Subject;
        entry.Level.Should().Be(expectedLevel);
        return entry;
    }

    private static void AssertNoText(RecordingLoggerProvider.LogEntry entry, string text)
    {
        entry.Message.Should().NotContain(text);
        entry.StructuredFieldsWithoutOriginalFormat()
            .Select(pair => pair.Value?.ToString())
            .Should()
            .NotContain(text);
    }
}
