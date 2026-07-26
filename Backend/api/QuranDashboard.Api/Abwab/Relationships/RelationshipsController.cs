using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Abwab.Relationships;

[ApiController]
[Route("api/abwab/relationships")]
public sealed class RelationshipsController(
    IAbwabRelationshipReadPort readPort,
    IAbwabRelationshipWritePort writePort,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{categoryId:guid}")]
    [Authorize(Policy = PermissionCatalogue.RelationshipView)]
    public async Task<ActionResult<ApiResponse<CategoryRelationshipListDto>>> GetForCategory(
        Guid categoryId,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var relationships = await readPort.GetForCategoryAsync(categoryId, includeDeleted, cancellationToken);

        return Ok(ApiResponse<CategoryRelationshipListDto>.Ok(relationships, ApiMessages.AbwabRelationshipsLoaded));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalogue.RelationshipAdd)]
    public async Task<ActionResult<ApiResponse<object>>> Add(AddRelationshipRequest request, CancellationToken cancellationToken)
    {
        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(
                request.RelationshipType,
                request.FirstCategoryId,
                request.SecondCategoryId,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { relationshipId }, ApiMessages.AbwabRelationshipAdded));
    }

    [HttpPut("{relationshipId:guid}")]
    [Authorize(Policy = PermissionCatalogue.RelationshipEdit)]
    public async Task<ActionResult<ApiResponse<object>>> Edit(
        Guid relationshipId,
        EditRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId,
                request.RelationshipType,
                request.FirstCategoryId,
                request.SecondCategoryId,
                request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabRelationshipEdited));
    }

    [HttpDelete("{relationshipId:guid}")]
    [Authorize(Policy = PermissionCatalogue.RelationshipDelete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid relationshipId,
        DeleteRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(
                relationshipId,
                request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabRelationshipDeleted));
    }

    [HttpPost("{relationshipId:guid}/restore")]
    [Authorize(Policy = PermissionCatalogue.RelationshipRestore)]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid relationshipId,
        RestoreRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.RestoreAsync(
            new RestoreRelationshipCommand(
                relationshipId,
                request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabRelationshipRestored));
    }
}
