using System.Text.Json;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Application.Abwab.Templates;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateAuditPayloadTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheApplicationEvent_CarriesTheFrozenTemplateSnapshotTargetPathCreatedTreeAndLevelCounts()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var payload = await LatestPayloadAsync(db);
        payload.GetProperty("type").GetString().Should().Be(TemplateAuditActions.Applied);

        var data = payload.GetProperty("data");
        data.GetProperty("DoorTemplateId").GetGuid().Should().Be(template.DoorTemplateId);
        data.GetProperty("TemplateSnapshot").GetProperty("Nodes").GetArrayLength().Should().Be(4);
        data.GetProperty("TargetPath").EnumerateArray().Select(p => p.GetString()).Should().Contain(target.Name);
        data.GetProperty("CreatedTree").GetArrayLength().Should().Be(2, "the created tree is rendered from its roots");
        data.GetProperty("CountsByLevel").EnumerateArray().Select(entry => entry.GetProperty("Count").GetInt32())
            .Should().Equal(2, 2);
    }

    [Fact]
    public async Task LaterTemplateEdits_LeaveTheStoredApplicationSnapshotUnchanged()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);
        var frozen = (await LatestPayloadAsync(db)).GetProperty("data").GetProperty("TemplateSnapshot").GetRawText();

        var node = await db.Set<QuranDashboard.Domain.Abwab.Templates.TemplateNode>().AsNoTracking()
            .SingleAsync(n => n.TemplateNodeId == nodes[0].TemplateNodeId);
        await writePort.EditTemplateNodeAsync(
            new EditTemplateNodeCommand(node.TemplateNodeId, "اسم بعد التطبيق", null, null, node.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var applicationEvent = await db.AbwabAuditEvents.AsNoTracking()
            .Where(e => e.Payload.Contains(TemplateAuditActions.Applied))
            .SingleAsync();

        JsonDocument.Parse(applicationEvent.Payload).RootElement
            .GetProperty("data").GetProperty("TemplateSnapshot").GetRawText()
            .Should().Be(frozen, "the snapshot is frozen at application time — later edits cannot change this rendering");
    }

    [Fact]
    public async Task TemplateCrud_ProducesTemplateHistoryEventsOnly_AndZeroMainProductAuditRows()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;

        var templateId = await writePort.AddDoorTemplateAsync(
            new AddDoorTemplateCommand("قالب جديد", null, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.AddTemplateNodeAsync(
            new AddTemplateNodeCommand(templateId, null, "جذر", null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var payloads = await db.AbwabAuditEvents.AsNoTracking().Select(e => e.Payload).ToListAsync();

        payloads.Should().OnlyContain(payload => payload.Contains(TemplateAuditActions.HistoryPrefix),
            "template CRUD renders only in the separate template-history view (§6.3)");
        payloads.Should().NotContain(payload => payload.Contains(TemplateAuditActions.Applied));
    }

    [Fact]
    public async Task ATemplateHistoryEvent_CarriesActorActionAndCompleteBeforeAndAfterTrees()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var node = await db.Set<QuranDashboard.Domain.Abwab.Templates.TemplateNode>().AsNoTracking()
            .SingleAsync(n => n.TemplateNodeId == nodes[1].TemplateNodeId);

        await writePort.EditTemplateNodeAsync(
            new EditTemplateNodeCommand(node.TemplateNodeId, "اسم معدّل", null, "وصف", node.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var data = (await LatestPayloadAsync(db)).GetProperty("data");
        data.GetProperty("DoorTemplateId").GetGuid().Should().Be(template.DoorTemplateId);
        data.GetProperty("Before").GetProperty("Nodes").GetArrayLength().Should().Be(2);
        data.GetProperty("After").GetProperty("Nodes").GetArrayLength().Should().Be(2);
        data.GetProperty("ChangedNodes").EnumerateArray()
            .Select(entry => entry.GetProperty("TemplateNodeId").GetGuid())
            .Should().Equal(node.TemplateNodeId);
    }

    private static async Task<JsonElement> LatestPayloadAsync(QuranDashboardDbContext db)
    {
        var latest = await db.AbwabAuditEvents.AsNoTracking()
            .Join(db.AbwabChangeSets.AsNoTracking(), e => e.ChangeSetId, c => c.Id, (e, c) => new { e, c })
            .OrderByDescending(pair => pair.c.ChangeSetSequence)
            .ThenByDescending(pair => pair.e.EventOrdinal)
            .Select(pair => pair.e.Payload)
            .FirstAsync();

        return JsonDocument.Parse(latest).RootElement;
    }
}
