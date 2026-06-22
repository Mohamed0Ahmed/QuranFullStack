using QuranDashboard.Application.Abstractions.Common.Paging;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// One item in the Unique Words list. <see cref="DisplayTextUthmani"/> is
/// preserved for backward compatibility during the UI transition. New UI code
/// should map display text from raw forms by mode: tashkeel uses
/// <see cref="TextUthmani"/>; simple uses <see cref="TextUthmaniSimple"/>.
/// <see cref="TextUthmani"/> is display-only and is not a direct search target.
/// <see cref="MissingSurahsCount"/> is derived as <c>114 - SurahsCount</c>.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D1.</remarks>
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
