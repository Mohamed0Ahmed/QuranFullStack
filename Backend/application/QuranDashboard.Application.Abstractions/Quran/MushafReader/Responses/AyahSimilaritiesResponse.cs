namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record SimilarAyahsResponse(
    string VerseKey,
    int Count,
    IReadOnlyList<SimilarAyahItemDto> Items);

public sealed record SimilarAyahItemDto(
    string TargetVerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    int PageNumber,
    int JuzNumber,
    int HizbNumber,
    int RubNumber,
    string TextUthmani,
    int Score,
    int Coverage,
    int MatchedWordsCount,
    string RelationshipDirection,
    bool HasReverseLink);

public static class SimilarAyahRelationshipDirections
{
    public const string Outgoing = "outgoing";
    public const string Incoming = "incoming";
    public const string Bidirectional = "bidirectional";
}
