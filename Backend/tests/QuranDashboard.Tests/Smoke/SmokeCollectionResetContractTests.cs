using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// What makes the ResetPerTest policy the SmokeCollection row of TestSupport/Execution/test-resources.tsv
// declares a checkable claim rather than a label: SmokeApiFixture.ResetAsync is the collection's single
// restore, and every table it names is dirtied here and read back empty. Two cases rather than one, each
// leaving rows behind in all seven tables, so the pair fails the moment the restore stops happening
// before every case instead of once.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeCollectionResetContractTests(SmokeApiFixture fixture)
{
    [Theory]
    [InlineData("الأولى")]
    [InlineData("الثانية")]
    public async Task ResetAsync_EmptiesEveryTableTheCollectionMutates_WhicheverCaseRanBefore(string caseName)
    {
        await fixture.ResetAsync();

        (await CountMutatedTablesAsync()).Should().OnlyContain(table => table.Value == 0);

        await DirtyEveryMutatedTableAsync(caseName);

        (await CountMutatedTablesAsync()).Should().OnlyContain(table => table.Value > 0);
    }

    // The half of the restore no TRUNCATE can perform: CachedAbwabTreeReader serves its last tree while
    // AbwabCacheGeneration's counter stands still, and raw SQL never moves that counter. Without the
    // invalidation inside ResetAsync the second read below answers from memory with the deleted section
    // still in it.
    [Fact]
    public async Task ResetAsync_InvalidatesTheAbwabReadCaches_SoATruncatedTreeIsNeverServedFromMemory()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient();
        await CreateSectionAsync("عقد الضبط: قسم المخزون المؤقت");

        using (var populated = await client.GetAsync("/api/abwab/tree"))
        {
            (await ApiEnvelope.ReadDataAsync(populated)).GetProperty("sections").GetArrayLength()
                .Should().Be(1);
        }

        await fixture.ResetAsync();

        using var restored = await client.GetAsync("/api/abwab/tree");
        (await ApiEnvelope.ReadDataAsync(restored)).GetProperty("sections").GetArrayLength()
            .Should().Be(0);
    }

    private async Task DirtyEveryMutatedTableAsync(string caseName)
    {
        using var client = fixture.CreateClientFor(SmokePersona.AuthenticatedUnknown);
        using var provisioned = await client.GetAsync("/api/access/me");
        provisioned.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var relations = scope.ServiceProvider.GetRequiredService<IAbwabRelationsWriter>();
        var templates = scope.ServiceProvider.GetRequiredService<IAbwabTemplatesWriter>();

        var sectionId = await CreateSectionAsync($"عقد الضبط: قسم {caseName}");
        var anchor = await doors.CreateAsync(
            sectionId,
            null,
            $"عقد الضبط: باب {caseName}",
            null,
            null,
            [$"عقد الضبط: كنية {caseName}"],
            CancellationToken.None);
        var target = await doors.CreateAsync(
            sectionId,
            null,
            $"عقد الضبط: باب مقابل {caseName}",
            null,
            null,
            [],
            CancellationToken.None);

        await relations.AddAsync(
            anchor.Id, AbwabRelationType.Similarity, null, [target.Id], CancellationToken.None);

        // A created template carries its own root node, so this one call reaches both template tables.
        await templates.CreateAsync($"عقد الضبط: قالب {caseName}", null, null, [], CancellationToken.None);
    }

    private async Task<int> CreateSectionAsync(string name)
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        return (await sections.CreateAsync(name, CancellationToken.None)).Id;
    }

    private async Task<IReadOnlyDictionary<string, int>> CountMutatedTablesAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new Dictionary<string, int>
        {
            ["users"] = await db.AccessUsers.CountAsync(),
            ["abwab_sections"] = await db.AbwabSections.CountAsync(),
            ["abwab_doors"] = await db.AbwabDoors.CountAsync(),
            ["abwab_door_aliases"] = await db.AbwabDoorAliases.CountAsync(),
            ["abwab_door_relations"] = await db.AbwabDoorRelations.CountAsync(),
            ["abwab_templates"] = await db.AbwabTemplates.CountAsync(),
            ["abwab_template_nodes"] = await db.AbwabTemplateNodes.CountAsync(),
        };
    }
}
