using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Api.Controllers.Access;

internal static class AccessOperationResponseExtensions
{
    public static ActionResult<ApiResponse<T>> ToActionResult<T>(
        this ControllerBase controller,
        AccessOperationResult<T> outcome,
        string successMessage)
    {
        return outcome.Failure switch
        {
            null => controller.Ok(ApiResponse<T>.Ok(outcome.Data, successMessage)),
            AccessOperationFailure.InvalidRequest => controller.BadRequest(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationInvalidRequest)),
            AccessOperationFailure.ActorNoLongerOwner => controller.StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<T>.Fail(ApiMessages.AccessOwnerRequired)),
            AccessOperationFailure.NotFound => controller.NotFound(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationUserNotFound)),
            AccessOperationFailure.TargetIsOwner => controller.BadRequest(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationOwnerTargetForbidden)),
            AccessOperationFailure.InvalidTransition => controller.BadRequest(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationInvalidTransition)),
            AccessOperationFailure.StaleVersion => controller.Conflict(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationStaleVersion)),
            AccessOperationFailure.InvalidPermissionCodes => controller.BadRequest(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationInvalidPermissionCodes)),
            AccessOperationFailure.SubjectAlreadyLinked => controller.Conflict(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationSubjectAlreadyLinked)),
            AccessOperationFailure.NormalizedEmailCollision => controller.Conflict(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationNormalizedEmailCollision)),
            AccessOperationFailure.InvalidRelinkEvidence => controller.BadRequest(ApiResponse<T>.Fail(ApiMessages.AccessAdministrationInvalidRelinkEvidence)),
            AccessOperationFailure.OwnerConfigurationNotReconciled => controller.BadRequest(
                ApiResponse<T>.Fail(ApiMessages.AccessAdministrationOwnerRelinkNotReconciled)),
            AccessOperationFailure.ConfirmationRequired => controller.BadRequest(
                ApiResponse<T>.Fail(ApiMessages.AccessAdministrationRelinkConfirmationRequired)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(AccessOperationFailure)} variant '{outcome.Failure}'."),
        };
    }
}
