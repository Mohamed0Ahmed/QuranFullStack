using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafSurahCatalog;

namespace QuranDashboard.Api.Controllers.MushafReader.Catalogs;

[ApiController]
[Route("api/mushaf/surahs")]
public sealed class MushafSurahCatalogController(GetMushafSurahCatalogHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع فهرس سور المصحف (114 سورة) بأسمائها وأرقامها وصفحات بدايتها للتنقل.
    /// </summary>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل فهرس السور بنجاح.</response>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MushafSurahCatalogResponse>>> Get(CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetMushafSurahCatalogQuery(), cancellationToken);
        return Ok(ApiResponse<MushafSurahCatalogResponse>.Ok(data, ApiMessages.MushafSurahCatalogLoaded));
    }
}
