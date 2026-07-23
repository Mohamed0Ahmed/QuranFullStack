using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Tree;

[Collection(nameof(AbwabDbCollection))]
public sealed class CategorySearchTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_MatchesEveryCorpusCase_ViaTheSharedNormalizer()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار البحث");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var representativesByExpected = new Dictionary<string, Category>();
        var order = 0;
        foreach (var testCase in NormalizationCorpus.Cases)
        {
            if (representativesByExpected.ContainsKey(testCase.Expected))
            {
                continue;
            }

            representativesByExpected[testCase.Expected] =
                AbwabTreeSeeding.NewRootCategory(testCase.Input, section.SectionId, sectionOrder: order, globalOrder: order);
            order++;
        }

        await AbwabTreeSeeding.InsertAsync(fixture, representativesByExpected.Values.ToArray());

        foreach (var testCase in NormalizationCorpus.Cases)
        {
            var expectedCategoryId = representativesByExpected[testCase.Expected].CategoryId;

            var result = await SearchAsync(testCase.Input);

            result.Matches.Should().Contain(
                m => m.CategoryId == expectedCategoryId,
                "corpus case {0} normalizes to '{1}' and must match its category via the shared §5.1 normalizer",
                testCase.Name,
                testCase.Expected);
        }
    }

    [Fact]
    public async Task Search_MatchesByAlias()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار البحث بالمرادف");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب الأصلي", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);
        await AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewAlias(root.CategoryId, "المرادف المستعار"));

        var result = await SearchAsync("المرادف المستعار");

        result.Matches.Should().Contain(m => m.CategoryId == root.CategoryId);
    }

    [Fact]
    public async Task Search_DoesNotMatchByDescription()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار الوصف");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب بلا صلة", section.SectionId, sectionOrder: 0, globalOrder: 0);
        root.Description = "نص وصفي يحتوي على كلمة فريدة للاختبار";
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var result = await SearchAsync("كلمة فريدة للاختبار");

        result.Matches.Should().NotContain(m => m.CategoryId == root.CategoryId);
    }

    [Fact]
    public async Task Search_WithBlankQuery_ReturnsNoMatches()
    {
        var result = await SearchAsync("   ");

        result.Matches.Should().BeEmpty();
    }

    private async Task<CategorySearchResultDto> SearchAsync(string query)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var port = new EfAbwabCoreReadPort(context, new FixedServerClock(DateTimeOffset.UnixEpoch));

        return await port.SearchCategoriesAsync(query, CancellationToken.None);
    }
}
