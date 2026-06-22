using QuranDashboard.Application.Abstractions.Common.Paging;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// One item in the Unique Words list. The Uthmani display text is the primary
/// user-facing label for both modes; raw technical keys are never used as
/// labels. <see cref="MissingSurahsCount"/> is derived as <c>114 - SurahsCount</c>.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D1.</remarks>
public sealed record UniqueWordListItemDto(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int MissingSurahsCount,
    string FirstVerseKey,
    string FirstLocation);
