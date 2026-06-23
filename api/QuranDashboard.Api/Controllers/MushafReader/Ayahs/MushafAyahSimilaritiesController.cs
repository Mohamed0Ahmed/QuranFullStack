using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahSimilaritiesController(GetSimilarAyahsHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع قائمة مسطّحة بالآيات القريبة في المعنى للآية المحددة، مع دمج
    /// الروابط الصادرة والواردة وإزالة التكرار ثنائي الاتجاه.
    /// </summary>
    /// <param name="verseKey">مفتاح الآية بصيغة <c>surah:ayah</c> (مثل <c>2:25</c>).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل الآيات القريبة في المعنى بنجاح، بما في ذلك القائمة الفارغة.</response>
    /// <response code="400">مفتاح آية غير صالح.</response>
    /// <response code="404">الآية غير موجودة.</response>
    [HttpGet("{verseKey}/similar-ayahs")]
    public async Task<ActionResult<ApiResponse<SimilarAyahsResponse>>> GetSimilarAyahs(
        string verseKey,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery(verseKey), cancellationToken);

        return outcome switch
        {
            GetSimilarAyahsOutcome.Success success =>
                Ok(ApiResponse<SimilarAyahsResponse>.Ok(success.Response, ApiMessages.MushafSimilarAyahsLoaded)),
            GetSimilarAyahsOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<SimilarAyahsResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetSimilarAyahsOutcome.NotFound =>
                NotFound(ApiResponse<SimilarAyahsResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetSimilarAyahsOutcome)} variant."),
        };
    }
}
