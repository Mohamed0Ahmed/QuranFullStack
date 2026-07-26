using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Templates;

public sealed class TemplateNode : IAbwabAuditable
{
    public Guid TemplateNodeId { get; set; }

    public Guid DoorTemplateId { get; set; }

    public Guid? ParentTemplateNodeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    // Deliberately a plain string: the first Abwab→Quran foreign key is owned by 031, so no ayah
    // reference or validation may be introduced here (NoPrematureQuranFkTests guards this).
    public string? RepresentativeQuranExcerpt { get; set; }

    public string? Description { get; set; }

    public int SiblingOrder { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }
}
