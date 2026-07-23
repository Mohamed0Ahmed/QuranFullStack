using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Abwab.Protection;

[ApiController]
[Route("api/abwab/protection")]
public sealed class ProtectionController(
    IAbwabCoreWritePort writePort,
    ProtectionResolver protectionResolver,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{categoryId:guid}")]
    [Authorize(Policy = PermissionCatalogue.ProtectionView)]
    public async Task<ActionResult<ApiResponse<CategoryProtectionProfileDto>>> GetEffectiveProtection(Guid categoryId, CancellationToken cancellationToken)
    {
        var profile = await protectionResolver.ResolveProfileAsync(categoryId, cancellationToken);
        if (profile is null)
        {
            return NotFound(ApiResponse<CategoryProtectionProfileDto>.Fail(ApiMessages.NotFound));
        }

        return Ok(ApiResponse<CategoryProtectionProfileDto>.Ok(profile, ApiMessages.OperationSuccess));
    }

    [HttpPost("{categoryId:guid}/{protectionType}/apply")]
    [Authorize(Policy = PermissionCatalogue.ProtectionApply)]
    public async Task<ActionResult<ApiResponse<object>>> Apply(
        Guid categoryId, ManualProtectionType protectionType, ApplyManualProtectionRequest request, CancellationToken cancellationToken)
    {
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(
                categoryId, protectionType, request.Scope, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabManualProtectionApplied));
    }

    [HttpPost("{categoryId:guid}/{protectionType}/lift")]
    [Authorize(Policy = PermissionCatalogue.ProtectionLift)]
    public async Task<ActionResult<ApiResponse<object>>> Lift(
        Guid categoryId, ManualProtectionType protectionType, LiftManualProtectionRequest request, CancellationToken cancellationToken)
    {
        await writePort.LiftManualProtectionAsync(
            new LiftManualProtectionCommand(
                categoryId, protectionType, request.ExpectedVersion,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabManualProtectionLifted));
    }

    [HttpPost("{categoryId:guid}/full-preset")]
    [Authorize(Policy = PermissionCatalogue.ProtectionApply)]
    public async Task<ActionResult<ApiResponse<object>>> ApplyFullPreset(
        Guid categoryId, ApplyFullProtectionPresetRequest request, CancellationToken cancellationToken)
    {
        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(
                categoryId, request.Scope, request.ExpectedVersions,
                ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration), currentUser.Sub),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, ApiMessages.AbwabFullProtectionPresetApplied));
    }
}
