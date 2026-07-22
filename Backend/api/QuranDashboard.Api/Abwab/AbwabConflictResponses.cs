using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Abwab;

// Maps Abwab kernel concurrency conflicts to the shared ApiResponse failure envelope with an exact,
// stable machine code (in `errors`) and a localized Arabic message. Owned here at the API boundary so
// the abwab.* conflict codes have a single mapping point for every future 029+ writer endpoint.
public static class AbwabConflictResponses
{
    public static bool TryMap(Exception exception, out int statusCode, out ApiResponse<object> response)
    {
        switch (exception)
        {
            case AbwabTimelineGenerationStaleException stale:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabTimelineGenerationStale, [stale.Code]);
                return true;

            case AbwabWriteBarrierClosedException barrier:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabWriteBarrierClosed, [barrier.Code]);
                return true;

            case AbwabStabilizationActiveException stabilization:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabStabilizationActive, [stabilization.Code]);
                return true;

            case PermissionAssignmentStaleException assignmentStale:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabPermissionAssignmentStale, [assignmentStale.Code]);
                return true;

            case PermissionBaselineLockedException baselineLocked:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabPermissionBaselineLocked, [baselineLocked.Code]);
                return true;

            case LastSystemOwnerException lastOwner:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(ApiMessages.AbwabLastSystemOwner, [lastOwner.Code]);
                return true;

            default:
                statusCode = 0;
                response = ApiResponse<object>.Fail(ApiMessages.UnexpectedError);
                return false;
        }
    }
}
