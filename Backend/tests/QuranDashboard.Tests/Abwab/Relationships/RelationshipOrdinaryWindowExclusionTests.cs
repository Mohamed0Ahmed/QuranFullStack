using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipOrdinaryWindowExclusionTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddDays(30);

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheFourRelationshipMutations_StartTheOrdinaryWindowZeroTimes()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture, new FixedServerClock(Now));
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await RunAllFourMutationsAsync(writePort, db, lower.CategoryId, higher.CategoryId);

        var endpoints = await db.AbwabCategories.AsNoTracking()
            .Where(c => c.CategoryId == lower.CategoryId || c.CategoryId == higher.CategoryId)
            .ToListAsync();

        endpoints.Should().HaveCount(2);
        endpoints.Should().OnlyContain(c => c.OrdinaryProtectionLastEditedAtUtc == null && c.OrdinaryProtectionActorSubject == null,
            "a relationship mutation never starts or restarts the ordinary 24-hour window on its endpoints");
    }

    [Fact]
    public async Task TheFourRelationshipMutations_AreBlockedByAnActiveOrdinaryWindowZeroTimes()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture, new FixedServerClock(Now));
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await StartOrdinaryWindowAsync(lower.CategoryId, "another-actor");
        await StartOrdinaryWindowAsync(higher.CategoryId, "another-actor");

        await RunAllFourMutationsAsync(writePort, db, lower.CategoryId, higher.CategoryId);

        var stillOpen = await db.AbwabCategories.AsNoTracking()
            .Where(c => c.CategoryId == lower.CategoryId || c.CategoryId == higher.CategoryId)
            .ToListAsync();

        stillOpen.Should().OnlyContain(c => c.OrdinaryProtectionActorSubject == "another-actor",
            "the window belongs to the other actor and a relationship mutation neither reads nor rewrites it");
    }

    private static async Task RunAllFourMutationsAsync(
        IAbwabRelationshipWritePort writePort, QuranDashboardDbContext db, Guid first, Guid second)
    {
        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, first, second, ExpectedTimelineGeneration.Of(0), "relationship-actor"),
            CancellationToken.None);

        await writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Opposite, first, second, await VersionAsync(db, relationshipId),
                ExpectedTimelineGeneration.Of(0), "relationship-actor"),
            CancellationToken.None);

        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, await VersionAsync(db, relationshipId), ExpectedTimelineGeneration.Of(0), "relationship-actor"),
            CancellationToken.None);

        await writePort.RestoreAsync(
            new RestoreRelationshipCommand(relationshipId, await VersionAsync(db, relationshipId), ExpectedTimelineGeneration.Of(0), "relationship-actor"),
            CancellationToken.None);
    }

    private static async Task<uint> VersionAsync(QuranDashboardDbContext db, Guid relationshipId) =>
        (await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId)).Version;

    private async Task StartOrdinaryWindowAsync(Guid categoryId, string actorSubject)
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        var category = await db.AbwabCategories.SingleAsync(c => c.CategoryId == categoryId);
        category.OrdinaryProtectionActorSubject = actorSubject;
        category.OrdinaryProtectionLastEditedAtUtc = Now.AddHours(-1);
        db.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
        await db.SaveChangesAsync();
    }
}
