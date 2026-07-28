using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;

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
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب للدورة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن للدورة", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(parent.Id, null, child.Id, parent.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task MoveAsync_IntoSelf_ThrowsCycleException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var door = await writer.CreateAsync(null, null, "سلوك: باب لنقل ذاتي", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(door.Id, null, door.Id, door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameAtRoot_ThrowsDuplicateNameException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        await writer.CreateAsync(null, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);
        var act = async () => await writer.CreateAsync(null, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<AbwabDuplicateNameException>();
    }

    [Fact]
    public async Task EditAsync_WithStaleVersion_ThrowsStaleVersionException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var door = await writer.CreateAsync(null, null, "سلوك: باب لتعديل قديم", null, null, [], CancellationToken.None);
        await writer.EditAsync(door.Id, "سلوك: تعديل أول", null, null, [], door.Version, CancellationToken.None);

        var act = async () => await writer.EditAsync(door.Id, "سلوك: تعديل ثانٍ", null, null, [], door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabStaleVersionException>();
    }

    [Fact]
    public async Task DeleteAsync_ArchivesDescendants_EditOnDescendantThenReturnsNotFound()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
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
        await using var scope = fixture.Services.CreateAsyncScope();
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
        await using var scope = fixture.Services.CreateAsyncScope();
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
        await using var scope = fixture.Services.CreateAsyncScope();
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

    // The single-door twin of BulkMoveAsync's overlap case: the destination scope IS the door's own
    // current scope, so a naive "live count + 1" counts the door twice (its DB row still shows the old
    // FK values) and leaves {1, 2, 4} instead of {1, 2, 3}.
    [Fact]
    public async Task MoveAsync_WhenDestinationEqualsCurrentScope_StaysContiguous()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لنقل مفرد في نفس النطاق", CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الأول", null, null, [], CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الثاني", null, null, [], CancellationToken.None);
        var third = await writer.CreateAsync(section.Id, null, "الثالث", null, null, [], CancellationToken.None);

        await writer.MoveAsync(third.Id, section.Id, null, third.Version, CancellationToken.None);

        (await OrderValuesOfSectionRootAsync(scope, section.Id))
            .Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    // Archive renumbers the scope 1..N-1 without the archived door, which keeps its old OrderValue.
    // Restore is the one write that puts a row back INTO a scope, so it has to renumber again — otherwise
    // the restored door collides with whichever sibling inherited its number.
    [Fact]
    public async Task RestoreAsync_AfterArchive_LeavesSiblingOrderContiguous()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لاستعادة مرتبة", CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الأول", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section.Id, null, "الثاني", null, null, [], CancellationToken.None);
        await writer.CreateAsync(section.Id, null, "الثالث", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(second.Id, second.Version, CancellationToken.None);

        var archived = await ReloadAsync(scope, second.Id);
        await writer.RestoreAsync(second.Id, archived.Version, CancellationToken.None);

        (await OrderValuesOfSectionRootAsync(scope, section.Id))
            .Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    // Archive only ever claims LIVE descendants, so a child archived earlier on its own was never part of
    // the parent's archive. Restoring the parent must not hand it back.
    [Fact]
    public async Task RestoreAsync_DoesNotResurrectIndependentlyArchivedDescendant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب لاستعادة انتقائية", null, null, [], CancellationToken.None);
        var archivedEarlier = await writer.CreateAsync(null, parent.Id, "سلوك: ابن مؤرشف مسبقًا", null, null, [], CancellationToken.None);
        var sweptIn = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يُؤرشف مع أبيه", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(archivedEarlier.Id, archivedEarlier.Version, CancellationToken.None);
        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);

        var archivedParent = await ReloadAsync(scope, parent.Id);
        await writer.RestoreAsync(parent.Id, archivedParent.Version, CancellationToken.None);

        (await ReloadAsync(scope, sweptIn.Id)).DeletedAtUtc
            .Should().BeNull("it was archived by this same operation, so restore gives it back");
        (await ReloadAsync(scope, archivedEarlier.Id)).DeletedAtUtc
            .Should().NotBeNull("the user archived it deliberately before the parent's archive ever claimed it");
    }

    // A section is only archivable once it holds no live doors, and sections have no restore route in this
    // slice — refusing the restore would strand the door forever, so it lands outside every section.
    [Fact]
    public async Task RestoreAsync_WhenSectionWasArchivedMeanwhile_DetachesRestoredSubtree()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم يُؤرشف بعد أبوابه", CancellationToken.None);
        var parent = await writer.CreateAsync(section.Id, null, "سلوك: أب في قسم مؤرشف", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(section.Id, parent.Id, "سلوك: ابن في قسم مؤرشف", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);
        (await sections.DeleteAsync(section.Id, CancellationToken.None))
            .Should().Be(AbwabSectionDeleteResult.Deleted);

        var archivedParent = await ReloadAsync(scope, parent.Id);
        var restored = await writer.RestoreAsync(parent.Id, archivedParent.Version, CancellationToken.None);

        restored!.Door.SectionId.Should().BeNull();
        restored.DetachedFromArchivedSection
            .Should().BeTrue("a null SectionId alone cannot be told apart from a door that never had one");
        (await ReloadAsync(scope, child.Id)).SectionId
            .Should().BeNull("a nested door inherits its parent's section, so the subtree detaches whole");
    }

    // The negative half of the indicator: a door restored into a section that is still live keeps that
    // section and reports no detach. Without this, a writer that always reported false would still pass.
    [Fact]
    public async Task RestoreAsync_WhenSectionIsStillLive_KeepsTheSectionAndReportsNoDetach()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم يبقى حيًّا", CancellationToken.None);
        var door = await writer.CreateAsync(section.Id, null, "سلوك: باب يعود إلى قسمه", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(door.Id, door.Version, CancellationToken.None);

        var archived = await ReloadAsync(scope, door.Id);
        var restored = await writer.RestoreAsync(door.Id, archived.Version, CancellationToken.None);

        restored!.Door.SectionId.Should().Be(section.Id);
        restored.DetachedFromArchivedSection.Should().BeFalse();
    }

    [Fact]
    public async Task BulkArchiveAsync_WithOneStaleVersion_LeavesBothDoorsLive()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
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
        await using var verifyScope = fixture.Services.CreateAsyncScope();
        var verifyWriter = verifyScope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        // All-or-nothing: even the door whose supplied version was correct must still be live.
        var editOutcome = await verifyWriter.EditAsync(first.Id, first.Name, null, null, [], first.Version, CancellationToken.None);
        editOutcome.Should().NotBeNull();
    }

    // Selecting a door and one of its own descendants in the same batch is legal — the UI selects rows,
    // not subtrees — and the descendant is then reached twice, once swept in by its ancestor and once as
    // a top-level entry. The response is a set of what was archived, so it may not report it twice.
    [Fact]
    public async Task BulkArchiveAsync_WhenBatchContainsBothAParentAndItsChild_ReportsEachIdOnce()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parent = await writer.CreateAsync(null, null, "سلوك: أب ضمن دفعة تحوي ابنه", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن ضمن دفعة تحوي أباه", null, null, [], CancellationToken.None);

        var archivedIds = await writer.BulkArchiveAsync(
            [new AbwabBulkDoorRef(parent.Id, parent.Version), new AbwabBulkDoorRef(child.Id, child.Version)],
            CancellationToken.None);

        archivedIds.Should().BeEquivalentTo([parent.Id, child.Id]);
        archivedIds.Should().OnlyHaveUniqueItems();
    }

    // T206 requires a removed alias to be SOFT-deleted, not hard-deleted. Asserting only the resulting
    // live alias list cannot tell the two apart — it reads identically either way. The proof is that the
    // dropped row is still THERE, carrying DeletedAtUtc.
    [Fact]
    public async Task EditAsync_ReplacingAliases_SoftDeletesTheDroppedOnes()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var door = await writer.CreateAsync(
            null, null, "سلوك: باب لدلالة الأسماء البديلة", null, null, ["مُبقى", "مُسقط"], CancellationToken.None);

        var edited = await writer.EditAsync(
            door.Id, door.Name, null, null, ["مُبقى", "مُضاف"], door.Version, CancellationToken.None);

        edited!.Aliases.Should().BeEquivalentTo(["مُبقى", "مُضاف"]);

        var allRows = await dbContext.AbwabDoorAliases
            .Where(a => a.DoorId == door.Id)
            .ToListAsync();

        allRows.Should().HaveCount(3, "the dropped alias row is retained, not removed — one dropped, one kept, one added");
        allRows.Single(a => a.Value == "مُسقط").DeletedAtUtc.Should().NotBeNull("the dropped alias is soft-deleted");
        allRows.Single(a => a.Value == "مُبقى").DeletedAtUtc.Should().BeNull("an unchanged alias is left alone");
        allRows.Single(a => a.Value == "مُضاف").DeletedAtUtc.Should().BeNull();
    }

    private static async Task<AbwabDoor> ReloadAsync(AsyncServiceScope scope, int doorId)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await dbContext.AbwabDoors.SingleAsync(d => d.Id == doorId);
    }

    private static async Task<IReadOnlyList<int>> OrderValuesOfSectionRootAsync(AsyncServiceScope scope, int sectionId)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await dbContext.AbwabDoors
            .Where(d => d.SectionId == sectionId && d.ParentId == null && d.DeletedAtUtc == null)
            .Select(d => d.OrderValue)
            .OrderBy(v => v)
            .ToListAsync();
    }
}
