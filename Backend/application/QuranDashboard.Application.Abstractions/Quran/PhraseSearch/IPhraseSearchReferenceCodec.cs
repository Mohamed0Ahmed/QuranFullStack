using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseSearchReferenceCodec
{
    string EncodeResolution(PhraseResolutionReference reference);
    bool TryDecodeResolution(string? value, out PhraseResolutionReference? reference);
    string EncodePath(PhrasePathReference reference);
    bool TryDecodePath(string? value, out PhrasePathReference? reference);
    string EncodeFullContext(PhraseFullContextReference reference);
    bool TryDecodeFullContext(string? value, out PhraseFullContextReference? reference);
    string EncodeCursor(PhraseCursorReference reference);
    bool TryDecodeCursor(string? value, out PhraseCursorReference? reference);
    ulong ComputeScope(PhraseContextSelection selection);
    ulong ComputeScope(PhraseFullContextReference context);
}

public sealed record PhraseResolutionReference(
    Guid BuildId,
    PhraseTextMode Mode,
    IReadOnlyList<int> ExactTokenIds);

public sealed record PhrasePathReference(
    Guid BuildId,
    PhraseTextMode Mode,
    PhraseContextSide Side,
    IReadOnlyList<int> QueryExactTokenIds,
    IReadOnlyList<int> SelectedExactTokenIds,
    bool EndsAtBoundary);

public sealed record PhraseFullContextReference(
    Guid BuildId,
    PhraseTextMode Mode,
    IReadOnlyList<int> QueryExactTokenIds,
    IReadOnlyList<int> PreviousExactTokenIds,
    IReadOnlyList<int> FollowingExactTokenIds);

public sealed record PhraseCursorReference(
    Guid BuildId,
    PhraseCursorKind Kind,
    int Offset,
    ulong Scope);

public sealed record PhraseContextSelection(
    PhraseResolutionReference Resolution,
    PhrasePathReference? Previous,
    PhrasePathReference? Following);

public sealed record PhraseContextBranchPaging(
    int PreviousOffset,
    int FollowingOffset,
    int PreviousPageSize,
    int FollowingPageSize);

public sealed record PhraseCursorPage(int Offset, int PageSize);

public enum PhraseContextSide : byte
{
    Previous = 1,
    Following = 2,
}

public enum PhraseCursorKind : byte
{
    PreviousBranches = 1,
    FollowingBranches = 2,
    ContextGroups = 3,
    ContextOccurrences = 4,
}
