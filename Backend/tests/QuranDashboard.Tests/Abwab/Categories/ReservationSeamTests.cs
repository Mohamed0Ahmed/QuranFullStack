using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class ReservationSeamTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InertChecker_NeverBlocksASubtreeDelete_BecauseNoRequestStorageExistsYetIn029()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture, reservationChecker: new InertDeletionReservationChecker());
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحجز", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(categoryId, category.Version, revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await act.Should().NotThrowAsync("the 029 reservation checker is an inert stub; it never blocks");
    }

    [Fact]
    public async Task InertChecker_AlwaysReportsNotReserved()
    {
        var checker = new InertDeletionReservationChecker();

        var reserved = await checker.IsReservedByPendingAsync([Guid.NewGuid(), Guid.NewGuid()], CancellationToken.None);

        reserved.Should().BeFalse();
    }

    [Fact]
    public async Task ASeamPoint_IsWiredIntoTheSubtreeDeleteHandler_ForA032PendingAwareCheckerToReplace()
    {
        var checkedIds = new List<IReadOnlyList<Guid>>();
        var probe = new ProbeReservationChecker(ids =>
        {
            checkedIds.Add(ids);
            return false;
        });

        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture, reservationChecker: probe);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(rootId, root.Version, revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        checkedIds.Should().ContainSingle();
        checkedIds[0].Should().BeEquivalentTo([rootId, childId], "032 would install a checker that inspects every affected category, root and descendants");
    }

    private sealed class ProbeReservationChecker(Func<IReadOnlyList<Guid>, bool> isReserved) : IDeletionReservationChecker
    {
        public Task<bool> IsReservedByPendingAsync(IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken) =>
            Task.FromResult(isReserved(categoryIds));
    }
}
