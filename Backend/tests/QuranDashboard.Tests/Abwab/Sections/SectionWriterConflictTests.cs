using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Sections;

[Collection(nameof(AbwabDbCollection))]
public sealed class SectionWriterConflictTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_WithADuplicateNormalizedName_MapsToSectionNameConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        await writePort.AddSectionAsync(new AddSectionCommand("قسم التلاوة", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
        var act = () => writePort.AddSectionAsync(new AddSectionCommand("قسم التلاوة", revision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.SectionNameConflict);
    }

    [Fact]
    public async Task Edit_ToADuplicateNormalizedName_MapsToSectionNameConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var firstId = await writePort.AddSectionAsync(new AddSectionCommand("قسم الأول", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var revisionBeforeSecond = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
        await writePort.AddSectionAsync(new AddSectionCommand("قسم الثاني", revisionBeforeSecond, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var second = await LoadSectionAsync(fixture, "قسم الثاني");
        var revisionBeforeEdit = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.EditSectionAsync(
            new EditSectionCommand(second.Id, "قسم الأول", second.Version, revisionBeforeEdit, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.SectionNameConflict);
        firstId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_ANonEmptySection_MapsToSectionNotEmpty()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionId = await writePort.AddSectionAsync(new AddSectionCommand("قسم به أبواب", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var revisionBeforeCategory = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
        await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب في القسم", null, null, null, sectionId, revisionBeforeCategory, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var section = await LoadSectionByIdAsync(fixture, sectionId);
        var revisionBeforeDelete = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.DeleteSectionAsync(
            new DeleteSectionCommand(sectionId, section.Version, revisionBeforeDelete, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.SectionNotEmpty);
    }

    [Fact]
    public async Task Delete_AnEmptySection_Succeeds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionId = await writePort.AddSectionAsync(new AddSectionCommand("قسم فارغ", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var section = await LoadSectionByIdAsync(fixture, sectionId);
        var revision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.DeleteSectionAsync(
            new DeleteSectionCommand(sectionId, section.Version, revision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rename_ThePermanentDefaultSection_MapsToPermanentDefaultSection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var permanentDefault = await db.AbwabSections.SingleAsync(s => s.IsPermanentDefault);
        var revision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.EditSectionAsync(
            new EditSectionCommand(permanentDefault.SectionId, "اسم جديد", permanentDefault.Version, revision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.PermanentDefaultSection);
    }

    [Fact]
    public async Task Delete_ThePermanentDefaultSection_MapsToPermanentDefaultSection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var permanentDefault = await db.AbwabSections.SingleAsync(s => s.IsPermanentDefault);
        var revision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.DeleteSectionAsync(
            new DeleteSectionCommand(permanentDefault.SectionId, permanentDefault.Version, revision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.PermanentDefaultSection);
    }

    [Fact]
    public async Task Add_ASecondPermanentDefaultSectionByName_IsNotBlockedByThePermanentDefaultCode()
    {
        // Duplicating the permanent default is prevented by the ordinary name-uniqueness check, not the
        // permanent-default guard (which only applies to rename/delete/duplicate OF the tracked flag itself).
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var act = () => writePort.AddSectionAsync(
            new AddSectionCommand(SectionConfiguration.PermanentDefaultSectionName, 0, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.SectionNameConflict);
    }

    [Fact]
    public async Task Reorder_ThePermanentDefaultSection_Succeeds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var permanentDefault = await db.AbwabSections.SingleAsync(s => s.IsPermanentDefault);
        var otherId = await writePort.AddSectionAsync(new AddSectionCommand("قسم آخر", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var other = await LoadSectionByIdAsync(fixture, otherId);
        var revision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.ReorderSectionsAsync(
            new ReorderSectionsCommand(
                [
                    new SectionOrderEntry(permanentDefault.SectionId, 5, permanentDefault.Version),
                    new SectionOrderEntry(other.Id, 0, other.Version),
                ],
                revision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();

        var reloaded = await db.AbwabSections.AsNoTracking().SingleAsync(s => s.IsPermanentDefault);
        reloaded.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Add_WithAStaleExpectedTreeRevision_MapsToTreeRevisionStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        await writePort.AddSectionAsync(new AddSectionCommand("قسم أول للاختبار", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var act = () => writePort.AddSectionAsync(new AddSectionCommand("قسم ثان للاختبار", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.TreeRevisionStale);
    }

    private static async Task<(Guid Id, uint Version)> LoadSectionAsync(PostgresFixture fixture, string name)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var section = await context.AbwabSections.AsNoTracking().SingleAsync(s => s.Name == name);
        return (section.SectionId, section.Version);
    }

    private static async Task<(Guid Id, uint Version)> LoadSectionByIdAsync(PostgresFixture fixture, Guid sectionId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var section = await context.AbwabSections.AsNoTracking().SingleAsync(s => s.SectionId == sectionId);
        return (section.SectionId, section.Version);
    }
}
