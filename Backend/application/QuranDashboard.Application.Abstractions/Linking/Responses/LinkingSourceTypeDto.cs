namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingSourceTypeDto(
    string Code,
    string ArabicLabel,
    int OccurrencesCount);
