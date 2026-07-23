using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Protection;

public sealed class ManualProtection : IAbwabAuditable
{
    public Guid ManualProtectionId { get; set; }

    public Guid CategoryId { get; set; }

    public ManualProtectionType ProtectionType { get; set; }

    public ManualProtectionScope ProtectionScope { get; set; }

    public string AppliedByActorSubject { get; set; } = string.Empty;

    public DateTimeOffset AppliedAtUtc { get; set; }

    public string? LiftedByActorSubject { get; set; }

    public DateTimeOffset? LiftedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }
}
