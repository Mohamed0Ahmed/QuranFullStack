using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

/// <summary>
/// المتشابهات اللفظية لآية محددة: مجموعات عبارات/نطاقات كلمات.
/// </summary>
[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahMutashabihatController(GetAyahMutashabihatHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع المتشابهات اللفظية للآية المحددة مجمّعة حسب المجموعة، مع
    /// حدوثات الآية المحددة وكل حدوثات المجموعة مرتبة بترتيب المصحف.
    /// </summary>
    /// <param name="verseKey">مفتاح الآية بصيغة <c>surah:ayah</c> (مثل <c>2:25</c>).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل المتشابهات اللفظية بنجاح، بما في ذلك القائمة الفارغة.</response>
    /// <response code="400">مفتاح آية غير صالح.</response>
    /// <response code="404">الآية غير موجودة.</response>
    [HttpGet("{verseKey}/mutashabihat")]
    public async Task<ActionResult<ApiResponse<AyahMutashabihatResponse>>> GetAyahMutashabihat(
        string verseKey,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery(verseKey), cancellationToken);

        return outcome switch
        {
            GetAyahMutashabihatOutcome.Success success =>
                Ok(ApiResponse<AyahMutashabihatResponse>.Ok(success.Response, ApiMessages.MushafAyahMutashabihatLoaded)),
            GetAyahMutashabihatOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<AyahMutashabihatResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetAyahMutashabihatOutcome.NotFound =>
                NotFound(ApiResponse<AyahMutashabihatResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAyahMutashabihatOutcome)} variant."),
        };
    }
}
