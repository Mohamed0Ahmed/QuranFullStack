using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Application.Abwab.Commands.Templates.ApplyTemplate;
using QuranDashboard.Application.Abwab.Commands.Templates.CreateTemplate;
using QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplate;
using QuranDashboard.Application.Abwab.Queries.GetTemplate;
using QuranDashboard.Application.Abwab.Queries.GetTemplates;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab")]
public sealed class AbwabTemplatesController(
    GetTemplatesHandler getTemplatesHandler,
    GetTemplateHandler getTemplateHandler,
    CreateTemplateHandler createTemplateHandler,
    DeleteTemplateHandler deleteTemplateHandler,
    ApplyTemplateHandler applyTemplateHandler,
    IAbwabCacheValidators validators) : ControllerBase
{
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabTemplateSummaryDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var etag = validators.TemplatesListETag();
        ConditionalGet.SetValidatorHeaders(Response, etag);

        if (ConditionalGet.Matches(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

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
        var etag = validators.TemplateETag(templateId);
        var outcome = await getTemplateHandler.HandleAsync(new GetTemplateQuery(templateId), cancellationToken);

        return outcome switch
        {
            GetTemplateOutcome.Success when ConditionalGet.Matches(Request, etag) =>
                NotModifiedWithValidator(etag),
            GetTemplateOutcome.Success success =>
                OkWithValidator(ApiResponse<AbwabTemplateDto>.Ok(success.Template, ApiMessages.AbwabTemplateLoaded), etag),
            GetTemplateOutcome.NotFound =>
                NotFound(ApiResponse<AbwabTemplateDto>.Fail(ApiMessages.AbwabTemplateNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetTemplateOutcome)} variant."),
        };
    }

    [HttpPost("templates")]
    [RequirePermission(AbwabPermissions.Templates.Create)]
    public async Task<ActionResult<ApiResponse<AbwabTemplateDto>>> Create(
        [FromBody] CreateTemplateBody body, CancellationToken cancellationToken)
    {
        var outcome = await createTemplateHandler.HandleAsync(
            new CreateTemplateCommand(body.Name, body.Description, body.RepresentativeAyahText, body.Aliases),
            cancellationToken);

        return outcome switch
        {
            CreateTemplateOutcome.Success success =>
                Created($"/api/abwab/templates/{success.Template.Id}",
                    ApiResponse<AbwabTemplateDto>.Ok(success.Template, ApiMessages.AbwabTemplateCreated)),
            CreateTemplateOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabTemplateDto>.Fail(ApiMessages.AbwabTemplateInvalidName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(CreateTemplateOutcome)} variant."),
        };
    }

    [HttpDelete("templates/{templateId:int}")]
    [RequirePermission(AbwabPermissions.Templates.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int templateId, CancellationToken cancellationToken)
    {
        var outcome = await deleteTemplateHandler.HandleAsync(
            new DeleteTemplateCommand(templateId), cancellationToken);

        return outcome switch
        {
            DeleteTemplateOutcome.Success => NoContent(),
            DeleteTemplateOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabTemplateNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(DeleteTemplateOutcome)} variant."),
        };
    }

    [HttpPost("templates/{templateId:int}/apply")]
    [RequirePermission(AbwabPermissions.Templates.Apply)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabDoorDto>>>> Apply(
        int templateId, [FromBody] ApplyTemplateBody body, CancellationToken cancellationToken)
    {
        var outcome = await applyTemplateHandler.HandleAsync(
            new ApplyTemplateCommand(templateId, body.TargetDoorIds), cancellationToken);

        return outcome switch
        {
            ApplyTemplateOutcome.Success success =>
                Created("/api/abwab/doors",
                    ApiResponse<IReadOnlyList<AbwabDoorDto>>.Ok(success.CreatedDoors, ApiMessages.AbwabTemplateApplied)),
            ApplyTemplateOutcome.InvalidRequest =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabTemplateApplyNoTargets)),
            ApplyTemplateOutcome.TargetArchived =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabTemplateApplyTargetArchived)),
            ApplyTemplateOutcome.EmptyTemplate =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabTemplateApplyEmpty)),
            ApplyTemplateOutcome.TemplateNotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabTemplateNotFound)),
            ApplyTemplateOutcome.TargetNotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorNotFound)),
            ApplyTemplateOutcome.Collision collision =>
                Conflict(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(
                    ApiMessages.AbwabTemplateApplyCollisionWith(collision.Collisions))),
            _ => throw new InvalidOperationException($"Unhandled {nameof(ApplyTemplateOutcome)} variant."),
        };
    }

    private OkObjectResult OkWithValidator<T>(ApiResponse<T> body, string etag)
    {
        ConditionalGet.SetValidatorHeaders(Response, etag);
        return Ok(body);
    }

    private StatusCodeResult NotModifiedWithValidator(string etag)
    {
        ConditionalGet.SetValidatorHeaders(Response, etag);
        return StatusCode(StatusCodes.Status304NotModified);
    }
}
