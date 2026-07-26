using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

public sealed class EfAbwabTemplateReadPort(QuranDashboardDbContext db, IServerClock clock) : IAbwabTemplateReadPort
{
    public const int SchemaVersion = 1;

    private const string TemplateHistoryActionPrefix = "template.history.";

    public async Task<DoorTemplateListDto> GetTemplatesAsync(bool includeDeleted, CancellationToken cancellationToken)
    {
        var templates = await db.AbwabDoorTemplates
            .AsNoTracking()
            .Where(t => includeDeleted || !t.IsDeleted)
            .OrderBy(t => t.NormalizedName)
            .ToListAsync(cancellationToken);

        var templateIds = templates.Select(t => t.DoorTemplateId).ToList();
        var nodeCounts = await db.AbwabTemplateNodes
            .AsNoTracking()
            .Where(n => templateIds.Contains(n.DoorTemplateId) && !n.IsDeleted)
            .GroupBy(n => n.DoorTemplateId)
            .Select(group => new { DoorTemplateId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.DoorTemplateId, entry => entry.Count, cancellationToken);

        return new DoorTemplateListDto(
            SchemaVersion,
            [.. templates.Select(t => ToSummary(t, nodeCounts.GetValueOrDefault(t.DoorTemplateId)))],
            ExpectedTimelineGeneration.Of(await CurrentGenerationAsync(cancellationToken)),
            clock.UtcNow);
    }

    public async Task<DoorTemplateDetailDto?> GetTemplateAsync(Guid doorTemplateId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var template = await db.AbwabDoorTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.DoorTemplateId == doorTemplateId, cancellationToken);

        if (template is null || (template.IsDeleted && !includeDeleted))
        {
            return null;
        }

        var nodes = await db.AbwabTemplateNodes
            .AsNoTracking()
            .Where(n => n.DoorTemplateId == doorTemplateId)
            .Where(n => includeDeleted || !n.IsDeleted)
            .OrderBy(n => n.SiblingOrder)
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(n => n.TemplateNodeId).ToList();
        var aliases = await db.AbwabTemplateNodeSearchAliases
            .AsNoTracking()
            .Where(a => nodeIds.Contains(a.TemplateNodeId))
            .Where(a => includeDeleted || !a.IsDeleted)
            .ToListAsync(cancellationToken);

        var revisionState = await db.AbwabRevisionStates.AsNoTracking()
            .SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken);

        return new DoorTemplateDetailDto(
            SchemaVersion,
            ToSummary(template, nodes.Count(n => !n.IsDeleted)),
            [.. nodes.Select(node => ToNodeDto(node, aliases))],
            ExpectedTimelineGeneration.Of(revisionState.TimelineGeneration),
            revisionState.TreeRevision,
            clock.UtcNow);
    }

    // The separate template-history projection (§6.3): template CRUD events never enter the main
    // product-audit list, so they are read here by their own action prefix.
    //
    // KNOWN COST, recorded rather than hidden: both predicates are substring matches over the
    // append-only `abwab_audit_events.payload` text column, so this is an unindexable linear scan
    // that degrades as the log grows. Giving the audit event first-class indexed action-kind and
    // aggregate-id columns would fix it, but that table is `028` kernel substrate and the audit read
    // model is `033`'s — neither is `030`'s to reshape. The RESPONSE size is bounded here regardless
    // (MaxHistoryEntries), so an old, heavily-edited template cannot return an unbounded payload;
    // the scan cost itself is the part left to `033`. Frozen in T075's budget file.
    public async Task<TemplateHistoryDto> GetHistoryAsync(Guid doorTemplateId, CancellationToken cancellationToken)
    {
        // One row beyond the cap is fetched purely to decide HasMore, then dropped: a second COUNT
        // query over the same unindexable predicate would double the scan cost to learn one bit.
        var page = await db.AbwabAuditEvents
            .AsNoTracking()
            .Where(e => e.Payload.Contains(TemplateHistoryActionPrefix) && e.Payload.Contains(doorTemplateId.ToString()))
            .Join(
                db.AbwabChangeSets.AsNoTracking(),
                auditEvent => auditEvent.ChangeSetId,
                changeSet => changeSet.Id,
                (auditEvent, changeSet) => new { auditEvent, changeSet })
            .OrderByDescending(pair => pair.changeSet.ChangeSetSequence)
            .ThenByDescending(pair => pair.auditEvent.EventOrdinal)
            .Select(pair => new TemplateHistoryEntryDto(
                pair.changeSet.ChangeSetSequence,
                pair.changeSet.ActorSubject,
                pair.auditEvent.ServerTimestampUtc,
                pair.auditEvent.Payload))
            .Take(IAbwabTemplateReadPort.MaxHistoryEntries + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > IAbwabTemplateReadPort.MaxHistoryEntries;

        return new TemplateHistoryDto(
            SchemaVersion,
            doorTemplateId,
            hasMore ? page.Take(IAbwabTemplateReadPort.MaxHistoryEntries).ToList() : page,
            hasMore,
            ExpectedTimelineGeneration.Of(await CurrentGenerationAsync(cancellationToken)),
            clock.UtcNow);
    }

    private static DoorTemplateSummaryDto ToSummary(DoorTemplate template, int nodeCount) => new(
        template.DoorTemplateId,
        template.Name,
        template.Description,
        template.TemplateRevision,
        template.IsDeleted,
        template.Version,
        nodeCount);

    private static TemplateNodeDto ToNodeDto(TemplateNode node, IReadOnlyList<TemplateNodeSearchAlias> aliases) => new(
        node.TemplateNodeId,
        node.ParentTemplateNodeId,
        node.Name,
        node.RepresentativeQuranExcerpt,
        node.Description,
        node.SiblingOrder,
        node.IsDeleted,
        node.Version,
        [.. aliases.Where(alias => alias.TemplateNodeId == node.TemplateNodeId)
            .Select(alias => new TemplateNodeAliasDto(alias.TemplateNodeSearchAliasId, alias.Value, alias.IsDeleted, alias.Version))]);

    private async Task<long> CurrentGenerationAsync(CancellationToken cancellationToken) =>
        (await db.AbwabRevisionStates.AsNoTracking().SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken))
            .TimelineGeneration;
}
