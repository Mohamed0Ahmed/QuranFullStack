namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingOperationViolation(
    LinkingOperationViolationCode Code,
    string? Field,
    string? Value);
