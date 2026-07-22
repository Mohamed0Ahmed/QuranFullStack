using QuranDashboard.Application.Abstractions.Abwab;

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

            default:
                statusCode = 0;
                response = ApiResponse<object>.Fail(ApiMessages.UnexpectedError);
                return false;
        }
    }
}
