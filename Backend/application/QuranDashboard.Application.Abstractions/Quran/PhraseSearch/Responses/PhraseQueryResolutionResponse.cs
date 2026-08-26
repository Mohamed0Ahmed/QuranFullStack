namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseQueryResolutionResponse(
    Guid ActiveBuildId,
    string Mode,
    string Status,
    IReadOnlyList<PhraseResolutionCandidateDto> Candidates);

public sealed record PhraseResolutionCandidateDto(
    string ResolutionRef,
    short WordCount,
    string DisplayText,
    IReadOnlyList<PhraseExactTokenDto> Tokens);

public sealed record PhraseExactTokenDto(
    int ExactTokenId,
    string TextUthmani);

public static class PhraseResolutionStatuses
{
    public const string Resolved = "resolved";
    public const string Ambiguous = "ambiguous";
    public const string Unresolved = "unresolved";
}
