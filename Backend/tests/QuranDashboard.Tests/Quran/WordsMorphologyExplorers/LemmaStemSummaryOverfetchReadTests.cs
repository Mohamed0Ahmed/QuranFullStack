using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

// B2 performance-review regression: the Lemma and Stem cold catalogues must build their type
// distributions and dominant lemma/root winners from SERVER-SIDE grouped (summary-grain) rows instead of
// transferring every matching morphology occurrence into memory. Every assertion pins byte-identical
// output — winners, per-POS distribution, first-occurrence coordinate, and counts — against an
// independent occurrence-grain oracle that reproduces the previous algorithm, so the refactor cannot
// silently change any lemma or stem.
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmaStemSummaryOverfetchReadTests(MorphologyExplorersTestFixture fixture)
{
    // ---- Lemmas ------------------------------------------------------------------------------------

    // Byte-identical guard: for every seeded lemma the reader's type distribution equals the occurrence-
    // grain oracle exactly — same POS set, per-POS counts, first-occurrence coordinate, and order.
    [Fact]
    public async Task LemmaSummary_TypeDistribution_MatchesOccurrenceGrainOracle()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var occurrencesByLemma = (await LemmaOccurrenceGrainQuery(db).ToListAsync())
            .GroupBy(o => o.LemmaId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<LemmaOccurrence>)g.ToList());

        var rows = await new EfLemmasReader(db).LoadWholeSummaryAsync(CancellationToken.None);

        rows.Should().NotBeEmpty();
        foreach (var row in rows)
        {
            var expected = LemmaDistributionOracle(occurrencesByLemma.GetValueOrDefault(row.Id, []));
            row.TypeDistribution
                .Select(t => new DistEntry(t.Code, t.ArabicLabel, t.EnglishLabel, t.OccurrencesCount, t.FirstSurahNumber, t.FirstAyahNumber, t.FirstWordNumber))
                .Should().Equal(expected, "lemma {0} distribution must stay byte-identical to the occurrence-grain result", row.Id);
        }
    }

    // The cold whole-summary load must no longer materialize the occurrence-grain rows: no single command
    // reads more rows than the (now grouped) second query would.
    [Fact]
    public async Task LemmaSummary_ColdLoad_DoesNotMaterializeOccurrenceGrain()
    {
        int occurrenceGrain, distinctGroups;
        await using (var probe = fixture.CreateScope())
        {
            var db = probe.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var occurrences = await LemmaOccurrenceGrainQuery(db).ToListAsync();
            occurrenceGrain = occurrences.Count;
            distinctGroups = occurrences.Select(o => (o.LemmaId, o.Pos)).Distinct().Count();
        }

        var interceptor = new RowMaterializationInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString).AddInterceptors(interceptor).Options;
        await using var intercepted = new QuranDashboardDbContext(options);

        interceptor.Reset();
        var rows = await new EfLemmasReader(intercepted).LoadWholeSummaryAsync(CancellationToken.None);

        occurrenceGrain.Should().BeGreaterThan(distinctGroups, "the seed must exercise per-(lemma,POS) fan-out or this test proves nothing");
        interceptor.MaxReadCount.Should().BeLessThan(occurrenceGrain, "the type distribution is grouped server-side, so no command transfers occurrence-grain rows");
        rows.Should().NotBeEmpty();
    }

    // ---- Stems -------------------------------------------------------------------------------------

    // Byte-identical guard: for every seeded stem the reader's dominant lemma, dominant root, and full
    // type distribution equal the occurrence-grain oracle exactly.
    [Fact]
    public async Task StemSummary_DominantsAndDistribution_MatchOccurrenceGrainOracle()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var occurrencesByStem = (await StemOccurrenceGrainQuery(db).ToListAsync())
            .GroupBy(o => o.StemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<StemOccurrence>)g.ToList());

        var rows = await new EfStemsReader(db).LoadWholeSummaryAsync(CancellationToken.None);

        rows.Should().NotBeEmpty();
        foreach (var row in rows)
        {
            var occ = occurrencesByStem.GetValueOrDefault(row.Id, []);
            var expectedLemma = StemDominantOracle(occ, o => o.LemmaId, o => o.LemmaText, o => o.LemmaBuckwalter);
            var expectedRoot = StemDominantOracle(occ, o => o.RootId, o => o.RootText, o => o.RootBuckwalter);

            row.DominantLemmaId.Should().Be(expectedLemma.Id, "stem {0} dominant lemma id", row.Id);
            row.DominantLemmaText.Should().Be(expectedLemma.Text, "stem {0} dominant lemma text", row.Id);
            row.DominantLemmaBuckwalter.Should().Be(expectedLemma.Buckwalter, "stem {0} dominant lemma buckwalter", row.Id);
            row.DominantRootId.Should().Be(expectedRoot.Id, "stem {0} dominant root id", row.Id);
            row.DominantRootText.Should().Be(expectedRoot.Text, "stem {0} dominant root text", row.Id);
            row.DominantRootBuckwalter.Should().Be(expectedRoot.Buckwalter, "stem {0} dominant root buckwalter", row.Id);

            row.TypeDistribution
                .Select(t => new DistEntry(t.Code, t.ArabicLabel, t.EnglishLabel, t.OccurrencesCount, t.FirstSurahNumber, t.FirstAyahNumber, t.FirstWordNumber))
                .Should().Equal(StemDistributionOracle(occ), "stem {0} distribution must stay byte-identical", row.Id);
        }
    }

    [Fact]
    public async Task StemSummary_ColdLoad_DoesNotMaterializeOccurrenceGrain()
    {
        int occurrenceGrain, distinctPosGroups;
        await using (var probe = fixture.CreateScope())
        {
            var db = probe.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var occurrences = await StemOccurrenceGrainQuery(db).ToListAsync();
            occurrenceGrain = occurrences.Count;
            distinctPosGroups = occurrences.Select(o => (o.StemId, o.Pos)).Distinct().Count();
        }

        var interceptor = new RowMaterializationInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString).AddInterceptors(interceptor).Options;
        await using var intercepted = new QuranDashboardDbContext(options);

        interceptor.Reset();
        var rows = await new EfStemsReader(intercepted).LoadWholeSummaryAsync(CancellationToken.None);

        occurrenceGrain.Should().BeGreaterThan(distinctPosGroups, "the seed must exercise per-(stem,POS) fan-out or this test proves nothing");
        interceptor.MaxReadCount.Should().BeLessThan(occurrenceGrain, "distribution/winner inputs are grouped server-side, so no command transfers occurrence-grain rows");
        rows.Should().NotBeEmpty();
    }

    // The catalogue set is unchanged: one summary row per catalogue entry (seed totals; the aggregate's
    // FROM quran_lemmas / quran_stems is untouched, so the real-DB baselines 4,817 / 11,843 hold too).
    [Fact]
    public async Task Summary_CatalogueCounts_PreserveSeedTotals()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var lemmas = await new EfLemmasReader(db).LoadWholeSummaryAsync(CancellationToken.None);
        var stems = await new EfStemsReader(db).LoadWholeSummaryAsync(CancellationToken.None);

        lemmas.Should().HaveCount(await db.QuranLemmas.CountAsync());
        stems.Should().HaveCount(await db.QuranStems.CountAsync());
        lemmas.Should().HaveCount(9);
        stems.Should().HaveCount(6);
    }

    // ---- Occurrence-grain oracle (independent reimplementation of the pre-B2 in-memory algorithm) -----

    private static IQueryable<LemmaOccurrence> LemmaOccurrenceGrainQuery(QuranDashboardDbContext db) =>
        from s in db.WordMorphologySegments.AsNoTracking()
        join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
        join t in db.PosTags.AsNoTracking() on s.Pos equals t.Code
        where s.LemmaId != null
        select new LemmaOccurrence(
            s.LemmaId!.Value, s.Pos, s.QuranWordId, w.SurahNumber, w.AyahNumber, w.WordNumber, t.ArabicLabel, t.EnglishLabel);

    private static IReadOnlyList<DistEntry> LemmaDistributionOracle(IReadOnlyList<LemmaOccurrence> occ) =>
        occ.GroupBy(o => o.Pos)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.QuranWordId).First();
                return new DistEntry(g.Key, first.ArabicLabel, first.EnglishLabel, g.Count(), first.SurahNumber, first.AyahNumber, first.WordNumber);
            })
            .OrderByDescending(d => d.OccurrencesCount)
            .ThenBy(d => d.FirstSurahNumber)
            .ThenBy(d => d.FirstAyahNumber)
            .ThenBy(d => d.FirstWordNumber)
            .ThenBy(d => d.Code, StringComparer.Ordinal)
            .ToList();

    private static IQueryable<StemOccurrence> StemOccurrenceGrainQuery(QuranDashboardDbContext db) =>
        from s in db.WordMorphologySegments.AsNoTracking()
        join w in db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
        join t in db.PosTags.AsNoTracking() on s.Pos equals t.Code
        join l in db.QuranLemmas.AsNoTracking() on s.LemmaId equals l.Id into lj
        from l in lj.DefaultIfEmpty()
        join r in db.QuranRoots.AsNoTracking() on s.RootId equals r.Id into rj
        from r in rj.DefaultIfEmpty()
        where s.Kind == "STEM" && s.StemId != null && !w.IsAyahMarker
        select new StemOccurrence(
            s.StemId!.Value, s.QuranWordId,
            s.LemmaId, l == null ? null : l.LemmaText, l == null ? null : l.LemmaBuckwalter,
            s.RootId, r == null ? null : r.RootText, r == null ? null : r.RootBuckwalter,
            s.Pos, t.ArabicLabel, t.EnglishLabel, w.SurahNumber, w.AyahNumber, w.WordNumber);

    private static IReadOnlyList<DistEntry> StemDistributionOracle(IReadOnlyList<StemOccurrence> occ) =>
        occ.GroupBy(o => o.Pos)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.SurahNumber).ThenBy(x => x.AyahNumber).ThenBy(x => x.WordNumber).ThenBy(x => x.QuranWordId).First();
                return new DistEntry(g.Key, first.ArabicLabel, first.EnglishLabel, g.Count(), first.SurahNumber, first.AyahNumber, first.WordNumber);
            })
            .OrderByDescending(d => d.OccurrencesCount)
            .ThenBy(d => d.FirstSurahNumber)
            .ThenBy(d => d.FirstAyahNumber)
            .ThenBy(d => d.FirstWordNumber)
            .ThenBy(d => d.Code, StringComparer.Ordinal)
            .ToList();

    private static (int? Id, string? Text, string? Buckwalter) StemDominantOracle(
        IReadOnlyList<StemOccurrence> occ,
        Func<StemOccurrence, int?> idSelector,
        Func<StemOccurrence, string?> textSelector,
        Func<StemOccurrence, string?> buckwalterSelector)
    {
        var winner = occ
            .Where(o => idSelector(o).HasValue)
            .GroupBy(o => idSelector(o)!.Value)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.SurahNumber).ThenBy(x => x.AyahNumber).ThenBy(x => x.WordNumber).ThenBy(x => x.QuranWordId).First();
                return new
                {
                    Id = g.Key,
                    Text = textSelector(first) ?? string.Empty,
                    Buckwalter = buckwalterSelector(first),
                    Count = g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber,
                };
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.SurahNumber)
            .ThenBy(x => x.AyahNumber)
            .ThenBy(x => x.WordNumber)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return winner is null ? (null, null, null) : (winner.Id, winner.Text, winner.Buckwalter);
    }

    private sealed record DistEntry(string Code, string ArabicLabel, string EnglishLabel, int OccurrencesCount, int FirstSurahNumber, int FirstAyahNumber, int FirstWordNumber);

    private sealed record LemmaOccurrence(int LemmaId, string Pos, int QuranWordId, int SurahNumber, int AyahNumber, int WordNumber, string ArabicLabel, string EnglishLabel);

    private sealed record StemOccurrence(
        int StemId, int QuranWordId,
        int? LemmaId, string? LemmaText, string? LemmaBuckwalter,
        int? RootId, string? RootText, string? RootBuckwalter,
        string Pos, string ArabicLabel, string EnglishLabel, int SurahNumber, int AyahNumber, int WordNumber);
}
