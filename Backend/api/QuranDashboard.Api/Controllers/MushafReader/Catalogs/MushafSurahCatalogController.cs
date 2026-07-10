using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafSurahCatalog;

namespace QuranDashboard.Api.Controllers.MushafReader.Catalogs;

[ApiController]
[Route("api/mushaf/surahs")]
public sealed class MushafSurahCatalogController(GetMushafSurahCatalogHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MushafSurahCatalogResponse>>> Get(CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetMushafSurahCatalogQuery(), cancellationToken);
        return Ok(ApiResponse<MushafSurahCatalogResponse>.Ok(data, ApiMessages.MushafSurahCatalogLoaded));
    }
}
