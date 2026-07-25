using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Relationships;

public sealed class CategoryRelationship : IAbwabAuditable
{
    public Guid CategoryRelationshipId { get; set; }

    public RelationshipType RelationshipType { get; set; }

    public Guid? LowerCategoryId { get; set; }

    public Guid? HigherCategoryId { get; set; }

    public Guid? SourceCategoryId { get; set; }

    public Guid? TargetCategoryId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }

    public bool IsDirectional => RelationshipType == RelationshipType.BroaderNarrower;

    public bool HasValidShape() => IsDirectional ? HasValidDirectionalShape() : HasValidMutualShape();

    public IReadOnlyList<Guid> EndpointCategoryIds =>
        EndpointsOf(RelationshipType, LowerCategoryId, HigherCategoryId, SourceCategoryId, TargetCategoryId);

    // The one place the stored columns are turned into an endpoint pair, so no reader — row or
    // application-side shape — can pick the columns of the wrong shape. Ordered: mutual is
    // (lower, higher), directional is (broader, narrower).
    public static IReadOnlyList<Guid> EndpointsOf(
        RelationshipType relationshipType,
        Guid? lowerCategoryId,
        Guid? higherCategoryId,
        Guid? sourceCategoryId,
        Guid? targetCategoryId) =>
        relationshipType == RelationshipType.BroaderNarrower
            ? [sourceCategoryId!.Value, targetCategoryId!.Value]
            : [lowerCategoryId!.Value, higherCategoryId!.Value];

    // Mutual endpoints are stored in canonical order so a reverse duplicate collapses onto the same
    // active unique-index key instead of becoming a second row for one relationship.
    public static (Guid Lower, Guid Higher) Canonicalize(Guid first, Guid second) =>
        first.CompareTo(second) < 0 ? (first, second) : (second, first);

    private bool HasValidMutualShape() =>
        LowerCategoryId is { } lower &&
        HigherCategoryId is { } higher &&
        SourceCategoryId is null &&
        TargetCategoryId is null &&
        lower.CompareTo(higher) < 0;

    private bool HasValidDirectionalShape() =>
        SourceCategoryId is { } source &&
        TargetCategoryId is { } target &&
        LowerCategoryId is null &&
        HigherCategoryId is null &&
        source != target;
}
