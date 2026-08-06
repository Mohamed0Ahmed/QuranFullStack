using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Commands.ConfirmLogtoSubjectRelink;
using QuranDashboard.Application.Access.Commands.PreviewLogtoSubjectRelink;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/users/{userId:int}/logto-sub/relink")]
[RequireOwner]
public sealed class AccessLogtoSubjectRelinkController(
    PreviewLogtoSubjectRelinkHandler previewHandler,
    ConfirmLogtoSubjectRelinkHandler confirmHandler) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<LogtoSubjectRelinkPreview>>> Preview(
        int userId,
        [FromBody] PreviewLogtoSubjectRelinkBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await previewHandler.HandleAsync(
            new PreviewLogtoSubjectRelinkCommand(userId, body.NewSub, body.EvidenceToken),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationRelinkPreviewLoaded);
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<AccessUserDetail>>> Confirm(
        int userId,
        [FromBody] ConfirmLogtoSubjectRelinkBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await confirmHandler.HandleAsync(
            new ConfirmLogtoSubjectRelinkCommand(
                userId,
                body.ExpectedVersion,
                body.OldSub,
                body.NewSub,
                body.Reason,
                body.Confirmed,
                body.EvidenceToken),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationRelinkConfirmed);
    }
}
