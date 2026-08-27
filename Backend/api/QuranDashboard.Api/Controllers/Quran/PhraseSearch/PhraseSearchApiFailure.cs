using QuranDashboard.Application.Quran.PhraseSearch;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

internal static class PhraseSearchApiFailure
{
    internal static ApiResponse<T> Invalid<T>(PhraseRequestInvalidKind kind) => kind switch
    {
        PhraseRequestInvalidKind.Mode => Fail<T>(
            PhraseSearchApiMessages.InvalidMode,
            PhraseSearchErrorCodes.InvalidMode),
        PhraseRequestInvalidKind.Query => Fail<T>(
            PhraseSearchApiMessages.InvalidQuery,
            PhraseSearchErrorCodes.InvalidQuery),
        PhraseRequestInvalidKind.QueryEncoding => Fail<T>(
            PhraseSearchApiMessages.InvalidQueryEncoding,
            PhraseSearchErrorCodes.InvalidQueryEncoding),
        PhraseRequestInvalidKind.QueryTooLong => Fail<T>(
            PhraseSearchApiMessages.QueryTooLong,
            PhraseSearchErrorCodes.QueryTooLong),
        PhraseRequestInvalidKind.Reference => Fail<T>(
            PhraseSearchApiMessages.InvalidReference,
            PhraseSearchErrorCodes.InvalidReference),
        PhraseRequestInvalidKind.Cursor => Fail<T>(
            PhraseSearchApiMessages.InvalidCursor,
            PhraseSearchErrorCodes.InvalidCursor),
        PhraseRequestInvalidKind.Paging => Fail<T>(
            PhraseSearchApiMessages.InvalidPaging,
            PhraseSearchErrorCodes.InvalidPaging),
        PhraseRequestInvalidKind.Length => Fail<T>(
            PhraseSearchApiMessages.InvalidLength,
            PhraseSearchErrorCodes.InvalidLength),
        PhraseRequestInvalidKind.Sort => Fail<T>(
            PhraseSearchApiMessages.InvalidSort,
            PhraseSearchErrorCodes.InvalidSort),
        PhraseRequestInvalidKind.Threshold => Fail<T>(
            PhraseSearchApiMessages.InvalidSimilarityThreshold,
            PhraseSearchErrorCodes.InvalidSimilarityThreshold),
        PhraseRequestInvalidKind.MinimumMatchedWords => Fail<T>(
            PhraseSearchApiMessages.InvalidMinimumMatchedWords,
            PhraseSearchErrorCodes.InvalidMinimumMatchedWords),
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseRequestInvalidKind)} value: {kind}."),
    };

    internal static ApiResponse<T> BuildChanged<T>() => Fail<T>(
        PhraseSearchApiMessages.IndexChanged,
        PhraseSearchErrorCodes.IndexChanged);

    internal static ApiResponse<T> Unavailable<T>() => Fail<T>(
        PhraseSearchApiMessages.IndexUnavailable,
        PhraseSearchErrorCodes.IndexUnavailable);

    internal static ApiResponse<T> SimilarityGroupNotFound<T>() => Fail<T>(
        PhraseSearchApiMessages.SimilarityGroupNotFound,
        PhraseSearchErrorCodes.SimilarityGroupNotFound);

    private static ApiResponse<T> Fail<T>(string message, string errorCode) =>
        ApiResponse<T>.Fail(message, [errorCode]);
}
