using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Sections;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

// Section-order facet: SortOrder round-trips as an ordinary field, not a separate registration (§8).
public sealed class SectionRestoreAdapter : IAbwabRestoreAdapter<Section, SectionRestoreSnapshot>
{
    public const int Schema = 1;

    public string PersistedType => "Section";

    public int SnapshotSchemaVersion => Schema;

    public SectionRestoreSnapshot Capture(Section product) => new(
        Schema,
        product.SectionId,
        product.Name,
        product.NormalizedName,
        product.SortOrder,
        product.IsPermanentDefault,
        product.IsDeleted,
        product.DeletedAtUtc);

    public Section Reconstruct(SectionRestoreSnapshot snapshot) => new()
    {
        SectionId = snapshot.SectionId,
        Name = snapshot.Name,
        NormalizedName = snapshot.NormalizedName,
        SortOrder = snapshot.SortOrder,
        IsPermanentDefault = snapshot.IsPermanentDefault,
        IsDeleted = snapshot.IsDeleted,
        DeletedAtUtc = snapshot.DeletedAtUtc,
    };
}
