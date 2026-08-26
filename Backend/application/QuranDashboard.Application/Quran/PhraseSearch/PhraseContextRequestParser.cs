using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

public sealed class PhraseContextRequestParser(IPhraseSearchReferenceCodec codec)
{
    internal bool TryParseSelection(
        string? resolutionValue,
        string? previousValue,
        string? followingValue,
        out PhraseContextSelection? selection)
    {
        selection = null;
        if (!codec.TryDecodeResolution(resolutionValue, out var resolution)
            || resolution is null
            || !TryDecodePath(previousValue, PhraseContextSide.Previous, resolution, out var previous)
            || !TryDecodePath(followingValue, PhraseContextSide.Following, resolution, out var following))
        {
            return false;
        }

        selection = new PhraseContextSelection(resolution, previous, following);
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
}
