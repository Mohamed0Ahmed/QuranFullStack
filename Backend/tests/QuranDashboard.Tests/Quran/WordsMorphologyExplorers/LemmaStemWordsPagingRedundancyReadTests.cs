using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmaStemWordsPagingRedundancyReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyLemmaId = 500;
    private const int HighFrequencyStemId = 600;

    private const int SegmentFanoutStemId = 606;

    [Theory]
    [InlineData(LemmaWordKind.Simple)]
    [InlineData(LemmaWordKind.Tashkeel)]
    public async Task Lemma_words_second_distinct_page_issues_no_new_db_commands_after_first_page(
        LemmaWordKind kind)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(new EfLemmasReader(dbContext), cache);

        var firstPage = await reader.GetLemmaWordsAsync(HighFrequencyLemmaId, kind, 1, 1, CancellationToken.None);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().ContainSingle();
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first page must reach the database");

        interceptor.Reset();

        var secondPage = await reader.GetLemmaWordsAsync(HighFrequencyLemmaId, kind, 2, 1, CancellationToken.None);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().ContainSingle();
        interceptor.CommandCount.Should().Be(
            0,
            "a distinct page for the same lemma/kind identity must slice the cached grouped list, not re-issue the full occurrence query");
    }

    [Theory]
    [InlineData(StemWordKind.Simple)]
    [InlineData(StemWordKind.Tashkeel)]
    public async Task Stem_words_second_distinct_page_issues_no_new_db_commands_after_first_page(
        StemWordKind kind)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(new EfStemsReader(dbContext), cache);

        var firstPage = await reader.GetStemWordsAsync(HighFrequencyStemId, kind, 1, 1, CancellationToken.None);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().ContainSingle();
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first page must reach the database");

        interceptor.Reset();

        var secondPage = await reader.GetStemWordsAsync(HighFrequencyStemId, kind, 2, 1, CancellationToken.None);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().ContainSingle();
        interceptor.CommandCount.Should().Be(
            0,
            "a distinct page for the same stem/kind identity must slice the cached grouped list, not re-issue the full occurrence query");
    }

    [Theory]
    [InlineData(LemmaWordKind.Simple)]
    [InlineData(LemmaWordKind.Tashkeel)]
    public async Task Lemma_words_paged_contents_and_total_count_match_the_pre_fix_algorithm_across_pages(
        LemmaWordKind kind)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var reader = scope.ServiceProvider.GetRequiredService<ILemmasReader>();

        for (var page = 1; page <= 3; page++)
        {
            var expected = await LoadLemmaWordsPageViaPreFixAlgorithmAsync(
                db, HighFrequencyLemmaId, kind == LemmaWordKind.Simple, page, pageSize: 1, CancellationToken.None);

            var actual = await reader.GetLemmaWordsAsync(HighFrequencyLemmaId, kind, page, 1, CancellationToken.None);

            actual.Should().NotBeNull();
            actual!.TotalCount.Should().Be(expected.TotalCount, $"page {page} TotalCount must stay byte-identical");
            actual.Items.Should().BeEquivalentTo(
                expected.Items,
                options => options.WithStrictOrdering(),
                $"page {page} contents must stay byte-identical to the pre-fix full-load/group/skip-take algorithm");
        }
    }

    [Theory]
    [InlineData(StemWordKind.Simple)]
    [InlineData(StemWordKind.Tashkeel)]
    public async Task Stem_words_paged_contents_and_total_count_match_the_pre_fix_algorithm_across_pages(
        StemWordKind kind)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var reader = scope.ServiceProvider.GetRequiredService<IStemsReader>();

        for (var page = 1; page <= 4; page++)
        {
            var expected = await LoadStemWordsPageViaPreFixAlgorithmAsync(
                db, SegmentFanoutStemId, kind == StemWordKind.Simple, page, pageSize: 1, CancellationToken.None);

            var actual = await reader.GetStemWordsAsync(SegmentFanoutStemId, kind, page, 1, CancellationToken.None);

            actual.Should().NotBeNull();
            actual!.TotalCount.Should().Be(expected.TotalCount, $"page {page} TotalCount must stay byte-identical");
            actual.Items.Should().BeEquivalentTo(
                expected.Items,
                options => options.WithStrictOrdering(),
                $"page {page} contents must stay byte-identical to the pre-fix full-load/group/skip-take algorithm");
        }
    }

    private static async Task<PagedResult<LemmaWordItemDto>> LoadLemmaWordsPageViaPreFixAlgorithmAsync(
        QuranDashboardDbContext db,
        int id,
        bool useSimpleWordIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = useSimpleWordIds
            ? await (
                from s in db.WordMorphologySegments.AsNoTracking()
                join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.LemmaId == id && !w.IsAyahMarker
                select new WordOccurrenceRow(
                    w.UniqueSimpleWordId, w.TextUthmaniSimple, w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id))
                .ToListAsync(cancellationToken)
            : await (
                from s in db.WordMorphologySegments.AsNoTracking()
                join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.LemmaId == id && !w.IsAyahMarker
                select new WordOccurrenceRow(
                    w.UniqueTashkeelWordId, w.TextUthmani, w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id))
                .ToListAsync(cancellationToken);

        var grouped = GroupAndOrderWordRows(rows);
        return SlicePage(grouped, page, pageSize, (uniqueWordId, display, count) =>
            new LemmaWordItemDto(uniqueWordId, display, count));
    }

    private static async Task<PagedResult<StemWordItemDto>> LoadStemWordsPageViaPreFixAlgorithmAsync(
        QuranDashboardDbContext db,
        int id,
        bool useSimpleWordIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = useSimpleWordIds
            ? await (
                from s in db.WordMorphologySegments.AsNoTracking()
                join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
                select new WordOccurrenceRow(
                    w.UniqueSimpleWordId, w.TextUthmaniSimple, w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id))
                .ToListAsync(cancellationToken)
            : await (
                from s in db.WordMorphologySegments.AsNoTracking()
                join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
                select new WordOccurrenceRow(
                    w.UniqueTashkeelWordId, w.TextUthmani, w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id))
                .ToListAsync(cancellationToken);

        var grouped = GroupAndOrderWordRows(rows);
        return SlicePage(grouped, page, pageSize, (uniqueWordId, display, count) =>
            new StemWordItemDto(uniqueWordId, display, count));
    }

    private static List<WordGroupRow> GroupAndOrderWordRows(IReadOnlyList<WordOccurrenceRow> rows) =>
        rows
            .Where(r => r.UniqueWordId.HasValue)
            .GroupBy(r => r.UniqueWordId!.Value)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .ThenBy(x => x.QuranWordId)
                    .First();

                return new WordGroupRow(g.Key, first.DisplayText, g.Count(), first.SurahNumber, first.AyahNumber, first.WordNumber);
            })
            .OrderBy(x => x.FirstSurahNumber)
            .ThenBy(x => x.FirstAyahNumber)
            .ThenBy(x => x.FirstWordNumber)
            .ThenBy(x => x.UniqueWordId)
            .ToList();

    private static PagedResult<TDto> SlicePage<TDto>(
        List<WordGroupRow> grouped,
        int page,
        int pageSize,
        Func<int, string, int, TDto> toDto)
    {
        var skip = ((long)page - 1) * pageSize;
        if (skip >= grouped.Count || skip > int.MaxValue)
        {
            return new PagedResult<TDto>(page, pageSize, grouped.Count, []);
        }

        var items = grouped
            .Skip((int)skip)
            .Take(pageSize)
            .Select(row => toDto(row.UniqueWordId, row.DisplayText, row.OccurrencesCount))
            .ToList();

        return new PagedResult<TDto>(page, pageSize, grouped.Count, items);
    }

    private sealed record WordOccurrenceRow(
        int? UniqueWordId,
        string DisplayText,
        int SurahNumber,
        int AyahNumber,
        int WordNumber,
        int QuranWordId);

    private sealed record WordGroupRow(
        int UniqueWordId,
        string DisplayText,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);
}
