namespace QuranDashboard.Application.Abstractions.Security.Permissions;

public sealed record PermissionDefinition(
    string Code,
    string ArabicLabel,
    string EnglishDescription,
    string GroupArabicLabel,
    int GroupDisplayOrder,
    int DisplayOrder);
