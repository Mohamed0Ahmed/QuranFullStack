namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// A distinct simple or tashkeel word identity among a root's words.
/// <see cref="UniqueWordId"/> is the deep-link target into Feature 014
/// (simple → unique simple word id, tashkeel → unique tashkeel word id); it is
/// not displayed.
/// </summary>
public sealed record RootWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);
