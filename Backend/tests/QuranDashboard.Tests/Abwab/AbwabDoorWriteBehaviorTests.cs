using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab;

// T215: the write rules proved directly against IAbwabDoorsWriter/IAbwabSectionsWriter, independent of
// HTTP — SmokeAbwabWriteTests already proves the same rules end-to-end through the controllers; these
// assert the writer's own contract (the exact exception type), which a status-code assertion cannot.
// No reset between tests: each test uses its own uniquely-named rows, the same discipline
// AbwabSchemaTests already relies on against this shared, non-truncated fixture.
[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabDoorWriteBehaviorTests(AbwabSchemaFixture fixture)
{
    [Fact]
    public async Task MoveAsync_IntoOwnDescendant_ThrowsCycleException()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب للدورة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن للدورة", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(parent.Id, null, child.Id, parent.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task MoveAsync_IntoSelf_ThrowsCycleException()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var door = await writer.CreateAsync(null, null, "سلوك: باب لنقل ذاتي", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(door.Id, null, door.Id, door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameAtRoot_ThrowsDuplicateNameException()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        await writer.CreateAsync(null, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);
        var act = async () => await writer.CreateAsync(null, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<AbwabDuplicateNameException>();
    }

    [Fact]
    public async Task EditAsync_WithStaleVersion_ThrowsStaleVersionException()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var door = await writer.CreateAsync(null, null, "سلوك: باب لتعديل قديم", null, null, [], CancellationToken.None);
        await writer.EditAsync(door.Id, "سلوك: تعديل أول", null, null, [], door.Version, CancellationToken.None);

        var act = async () => await writer.EditAsync(door.Id, "سلوك: تعديل ثانٍ", null, null, [], door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabStaleVersionException>();
    }

    [Fact]
    public async Task DeleteAsync_ArchivesDescendants_EditOnDescendantThenReturnsNotFound()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب للأرشفة المباشرة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن للأرشفة المباشرة", null, null, [], CancellationToken.None);

        var deleted = await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);
        deleted.Should().BeTrue();

        // EditAsync's own not-found scope (deleted_at IS NULL) is the observable proof the subtree
        // archive reached the child too, without a read endpoint to query it directly (that's phase 3).
        var editOutcome = await writer.EditAsync(child.Id, "أي اسم", null, null, [], child.Version, CancellationToken.None);
        editOutcome.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_WhileParentStillArchived_ThrowsParentStillArchivedException()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب يبقى مؤرشفًا مباشرة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يحاول استعادة منفردة", null, null, [], CancellationToken.None);
        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);

        var act = async () => await writer.RestoreAsync(child.Id, child.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabParentStillArchivedException>();
    }

    [Fact]
    public async Task ReorderAsync_ProducesContiguousOrderValues()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        // A dedicated section scopes this test's siblings away from every other test's root-level doors.
        var section = await sections.CreateAsync("سلوك: قسم لإعادة الترتيب", CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الأول", null, null, [], CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الثاني", null, null, [], CancellationToken.None);
        var third = await writer.CreateAsync(section.Id, null, "الثالث", null, null, [], CancellationToken.None);

        var moved = await writer.ReorderAsync(third.Id, 1, third.Version, CancellationToken.None);

        moved.Should().NotBeNull();
        moved!.OrderValue.Should().Be(1);

        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var orderValues = await dbContext.AbwabDoors
            .Where(d => d.SectionId == section.Id && d.DeletedAtUtc == null)
            .Select(d => d.OrderValue)
            .OrderBy(v => v)
            .ToListAsync();

        orderValues.Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    // Discriminating case for BulkMoveAsync: when a moved door's OLD scope IS the destination scope,
    // a naive "existing live count + 1" double-counts it (the DB row still shows the old FK values
    // until SaveChanges), producing gaps like {1, 4, 5} instead of {1, 2, 3}. A destination-scope
    // move that doesn't actually change scope for any door is the sharpest version of this — the
    // move-into-a-different-scope case in the smoke suite's BulkMoveDoors test cannot expose it.
    [Fact]
    public async Task BulkMoveAsync_WhenDestinationEqualsSourceScope_StaysContiguous()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لنقل جماعي في نفس النطاق", CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الأول", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section.Id, null, "الثاني", null, null, [], CancellationToken.None);
        var third = await writer.CreateAsync(section.Id, null, "الثالث", null, null, [], CancellationToken.None);

        // second and third "move" into the very section they already live in, at its root — the
        // destination scope equals their own current scope.
        await writer.BulkMoveAsync(
            [new AbwabBulkDoorRef(second.Id, second.Version), new AbwabBulkDoorRef(third.Id, third.Version)],
            section.Id,
            null,
            CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var orderValues = await dbContext.AbwabDoors
            .Where(d => d.SectionId == section.Id && d.DeletedAtUtc == null)
            .Select(d => d.OrderValue)
            .OrderBy(v => v)
            .ToListAsync();

        orderValues.Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task BulkArchiveAsync_WithOneStaleVersion_LeavesBothDoorsLive()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var first = await writer.CreateAsync(null, null, "سلوك: أول باب للأرشفة الجماعية", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(null, null, "سلوك: ثاني باب للأرشفة الجماعية", null, null, [], CancellationToken.None);

        var act = async () => await writer.BulkArchiveAsync(
            [new AbwabBulkDoorRef(first.Id, first.Version), new AbwabBulkDoorRef(second.Id, 999_999)],
            CancellationToken.None);

        await act.Should().ThrowAsync<AbwabStaleVersionException>();

        // A fresh scope, not the one above: a failed SaveChanges leaves its DbContext's change tracker
        // holding the rejected in-memory edits, same as a real request would never reuse a DbContext
        // across two separate HTTP calls — reusing the poisoned one here would fail this check for a
        // reason unrelated to what it verifies.
        await using var verifyScope = fixture.CreateServiceProvider().CreateAsyncScope();
        var verifyWriter = verifyScope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        // All-or-nothing: even the door whose supplied version was correct must still be live.
        var editOutcome = await verifyWriter.EditAsync(first.Id, first.Name, null, null, [], first.Version, CancellationToken.None);
        editOutcome.Should().NotBeNull();
    }
}
