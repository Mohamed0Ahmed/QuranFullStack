using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Abwab;

public static class AbwabConflictResponses
{
    private static readonly IReadOnlyDictionary<string, string> WriteConflictMessages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [AbwabConflictCodes.SectionNameConflict] = ApiMessages.AbwabSectionNameConflict,
        [AbwabConflictCodes.SectionNotEmpty] = ApiMessages.AbwabSectionNotEmpty,
        [AbwabConflictCodes.PermanentDefaultSection] = ApiMessages.AbwabPermanentDefaultSection,
        [AbwabConflictCodes.CategoryNameConflict] = ApiMessages.AbwabCategoryNameConflict,
        [AbwabConflictCodes.CategoryAliasConflict] = ApiMessages.AbwabCategoryAliasConflict,
        [AbwabConflictCodes.CategoryCycle] = ApiMessages.AbwabCategoryCycle,
        [AbwabConflictCodes.CategoryOverlappingMove] = ApiMessages.AbwabCategoryOverlappingMove,
        [AbwabConflictCodes.CategoryUnavailable] = ApiMessages.AbwabCategoryUnavailable,
        [AbwabConflictCodes.CategoryReservedByPending] = ApiMessages.AbwabCategoryReservedByPending,
        [AbwabConflictCodes.ManualProtection] = ApiMessages.AbwabManualProtection,
        [AbwabConflictCodes.ManualProtectionScopeConflict] = ApiMessages.AbwabManualProtectionScopeConflict,
        [AbwabConflictCodes.OrdinaryProtection] = ApiMessages.AbwabOrdinaryProtection,
        [AbwabConflictCodes.RelationshipDuplicate] = ApiMessages.AbwabRelationshipDuplicate,
        [AbwabConflictCodes.RelationshipCycle] = ApiMessages.AbwabRelationshipCycle,
        [AbwabConflictCodes.TemplateCycle] = ApiMessages.AbwabTemplateCycle,
        [AbwabConflictCodes.TemplateRevisionStale] = ApiMessages.AbwabTemplateRevisionStale,
        [AbwabConflictCodes.RowStale] = ApiMessages.AbwabRowStale,
        [AbwabConflictCodes.TreeRevisionStale] = ApiMessages.AbwabTreeRevisionStale,
    };

    public static bool TryMap(Exception exception, out int statusCode, out ApiResponse<object> response)
    {
        switch (exception)
        {
            case AbwabWriteConflictException writeConflict:
                statusCode = StatusCodes.Status409Conflict;
                response = ApiResponse<object>.Fail(
                    WriteConflictMessages.GetValueOrDefault(writeConflict.Code, ApiMessages.UnexpectedError),
                    [writeConflict.Code]);
                return true;

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
