using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access;

internal static class AccessAdministrationValidation
{
    public const int MaximumReasonLength = 1024;

    public static bool TryGetReason(string? value, out string reason)
    {
        reason = value?.Trim() ?? string.Empty;
        return reason.Length is > 0 and <= MaximumReasonLength;
    }

    public static bool IsValidPage(int page, int pageSize) =>
        page >= AccessUserPaging.MinimumPage
        && pageSize is >= AccessUserPaging.MinimumPageSize and <= AccessUserPaging.MaximumPageSize;
}
