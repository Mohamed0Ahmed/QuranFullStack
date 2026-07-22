using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Abwab;

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
