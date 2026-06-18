using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Api.Controllers.Mushaf;

/// <summary>
/// تحليل كلمة واحدة: الهوية والصرف والمقاطع الملونة المرتبطة.
/// </summary>
[ApiController]
[Route("api/mushaf/words")]
public sealed class MushafWordAnalysisController(GetWordAnalysisHandler handler) : ControllerBase
{
    /// <summary>
    /// يُرجع تحليل كلمة قابلة للقراءة (ليست علامة نهاية آية): تكرارها،
    /// صرفها الرئيسي، ومقاطعها المرتبة مع فتحات لون ثابتة وإعراب مبسّط.
    /// </summary>
    /// <param name="wordLocation">موقع الكلمة بصيغة <c>surah:ayah:word</c> (مثل <c>2:25:3</c>).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل تحليل الكلمة بنجاح.</response>
    /// <response code="400">موقع غير صالح أو كلمة غير قابلة للتحليل (علامة آية).</response>
    /// <response code="404">الكلمة غير موجودة أو بيانات التحليل ناقصة.</response>
    [HttpGet("{wordLocation}/analysis")]
    public async Task<ActionResult<ApiResponse<WordAnalysisResponse>>> GetAnalysis(
        string wordLocation,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery(wordLocation),
            cancellationToken);

        return outcome switch
        {
            GetWordAnalysisOutcome.Success success =>
                Ok(ApiResponse<WordAnalysisResponse>.Ok(success.Response, ApiMessages.MushafWordAnalysisLoaded)),
            GetWordAnalysisOutcome.InvalidWordLocation =>
                BadRequest(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafInvalidWordLocation)),
            GetWordAnalysisOutcome.NotAnalyzable =>
                BadRequest(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafWordNotAnalyzable)),
            GetWordAnalysisOutcome.NotFound =>
                NotFound(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.NotFound)),
            GetWordAnalysisOutcome.IncompleteData =>
                NotFound(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafWordAnalysisIncomplete)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordAnalysisOutcome)} variant."),
        };
    }
}
