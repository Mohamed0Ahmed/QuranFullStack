using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

namespace QuranDashboard.Api.Controllers.MushafReader.Pages;

[ApiController]
[Route("api/mushaf/pages")]
public sealed class MushafPagesController(GetMushafPageHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع صفحة مصحف واحدة مرقّمة (1–604): الأسطر والكلمات بترتيب المصحف،
    /// سياق السور والجزء والحزب والربع، وعلامات السجدة/التقسيم بقاعدة السطر الأول.
    /// </summary>
    /// <param name="pageNumber">رقم الصفحة (1–604).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل الصفحة بنجاح.</response>
    /// <response code="400">رقم صفحة غير صالح.</response>
    /// <response code="404">الصفحة غير موجودة في قاعدة البيانات.</response>
    [HttpGet("{pageNumber}")]
    public async Task<ActionResult<ApiResponse<MushafPageResponse>>> Get(
        string pageNumber,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(pageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPage))
        {
            return BadRequest(ApiResponse<MushafPageResponse>.Fail(ApiMessages.MushafInvalidPageNumber));
        }

        var outcome = await handler.HandleAsync(new GetMushafPageQuery(parsedPage), cancellationToken);

        return outcome switch
        {
            GetMushafPageOutcome.Success success =>
                Ok(ApiResponse<MushafPageResponse>.Ok(success.Response, ApiMessages.MushafPageLoaded)),
            GetMushafPageOutcome.InvalidPageNumber =>
                BadRequest(ApiResponse<MushafPageResponse>.Fail(ApiMessages.MushafInvalidPageNumber)),
            GetMushafPageOutcome.NotFound =>
                NotFound(ApiResponse<MushafPageResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetMushafPageOutcome)} variant."),
        };
    }
}
