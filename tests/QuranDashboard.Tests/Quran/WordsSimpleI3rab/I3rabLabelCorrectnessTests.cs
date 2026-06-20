using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabLabelCorrectnessTests(I3rabGenerationTestFixture fixture)
{
    // Seed labels the rule layer deliberately replaced (coverage report §7). None must ever surface
    // on a committed segment — the catalogue owns the user-facing Arabic, not quran_pos_tags.
    private static readonly string[] KnownWrongSeedLabels =
    [
        "تاء تأنيث", // T seed label — corrected to ظرف زمان
        "قسم",        // INL seed label — corrected to حروف مقطّعة
        "حرف استثناء" // REM seed label — corrected to حرف استئناف
    ];

    [Fact]
    public async Task Every_committed_label_equals_its_rule_and_no_known_wrong_seed_value_appears()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var result = await fixture.RunGenerationAsync(I3rabGenerationTestFixture.CompleteMorphologyCounts);
        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var pairs = await dbContext.WordMorphologySegments.AsNoTracking()
            .Join(
                dbContext.QuranI3rabRules.AsNoTracking(),
                segment => segment.I3rabRuleId,
                rule => rule.Id,
                (segment, rule) => new { segment.Id, segment.I3rabArabic, RuleArabic = rule.I3rabArabic })
            .ToListAsync();

        // The inner join keeps only resolvable rule ids, so matching the total segment count proves
        // i3rab_rule_id always resolves (0 dangling).
        var totalSegments = await dbContext.WordMorphologySegments.AsNoTracking().CountAsync();
        pairs.Should().HaveCount(totalSegments);

        // Each committed label is exactly its joined rule's label — the rule layer is the single owner.
        pairs.Should().OnlyContain(pair => pair.I3rabArabic == pair.RuleArabic);

        var labels = await dbContext.WordMorphologySegments.AsNoTracking()
            .Select(segment => segment.I3rabArabic)
            .ToListAsync();
        labels.Should().NotIntersectWith(KnownWrongSeedLabels);
    }
}
