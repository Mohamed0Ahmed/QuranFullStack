using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyDimensionTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Dimensions_are_deduplicated_on_arabic_text()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var roots = await dbContext.QuranRoots.AsNoTracking().ToListAsync();
        var rootTexts = roots.Select(r => r.RootText).ToList();
        rootTexts.Distinct().Should().HaveCount(rootTexts.Count, "roots should be unique by Arabic text");
    }

    [Fact]
    public async Task Null_segment_buckwalter_values_have_null_segment_dimension_links()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var row12 = await dbContext.WordMorphologies
            .AsNoTracking()
            .FirstAsync(m => m.Location == "1:1:2");

        row12.RootId.Should().BeNull("word 1:1:2 has no QUL Arabic root");
        row12.LemmaId.Should().BeNull("word 1:1:2 has no QUL Arabic lemma");

        var segment = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .FirstAsync(s => s.QuranWordId == row12.QuranWordId && s.Kind == "STEM");

        segment.RootBuckwalter.Should().BeNull();
        segment.LemmaBuckwalter.Should().BeNull();
        segment.RootId.Should().BeNull();
        segment.LemmaId.Should().BeNull();
    }

    [Fact]
    public async Task Words_count_and_first_word_order_are_correct()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var roots = await dbContext.QuranRoots.AsNoTracking().ToListAsync();
        roots.Should().OnlyContain(r => r.FirstWordOrderInMushaf > 0);

        var sharedRoot = roots.Single(r => r.RootText == MorphologySyntheticSeed.RootValue3);
        sharedRoot.WordsCount.Should().Be(2);
        sharedRoot.DistinctLemmasCount.Should().Be(2);

        var singleRoot = roots.Single(r => r.RootText == MorphologySyntheticSeed.RootValue2);
        singleRoot.WordsCount.Should().Be(1);
    }

    [Fact]
    public async Task Lemma_root_id_links_to_co_occurring_root()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sharedRoot = await dbContext.QuranRoots.AsNoTracking()
            .FirstAsync(r => r.RootText == MorphologySyntheticSeed.RootValue3);

        var sharedRootLemmas = await dbContext.QuranLemmas.AsNoTracking()
            .Where(l => l.RootId == sharedRoot.Id)
            .ToListAsync();

        sharedRootLemmas.Should().HaveCount(2);
        sharedRootLemmas.Select(l => l.LemmaText).Should().Contain(MorphologySyntheticSeed.LemmaValue3);
    }

    [Fact]
    public async Task No_dangling_dimension_references()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var danglingRoots = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            WHERE m.root_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_roots r WHERE r.id = m.root_id)
            """).FirstAsync();

        var danglingLemmas = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            WHERE m.lemma_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_lemmas l WHERE l.id = m.lemma_id)
            """).FirstAsync();

        var danglingStems = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology m
            WHERE m.stem_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_stems s WHERE s.id = m.stem_id)
            """).FirstAsync();

        danglingRoots.Should().Be(0);
        danglingLemmas.Should().Be(0);
        danglingStems.Should().Be(0);
    }

    [Fact]
    public async Task Segment_dimension_ids_resolve_without_dangling_references()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var danglingSegmentRoots = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments s
            WHERE s.root_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_roots r WHERE r.id = s.root_id)
            """).FirstAsync();

        var danglingSegmentLemmas = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments s
            WHERE s.lemma_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_lemmas l WHERE l.id = s.lemma_id)
            """).FirstAsync();

        danglingSegmentRoots.Should().Be(0);
        danglingSegmentLemmas.Should().Be(0);
    }

    [Fact]
    public async Task Segment_dimension_ids_follow_source_policy()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var nonStemLemmaIds = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .CountAsync(segment => segment.Kind != "STEM" && segment.LemmaId != null);
        var unresolvedRoots = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .CountAsync(segment => segment.RootBuckwalter != null && segment.RootId == null);
        var unresolvedStemLemmas = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .CountAsync(segment =>
                segment.Kind == "STEM"
                && segment.LemmaBuckwalter != null
                && segment.LemmaId == null);

        nonStemLemmaIds.Should().Be(0);
        unresolvedRoots.Should().Be(0);
        unresolvedStemLemmas.Should().Be(0);
    }

    [Fact]
    public async Task Shared_root_deduplicates_into_single_dimension_row()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var root121 = await dbContext.WordMorphologies.AsNoTracking()
            .FirstAsync(m => m.Location == "1:2:1");
        var root122 = await dbContext.WordMorphologies.AsNoTracking()
            .FirstAsync(m => m.Location == "1:2:2");

        root121.RootId.Should().NotBeNull();
        root122.RootId.Should().NotBeNull();
        root121.RootId.Should().Be(root122.RootId, "words sharing the same QUL root should reference the same dimension row");
    }

    [Fact]
    public async Task Segment_stem_ids_follow_source_policy()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var nonStemWithStemId = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .CountAsync(segment => segment.Kind != "STEM" && segment.StemId != null);
        nonStemWithStemId.Should().Be(0, "non-STEM segments never carry stem_id");

        var stemSegments = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .Where(segment => segment.Kind == "STEM")
            .ToListAsync();
        var headStemByWord = await dbContext.WordMorphologies
            .AsNoTracking()
            .ToDictionaryAsync(m => m.QuranWordId, m => m.StemId);

        stemSegments.Should().NotBeEmpty();
        stemSegments.Should().OnlyContain(segment => segment.StemId != null,
            "the synthetic source is all single-STEM words, which reuse the word head stem");
        stemSegments.Should().OnlyContain(segment => segment.StemId == headStemByWord[segment.QuranWordId]);

        var danglingSegmentStems = await dbContext.Database.SqlQueryRaw<int>(
            """
            SELECT count(*)::int AS "Value"
            FROM quran_word_morphology_segments s
            WHERE s.stem_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM quran_stems st WHERE st.id = s.stem_id)
            """).FirstAsync();
        danglingSegmentStems.Should().Be(0);
    }

    [Fact]
    public async Task Dimension_resolves_check_passes()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: readableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);
        result.Totals!.RootRows.Should().BeGreaterThan(0);
        result.Totals!.LemmaRows.Should().BeGreaterThan(0);
        result.Totals!.StemRows.Should().BeGreaterThan(0);
    }
}
