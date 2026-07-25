using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abwab.Relationships;

// Canonicalizing here — and only here — is what makes a reverse mutual submission collapse onto the
// same active unique-index key instead of becoming a second row for one relationship.
internal readonly record struct RelationshipShape(
    RelationshipType RelationshipType,
    Guid? LowerCategoryId,
    Guid? HigherCategoryId,
    Guid? SourceCategoryId,
    Guid? TargetCategoryId)
{
    public static RelationshipShape From(RelationshipType relationshipType, Guid firstCategoryId, Guid secondCategoryId)
    {
        if (firstCategoryId == secondCategoryId)
        {
            throw new ArgumentException("A relationship cannot link a category to itself.", nameof(firstCategoryId));
        }

        if (relationshipType == RelationshipType.BroaderNarrower)
        {
            return new RelationshipShape(relationshipType, null, null, firstCategoryId, secondCategoryId);
        }

        var (lower, higher) = CategoryRelationship.Canonicalize(firstCategoryId, secondCategoryId);
        return new RelationshipShape(relationshipType, lower, higher, null, null);
    }

    public static RelationshipShape Of(CategoryRelationship relationship) => new(
        relationship.RelationshipType,
        relationship.LowerCategoryId,
        relationship.HigherCategoryId,
        relationship.SourceCategoryId,
        relationship.TargetCategoryId);

    public bool IsDirectional => RelationshipType == RelationshipType.BroaderNarrower;

    public IReadOnlyList<Guid> EndpointCategoryIds => IsDirectional
        ? [SourceCategoryId!.Value, TargetCategoryId!.Value]
        : [LowerCategoryId!.Value, HigherCategoryId!.Value];

    public CategoryRelationship ToNewRow(Guid categoryRelationshipId)
    {
        var relationship = new CategoryRelationship { CategoryRelationshipId = categoryRelationshipId };
        ApplyTo(relationship);
        return relationship;
    }

    public void ApplyTo(CategoryRelationship relationship)
    {
        relationship.RelationshipType = RelationshipType;
        relationship.LowerCategoryId = LowerCategoryId;
        relationship.HigherCategoryId = HigherCategoryId;
        relationship.SourceCategoryId = SourceCategoryId;
        relationship.TargetCategoryId = TargetCategoryId;
    }
}
