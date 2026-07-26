using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abwab.Relationships;

// §6.3 specialized relationship payload shapes. They carry STRUCTURE only — the Broader/Narrower
// inverse label and the type labels are derived for display by the render component, never stored
// as a second row and never duplicated as Arabic strings across two layers.
public sealed record RelationshipEndpointAuditView(
    Guid CategoryId,
    string Name,
    string? SectionName,
    IReadOnlyList<string> HistoricalPath);

public sealed record RelationshipStateAuditView(
    RelationshipType RelationshipType,
    bool IsDirectional,
    RelationshipEndpointAuditView From,
    RelationshipEndpointAuditView To);

// No protection-blocker facet: applicable Relationship protection on any endpoint aborts the whole
// mutation before a ChangeSet exists, so no audited relationship event can ever carry one. A blocked
// attempt surfaces as the abwab.manual_protection conflict instead of an audit row.
public sealed record RelationshipAuditView(
    Guid CategoryRelationshipId,
    string Action,
    RelationshipStateAuditView? Before,
    RelationshipStateAuditView? After);
