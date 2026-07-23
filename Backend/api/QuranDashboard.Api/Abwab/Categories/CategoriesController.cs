using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Abwab.Categories;

// T061: explicit category actions only — no drag semantics. Move and reorder are distinct verbs;
// subtree-delete/operation-restore are the atomic pair. Every mutation carries ExpectedTimelineGeneration
// plus expected xmin/TreeRevision where structural.
[ApiController]
[Route("api/abwab/categories")]
public sealed class CategoriesController(IAbwabCoreWritePort writePort, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PermissionCatalogue.CategoryAdd)]
    public async Task<ActionResult<ApiResponse<object>>> Add(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand(
                request.Name,
                request.Description,
                request.RepresentativeQuranExcerpt,
                request.ParentCategoryId,
                request.SectionId,
                request.ExpectedTreeRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { categoryId }, ApiMessages.AbwabCategoryAdded));
    }

    [HttpPut("{categoryId:guid}")]
    [Authorize(Policy = PermissionCatalogue.CategoryEdit)]
    public async Task<ActionResult<ApiResponse<object>>> Edit(Guid categoryId, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        await writePort.EditCategoryAsync(
            new EditCategoryCommand(
                categoryId,
                request.Name,
                request.Description,
                request.RepresentativeQuranExcerpt,
                request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoryEdited));
    }

    [HttpPost("move")]
    [Authorize(Policy = PermissionCatalogue.CategoryMove)]
    public async Task<ActionResult<ApiResponse<object>>> Move(MoveCategoriesRequest request, CancellationToken cancellationToken)
    {
        var moves = request.Moves
            .Select(m => new CategoryMoveEntry(m.CategoryId, m.NewParentCategoryId, m.NewSectionId, m.ExpectedVersion))
            .ToList();

        await writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand(moves, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoriesMoved));
    }

    [HttpPost("reorder")]
    [Authorize(Policy = PermissionCatalogue.CategoryReorder)]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(ReorderCategoriesRequest request, CancellationToken cancellationToken)
    {
        var orders = request.Orders
            .Select(o => new CategoryOrderEntry(o.CategoryId, o.NewOrder, o.ExpectedVersion))
            .ToList();

        await writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                request.Scope,
                request.ParentCategoryId,
                request.SectionId,
                orders,
                request.ExpectedTreeRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoriesReordered));
    }

    [HttpPost("{categoryId:guid}/subtree-delete")]
    [Authorize(Policy = PermissionCatalogue.CategoryDelete)]
    public async Task<ActionResult<ApiResponse<object>>> SubtreeDelete(Guid categoryId, SubtreeDeleteRequest request, CancellationToken cancellationToken)
    {
        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(
                categoryId,
                request.ExpectedVersion,
                request.ExpectedTreeRevision,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
                currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { deletionOperationId }, ApiMessages.AbwabCategorySubtreeDeleted));
    }

    [HttpPost("operation-restore/{deletionOperationId:guid}")]
    [Authorize(Policy = PermissionCatalogue.CategoryDelete)]
    public async Task<ActionResult<ApiResponse<object>>> OperationRestore(Guid deletionOperationId, OperationRestoreRequest request, CancellationToken cancellationToken)
    {
        await writePort.OperationRestoreAsync(
            new OperationRestoreCommand(deletionOperationId, request.ExpectedTreeRevision, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoryOperationRestored));
    }

    [HttpPost("{categoryId:guid}/aliases")]
    [Authorize(Policy = PermissionCatalogue.CategoryEdit)]
    public async Task<ActionResult<ApiResponse<object>>> AddAlias(Guid categoryId, AddCategoryAliasRequest request, CancellationToken cancellationToken)
    {
        var aliasId = await writePort.AddCategoryAliasAsync(
            new AddCategoryAliasCommand(categoryId, request.Value, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { aliasId }, ApiMessages.AbwabCategoryAliasAdded));
    }

    [HttpPut("aliases/{aliasId:guid}")]
    [Authorize(Policy = PermissionCatalogue.CategoryEdit)]
    public async Task<ActionResult<ApiResponse<object>>> EditAlias(Guid aliasId, EditCategoryAliasRequest request, CancellationToken cancellationToken)
    {
        await writePort.EditCategoryAliasAsync(
            new EditCategoryAliasCommand(aliasId, request.Value, request.ExpectedVersion, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoryAliasEdited));
    }

    [HttpDelete("aliases/{aliasId:guid}")]
    [Authorize(Policy = PermissionCatalogue.CategoryEdit)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveAlias(Guid aliasId, RemoveCategoryAliasRequest request, CancellationToken cancellationToken)
    {
        await writePort.RemoveCategoryAliasAsync(
            new RemoveCategoryAliasCommand(aliasId, request.ExpectedVersion, ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabCategoryAliasRemoved));
    }
}
