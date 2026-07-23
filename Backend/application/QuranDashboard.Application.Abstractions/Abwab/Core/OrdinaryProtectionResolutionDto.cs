namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record OrdinaryProtectionResolutionDto(
    bool IsActive,
    string? ActorSubject,
    DateTimeOffset? LastEditedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
