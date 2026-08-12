namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingWorkspaceViolation(
    LinkingWorkspaceViolationCode Code,
    string? Field,
    string? Value);
