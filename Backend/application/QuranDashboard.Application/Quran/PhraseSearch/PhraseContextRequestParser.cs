using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

public sealed class PhraseContextRequestParser(IPhraseSearchReferenceCodec codec)
{
    internal bool TryParseSelection(
        string? resolutionValue,
        string? previousValue,
        string? followingValue,
        out PhraseContextSelection? selection)
        => TryParseSelection(
            resolutionValue,
            previousValue,
            followingValue,
            null,
            null,
            out selection);

    internal bool TryParseSelection(
        string? resolutionValue,
        string? previousValue,
        string? followingValue,
        string? previousAlternativesValue,
        string? followingAlternativesValue,
        out PhraseContextSelection? selection)
    {
        selection = null;
        if (!codec.TryDecodeResolution(resolutionValue, out var resolution)
            || resolution is null
            || !TryDecodePath(previousValue, PhraseContextSide.Previous, resolution, out var previous)
            || !TryDecodePath(followingValue, PhraseContextSide.Following, resolution, out var following)
            || !TryDecodeAlternative(
                previousAlternativesValue,
                PhraseContextSide.Previous,
                resolution,
                previous,
                out var previousAlternatives)
            || !TryDecodeAlternative(
                followingAlternativesValue,
                PhraseContextSide.Following,
                resolution,
                following,
                out var followingAlternatives))
        {
            return false;
        }

        selection = new PhraseContextSelection(
            resolution,
            previous,
            following,
            previousAlternatives,
            followingAlternatives);
        return true;
    }

    internal bool TryParseCursor(
        string? value,
        Guid buildId,
        PhraseCursorKind kind,
        ulong scope,
        out int offset)
    {
        offset = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!codec.TryDecodeCursor(value, out var cursor)
            || cursor is null
            || cursor.BuildId != buildId
            || cursor.Kind != kind
            || cursor.Scope != scope
            || cursor.Offset < 0)
        {
            return false;
        }

        offset = cursor.Offset;
        return true;
    }

    internal static bool TryPageSize(int? value, out int pageSize)
    {
        pageSize = value ?? PhraseSearchPaging.DefaultPageSize;
        return pageSize > 0 && pageSize <= PhraseSearchPaging.MaximumPageSize;
    }

    internal static bool TryResultPageSize(int? value, out int pageSize)
    {
        pageSize = value ?? PhraseSearchPaging.DefaultPageSize;
        return pageSize > 0 && pageSize <= PhraseSearchPaging.MaximumContextResultPageSize;
    }

    private bool TryDecodePath(
        string? value,
        PhraseContextSide side,
        PhraseResolutionReference resolution,
        out PhrasePathReference? path)
    {
        path = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!codec.TryDecodePath(value, out var decoded)
            || decoded is null
            || decoded.BuildId != resolution.BuildId
            || decoded.Mode != resolution.Mode
            || decoded.Side != side
            || !decoded.QueryExactTokenIds.SequenceEqual(resolution.ExactTokenIds))
        {
            return false;
        }

        path = decoded;
        return true;
    }

    private bool TryDecodeAlternative(
        string? value,
        PhraseContextSide side,
        PhraseResolutionReference resolution,
        PhrasePathReference? path,
        out PhraseContextAlternativeReference? alternatives)
    {
        alternatives = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!codec.TryDecodeAlternative(value, out var decoded)
            || decoded is null
            || path?.EndsAtBoundary == true
            || decoded.BuildId != resolution.BuildId
            || decoded.Mode != resolution.Mode
            || decoded.Side != side
            || !decoded.QueryExactTokenIds.SequenceEqual(resolution.ExactTokenIds)
            || !decoded.CommittedPathExactTokenIds.SequenceEqual(path?.SelectedExactTokenIds ?? []))
        {
            return false;
        }

        alternatives = decoded;
        return true;
    }
}
