namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// Summary of a selected unique word, used to restore modal state from a
/// shared URL before or alongside a drill-down read. Same shape as a list
/// item without list paging. <see cref="DisplayTextUthmani"/> is preserved
/// for backward compatibility during the UI transition; new UI code should
/// map tashkeel display from <see cref="TextUthmani"/> and simple display
/// from <see cref="TextUthmaniSimple"/>. <see cref="TextUthmani"/> is
/// display-only and is not a direct search target.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D2.</remarks>
public sealed record UniqueWordSummaryDto(
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
