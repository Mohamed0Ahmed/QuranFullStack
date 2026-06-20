using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

/// <summary>
/// دراسة آية محددة: الهوية الأساسية مع التفسير والترجمة والإعراب الكامل معاً.
/// </summary>
[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahStudyController(GetAyahStudyHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع سياق دراسة آية واحدة: بيانات الآية الأساسية مع مصدر التفسير
    /// والترجمة والإعراب الكامل المحدّد (أو الافتراضي من الإعدادات) محمّلة معاً.
    /// </summary>
    /// <param name="verseKey">مفتاح الآية بصيغة <c>surah:ayah</c> (مثل <c>2:25</c>).</param>
    /// <param name="tafsirSource">مفتاح مصدر التفسير (اختياري؛ الافتراضي من <c>MushafReader:DefaultTafsirSourceKey</c>).</param>
    /// <param name="translationSource">مفتاح مصدر الترجمة (اختياري).</param>
    /// <param name="fullI3rabSource">مفتاح مصدر الإعراب الكامل (اختياري).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل دراسة الآية بنجاح.</response>
    /// <response code="400">مفتاح آية غير صالح.</response>
    /// <response code="404">الآية غير موجودة.</response>
    [HttpGet("{verseKey}/study")]
    public async Task<ActionResult<ApiResponse<AyahStudyResponse>>> GetStudy(
        string verseKey,
        [FromQuery] string? tafsirSource,
        [FromQuery] string? translationSource,
        [FromQuery] string? fullI3rabSource,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery(verseKey, tafsirSource, translationSource, fullI3rabSource),
            cancellationToken);

        return outcome switch
        {
            GetAyahStudyOutcome.Success success =>
                Ok(ApiResponse<AyahStudyResponse>.Ok(success.Response, ApiMessages.MushafAyahStudyLoaded)),
            GetAyahStudyOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<AyahStudyResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetAyahStudyOutcome.NotFound =>
                NotFound(ApiResponse<AyahStudyResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAyahStudyOutcome)} variant."),
        };
    }
}
