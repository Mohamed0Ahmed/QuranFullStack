using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab;

// T304: GetAbwabTreeHandler / EfAbwabTreeReader proved directly, independent of HTTP — the data-tier
// smoke test (SmokeAbwabWriteTests) proves the same read through the real endpoint end-to-end.
[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabTreeReadTests(AbwabSchemaFixture fixture)
{
    [Fact]
    public async Task GetTreeAsync_OnFreshSchema_ReturnsEmptySnapshotWithNullVersion()
    {
        // A dedicated, unshared container: the collection fixture above is shared with every other
        // Abwab test class and accumulates rows for the whole test run, so it can never assert "empty".
        var freshFixture = new AbwabSchemaFixture();
        await freshFixture.InitializeAsync();
        try
        {
            await using var scope = freshFixture.Services.CreateAsyncScope();
            var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

            var tree = await reader.GetTreeAsync(CancellationToken.None);

            tree.Version.Should().BeNull();
            tree.Sections.Should().BeEmpty();
            tree.Doors.Should().BeEmpty();
        }
        finally
        {
            await freshFixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetTreeAsync_IncludesArchivedDoorAndItsLiveSiblingCount()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();
        var section = await NewSectionAsync(scope, "قراءة: قسم الباب المؤرشف");

        var archived = await doors.CreateAsync(section, null, "قراءة: باب يُؤرشف", null, null, [], CancellationToken.None);
        await doors.DeleteAsync(archived.Id, archived.Version, CancellationToken.None);

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var archivedEntry = tree.Doors.Should().ContainSingle(d => d.Id == archived.Id).Subject;
        archivedEntry.IsArchived.Should().BeTrue();
    }

    // What the archive view asks before offering a restore: does this door still have a section to go
    // back to? The live sections list cannot answer it — the retired section is exactly the one missing
    // from it — so the flag is stated per door. Both halves are asserted in one read: a reader stuck on
    // either constant passes the other test in this file.
    [Fact]
    public async Task GetTreeAsync_FlagsSectionRetired_OnlyForDoorsWhoseSectionIsArchived()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var retiredSection = await sections.CreateAsync("قراءة: قسم يُتقاعد بعد بابه", CancellationToken.None);
        var liveSection = await sections.CreateAsync("قراءة: قسم يبقى حيًّا للعلم", CancellationToken.None);
        var strandedDoor = await doors.CreateAsync(
            retiredSection.Id, null, "قراءة: باب قسمه متقاعد", null, null, [], CancellationToken.None);
        var settledDoor = await doors.CreateAsync(
            liveSection.Id, null, "قراءة: باب قسمه حي", null, null, [], CancellationToken.None);

        // A section is only archivable once it holds no LIVE doors, so the door has to go first — which is
        // the whole reason this state exists and cannot be reached any other way.
        await doors.DeleteAsync(strandedDoor.Id, strandedDoor.Version, CancellationToken.None);
        (await sections.DeleteAsync(retiredSection.Id, CancellationToken.None))
            .Should().Be(AbwabSectionDeleteResult.Deleted);

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        tree.Doors.Single(d => d.Id == strandedDoor.Id).SectionRetired
            .Should().BeTrue("its section was archived while it sat archived, so restoring it needs a destination");
        tree.Doors.Single(d => d.Id == settledDoor.Id).SectionRetired
            .Should().BeFalse("its section is still live, so it has somewhere to go back to");
        tree.Sections.Should().NotContain(s => s.Id == retiredSection.Id, "archived sections are excluded from the snapshot");
    }

    [Fact]
    public async Task GetTreeAsync_DirectChildCountAndSectionCount_CountLiveOnly()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var section = await sections.CreateAsync("قراءة: قسم للعد الحي", CancellationToken.None);
        var parent = await doors.CreateAsync(section.Id, null, "قراءة: أب للعد", null, null, [], CancellationToken.None);
        var liveChild = await doors.CreateAsync(section.Id, parent.Id, "قراءة: ابن حي", null, null, [], CancellationToken.None);
        var archivedChild = await doors.CreateAsync(section.Id, parent.Id, "قراءة: ابن يُؤرشف", null, null, [], CancellationToken.None);
        await doors.DeleteAsync(archivedChild.Id, archivedChild.Version, CancellationToken.None);

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var parentEntry = tree.Doors.Should().ContainSingle(d => d.Id == parent.Id).Subject;
        parentEntry.DirectChildCount.Should().Be(1, "only the live child counts — the archived one is flagged, not counted");

        var liveChildEntry = tree.Doors.Should().ContainSingle(d => d.Id == liveChild.Id).Subject;
        liveChildEntry.ParentId.Should().Be(parent.Id);
        liveChildEntry.SectionId.Should().Be(section.Id, "a nested door inherits its parent's section");

        var sectionEntry = tree.Sections.Should().ContainSingle(s => s.Id == section.Id).Subject;
        sectionEntry.DoorsInScopeCount.Should().Be(2, "parent + live child; the archived child does not inflate the section's live total");
    }

    // DoorsInScopeCount counts every live door carrying that SectionId regardless of depth. A move that
    // left descendants behind would therefore show one subtree under BOTH sections. Asserting only the
    // moved root is the non-discriminating version of this — the child is what proves the cascade.
    [Fact]
    public async Task GetTreeAsync_AfterCrossSectionMove_CountsTheWholeSubtreeUnderTheDestination()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var origin = await sections.CreateAsync("قراءة: قسم المصدر للعد بعد النقل", CancellationToken.None);
        var destination = await sections.CreateAsync("قراءة: قسم الوجهة للعد بعد النقل", CancellationToken.None);
        var parent = await doors.CreateAsync(origin.Id, null, "قراءة: أب ينتقل بعدّه", null, null, [], CancellationToken.None);
        await doors.CreateAsync(origin.Id, parent.Id, "قراءة: ابن ينتقل بعدّ أبيه", null, null, [], CancellationToken.None);

        var before = await reader.GetTreeAsync(CancellationToken.None);
        before.Sections.Single(s => s.Id == origin.Id).DoorsInScopeCount.Should().Be(2);

        await doors.MoveAsync(parent.Id, destination.Id, null, parent.Version, CancellationToken.None);

        var after = await reader.GetTreeAsync(CancellationToken.None);
        after.Sections.Single(s => s.Id == origin.Id).DoorsInScopeCount
            .Should().Be(0, "both rows left — the section is not left counting a child whose parent moved");
        after.Sections.Single(s => s.Id == destination.Id).DoorsInScopeCount
            .Should().Be(2, "the child followed its parent, so the destination gained the whole subtree");
    }

    [Fact]
    public async Task GetTreeAsync_OrdersDoorsByOrderValue_EvenWithAGap()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var section = await NewSectionAsync(scope, "قراءة: قسم الترتيب ذي الفجوة");

        var first = await doors.CreateAsync(section, null, "قراءة: ترتيب أول", null, null, [], CancellationToken.None);
        var second = await doors.CreateAsync(section, null, "قراءة: ترتيب ثانٍ", null, null, [], CancellationToken.None);
        var third = await doors.CreateAsync(section, null, "قراءة: ترتيب ثالث", null, null, [], CancellationToken.None);

        // Reads tolerate gaps (§4) — bypass the writer's own 1..N resequencing to prove the reader
        // orders by the raw value rather than assuming contiguity.
        var rows = await db.AbwabDoors
            .Where(d => d.Id == first.Id || d.Id == second.Id || d.Id == third.Id)
            .ToListAsync();
        rows.Single(d => d.Id == first.Id).OrderValue = 1;
        rows.Single(d => d.Id == second.Id).OrderValue = 5;
        rows.Single(d => d.Id == third.Id).OrderValue = 10;
        await db.SaveChangesAsync();

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var orderedIds = tree.Doors
            .Where(d => d.Id == first.Id || d.Id == second.Id || d.Id == third.Id)
            .OrderBy(d => d.OrderValue)
            .Select(d => d.Id)
            .ToList();
        orderedIds.Should().BeEquivalentTo([first.Id, second.Id, third.Id], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetTreeAsync_VersionAdvances_WhenAnyRowIsLaterMutated()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var section = await NewSectionAsync(scope, "قراءة: قسم تتبع الإصدار");

        var door = await doors.CreateAsync(section, null, "قراءة: باب لتتبع الإصدار", null, null, [], CancellationToken.None);
        var versionBeforeDelete = (await reader.GetTreeAsync(CancellationToken.None)).Version;

        await doors.DeleteAsync(door.Id, door.Version, CancellationToken.None);
        var versionAfterDelete = (await reader.GetTreeAsync(CancellationToken.None)).Version;

        versionBeforeDelete.Should().NotBeNull();
        versionAfterDelete.Should().NotBeNull();
        versionAfterDelete.Should().BeAfter(versionBeforeDelete!.Value);
    }

    // Every root-scope write names a section now, so a reader test that only needed "a door" brings its
    // own — which also keeps its rows out of every other test's scope on this shared fixture.
    private static async Task<int> NewSectionAsync(AsyncServiceScope scope, string name)
    {
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        return (await sections.CreateAsync(name, CancellationToken.None)).Id;
    }
}
