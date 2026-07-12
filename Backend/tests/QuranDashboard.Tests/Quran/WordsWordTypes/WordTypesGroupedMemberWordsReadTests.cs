using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedMemberWordsReadTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;
    private const int MuthalTashkeelId = 191008;
    private const int MarkerTashkeelId = 191007;

    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);
    private static readonly WordTypeFilter VerbScope = new("verb", null, null, null, null);

    // Member words are the Words table restricted to a single numeric head root/stem/lemma. Membership,
    // grouping, and every measure must match a numeric-ID-scoped baseline row-for-row. The baseline is
    // built from the same (unique_tashkeel_word_id, context_code) grouping the Words table uses, filtered
    // only by the numeric dimension column — never by any *_text/*Text display label.
    [Theory]
    [InlineData(WordTypeGroupedDimensionKind.Root, 190700)]
    [InlineData(WordTypeGroupedDimensionKind.Stem, 190600)]
    [InlineData(WordTypeGroupedDimensionKind.Lemma, 190500)]
    public async Task GroupedMemberWords_RootStemLemma_MatchNumericIdScopedWordsBaselineRowForRow(
        WordTypeGroupedDimensionKind kind,
        int dimensionId)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var baseline = await LoadNumericDimensionWordsBaselineAsync(db, kind, dimensionId, NounScope);
        var actual = await MembersAsync(reader, kind, dimensionId, NounScope);

        actual.Should().NotBeNull();
        actual!.TotalCount.Should().Be(baseline.Count);
        actual.Items
            .Select(item => (item.TashkeelWordId, item.ContextCode, item.OccurrencesCount, item.AyahsCount, item.SurahsCount))
            .Should()
            .Equal(baseline.Select(row => (row.TashkeelWordId, row.ContextCode, row.OccurrencesCount, row.AyahsCount, row.SurahsCount)));
    }

    // The same tashkeel word used as noun, proper noun, and adjective stays three separate member rows;
    // grouping is by (tashkeel, head_pos), never a single distinct-word collapse.
    [Fact]
    public async Task GroupedMemberWords_NounParent_PreservesHeadPosUsageSplit()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var members = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope);

        var kalimaRows = members!.Items.Where(item => item.TashkeelWordId == KalimaTashkeelId).ToList();
        kalimaRows.Should().HaveCount(3);
        kalimaRows.Select(item => item.ContextCode).Should().BeEquivalentTo(["N", "PN", "ADJ"]);
        kalimaRows.Should().OnlyContain(item => item.OccurrencesCount == 1);
    }

    // Verb tense contexts also stay split: root 190701 spans past/present/imperative; narrowing the base
    // by tense=past leaves only the past context, proving the filter re-scopes before grouping.
    [Fact]
    public async Task GroupedMemberWords_VerbParent_PreservesTenseUsageSplit()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var allTenses = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, VerbScope);
        allTenses!.Items.Select(item => item.ContextCode)
            .Should().BeEquivalentTo(["past", "present", "imperative"]);

        var pastOnly = await MembersAsync(
            reader,
            WordTypeGroupedDimensionKind.Root,
            190701,
            new WordTypeFilter("verb", null, null, "past", null));
        pastOnly!.Items.Should().ContainSingle().Which.ContextCode.Should().Be("past");
    }

    // An exact child code pins a single context without changing the grouping contract.
    [Fact]
    public async Task GroupedMemberWords_ExactChildPinsContextWithoutChangingGroupingContract()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var members = await MembersAsync(
            reader,
            WordTypeGroupedDimensionKind.Root,
            190700,
            new WordTypeFilter("noun", "PN", null, null, null));

        var single = members!.Items.Should().ContainSingle().Subject;
        single.TashkeelWordId.Should().Be(KalimaTashkeelId);
        single.ContextCode.Should().Be("PN");
    }

    // The four measures stay distinct: morphology occurrences, member word-context rows, distinct ayahs,
    // and distinct surahs are computed and reported separately (not aliases of one number).
    [Fact]
    public async Task GroupedMemberWords_ReportsOccurrenceRowAyahAndSurahMeasuresSeparately()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var summary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190701, VerbScope),
            CancellationToken.None);
        var members = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, VerbScope);

        summary!.OccurrencesCount.Should().Be(3);
        members!.TotalCount.Should().Be(3);
        members.Items.Sum(item => item.OccurrencesCount).Should().Be(3);
        summary.AyahsCount.Should().Be(2);
        summary.SurahsCount.Should().Be(2);
        summary.AyahsCount.Should().NotBe(summary.OccurrencesCount);
    }

    // Paging happens after word-context grouping: TotalCount is the grouped row count, each page slices
    // the ordered rows, and an out-of-range page is a non-null empty page (not a 404).
    [Fact]
    public async Task GroupedMemberWords_PaginatesAfterWordContextGrouping()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page1 = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 1, 1);
        var page2 = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 2, 1);
        var page3 = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 3, 1);
        var page4 = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 4, 1);

        foreach (var page in new[] { page1, page2, page3, page4 })
        {
            page!.TotalCount.Should().Be(3);
        }

        page1!.Items.Should().ContainSingle();
        page2!.Items.Should().ContainSingle();
        page3!.Items.Should().ContainSingle();
        page4!.Items.Should().BeEmpty();

        new[] { page1, page2, page3 }
            .Select(page => page!.Items.Single().ContextCode)
            .Should().OnlyHaveUniqueItems();
    }

    // Markers, null head dimensions, and segment-only dimensions never appear. The alternate IDs carried
    // only by a fixture segment (root 190701 / lemma 190502 / stem 190602) are absent from the head-grain
    // noun scope, and the real head members never include the marker or null-dimension words.
    [Fact]
    public async Task GroupedMemberWords_MarkersNullAndSegmentOnlyDimensionsNeverAppear()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        (await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, NounScope)).Should().BeNull();
        (await MembersAsync(reader, WordTypeGroupedDimensionKind.Lemma, 190502, NounScope)).Should().BeNull();
        (await MembersAsync(reader, WordTypeGroupedDimensionKind.Stem, 190602, NounScope)).Should().BeNull();

        var headMembers = await MembersAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope);
        headMembers!.Items.Should().NotContain(item => item.TashkeelWordId == MarkerTashkeelId);
        headMembers.Items.Should().NotContain(item => item.TashkeelWordId == MuthalTashkeelId);
    }

    [Fact]
    public async Task GroupedWordsHandler_InvalidPaging_ReturnsInvalidPaging()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeGroupedWordsHandler>();

        var zeroPage = await handler.HandleAsync(
            new GetWordTypeGroupedWordsQuery("roots", 190700, "noun", null, null, null, null, 0, 25),
            CancellationToken.None);
        var oversizePageSize = await handler.HandleAsync(
            new GetWordTypeGroupedWordsQuery("roots", 190700, "noun", null, null, null, null, 1, 101),
            CancellationToken.None);

        zeroPage.Should().BeOfType<GetWordTypeGroupedWordsOutcome.InvalidPaging>();
        oversizePageSize.Should().BeOfType<GetWordTypeGroupedWordsOutcome.InvalidPaging>();
    }

    private static Task<PagedResult<WordTypeGroupedMemberWordDto>?> MembersAsync(
        IWordTypesReader reader,
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        WordTypeFilter filter,
        int page = 1,
        int pageSize = 100) =>
        reader.GetGroupedMemberWordsAsync(
            new WordTypeGroupedSelection(kind, dimensionId, filter),
            page,
            pageSize,
            CancellationToken.None);

    // Independent numeric-ID-scoped Words baseline. Applies the same type/child/secondary scope as the
    // reader, filters by the allowlisted numeric dimension column ONLY, then groups by
    // (unique_tashkeel_word_id, context_code) exactly like the Words table. No display text participates.
    private static async Task<IReadOnlyList<MemberWordBaselineRow>> LoadNumericDimensionWordsBaselineAsync(
        QuranDashboardDbContext db,
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        WordTypeFilter filter)
    {
        var projected = await (
            from m in db.WordMorphologies.AsNoTracking()
            join w in db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            join pos in db.PosTags.AsNoTracking() on m.HeadPos equals pos.Code
            where !w.IsAyahMarker && w.UniqueTashkeelWordId != null
            select new
            {
                TashkeelWordId = w.UniqueTashkeelWordId!.Value,
                QuranWordId = w.Id,
                w.AyahId,
                SurahNumber = (int)w.SurahNumber,
                m.HeadPos,
                m.IsVerb,
                m.VerbTense,
                m.VerbVoice,
                m.CaseFeature,
                PosCategory = pos.Category,
                m.RootId,
                m.StemId,
                m.LemmaId,
            }).ToListAsync();

        var type = filter.Type;
        var source = projected.Select(r => new BaselineSourceRow(
            r.TashkeelWordId,
            r.QuranWordId,
            r.AyahId,
            r.SurahNumber,
            r.HeadPos,
            r.IsVerb,
            r.VerbTense,
            r.VerbVoice,
            r.CaseFeature,
            r.PosCategory,
            r.RootId,
            r.StemId,
            r.LemmaId));

        var scoped = type switch
        {
            "noun" => source.Where(r => r.PosCategory == "noun"),
            "verb" => source.Where(r => r.IsVerb),
            "particle" => source.Where(r => r.PosCategory == "particle" && r.HeadPos != "INL"),
            "inl" => source.Where(r => r.HeadPos == "INL"),
            _ => source,
        };

        if (!string.IsNullOrWhiteSpace(filter.ChildCode))
        {
            scoped = type == "verb"
                ? scoped.Where(r => (r.VerbTense ?? WordTypeRowContext.Unspecified) == filter.ChildCode)
                : scoped.Where(r => r.HeadPos == filter.ChildCode);
        }

        if (HasConcreteFilter(filter.Case))
        {
            scoped = filter.Case == "null"
                ? scoped.Where(r => r.CaseFeature == null)
                : scoped.Where(r => r.CaseFeature == filter.Case);
        }

        if (HasConcreteFilter(filter.Tense))
        {
            scoped = scoped.Where(r => r.VerbTense == filter.Tense);
        }

        if (HasConcreteFilter(filter.Voice))
        {
            scoped = scoped.Where(r => r.VerbVoice == filter.Voice);
        }

        scoped = kind switch
        {
            WordTypeGroupedDimensionKind.Root => scoped.Where(r => r.RootId == dimensionId),
            WordTypeGroupedDimensionKind.Stem => scoped.Where(r => r.StemId == dimensionId),
            WordTypeGroupedDimensionKind.Lemma => scoped.Where(r => r.LemmaId == dimensionId),
            _ => scoped,
        };

        return scoped
            .GroupBy(r => new
            {
                r.TashkeelWordId,
                Context = type == "verb" ? (r.VerbTense ?? WordTypeRowContext.Unspecified) : r.HeadPos,
            })
            .Select(g => new MemberWordBaselineRow(
                g.Key.TashkeelWordId,
                g.Key.Context,
                g.Count(),
                g.Select(x => x.AyahId).Distinct().Count(),
                g.Select(x => x.SurahNumber).Distinct().Count(),
                g.Min(x => x.QuranWordId)))
            .OrderByDescending(row => row.OccurrencesCount)
            .ThenBy(row => row.FirstWordOrder)
            .ThenBy(row => row.TashkeelWordId)
            .ThenBy(row => row.ContextCode, StringComparer.Ordinal)
            .ToList();
    }

    private static bool HasConcreteFilter(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value != "all";

    private sealed record BaselineSourceRow(
        int TashkeelWordId,
        int QuranWordId,
        int AyahId,
        int SurahNumber,
        string HeadPos,
        bool IsVerb,
        string? VerbTense,
        string? VerbVoice,
        string? CaseFeature,
        string PosCategory,
        int? RootId,
        int? StemId,
        int? LemmaId);

    private sealed record MemberWordBaselineRow(
        int TashkeelWordId,
        string ContextCode,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int FirstWordOrder);
}
