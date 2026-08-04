using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Queries.GetAbwabTree;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/tree")]
public sealed class AbwabTreeController(
    GetAbwabTreeHandler treeHandler,
    IAbwabCacheValidators validators) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AbwabTreeDto>>> Get(CancellationToken cancellationToken)
    {
        var etag = validators.TreeETag();
        ConditionalGet.SetValidatorHeaders(Response, etag);

        if (ConditionalGet.Matches(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await treeHandler.HandleAsync(new GetAbwabTreeQuery(), cancellationToken);

        return outcome switch
        {
            GetAbwabTreeOutcome.Success success =>
                Ok(ApiResponse<AbwabTreeDto>.Ok(success.Tree, ApiMessages.AbwabTreeLoaded)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAbwabTreeOutcome)} variant."),
        };
    }
}
