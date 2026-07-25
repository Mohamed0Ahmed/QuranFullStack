using System.ComponentModel.DataAnnotations;
using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Api.Abwab.Relationships;

// A self-link is malformed input, not a domain conflict: it fails here as the framework HTTP 400 the
// [ApiController] validation convention produces, so no abwab.* body code is invented for it.
public sealed record AddRelationshipRequest(
    RelationshipType RelationshipType,
    Guid FirstCategoryId,
    Guid SecondCategoryId,
    long ExpectedTimelineGeneration) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        RelationshipRequestValidation.EndpointsDiffer(FirstCategoryId, SecondCategoryId);
}

public sealed record EditRelationshipRequest(
    RelationshipType RelationshipType,
    Guid FirstCategoryId,
    Guid SecondCategoryId,
    uint ExpectedVersion,
    long ExpectedTimelineGeneration) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        RelationshipRequestValidation.EndpointsDiffer(FirstCategoryId, SecondCategoryId);
}

public sealed record DeleteRelationshipRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record RestoreRelationshipRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

internal static class RelationshipRequestValidation
{
    public static IEnumerable<ValidationResult> EndpointsDiffer(Guid first, Guid second)
    {
        if (first == second)
        {
            yield return new ValidationResult(
                ApiMessages.AbwabRelationshipSelfLink,
                [nameof(AddRelationshipRequest.FirstCategoryId), nameof(AddRelationshipRequest.SecondCategoryId)]);
        }
    }
}
