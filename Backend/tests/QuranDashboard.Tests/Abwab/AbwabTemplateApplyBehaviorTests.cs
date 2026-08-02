using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab;

// The apply writer copies a template subtree into live doors and had no tests at all
// (`docs/TESTING_DEBT.md` row 7). Only the section-inheritance obligation is paid here — the rest of
// row 7 (offsets, aliases, all-or-nothing, 409 collisions) stays open.
[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabTemplateApplyBehaviorTests(AbwabSchemaFixture fixture)
{
    // The `int` plumbing makes a section-LESS copy impossible to express, but not a copy carrying the
    // WRONG section: level ≥ 2 relays the value through CopiedNode rather than re-reading the target, so
    // a regression there would section the top level correctly and drift underneath it. Hence "at every
    // depth" — a one-level template proves nothing about the relay.
    [Fact]
    public async Task ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var templates = scope.ServiceProvider.GetRequiredService<IAbwabTemplatesWriter>();
        var apply = scope.ServiceProvider.GetRequiredService<IAbwabTemplateApplyWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var section = await sections.CreateAsync("قالب: قسم هدف التطبيق", CancellationToken.None);
        var target = await doors.CreateAsync(section.Id, null, "قالب: باب هدف التطبيق", null, null, [], CancellationToken.None);

        var template = await templates.CreateAsync("قالب: قالب بمستويين", null, null, [], CancellationToken.None);
        var rootNodeId = template.Nodes.Single(node => node.ParentNodeId is null).Id;
        var level1 = await templates.AddNodeAsync(
            template.Id, rootNodeId, "قالب: عقدة المستوى الأول", null, null, [], CancellationToken.None);
        await templates.AddNodeAsync(
            template.Id, level1.Id, "قالب: عقدة المستوى الثاني", null, null, [], CancellationToken.None);

        var created = await apply.ApplyAsync(template.Id, [target.Id], CancellationToken.None);

        // The response only carries the top level, so the deeper rows are read back from the database —
        // the whole point of the assertion is the level the response cannot show.
        created.Should().OnlyContain(door => door.SectionId == section.Id);

        var descendantIds = await DescendantIdsAsync(dbContext, target.Id);
        descendantIds.Should().HaveCount(2, "one node per template level was copied under the target");

        var descendants = await dbContext.AbwabDoors
            .Where(door => descendantIds.Contains(door.Id))
            .ToListAsync();
        descendants.Should().OnlyContain(door => door.SectionId == section.Id);
    }

    private static async Task<List<int>> DescendantIdsAsync(QuranDashboardDbContext dbContext, int doorId)
    {
        var childrenByParent = (await dbContext.AbwabDoors
                .Where(door => door.ParentId != null)
                .Select(door => new { door.Id, ParentId = door.ParentId!.Value })
                .ToListAsync())
            .GroupBy(door => door.ParentId)
            .ToDictionary(group => group.Key, group => group.Select(door => door.Id).ToList());

        var result = new List<int>();
        var queue = new Queue<int>([doorId]);
        while (queue.Count > 0)
        {
            if (!childrenByParent.TryGetValue(queue.Dequeue(), out var children))
            {
                continue;
            }

            result.AddRange(children);
            children.ForEach(queue.Enqueue);
        }

        return result;
    }
}
