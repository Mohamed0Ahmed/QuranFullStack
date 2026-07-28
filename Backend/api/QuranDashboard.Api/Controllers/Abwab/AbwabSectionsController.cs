using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Sections.CreateSection;
using QuranDashboard.Application.Abwab.Commands.Sections.RenameSection;
using QuranDashboard.Application.Abwab.Commands.Sections.DeleteSection;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/sections")]
public sealed class AbwabSectionsController(
    CreateSectionHandler createHandler,
    RenameSectionHandler renameHandler,
    DeleteSectionHandler deleteHandler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AbwabSectionDto>>> Create(
        [FromBody] CreateSectionCommand command,
        CancellationToken cancellationToken)
    {
        var outcome = await createHandler.HandleAsync(command, cancellationToken);

        return outcome switch
        {
            CreateSectionOutcome.Success success =>
                Created($"api/abwab/sections/{success.Section.Id}",
                    ApiResponse<AbwabSectionDto>.Ok(success.Section, ApiMessages.AbwabSectionCreated)),
            CreateSectionOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionInvalidName)),
            CreateSectionOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(CreateSectionOutcome)} variant."),
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AbwabSectionDto>>> Rename(
        int id,
        [FromBody] RenameSectionBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await renameHandler.HandleAsync(
            new RenameSectionCommand(id, body.Name, body.Version), cancellationToken);

        return outcome switch
        {
            RenameSectionOutcome.Success success =>
                Ok(ApiResponse<AbwabSectionDto>.Ok(success.Section, ApiMessages.AbwabSectionRenamed)),
            RenameSectionOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionInvalidName)),
            RenameSectionOutcome.NotFound =>
                NotFound(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionNotFound)),
            RenameSectionOutcome.StaleVersion =>
                Conflict(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionStaleVersion)),
            RenameSectionOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(RenameSectionOutcome)} variant."),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken cancellationToken)
    {
        var outcome = await deleteHandler.HandleAsync(new DeleteSectionCommand(id), cancellationToken);

        return outcome switch
        {
            DeleteSectionOutcome.Success => NoContent(),
            DeleteSectionOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabSectionNotFound)),
            DeleteSectionOutcome.HasLiveDoors =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabSectionHasLiveDoors)),
            DeleteSectionOutcome.StaleVersion =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabSectionStaleVersion)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(DeleteSectionOutcome)} variant."),
        };
    }
}
