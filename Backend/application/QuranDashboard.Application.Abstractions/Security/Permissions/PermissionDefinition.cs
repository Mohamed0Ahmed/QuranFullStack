namespace QuranDashboard.Application.Abstractions.Security.Permissions;

public sealed record PermissionDefinition(
    string Code,
    string ArabicLabel,
    string EnglishDescription,
    string Group,
    int GroupDisplayOrder,
    int DisplayOrder);
