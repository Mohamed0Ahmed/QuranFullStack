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
            await using var scope = freshFixture.CreateServiceProvider().CreateAsyncScope();
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
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var archived = await doors.CreateAsync(null, null, "قراءة: باب يُؤرشف", null, null, [], CancellationToken.None);
        await doors.DeleteAsync(archived.Id, archived.Version, CancellationToken.None);

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var archivedEntry = tree.Doors.Should().ContainSingle(d => d.Id == archived.Id).Subject;
        archivedEntry.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task GetTreeAsync_DirectChildCountAndSectionCount_CountLiveOnly()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
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

    [Fact]
    public async Task GetTreeAsync_OrdersDoorsByOrderValue_EvenWithAGap()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var first = await doors.CreateAsync(null, null, "قراءة: ترتيب أول", null, null, [], CancellationToken.None);
        var second = await doors.CreateAsync(null, null, "قراءة: ترتيب ثانٍ", null, null, [], CancellationToken.None);
        var third = await doors.CreateAsync(null, null, "قراءة: ترتيب ثالث", null, null, [], CancellationToken.None);

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
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var door = await doors.CreateAsync(null, null, "قراءة: باب لتتبع الإصدار", null, null, [], CancellationToken.None);
        var versionBeforeDelete = (await reader.GetTreeAsync(CancellationToken.None)).Version;

        await doors.DeleteAsync(door.Id, door.Version, CancellationToken.None);
        var versionAfterDelete = (await reader.GetTreeAsync(CancellationToken.None)).Version;

        versionBeforeDelete.Should().NotBeNull();
        versionAfterDelete.Should().NotBeNull();
        versionAfterDelete.Should().BeAfter(versionBeforeDelete!.Value);
    }
}
