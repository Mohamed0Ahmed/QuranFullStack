using System.ComponentModel.DataAnnotations;
using QuranDashboard.Application.Abstractions.Abwab.Templates;

namespace QuranDashboard.Api.Abwab.Templates;

public sealed record AddDoorTemplateRequest(
    [property: Required] string Name,
    string? Description,
    long ExpectedTimelineGeneration);

public sealed record EditDoorTemplateRequest(
    [property: Required] string Name,
    string? Description,
    uint ExpectedVersion,
    long ExpectedTimelineGeneration);

public sealed record DeleteDoorTemplateRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record RestoreDoorTemplateRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record AddTemplateNodeRequest(
    Guid? ParentTemplateNodeId,
    [property: Required] string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    long ExpectedTemplateRevision,
    long ExpectedTimelineGeneration);

public sealed record EditTemplateNodeRequest(
    [property: Required] string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    uint ExpectedVersion,
    long ExpectedTimelineGeneration);

// An explicit action endpoint, not a drag payload: the destination is named, never inferred from a
// drop position (§14.3 no-drag).
public sealed record ReparentTemplateNodeRequest(
    Guid? NewParentTemplateNodeId,
    uint ExpectedVersion,
    long ExpectedTemplateRevision,
    long ExpectedTimelineGeneration);

// A duplicated or empty id list is malformed input, not a domain conflict: it fails here as the
// framework HTTP 400 the [ApiController] validation convention produces, so no abwab.* body code is
// invented for it. A count mismatch against the live siblings is genuine staleness and stays a 409.
// The rule itself lives in TemplateNodeOrderRules so this layer and the handler cannot drift apart.
public sealed record ReorderTemplateNodesRequest(
    Guid? ParentTemplateNodeId,
    [property: Required] IReadOnlyList<Guid> OrderedTemplateNodeIds,
    long ExpectedTemplateRevision,
    long ExpectedTimelineGeneration) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TemplateNodeOrderRules.IsWellFormedOrder(OrderedTemplateNodeIds))
        {
            yield return new ValidationResult(
                ApiMessages.AbwabTemplateReorderMalformed, [nameof(OrderedTemplateNodeIds)]);
        }
    }
}

public sealed record RemoveTemplateNodeRequest(
    uint ExpectedVersion,
    long ExpectedTemplateRevision,
    long ExpectedTimelineGeneration);

public sealed record AddTemplateNodeAliasRequest(
    [property: Required] string Value,
    long ExpectedTimelineGeneration);

public sealed record EditTemplateNodeAliasRequest(
    [property: Required] string Value,
    uint ExpectedVersion,
    long ExpectedTimelineGeneration);

public sealed record RemoveTemplateNodeAliasRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record RestoreTemplateNodeAliasRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record ApplyDoorTemplateRequest(
    Guid TargetCategoryId,
    long ExpectedTemplateRevision,
    long ExpectedTreeRevision,
    uint ExpectedTargetVersion,
    long ExpectedTimelineGeneration);
