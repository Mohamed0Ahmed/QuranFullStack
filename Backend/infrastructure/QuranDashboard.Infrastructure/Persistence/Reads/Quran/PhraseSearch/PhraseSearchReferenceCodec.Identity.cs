using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

internal sealed partial class PhraseSearchReferenceCodec
{
    public string EncodeResolution(PhraseResolutionReference reference)
    {
        ValidateIdentity(reference.BuildId, reference.Mode, reference.ExactTokenIds);
        return Encode(writer =>
        {
            writer.Write(FormatVersion);
            writer.Write(ResolutionKind);
            WriteGuid(writer, reference.BuildId);
            writer.Write((byte)reference.Mode);
            WriteTokenIds(writer, reference.ExactTokenIds);
        });
    }

    public bool TryDecodeResolution(string? value, out PhraseResolutionReference? reference)
    {
        reference = null;
        if (!TryOpen(value, ResolutionKind, out var reader, out var stream))
        {
            return false;
        }

        using (reader)
        using (stream)
        {
            try
            {
                var buildId = ReadGuid(reader);
                var mode = ReadMode(reader);
                var tokenIds = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens);
                if (!AtPayloadEnd(reader, stream))
                {
                    return false;
                }

                reference = new PhraseResolutionReference(buildId, mode, tokenIds);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
            {
                return false;
            }
        }
    }

    public string EncodePath(PhrasePathReference reference)
    {
        ValidateIdentity(reference.BuildId, reference.Mode, reference.QueryExactTokenIds);
        ValidateTokens(reference.SelectedExactTokenIds, allowEmpty: true);
        if (!Enum.IsDefined(reference.Side)
            || reference.QueryExactTokenIds.Count + reference.SelectedExactTokenIds.Count
                > PhraseSearchQueryLimits.MaximumResolvedTokens)
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        return Encode(writer =>
        {
            writer.Write(FormatVersion);
            writer.Write(PathKind);
            WriteGuid(writer, reference.BuildId);
            writer.Write((byte)reference.Mode);
            writer.Write((byte)reference.Side);
            writer.Write(reference.EndsAtBoundary);
            WriteTokenIds(writer, reference.QueryExactTokenIds);
            WriteTokenIds(writer, reference.SelectedExactTokenIds);
        });
    }

    public bool TryDecodePath(string? value, out PhrasePathReference? reference)
    {
        reference = null;
        if (!TryOpen(value, PathKind, out var reader, out var stream))
        {
            return false;
        }

        using (reader)
        using (stream)
        {
            try
            {
                var buildId = ReadGuid(reader);
                var mode = ReadMode(reader);
                var side = (PhraseContextSide)reader.ReadByte();
                var endsAtBoundary = reader.ReadBoolean();
                var query = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens);
                var selected = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens, allowEmpty: true);
                if (!Enum.IsDefined(side)
                    || query.Count + selected.Count > PhraseSearchQueryLimits.MaximumResolvedTokens
                    || !AtPayloadEnd(reader, stream))
                {
                    return false;
                }

                reference = new PhrasePathReference(buildId, mode, side, query, selected, endsAtBoundary);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
            {
                return false;
            }
        }
    }

    public string EncodeAlternative(PhraseContextAlternativeReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ValidateIdentity(reference.BuildId, reference.Mode, reference.QueryExactTokenIds);
        ValidateTokens(reference.CommittedPathExactTokenIds, allowEmpty: true);
        var alternativeTokenIds = CanonicalizeAlternativeTokenIds(reference.AlternativeExactTokenIds);
        if (!Enum.IsDefined(reference.Side)
            || reference.QueryExactTokenIds.Count + reference.CommittedPathExactTokenIds.Count
                + alternativeTokenIds.Count
                > PhraseSearchQueryLimits.MaximumResolvedTokens)
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        return Encode(writer =>
        {
            writer.Write(FormatVersion);
            writer.Write(AlternativeKind);
            WriteGuid(writer, reference.BuildId);
            writer.Write((byte)reference.Mode);
            writer.Write((byte)reference.Side);
            WriteTokenIds(writer, reference.QueryExactTokenIds);
            WriteTokenIds(writer, reference.CommittedPathExactTokenIds);
            WriteTokenIds(writer, alternativeTokenIds);
        });
    }

    public bool TryDecodeAlternative(string? value, out PhraseContextAlternativeReference? reference)
    {
        reference = null;
        if (!TryOpen(value, AlternativeKind, out var reader, out var stream))
        {
            return false;
        }

        using (reader)
        using (stream)
        {
            try
            {
                var buildId = ReadGuid(reader);
                var mode = ReadMode(reader);
                var side = (PhraseContextSide)reader.ReadByte();
                var query = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens);
                var committedPath = ReadTokenIds(
                    reader,
                    PhraseSearchQueryLimits.MaximumResolvedTokens,
                    allowEmpty: true);
                var alternatives = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens);
                if (!Enum.IsDefined(side)
                    || query.Count + committedPath.Count + alternatives.Count
                        > PhraseSearchQueryLimits.MaximumResolvedTokens
                    || !IsCanonicalAlternativeTokenIds(alternatives)
                    || !AtPayloadEnd(reader, stream))
                {
                    return false;
                }

                reference = new PhraseContextAlternativeReference(
                    buildId,
                    mode,
                    side,
                    query,
                    committedPath,
                    alternatives);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
            {
                return false;
            }
        }
    }

    public string EncodeFullContext(PhraseFullContextReference reference)
    {
        ValidateIdentity(reference.BuildId, reference.Mode, reference.QueryExactTokenIds);
        ValidateTokens(reference.PreviousExactTokenIds, allowEmpty: true);
        ValidateTokens(reference.FollowingExactTokenIds, allowEmpty: true);
        if (reference.QueryExactTokenIds.Count
            + reference.PreviousExactTokenIds.Count
            + reference.FollowingExactTokenIds.Count > PhraseSearchQueryLimits.MaximumResolvedTokens)
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        return Encode(writer =>
        {
            writer.Write(FormatVersion);
            writer.Write(FullContextKind);
            WriteGuid(writer, reference.BuildId);
            writer.Write((byte)reference.Mode);
            WriteTokenIds(writer, reference.QueryExactTokenIds);
            WriteTokenIds(writer, reference.PreviousExactTokenIds);
            WriteTokenIds(writer, reference.FollowingExactTokenIds);
        });
    }

    public bool TryDecodeFullContext(string? value, out PhraseFullContextReference? reference)
    {
        reference = null;
        if (!TryOpen(value, FullContextKind, out var reader, out var stream))
        {
            return false;
        }

        using (reader)
        using (stream)
        {
            try
            {
                var buildId = ReadGuid(reader);
                var mode = ReadMode(reader);
                var query = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens);
                var previous = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens, allowEmpty: true);
                var following = ReadTokenIds(reader, PhraseSearchQueryLimits.MaximumResolvedTokens, allowEmpty: true);
                if (query.Count + previous.Count + following.Count > PhraseSearchQueryLimits.MaximumResolvedTokens
                    || !AtPayloadEnd(reader, stream))
                {
                    return false;
                }

                reference = new PhraseFullContextReference(buildId, mode, query, previous, following);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
            {
                return false;
            }
        }
    }
}
