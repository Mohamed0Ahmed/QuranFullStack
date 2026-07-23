using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Abwab.Sections;

// T061: explicit section actions only — no drag semantics. Every mutation carries
// ExpectedTimelineGeneration (and expected xmin where the row is targeted directly).
[ApiController]
[Route("api/abwab/sections")]
public sealed class SectionsController(IAbwabCoreWritePort writePort, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PermissionCatalogue.SectionAdd)]
    public async Task<ActionResult<ApiResponse<object>>> Add(AddSectionRequest request, CancellationToken cancellationToken)
    {
        var sectionId = await writePort.AddSectionAsync(
            new AddSectionCommand(request.Name, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { sectionId }, ApiMessages.AbwabSectionAdded));
    }

    [HttpPut("{sectionId:guid}")]
    [Authorize(Policy = PermissionCatalogue.SectionEdit)]
    public async Task<ActionResult<ApiResponse<object>>> Edit(Guid sectionId, EditSectionRequest request, CancellationToken cancellationToken)
    {
        await writePort.EditSectionAsync(
            new EditSectionCommand(sectionId, request.Name, request.ExpectedVersion, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabSectionEdited));
    }

    [HttpPost("reorder")]
    [Authorize(Policy = PermissionCatalogue.SectionReorder)]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(ReorderSectionsRequest request, CancellationToken cancellationToken)
    {
        var orders = request.Orders
            .Select(o => new SectionOrderEntry(o.SectionId, o.SortOrder, o.ExpectedVersion))
            .ToList();

        await writePort.ReorderSectionsAsync(
            new ReorderSectionsCommand(orders, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabSectionsReordered));
    }

    [HttpDelete("{sectionId:guid}")]
    [Authorize(Policy = PermissionCatalogue.SectionDelete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid sectionId, DeleteSectionRequest request, CancellationToken cancellationToken)
    {
        await writePort.DeleteSectionAsync(
            new DeleteSectionCommand(sectionId, request.ExpectedVersion, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabSectionDeleted));
    }
}
