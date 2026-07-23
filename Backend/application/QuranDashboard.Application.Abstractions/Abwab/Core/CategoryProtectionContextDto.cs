namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategoryProtectionContextDto(
    Guid CategoryId,
    IReadOnlyList<Guid> AncestorIds,
    IReadOnlyList<ActiveManualProtectionRecordDto> ActiveProtections,
    string? OrdinaryProtectionActorSubject,
    DateTimeOffset? OrdinaryProtectionLastEditedAtUtc,
    long TimelineGeneration);
