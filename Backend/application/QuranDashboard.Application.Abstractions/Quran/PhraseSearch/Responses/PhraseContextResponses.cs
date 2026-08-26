namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseContextBranchesResponse(
    Guid ActiveBuildId,
    PhraseResolvedQueryDto Query,
    PhraseSelectedPathDto PreviousSelection,
    PhraseSelectedPathDto FollowingSelection,
    PhraseContextSidePageDto Previous,
    PhraseContextSidePageDto Following,
    int TotalOccurrenceCount,
    int? ExactFullContextCount);

public sealed record PhraseResolvedQueryDto(
    string ResolutionRef,
    string Mode,
    IReadOnlyList<PhraseExactTokenDto> Tokens);

public sealed record PhraseSelectedPathDto(
    string? SelectionRef,
    bool EndsAtBoundary,
    IReadOnlyList<PhraseExactTokenDto> Tokens);

public sealed record PhraseContextSidePageDto(
    long PassesThroughCount,
    long SideEndsHereCount,
    int TotalOptions,
    string? NextCursor,
    IReadOnlyList<PhraseContextBranchOptionDto> Options);

public sealed record PhraseContextBranchOptionDto(
    string SelectionRef,
    int? ExactTokenId,
    string DisplayText,
    string? BoundaryKind,
    long PassesThroughCount,
    long SideEndsHereCount);

public sealed record PhraseContextGroupsResponse(
    Guid ActiveBuildId,
    PhraseResolvedQueryDto Query,
    PhraseSelectedPathDto PreviousSelection,
    PhraseSelectedPathDto FollowingSelection,
    int TotalCount,
    string? NextCursor,
    IReadOnlyList<PhraseFullContextGroupDto> Items);

public sealed record PhraseContextResultsResponse(
    Guid ActiveBuildId,
    int TotalCount,
    IReadOnlyList<PhraseContextOccurrenceDto> Items);

public sealed record PhraseFullContextGroupDto(
    string ContextRef,
    IReadOnlyList<PhraseExactTokenDto> PreviousTokens,
    IReadOnlyList<PhraseExactTokenDto> QueryTokens,
    IReadOnlyList<PhraseExactTokenDto> FollowingTokens,
    int ExactFullContextCount,
    string RepresentativeSurahNameArabic,
    short RepresentativeAyahNumber,
    string RepresentativeVerseKey);

public sealed record PhraseContextOccurrencesResponse(
    Guid ActiveBuildId,
    PhraseFullContextDto Context,
    int TotalCount,
    string? NextCursor,
    IReadOnlyList<PhraseContextOccurrenceDto> Items);

public sealed record PhraseFullContextDto(
    string ContextRef,
    string Mode,
    IReadOnlyList<PhraseExactTokenDto> PreviousTokens,
    IReadOnlyList<PhraseExactTokenDto> QueryTokens,
    IReadOnlyList<PhraseExactTokenDto> FollowingTokens,
    int ExactFullContextCount);

public sealed record PhraseContextOccurrenceDto(
    long OccurrenceId,
    int AyahId,
    string VerseKey,
    short SurahNumber,
    string SurahNameArabic,
    short AyahNumber,
    short PageFrom,
    short PageTo,
    short StartWordNumber,
    short EndWordNumber,
    IReadOnlyList<PhraseAyahWordDto> Words,
    PhraseContextHighlightsDto Highlights);

public sealed record PhraseContextHighlightsDto(
    IReadOnlyList<int> QueryQuranWordIds,
    IReadOnlyList<int> PreviousQuranWordIds,
    IReadOnlyList<int> FollowingQuranWordIds);

public static class PhraseContextBoundaryKinds
{
    public const string AyahStart = "ayah-start";
    public const string AyahEnd = "ayah-end";
}
