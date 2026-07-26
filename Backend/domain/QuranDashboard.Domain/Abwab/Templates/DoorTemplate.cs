using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Templates;

public sealed class DoorTemplate : IAbwabAuditable
{
    public Guid DoorTemplateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Aggregate logical counter (§6.4): bumped exactly once per grouped structural operation, and
    // deliberately excluded from restore snapshots — it is current technical state, not product state.
    public long TemplateRevision { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }
}
