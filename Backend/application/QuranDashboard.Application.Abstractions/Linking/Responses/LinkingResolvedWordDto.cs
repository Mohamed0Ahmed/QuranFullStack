namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingResolvedWordDto(
    int QuranWordId,
    int WordNumber,
    string TextUthmani,
    bool IsAyahMarker);
