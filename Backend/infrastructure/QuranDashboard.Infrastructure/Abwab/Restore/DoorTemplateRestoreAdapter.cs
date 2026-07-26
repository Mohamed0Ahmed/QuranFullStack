using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

// ONE adapter for the whole aggregate (§8): TemplateNode and TemplateNodeSearchAlias round-trip here
// as facets, never as separate registrations.
public sealed class DoorTemplateRestoreAdapter : IAbwabRestoreAdapter<DoorTemplateAggregate, DoorTemplateRestoreSnapshot>
{
    public const int Schema = 1;

    public string PersistedType => "DoorTemplate";

    public int SnapshotSchemaVersion => Schema;

    public DoorTemplateRestoreSnapshot Capture(DoorTemplateAggregate product)
    {
        ArgumentNullException.ThrowIfNull(product);
        var template = product.Template;

        return new DoorTemplateRestoreSnapshot(
            Schema,
            template.DoorTemplateId,
            template.Name,
            template.NormalizedName,
            template.Description,
            template.IsDeleted,
            template.DeletedAtUtc,
            [.. product.Nodes.Select(CaptureNode)],
            [.. product.Aliases.Select(CaptureAlias)]);
    }

    public DoorTemplateAggregate Reconstruct(DoorTemplateRestoreSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        GuardAcyclic(snapshot.Nodes);

        var template = new DoorTemplate
        {
            DoorTemplateId = snapshot.DoorTemplateId,
            Name = snapshot.Name,
            NormalizedName = snapshot.NormalizedName,
            Description = snapshot.Description,
            IsDeleted = snapshot.IsDeleted,
            DeletedAtUtc = snapshot.DeletedAtUtc,
        };

        return new DoorTemplateAggregate(
            template,
            [.. snapshot.Nodes.Select(node => ReconstructNode(node, template.DoorTemplateId))],
            [.. snapshot.Aliases.Select(ReconstructAlias)]);
    }

    // A reconstruction that would produce a cyclic template fails rather than persisting an invalid
    // structure — §7.4's "no cyclic template can be saved, applied, rendered, or restored" — and it
    // fails with the same §11 code the writer raises, so restore and write are one channel.
    private static void GuardAcyclic(IReadOnlyList<TemplateNodeRestoreSnapshot> nodes)
    {
        if (TemplateParentChainRules.FindCycleNodeId(
                nodes.Select(node => (node.TemplateNodeId, node.ParentTemplateNodeId))) is { } cycleNodeId)
        {
            throw new AbwabWriteConflictException(
                AbwabConflictCodes.TemplateCycle,
                $"The template snapshot forms a parent cycle at node {cycleNodeId}; a cyclic template can never be restored.");
        }
    }

    private static TemplateNodeRestoreSnapshot CaptureNode(TemplateNode node) => new(
        node.TemplateNodeId,
        node.ParentTemplateNodeId,
        node.Name,
        node.NormalizedName,
        node.RepresentativeQuranExcerpt,
        node.Description,
        node.SiblingOrder,
        node.IsDeleted,
        node.DeletedAtUtc);

    private static TemplateNode ReconstructNode(TemplateNodeRestoreSnapshot snapshot, Guid doorTemplateId) => new()
    {
        TemplateNodeId = snapshot.TemplateNodeId,
        DoorTemplateId = doorTemplateId,
        ParentTemplateNodeId = snapshot.ParentTemplateNodeId,
        Name = snapshot.Name,
        NormalizedName = snapshot.NormalizedName,
        RepresentativeQuranExcerpt = snapshot.RepresentativeQuranExcerpt,
        Description = snapshot.Description,
        SiblingOrder = snapshot.SiblingOrder,
        IsDeleted = snapshot.IsDeleted,
        DeletedAtUtc = snapshot.DeletedAtUtc,
    };

    private static TemplateNodeAliasRestoreSnapshot CaptureAlias(TemplateNodeSearchAlias alias) => new(
        alias.TemplateNodeSearchAliasId,
        alias.TemplateNodeId,
        alias.Value,
        alias.NormalizedValue,
        alias.IsDeleted,
        alias.DeletedAtUtc);

    private static TemplateNodeSearchAlias ReconstructAlias(TemplateNodeAliasRestoreSnapshot snapshot) => new()
    {
        TemplateNodeSearchAliasId = snapshot.TemplateNodeSearchAliasId,
        TemplateNodeId = snapshot.TemplateNodeId,
        Value = snapshot.Value,
        NormalizedValue = snapshot.NormalizedValue,
        IsDeleted = snapshot.IsDeleted,
        DeletedAtUtc = snapshot.DeletedAtUtc,
    };
}
