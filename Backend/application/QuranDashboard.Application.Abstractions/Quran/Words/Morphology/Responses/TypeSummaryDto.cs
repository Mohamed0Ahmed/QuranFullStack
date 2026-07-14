namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

public sealed record TypeSummaryDto(
    string Code,
    string ArabicLabel,
    int OccurrencesCount);
