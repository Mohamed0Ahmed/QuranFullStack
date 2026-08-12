namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingDescriptorViolation(
    LinkingDescriptorViolationCode Code,
    string Field,
    string? Value);
