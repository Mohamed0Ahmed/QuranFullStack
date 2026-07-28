using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Queries.GetAbwabTree;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/tree")]
public sealed class AbwabTreeController(GetAbwabTreeHandler treeHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AbwabTreeDto>>> Get(CancellationToken cancellationToken)
    {
        var outcome = await treeHandler.HandleAsync(new GetAbwabTreeQuery(), cancellationToken);

        return outcome switch
        {
            GetAbwabTreeOutcome.Success success =>
                Ok(ApiResponse<AbwabTreeDto>.Ok(success.Tree, ApiMessages.AbwabTreeLoaded)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAbwabTreeOutcome)} variant."),
        };
    }
}
