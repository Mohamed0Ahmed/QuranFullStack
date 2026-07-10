using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabLabelCorrectnessTests(I3rabGenerationTestFixture fixture)
{

    private static readonly string[] KnownWrongSeedLabels =
    [
        "تاء تأنيث",
        "قسم",
        "حرف استثناء"
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

        var totalSegments = await dbContext.WordMorphologySegments.AsNoTracking().CountAsync();
        pairs.Should().HaveCount(totalSegments);

        pairs.Should().OnlyContain(pair => pair.I3rabArabic == pair.RuleArabic);

        var labels = await dbContext.WordMorphologySegments.AsNoTracking()
            .Select(segment => segment.I3rabArabic)
            .ToListAsync();
        labels.Should().NotIntersectWith(KnownWrongSeedLabels);
    }
}
