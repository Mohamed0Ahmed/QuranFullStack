namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategoryProtectionProfileDto(
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    Guid CategoryId,
    IReadOnlyList<ManualProtectionResolutionDto> ManualProtections,
    OrdinaryProtectionResolutionDto OrdinaryProtection,
    DateTimeOffset ServerTimeUtc) : IAbwabActionableRead;
