using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Abwab.Templates;

[ApiController]
[Route("api/abwab/templates")]
public sealed class TemplatesController(
    IAbwabTemplateReadPort readPort,
    IAbwabTemplateWritePort writePort,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalogue.TemplateView)]
    public async Task<ActionResult<ApiResponse<DoorTemplateListDto>>> GetTemplates(
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var templates = await readPort.GetTemplatesAsync(includeDeleted, cancellationToken);

        return Ok(ApiResponse<DoorTemplateListDto>.Ok(templates, ApiMessages.AbwabTemplatesLoaded));
    }

    [HttpGet("{doorTemplateId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateView)]
    public async Task<ActionResult<ApiResponse<DoorTemplateDetailDto>>> GetTemplate(
        Guid doorTemplateId,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var template = await readPort.GetTemplateAsync(doorTemplateId, includeDeleted, cancellationToken);

        return template is null
            ? NotFound(ApiResponse<DoorTemplateDetailDto>.Fail(ApiMessages.AbwabTemplateNotFound))
            : Ok(ApiResponse<DoorTemplateDetailDto>.Ok(template, ApiMessages.AbwabTemplateLoaded));
    }

    [HttpGet("{doorTemplateId:guid}/history")]
    [Authorize(Policy = PermissionCatalogue.TemplateView)]
    public async Task<ActionResult<ApiResponse<TemplateHistoryDto>>> GetHistory(
        Guid doorTemplateId,
        CancellationToken cancellationToken)
    {
        var history = await readPort.GetHistoryAsync(doorTemplateId, cancellationToken);

        return Ok(ApiResponse<TemplateHistoryDto>.Ok(history, ApiMessages.AbwabTemplateHistoryLoaded));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalogue.TemplateAdd)]
    public async Task<ActionResult<ApiResponse<object>>> Add(AddDoorTemplateRequest request, CancellationToken cancellationToken)
    {
        var doorTemplateId = await writePort.AddDoorTemplateAsync(
            new AddDoorTemplateCommand(
                request.Name, request.Description, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { doorTemplateId }, ApiMessages.AbwabTemplateAdded));
    }

    [HttpPut("{doorTemplateId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> Edit(
        Guid doorTemplateId,
        EditDoorTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.EditDoorTemplateAsync(
            new EditDoorTemplateCommand(
                doorTemplateId, request.Name, request.Description, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateEdited));
    }

    [HttpDelete("{doorTemplateId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateDelete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid doorTemplateId,
        DeleteDoorTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.DeleteDoorTemplateAsync(
            new DeleteDoorTemplateCommand(
                doorTemplateId, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateDeleted));
    }

    [HttpPost("{doorTemplateId:guid}/restore")]
    [Authorize(Policy = PermissionCatalogue.TemplateRestore)]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid doorTemplateId,
        RestoreDoorTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.RestoreDoorTemplateAsync(
            new RestoreDoorTemplateCommand(
                doorTemplateId, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateRestored));
    }

    [HttpPost("{doorTemplateId:guid}/nodes")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> AddNode(
        Guid doorTemplateId,
        AddTemplateNodeRequest request,
        CancellationToken cancellationToken)
    {
        var templateNodeId = await writePort.AddTemplateNodeAsync(
            new AddTemplateNodeCommand(
                doorTemplateId, request.ParentTemplateNodeId, request.Name, request.RepresentativeQuranExcerpt,
                request.Description, request.ExpectedTemplateRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { templateNodeId }, ApiMessages.AbwabTemplateNodeAdded));
    }

    [HttpPut("nodes/{templateNodeId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> EditNode(
        Guid templateNodeId,
        EditTemplateNodeRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.EditTemplateNodeAsync(
            new EditTemplateNodeCommand(
                templateNodeId, request.Name, request.RepresentativeQuranExcerpt, request.Description,
                request.ExpectedVersion, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateNodeEdited));
    }

    [HttpPost("nodes/{templateNodeId:guid}/reparent")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> ReparentNode(
        Guid templateNodeId,
        ReparentTemplateNodeRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.ReparentTemplateNodeAsync(
            new ReparentTemplateNodeCommand(
                templateNodeId, request.NewParentTemplateNodeId, request.ExpectedVersion, request.ExpectedTemplateRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateNodeReparented));
    }

    [HttpPost("{doorTemplateId:guid}/nodes/reorder")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> ReorderNodes(
        Guid doorTemplateId,
        ReorderTemplateNodesRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.ReorderTemplateNodesAsync(
            new ReorderTemplateNodesCommand(
                doorTemplateId, request.ParentTemplateNodeId, request.OrderedTemplateNodeIds, request.ExpectedTemplateRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateNodesReordered));
    }

    [HttpDelete("nodes/{templateNodeId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveNode(
        Guid templateNodeId,
        RemoveTemplateNodeRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.RemoveTemplateNodeAsync(
            new RemoveTemplateNodeCommand(
                templateNodeId, request.ExpectedVersion, request.ExpectedTemplateRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateNodeRemoved));
    }

    [HttpPost("nodes/{templateNodeId:guid}/aliases")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> AddNodeAlias(
        Guid templateNodeId,
        AddTemplateNodeAliasRequest request,
        CancellationToken cancellationToken)
    {
        var templateNodeSearchAliasId = await writePort.AddTemplateNodeAliasAsync(
            new AddTemplateNodeAliasCommand(
                templateNodeId, request.Value, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { templateNodeSearchAliasId }, ApiMessages.AbwabTemplateAliasAdded));
    }

    [HttpPut("aliases/{templateNodeSearchAliasId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> EditNodeAlias(
        Guid templateNodeSearchAliasId,
        EditTemplateNodeAliasRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.EditTemplateNodeAliasAsync(
            new EditTemplateNodeAliasCommand(
                templateNodeSearchAliasId, request.Value, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateAliasEdited));
    }

    [HttpDelete("aliases/{templateNodeSearchAliasId:guid}")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveNodeAlias(
        Guid templateNodeSearchAliasId,
        RemoveTemplateNodeAliasRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.RemoveTemplateNodeAliasAsync(
            new RemoveTemplateNodeAliasCommand(
                templateNodeSearchAliasId, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateAliasRemoved));
    }

    [HttpPost("aliases/{templateNodeSearchAliasId:guid}/restore")]
    [Authorize(Policy = PermissionCatalogue.TemplateEdit)]
    public async Task<ActionResult<ApiResponse<object>>> RestoreNodeAlias(
        Guid templateNodeSearchAliasId,
        RestoreTemplateNodeAliasRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.RestoreTemplateNodeAliasAsync(
            new RestoreTemplateNodeAliasCommand(
                templateNodeSearchAliasId, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateAliasRestored));
    }

    [HttpPost("{doorTemplateId:guid}/apply")]
    [Authorize(Policy = PermissionCatalogue.TemplateApply)]
    public async Task<ActionResult<ApiResponse<object>>> Apply(
        Guid doorTemplateId,
        ApplyDoorTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await writePort.ApplyDoorTemplateAsync(
            new ApplyDoorTemplateCommand(
                doorTemplateId, request.TargetCategoryId, request.ExpectedTemplateRevision, request.ExpectedTreeRevision,
                request.ExpectedTargetVersion, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabTemplateApplied));
    }
}
