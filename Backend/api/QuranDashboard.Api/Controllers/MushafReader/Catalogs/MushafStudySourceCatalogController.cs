using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafStudySourceCatalog;

namespace QuranDashboard.Api.Controllers.MushafReader.Catalogs;

[ApiController]
[Route("api/mushaf/study-sources")]
public sealed class MushafStudySourceCatalogController(GetMushafStudySourceCatalogHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع فهرس مصادر الدراسة المتاحة لقارئ المصحف (التفاسير والترجمات وما شابه) بترتيب العرض.
    /// </summary>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل فهرس مصادر الدراسة بنجاح.</response>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MushafStudySourceCatalogResponse>>> Get(CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetMushafStudySourceCatalogQuery(), cancellationToken);
        return Ok(ApiResponse<MushafStudySourceCatalogResponse>.Ok(data, ApiMessages.MushafStudySourceCatalogLoaded));
    }
}
