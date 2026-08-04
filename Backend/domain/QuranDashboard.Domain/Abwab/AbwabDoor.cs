namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoor
{
    public int Id { get; set; }

    public int SectionId { get; set; }
    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Free text only — never an FK or a verified Quran reference.
    public string? RepresentativeAyahText { get; set; }

    public int OrderValue { get; set; }

    public int? GlobalOrderValue { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    public uint Version { get; set; }
}
