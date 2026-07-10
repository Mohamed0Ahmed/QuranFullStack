namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record AyahMutashabihatResponse(
    string VerseKey,
    int GroupCount,
    IReadOnlyList<MutashabihatGroupDto> Groups);

public sealed record MutashabihatGroupDto(
    string GroupKey,
    int SourceGroupId,
    string RepresentativeVerseKey,
    int RepresentativeWordFrom,
    int RepresentativeWordTo,
    string? PhraseTextUthmani,
    int OccurrenceCount,
    int DistinctAyahCount,
    int DistinctSurahCount,
    IReadOnlyList<MutashabihatSelectedOccurrenceDto> SelectedOccurrences,
    IReadOnlyList<MutashabihatOccurrenceDto> Occurrences);

public sealed record MutashabihatSelectedOccurrenceDto(
    string VerseKey,
    int WordFrom,
    int WordTo,
    bool IsRepresentative,
    string? PhraseTextUthmani);

public sealed record MutashabihatOccurrenceDto(
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    int PageNumber,
    int WordFrom,
    int WordTo,
    bool IsSelectedAyah,
    bool IsRepresentative,
    string TextUthmani,
    string? PhraseTextUthmani);

public static class MutashabihatGroupKeys
{
    public static string FromSourceGroupId(int sourceGroupId) => $"mutashabihat:{sourceGroupId}";
}
