namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record SectionSnapshotDto(
    Guid SectionId,
    string Name,
    string NormalizedName,
    int SortOrder,
    bool IsPermanentDefault);
