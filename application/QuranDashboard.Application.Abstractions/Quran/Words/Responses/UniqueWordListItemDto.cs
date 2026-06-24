using QuranDashboard.Application.Abstractions.Common.Paging;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordListItemDto(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    string TextUthmani,
    string TextUthmaniSimple,
    string TextImlaeiSimple,
    string? WordKeyImlaeiSimple,
    string? QpcGlyph,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int MissingSurahsCount,
    string FirstVerseKey,
    string FirstLocation);
