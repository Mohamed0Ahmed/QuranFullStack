using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Queries.GetTemplate;
using QuranDashboard.Application.Abwab.Queries.GetTemplates;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab")]
public sealed class AbwabTemplatesController(
    GetTemplatesHandler getTemplatesHandler,
    GetTemplateHandler getTemplateHandler) : ControllerBase
{
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabTemplateSummaryDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var outcome = await getTemplatesHandler.HandleAsync(new GetTemplatesQuery(), cancellationToken);

        return outcome switch
        {
            GetTemplatesOutcome.Success success =>
                Ok(ApiResponse<IReadOnlyList<AbwabTemplateSummaryDto>>.Ok(success.Templates, ApiMessages.AbwabTemplatesLoaded)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetTemplatesOutcome)} variant."),
        };
    }

    [HttpGet("templates/{templateId:int}")]
    public async Task<ActionResult<ApiResponse<AbwabTemplateDto>>> Get(
        int templateId, CancellationToken cancellationToken)
    {
        var outcome = await getTemplateHandler.HandleAsync(new GetTemplateQuery(templateId), cancellationToken);

        return outcome switch
        {
            GetTemplateOutcome.Success success =>
                Ok(ApiResponse<AbwabTemplateDto>.Ok(success.Template, ApiMessages.AbwabTemplateLoaded)),
            GetTemplateOutcome.NotFound =>
                NotFound(ApiResponse<AbwabTemplateDto>.Fail(ApiMessages.AbwabTemplateNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetTemplateOutcome)} variant."),
        };
    }
}
