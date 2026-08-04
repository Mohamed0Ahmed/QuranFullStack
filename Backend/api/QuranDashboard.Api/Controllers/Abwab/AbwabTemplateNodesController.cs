using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Templates.AddTemplateNode;
using QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplateNode;
using QuranDashboard.Application.Abwab.Commands.Templates.EditTemplateNode;
using QuranDashboard.Application.Abwab.Commands.Templates.ReorderTemplateNode;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab")]
public sealed class AbwabTemplateNodesController(
    AddTemplateNodeHandler addNodeHandler,
    EditTemplateNodeHandler editNodeHandler,
    ReorderTemplateNodeHandler reorderNodeHandler,
    DeleteTemplateNodeHandler deleteNodeHandler) : ControllerBase
{
    [HttpPost("templates/{templateId:int}/nodes")]
    public async Task<ActionResult<ApiResponse<AbwabTemplateNodeDto>>> Add(
        int templateId, [FromBody] AddTemplateNodeBody body, CancellationToken cancellationToken)
    {
        var outcome = await addNodeHandler.HandleAsync(
            new AddTemplateNodeCommand(
                templateId, body.ParentNodeId, body.Name, body.Description, body.RepresentativeAyahText, body.Aliases),
            cancellationToken);

        return outcome switch
        {
            AddTemplateNodeOutcome.Success success =>
                Created($"/api/abwab/template-nodes/{success.Node.Id}",
                    ApiResponse<AbwabTemplateNodeDto>.Ok(success.Node, ApiMessages.AbwabTemplateNodeCreated)),
            AddTemplateNodeOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeInvalidName)),
            AddTemplateNodeOutcome.MissingParent =>
                BadRequest(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeMissingParent)),
            AddTemplateNodeOutcome.TemplateNotFound =>
                NotFound(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNotFound)),
            AddTemplateNodeOutcome.ParentNotFound =>
                NotFound(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeParentNotFound)),
            AddTemplateNodeOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(AddTemplateNodeOutcome)} variant."),
        };
    }

    [HttpPut("template-nodes/{nodeId:int}")]
    public async Task<ActionResult<ApiResponse<AbwabTemplateNodeDto>>> Edit(
        int nodeId, [FromBody] EditTemplateNodeBody body, CancellationToken cancellationToken)
    {
        var outcome = await editNodeHandler.HandleAsync(
            new EditTemplateNodeCommand(nodeId, body.Name, body.Description, body.RepresentativeAyahText, body.Aliases),
            cancellationToken);

        return outcome switch
        {
            EditTemplateNodeOutcome.Success success =>
                Ok(ApiResponse<AbwabTemplateNodeDto>.Ok(success.Node, ApiMessages.AbwabTemplateNodeEdited)),
            EditTemplateNodeOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeInvalidName)),
            EditTemplateNodeOutcome.NotFound =>
                NotFound(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeNotFound)),
            EditTemplateNodeOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(EditTemplateNodeOutcome)} variant."),
        };
    }

    [HttpPost("template-nodes/{nodeId:int}/order")]
    public async Task<ActionResult<ApiResponse<AbwabTemplateNodeDto>>> Reorder(
        int nodeId, [FromBody] ReorderTemplateNodeBody body, CancellationToken cancellationToken)
    {
        var outcome = await reorderNodeHandler.HandleAsync(
            new ReorderTemplateNodeCommand(nodeId, body.Position), cancellationToken);

        return outcome switch
        {
            ReorderTemplateNodeOutcome.Success success =>
                Ok(ApiResponse<AbwabTemplateNodeDto>.Ok(success.Node, ApiMessages.AbwabTemplateNodeReordered)),
            ReorderTemplateNodeOutcome.IsRoot =>
                BadRequest(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateRootNotReorderable)),
            ReorderTemplateNodeOutcome.InvalidPosition =>
                BadRequest(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeInvalidPosition)),
            ReorderTemplateNodeOutcome.NotFound =>
                NotFound(ApiResponse<AbwabTemplateNodeDto>.Fail(ApiMessages.AbwabTemplateNodeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(ReorderTemplateNodeOutcome)} variant."),
        };
    }

    [HttpDelete("template-nodes/{nodeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int nodeId, CancellationToken cancellationToken)
    {
        var outcome = await deleteNodeHandler.HandleAsync(
            new DeleteTemplateNodeCommand(nodeId), cancellationToken);

        return outcome switch
        {
            DeleteTemplateNodeOutcome.Success => NoContent(),
            DeleteTemplateNodeOutcome.IsRoot =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabTemplateRootNotDeletable)),
            DeleteTemplateNodeOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabTemplateNodeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(DeleteTemplateNodeOutcome)} variant."),
        };
    }
}
