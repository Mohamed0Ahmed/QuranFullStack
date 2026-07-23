using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed class ManualProtectionRestoreAdapter : IAbwabRestoreAdapter<ManualProtection, ManualProtectionRestoreSnapshot>
{
    public const int Schema = 1;

    public string PersistedType => "ManualProtection";

    public int SnapshotSchemaVersion => Schema;

    public ManualProtectionRestoreSnapshot Capture(ManualProtection product) => new(
        Schema,
        product.ManualProtectionId,
        product.CategoryId,
        product.ProtectionType,
        product.ProtectionScope,
        product.AppliedByActorSubject,
        product.AppliedAtUtc,
        product.LiftedByActorSubject,
        product.LiftedAtUtc,
        product.IsDeleted,
        product.DeletedAtUtc);

    public ManualProtection Reconstruct(ManualProtectionRestoreSnapshot snapshot) => new()
    {
        ManualProtectionId = snapshot.ManualProtectionId,
        CategoryId = snapshot.CategoryId,
        ProtectionType = snapshot.ProtectionType,
        ProtectionScope = snapshot.ProtectionScope,
        AppliedByActorSubject = snapshot.AppliedByActorSubject,
        AppliedAtUtc = snapshot.AppliedAtUtc,
        LiftedByActorSubject = snapshot.LiftedByActorSubject,
        LiftedAtUtc = snapshot.LiftedAtUtc,
        IsDeleted = snapshot.IsDeleted,
        DeletedAtUtc = snapshot.DeletedAtUtc,
    };
}
