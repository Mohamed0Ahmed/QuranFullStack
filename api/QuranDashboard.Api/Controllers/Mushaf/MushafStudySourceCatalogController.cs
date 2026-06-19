using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafStudySourceCatalog;

namespace QuranDashboard.Api.Controllers.Mushaf;

[ApiController]
[Route("api/mushaf/study-sources")]
public sealed class MushafStudySourceCatalogController(GetMushafStudySourceCatalogHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MushafStudySourceCatalogResponse>>> Get(CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetMushafStudySourceCatalogQuery(), cancellationToken);
        return Ok(ApiResponse<MushafStudySourceCatalogResponse>.Ok(data, ApiMessages.MushafStudySourceCatalogLoaded));
    }
}
