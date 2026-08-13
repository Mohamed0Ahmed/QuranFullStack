using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingConfirmedDoorState(
    int DoorId,
    string DoorName,
    bool IsArchived,
    uint DoorVersion,
    IReadOnlyList<LinkingConfirmedDoorAyah> Ayahs,
    IReadOnlyList<LinkingConfirmedContribution> Contributions);

public sealed record LinkingConfirmedDoorAyah(
    long Id,
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    IReadOnlyList<int> QuranWordIds);

public sealed record LinkingConfirmedContribution(
    long Id,
    uint Version,
    string SourceIdentity,
    LinkingSourceKind SourceKind,
    string Label,
    LinkingContributionMode ContributionMode,
    int OrderValue,
    IReadOnlyList<LinkingConfirmedUnit> Units);

public sealed record LinkingConfirmedUnit(
    long Id,
    string Identity,
    int OrderValue,
    bool IsGrouped,
    IReadOnlyList<LinkingConfirmedAyah> Ayahs);

public sealed record LinkingConfirmedAyah(
    long Id,
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    int OrderValue,
    IReadOnlyList<int> QuranWordIds,
    IReadOnlyList<string> Descriptions);
