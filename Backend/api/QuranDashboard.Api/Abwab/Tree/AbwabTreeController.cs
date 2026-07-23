using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Tree;

namespace QuranDashboard.Api.Abwab.Tree;

[ApiController]
[Route("api/abwab/tree")]
public sealed class AbwabTreeController(
    GetAbwabTreeSnapshotHandler snapshotHandler,
    SearchAbwabCategoriesHandler searchHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AbwabTreeSnapshotDto>>> GetSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotHandler.HandleAsync(new GetAbwabTreeSnapshotQuery(), cancellationToken);
        return Ok(ApiResponse<AbwabTreeSnapshotDto>.Ok(snapshot, ApiMessages.AbwabTreeSnapshotLoaded));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<CategorySearchResultDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var result = await searchHandler.HandleAsync(new SearchAbwabCategoriesQuery(query ?? string.Empty), cancellationToken);
        return Ok(ApiResponse<CategorySearchResultDto>.Ok(result, ApiMessages.AbwabCategorySearchLoaded));
    }
}
