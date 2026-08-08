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

    public static bool TryGetOptionalReason(string? value, out string? reason)
    {
        var trimmed = value?.Trim();
        reason = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        return reason is null || reason.Length <= MaximumReasonLength;
    }

    public static bool IsValidPage(int page, int pageSize) =>
        page >= AccessUserPaging.MinimumPage
        && pageSize is >= AccessUserPaging.MinimumPageSize and <= AccessUserPaging.MaximumPageSize;
}
