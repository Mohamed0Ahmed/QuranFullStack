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
        var section = await NewSectionAsync(scope, "سلوك: قسم الدورة");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب للدورة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن للدورة", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(parent.Id, null, child.Id, parent.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task MoveAsync_IntoSelf_ThrowsCycleException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم النقل الذاتي");

        var door = await writer.CreateAsync(section, null, "سلوك: باب لنقل ذاتي", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(door.Id, null, door.Id, door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabCycleException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameAtRoot_ThrowsDuplicateNameException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم الاسم المكرر");

        await writer.CreateAsync(section, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);
        var act = async () => await writer.CreateAsync(section, null, "سلوك: اسم مكرر للكتابة المباشرة", null, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<AbwabDuplicateNameException>();
    }

    [Fact]
    public async Task EditAsync_WithStaleVersion_ThrowsStaleVersionException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم التعديل القديم");

        var door = await writer.CreateAsync(section, null, "سلوك: باب لتعديل قديم", null, null, [], CancellationToken.None);
        await writer.EditAsync(door.Id, "سلوك: تعديل أول", null, null, [], door.Version, CancellationToken.None);

        var act = async () => await writer.EditAsync(door.Id, "سلوك: تعديل ثانٍ", null, null, [], door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabStaleVersionException>();
    }

    [Fact]
    public async Task DeleteAsync_ArchivesDescendants_EditOnDescendantThenReturnsNotFound()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم الأرشفة المباشرة");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب للأرشفة المباشرة", null, null, [], CancellationToken.None);
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
        var section = await NewSectionAsync(scope, "سلوك: قسم الأب المؤرشف");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب يبقى مؤرشفًا مباشرة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يحاول استعادة منفردة", null, null, [], CancellationToken.None);
        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);

        var act = async () => await writer.RestoreAsync(child.Id, null, child.Version, CancellationToken.None);

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

        var moved = await writer.ReorderAsync(third.Id, 1, AbwabReorderScope.Section, third.Version, CancellationToken.None);

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

    // §5.1's whole point, proved both directions: a Section reorder never touches GlobalOrderValue,
    // and (below) a Global reorder never touches OrderValue. The two spaces are independent.
    [Fact]
    public async Task ReorderAsync_WithSectionScope_LeavesGlobalOrderValueUntouched()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لترتيب قسمي لا يمس العام", CancellationToken.None);
        var first = await writer.CreateAsync(section.Id, null, "سلوك: أول باب لترتيب قسمي", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section.Id, null, "سلوك: ثاني باب لترتيب قسمي", null, null, [], CancellationToken.None);

        await writer.ReorderAsync(second.Id, 1, AbwabReorderScope.Section, second.Version, CancellationToken.None);

        (await ReloadAsync(scope, first.Id)).GlobalOrderValue.Should().Be(first.GlobalOrderValue);
        (await ReloadAsync(scope, second.Id)).GlobalOrderValue.Should().Be(second.GlobalOrderValue);
    }

    [Fact]
    public async Task ReorderAsync_WithGlobalScope_LeavesOrderValueUntouched()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لترتيب عام لا يمس القسمي", CancellationToken.None);
        var first = await writer.CreateAsync(section.Id, null, "سلوك: أول باب لترتيب عام", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section.Id, null, "سلوك: ثاني باب لترتيب عام", null, null, [], CancellationToken.None);

        // Created back-to-back with nothing else touching the global sequence in between, so their
        // global positions are contiguous regardless of what the shared table already holds.
        var firstGlobalBefore = first.GlobalOrderValue!.Value;
        var secondGlobalBefore = second.GlobalOrderValue!.Value;
        secondGlobalBefore.Should().Be(firstGlobalBefore + 1);

        await writer.ReorderAsync(second.Id, firstGlobalBefore, AbwabReorderScope.Global, second.Version, CancellationToken.None);

        (await ReloadAsync(scope, first.Id)).OrderValue.Should().Be(1);
        (await ReloadAsync(scope, second.Id)).OrderValue.Should().Be(2);

        // The write itself: second took first's old global position, and first shifted to what was
        // second's — proving the Global branch actually renumbers GlobalOrderValue, not a no-op.
        (await ReloadAsync(scope, second.Id)).GlobalOrderValue.Should().Be(firstGlobalBefore);
        (await ReloadAsync(scope, first.Id)).GlobalOrderValue.Should().Be(secondGlobalBefore);
    }

    // The discriminating case from plan §5.1: root membership, not section membership, is what moves
    // the global sequence — a root that changes section but stays a root leaves it untouched.
    [Fact]
    public async Task MoveAsync_RootToRootAcrossSections_LeavesGlobalOrderValueUnchanged()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var origin = await sections.CreateAsync("سلوك: قسم مصدر لنقل جذري عام", CancellationToken.None);
        var destination = await sections.CreateAsync("سلوك: قسم وجهة لنقل جذري عام", CancellationToken.None);
        var door = await writer.CreateAsync(origin.Id, null, "سلوك: باب ينتقل جذريًا بين قسمين", null, null, [], CancellationToken.None);

        var moved = await writer.MoveAsync(door.Id, destination.Id, null, door.Version, CancellationToken.None);

        moved!.SectionId.Should().Be(destination.Id);
        moved.GlobalOrderValue.Should().Be(door.GlobalOrderValue);
    }

    // §6's third trap, exercised through MoveAsync directly: a nested→root move still shows
    // parent_id set in the database read (pre-SaveChanges), so MaintainGlobalOrderAsync's "remaining"
    // query can never return it — it must be appended in code, or this silently drops the door.
    [Fact]
    public async Task MoveAsync_NestedToRoot_AppendsToEndOfGlobalSequence()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var section = await NewSectionAsync(scope, "سلوك: قسم الانتقال إلى الجذر");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب لابن ينتقل إلى الجذر", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن ينتقل إلى الجذر عالميًا", null, null, [], CancellationToken.None);

        var liveRootCountBeforeMove = await dbContext.AbwabDoors
            .CountAsync(d => d.ParentId == null && d.DeletedAtUtc == null);

        var moved = await writer.MoveAsync(child.Id, section, null, child.Version, CancellationToken.None);

        moved!.GlobalOrderValue.Should().Be(liveRootCountBeforeMove + 1);
    }

    // The mirror case: a root→nested move nulls the door's own global value and shifts every LATER
    // root down by one, the same relative-delta proof DeleteAsync's archive test below uses.
    [Fact]
    public async Task MoveAsync_RootToNested_NullsGlobalOrderValueAndShiftsLaterRootsDown()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لنقل جذر إلى ابن يزيح ما بعده", CancellationToken.None);
        var parent = await writer.CreateAsync(section.Id, null, "سلوك: أب يستقبل جذرًا سابقًا", null, null, [], CancellationToken.None);
        var mover = await writer.CreateAsync(section.Id, null, "سلوك: جذر ينتقل إلى ابن", null, null, [], CancellationToken.None);
        var later = await writer.CreateAsync(section.Id, null, "سلوك: جذر لاحق يُزاح لأسفل", null, null, [], CancellationToken.None);
        var laterGlobalBefore = later.GlobalOrderValue!.Value;

        var moved = await writer.MoveAsync(mover.Id, null, parent.Id, mover.Version, CancellationToken.None);

        moved!.GlobalOrderValue.Should().BeNull();
        (await ReloadAsync(scope, later.Id)).GlobalOrderValue.Should().Be(laterGlobalBefore - 1);
    }

    // Archive nulls the archived root's own global value and shifts every LATER root down by one —
    // proved as a relative delta, not an absolute count, since the global sequence spans the whole
    // shared table (every other test's root doors included), not just this test's own section.
    [Fact]
    public async Task DeleteAsync_ArchivesRoot_NullsGlobalOrderValueAndShiftsLaterRootsDown()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم لأرشفة جذر يزيح ما بعده", CancellationToken.None);
        var first = await writer.CreateAsync(section.Id, null, "سلوك: أول جذر للأرشفة العامة", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section.Id, null, "سلوك: ثاني جذر للأرشفة العامة", null, null, [], CancellationToken.None);
        var third = await writer.CreateAsync(section.Id, null, "سلوك: ثالث جذر للأرشفة العامة", null, null, [], CancellationToken.None);
        var thirdGlobalBefore = third.GlobalOrderValue!.Value;

        await writer.DeleteAsync(second.Id, second.Version, CancellationToken.None);

        (await ReloadAsync(scope, second.Id)).GlobalOrderValue.Should().BeNull();
        (await ReloadAsync(scope, first.Id)).GlobalOrderValue.Should().Be(first.GlobalOrderValue);
        (await ReloadAsync(scope, third.Id)).GlobalOrderValue.Should().Be(thirdGlobalBefore - 1);
    }

    // §5.2: restore appends at the end of the CURRENT global sequence, derived from
    // EfAbwabDoorsWriter.cs's existing restore-appends-in-its-own-scope semantic, not guessed.
    [Fact]
    public async Task RestoreAsync_OfArchivedRoot_AppendsToEndOfGlobalSequence()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var section = await NewSectionAsync(scope, "سلوك: قسم الاستعادة آخر التسلسل");

        var door = await writer.CreateAsync(section, null, "سلوك: جذر يُستعاد آخر التسلسل العام", null, null, [], CancellationToken.None);
        await writer.DeleteAsync(door.Id, door.Version, CancellationToken.None);
        var archived = await ReloadAsync(scope, door.Id);

        var liveRootCountBeforeRestore = await dbContext.AbwabDoors
            .CountAsync(d => d.ParentId == null && d.DeletedAtUtc == null);

        var restored = await writer.RestoreAsync(door.Id, null, archived.Version, CancellationToken.None);

        restored!.GlobalOrderValue.Should().Be(liveRootCountBeforeRestore + 1);
    }

    // M1: at root scope there is no parent to derive a section from, so an unstated one has no answer.
    [Fact]
    public async Task CreateAsync_AtRootWithoutSection_ThrowsSectionRequiredException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var act = async () => await writer.CreateAsync(null, null, "سلوك: جذر بلا قسم مرفوض", null, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionRequiredException>();
    }

    // Roots in DIFFERENT sections still share the one global sequence: the per-scope home is
    // (section, parent), the global order spans every root regardless of section.
    [Fact]
    public async Task CreateAsync_RootsInDifferentSections_ShareOneGlobalSequence()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var first = await NewSectionAsync(scope, "سلوك: قسم أول للتسلسل العام المشترك");
        var second = await NewSectionAsync(scope, "سلوك: قسم ثانٍ للتسلسل العام المشترك");
        var inFirst = await writer.CreateAsync(first, null, "سلوك: جذر القسم الأول للتسلسل العام", null, null, [], CancellationToken.None);
        var inSecond = await writer.CreateAsync(second, null, "سلوك: جذر القسم الثاني للتسلسل العام", null, null, [], CancellationToken.None);

        inSecond.GlobalOrderValue.Should().Be(inFirst.GlobalOrderValue + 1);
    }

    // M3: the move counterpart. Nesting under a parent inherits that parent's section, so root scope is
    // the one destination that has to be named.
    [Fact]
    public async Task MoveAsync_ToRootWithoutSection_ThrowsSectionRequiredException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم نقل بلا وجهة");

        var door = await writer.CreateAsync(section, null, "سلوك: باب يُنقل بلا قسم وجهة", null, null, [], CancellationToken.None);

        var act = async () => await writer.MoveAsync(door.Id, null, null, door.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionRequiredException>();
    }

    // M5: the bulk path resolves the target BEFORE it loads the doors, so the rejection is about the
    // request's shape and fires regardless of what the batch names.
    [Fact]
    public async Task BulkMoveAsync_ToRootWithoutSection_ThrowsSectionRequiredException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم نقل جماعي بلا وجهة");

        var first = await writer.CreateAsync(section, null, "سلوك: أول باب لنقل جماعي بلا قسم", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section, null, "سلوك: ثاني باب لنقل جماعي بلا قسم", null, null, [], CancellationToken.None);

        var act = async () => await writer.BulkMoveAsync(
            [new AbwabBulkDoorRef(first.Id, first.Version), new AbwabBulkDoorRef(second.Id, second.Version)],
            null, null, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionRequiredException>();
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
        await writer.RestoreAsync(second.Id, null, archived.Version, CancellationToken.None);

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
        var section = await NewSectionAsync(scope, "سلوك: قسم الاستعادة الانتقائية");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب لاستعادة انتقائية", null, null, [], CancellationToken.None);
        var archivedEarlier = await writer.CreateAsync(null, parent.Id, "سلوك: ابن مؤرشف مسبقًا", null, null, [], CancellationToken.None);
        var sweptIn = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يُؤرشف مع أبيه", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(archivedEarlier.Id, archivedEarlier.Version, CancellationToken.None);
        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);

        var archivedParent = await ReloadAsync(scope, parent.Id);
        await writer.RestoreAsync(parent.Id, null, archivedParent.Version, CancellationToken.None);

        (await ReloadAsync(scope, sweptIn.Id)).DeletedAtUtc
            .Should().BeNull("it was archived by this same operation, so restore gives it back");
        (await ReloadAsync(scope, archivedEarlier.Id)).DeletedAtUtc
            .Should().NotBeNull("the user archived it deliberately before the parent's archive ever claimed it");
    }

    // M7: a section is only archivable once it holds no live doors, and sections have no restore route —
    // so the stored section is not a destination and the caller has to name one. Refusing beats putting
    // the door somewhere nobody chose.
    [Fact]
    public async Task RestoreAsync_RootWhoseSectionRetired_WithoutDestination_Throws()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم يُؤرشف بعد أبوابه", CancellationToken.None);
        var door = await writer.CreateAsync(section.Id, null, "سلوك: باب في قسم مؤرشف بلا وجهة", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(door.Id, door.Version, CancellationToken.None);
        (await sections.DeleteAsync(section.Id, CancellationToken.None))
            .Should().Be(AbwabSectionDeleteResult.Deleted);

        var archived = await ReloadAsync(scope, door.Id);
        var act = async () => await writer.RestoreAsync(door.Id, null, archived.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionRequiredException>();
    }

    // M7: the same door restored WITH a destination lands in it, and takes its restored subtree along —
    // a nested door's section is its parent's, so half a re-section would break that on the read side.
    [Fact]
    public async Task RestoreAsync_WithDestination_ResectionsTheRestoredSubtree()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var retired = await sections.CreateAsync("سلوك: قسم يُتقاعد تحت شجرة", CancellationToken.None);
        var destination = await sections.CreateAsync("سلوك: قسم وجهة الاسترجاع", CancellationToken.None);
        var parent = await writer.CreateAsync(retired.Id, null, "سلوك: أب يُستعاد إلى قسم آخر", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يتبع أباه إلى القسم الجديد", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(parent.Id, parent.Version, CancellationToken.None);
        (await sections.DeleteAsync(retired.Id, CancellationToken.None))
            .Should().Be(AbwabSectionDeleteResult.Deleted);

        var archivedParent = await ReloadAsync(scope, parent.Id);
        var restored = await writer.RestoreAsync(parent.Id, destination.Id, archivedParent.Version, CancellationToken.None);

        restored!.SectionId.Should().Be(destination.Id);
        (await ReloadAsync(scope, child.Id)).SectionId
            .Should().Be(destination.Id, "a nested door inherits its parent's section, so the subtree is re-sectioned whole");
    }

    // M7's negative half: a door whose section is still live goes back where it came from when the
    // caller states nothing. Without this, a writer that always demanded a destination would still pass.
    [Fact]
    public async Task RestoreAsync_RootKeepsStoredLiveSection_WhenBodyNull()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم يبقى حيًّا", CancellationToken.None);
        var door = await writer.CreateAsync(section.Id, null, "سلوك: باب يعود إلى قسمه", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(door.Id, door.Version, CancellationToken.None);

        var archived = await ReloadAsync(scope, door.Id);
        var restored = await writer.RestoreAsync(door.Id, null, archived.Version, CancellationToken.None);

        restored!.SectionId.Should().Be(section.Id);
    }

    // M8: a child derives from its live parent, exactly as child create does.
    [Fact]
    public async Task RestoreAsync_Child_DerivesLiveParentsSection_WhenBodyNull()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم اشتقاق الابن عند الاستعادة");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب حي لابن يُستعاد", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يشتق قسم أبيه عند الاستعادة", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(child.Id, child.Version, CancellationToken.None);

        var archived = await ReloadAsync(scope, child.Id);
        var restored = await writer.RestoreAsync(child.Id, null, archived.Version, CancellationToken.None);

        restored!.SectionId.Should().Be(section);
    }

    // M8: and a stated section that disagrees with the parent is a caller bug, refused the same way
    // child create refuses it — not silently overwritten.
    [Fact]
    public async Task RestoreAsync_Child_WithConflictingSection_Throws()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var parentSection = await NewSectionAsync(scope, "سلوك: قسم الأب لتعارض الاستعادة");
        var otherSection = await NewSectionAsync(scope, "سلوك: قسم مخالف لتعارض الاستعادة");
        var parent = await writer.CreateAsync(parentSection, null, "سلوك: أب في قسم لتعارض الاستعادة", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن يُستعاد بقسم مخالف", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(child.Id, child.Version, CancellationToken.None);

        var archived = await ReloadAsync(scope, child.Id);
        var act = async () => await writer.RestoreAsync(child.Id, otherSection, archived.Version, CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionParentMismatchException>();
    }

    // M-b: the restore loop only gives back what THIS archive took, so a descendant an earlier operation
    // archived separately is not restored here. The re-section must still reach it — a live-only or
    // restored-only cascade satisfies every assertion that ignores this row, and then resurfaces it, on
    // its own later restore, in a section its parent has already left.
    [Fact]
    public async Task RestoreAsync_RootIntoDifferentSection_ResectionsSeparatelyArchivedDescendants()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var origin = await NewSectionAsync(scope, "سلوك: قسم أصل لتتالي الاسترجاع");
        var destination = await NewSectionAsync(scope, "سلوك: قسم وجهة لتتالي الاسترجاع");
        var root = await writer.CreateAsync(origin, null, "سلوك: جذر يُستعاد إلى قسم مختلف", null, null, [], CancellationToken.None);
        var descendant = await writer.CreateAsync(null, root.Id, "سلوك: سليل مؤرشف بعملية منفصلة", null, null, [], CancellationToken.None);

        // Two separate archives: the descendant's own, then the root's — which cannot claim an already
        // archived row, so restoring the root leaves the descendant archived.
        await writer.DeleteAsync(descendant.Id, descendant.Version, CancellationToken.None);
        await writer.DeleteAsync(root.Id, root.Version, CancellationToken.None);

        var archivedRoot = await ReloadAsync(scope, root.Id);
        await writer.RestoreAsync(root.Id, destination, archivedRoot.Version, CancellationToken.None);

        var stillArchived = await ReloadAsync(scope, descendant.Id);
        stillArchived.DeletedAtUtc.Should().NotBeNull("this restore never claimed it");
        stillArchived.SectionId.Should().Be(destination, "the re-section reaches the rows the restore did not");
    }

    // M-a: the same scenario carried one step further. The descendant's own restore must derive from its
    // parent's CURRENT section — trusting the value stored on the archived row is exactly the defect the
    // cascade above closes, and a NOT NULL column cannot see it, since the stored value is present.
    [Fact]
    public async Task RestoreAsync_ChildRestoredAfterAncestorResection_DerivesParentsCurrentSection()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var origin = await NewSectionAsync(scope, "سلوك: قسم أصل لاشتقاق بعد إعادة التقسيم");
        var destination = await NewSectionAsync(scope, "سلوك: قسم وجهة لاشتقاق بعد إعادة التقسيم");
        var root = await writer.CreateAsync(origin, null, "سلوك: جذر يُعاد تقسيمه قبل ابنه", null, null, [], CancellationToken.None);
        var descendant = await writer.CreateAsync(null, root.Id, "سلوك: سليل يُستعاد بعد أبيه", null, null, [], CancellationToken.None);

        await writer.DeleteAsync(descendant.Id, descendant.Version, CancellationToken.None);
        await writer.DeleteAsync(root.Id, root.Version, CancellationToken.None);

        var archivedRoot = await ReloadAsync(scope, root.Id);
        await writer.RestoreAsync(root.Id, destination, archivedRoot.Version, CancellationToken.None);

        var archivedDescendant = await ReloadAsync(scope, descendant.Id);
        var restored = await writer.RestoreAsync(descendant.Id, null, archivedDescendant.Version, CancellationToken.None);

        restored!.SectionId.Should().Be(destination);
    }

    // Reads group and count by SectionId at any depth (`Reads/Abwab/README.md`) and nothing re-derives a
    // nested door's section, so a door that changes section has to take its subtree with it. The archived
    // grandchild is the discriminating row: a live-only cascade satisfies every assertion that ignores it,
    // and then restores that grandchild into a section its parent left.
    [Fact]
    public async Task MoveAsync_AcrossSections_CarriesEveryDescendantIncludingArchivedOnes()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var origin = await sections.CreateAsync("سلوك: قسم المصدر للنقل الشجري", CancellationToken.None);
        var destination = await sections.CreateAsync("سلوك: قسم الوجهة للنقل الشجري", CancellationToken.None);
        var parent = await writer.CreateAsync(origin.Id, null, "سلوك: أب ينتقل بقسمه", null, null, [], CancellationToken.None);
        var child = await writer.CreateAsync(origin.Id, parent.Id, "سلوك: ابن ينتقل مع أبيه", null, null, [], CancellationToken.None);
        var grandchild = await writer.CreateAsync(origin.Id, child.Id, "سلوك: حفيد مؤرشف ينتقل مع جده", null, null, [], CancellationToken.None);
        await writer.DeleteAsync(grandchild.Id, grandchild.Version, CancellationToken.None);

        var moved = await writer.MoveAsync(
            parent.Id, destination.Id, null, (await ReloadAsync(scope, parent.Id)).Version, CancellationToken.None);

        moved!.SectionId.Should().Be(destination.Id);
        (await ReloadAsync(scope, child.Id)).SectionId.Should().Be(destination.Id);
        (await ReloadAsync(scope, grandchild.Id)).SectionId
            .Should().Be(destination.Id, "an archived descendant keeps its parent_id through soft-delete, so it must follow too");
    }

    [Fact]
    public async Task BulkMoveAsync_AcrossSections_CarriesEachMovedDoorsSubtree()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var origin = await sections.CreateAsync("سلوك: قسم المصدر للنقل الجماعي الشجري", CancellationToken.None);
        var destination = await sections.CreateAsync("سلوك: قسم الوجهة للنقل الجماعي الشجري", CancellationToken.None);
        var first = await writer.CreateAsync(origin.Id, null, "سلوك: أول باب لنقل جماعي عبر الأقسام", null, null, [], CancellationToken.None);
        var firstChild = await writer.CreateAsync(origin.Id, first.Id, "سلوك: ابن الأول في نقل جماعي", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(origin.Id, null, "سلوك: ثاني باب لنقل جماعي عبر الأقسام", null, null, [], CancellationToken.None);

        await writer.BulkMoveAsync(
            [new AbwabBulkDoorRef(first.Id, first.Version), new AbwabBulkDoorRef(second.Id, second.Version)],
            destination.Id, null, CancellationToken.None);

        (await ReloadAsync(scope, firstChild.Id)).SectionId
            .Should().Be(destination.Id, "the bulk path owes the same subtree guarantee as the single move");
    }

    // `int?` cannot tell an omitted sectionId from an explicit null, so under a parent null has to mean
    // "unspecified" and derive — the one place the root-scope rejection deliberately does not reach.
    [Fact]
    public async Task CreateAsync_UnderAParent_DerivesTheSectionWhenNoneIsStated()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var section = await sections.CreateAsync("سلوك: قسم يُشتق للابن", CancellationToken.None);
        var parent = await writer.CreateAsync(section.Id, null, "سلوك: أب يمنح قسمه", null, null, [], CancellationToken.None);

        var child = await writer.CreateAsync(null, parent.Id, "سلوك: ابن بلا قسم مذكور", null, null, [], CancellationToken.None);

        child.SectionId.Should().Be(section.Id);
    }

    [Fact]
    public async Task CreateAsync_UnderAParentInAnotherSection_ThrowsSectionParentMismatchException()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();

        var parentSection = await sections.CreateAsync("سلوك: قسم الأب للتعارض", CancellationToken.None);
        var otherSection = await sections.CreateAsync("سلوك: قسم مخالف للتعارض", CancellationToken.None);
        var parent = await writer.CreateAsync(parentSection.Id, null, "سلوك: أب في قسم محدد", null, null, [], CancellationToken.None);

        var act = async () => await writer.CreateAsync(
            otherSection.Id, parent.Id, "سلوك: ابن بقسم مخالف", null, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<AbwabSectionParentMismatchException>();
    }

    [Fact]
    public async Task BulkArchiveAsync_WithOneStaleVersion_LeavesBothDoorsLive()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var section = await NewSectionAsync(scope, "سلوك: قسم الأرشفة الجماعية القديمة");

        var first = await writer.CreateAsync(section, null, "سلوك: أول باب للأرشفة الجماعية", null, null, [], CancellationToken.None);
        var second = await writer.CreateAsync(section, null, "سلوك: ثاني باب للأرشفة الجماعية", null, null, [], CancellationToken.None);

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
        var section = await NewSectionAsync(scope, "سلوك: قسم الدفعة التي تحوي أبًا وابنه");

        var parent = await writer.CreateAsync(section, null, "سلوك: أب ضمن دفعة تحوي ابنه", null, null, [], CancellationToken.None);
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
        var section = await NewSectionAsync(scope, "سلوك: قسم الأسماء البديلة");

        var door = await writer.CreateAsync(
            section, null, "سلوك: باب لدلالة الأسماء البديلة", null, null, ["مُبقى", "مُسقط"], CancellationToken.None);

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

    // Every root-scope write names a section now, so each test that used to create at (NULL, NULL) brings
    // its own — which also scopes its siblings away from every other test's rows on this shared fixture.
    private static async Task<int> NewSectionAsync(AsyncServiceScope scope, string name)
    {
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        return (await sections.CreateAsync(name, CancellationToken.None)).Id;
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
