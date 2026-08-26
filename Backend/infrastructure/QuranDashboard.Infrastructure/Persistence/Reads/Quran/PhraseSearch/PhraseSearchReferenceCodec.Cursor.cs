using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

internal sealed partial class PhraseSearchReferenceCodec
{
    public string EncodeCursor(PhraseCursorReference reference)
    {
        if (reference.BuildId == Guid.Empty || !Enum.IsDefined(reference.Kind) || reference.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        return Encode(writer =>
        {
            writer.Write(FormatVersion);
            writer.Write(CursorKind);
            WriteGuid(writer, reference.BuildId);
            writer.Write((byte)reference.Kind);
            WriteInt32(writer, reference.Offset);
            WriteUInt64(writer, reference.Scope);
        });
    }

    public bool TryDecodeCursor(string? value, out PhraseCursorReference? reference)
    {
        reference = null;
        if (!TryOpen(value, CursorKind, out var reader, out var stream))
        {
            return false;
        }

        using (reader)
        using (stream)
        {
            try
            {
                var buildId = ReadGuid(reader);
                var kind = (PhraseCursorKind)reader.ReadByte();
                var offset = ReadInt32(reader);
                var scope = ReadUInt64(reader);
                if (!Enum.IsDefined(kind) || offset < 0 || !AtPayloadEnd(reader, stream))
                {
                    return false;
                }

                reference = new PhraseCursorReference(buildId, kind, offset, scope);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
            {
                return false;
            }
        }
    }

    public ulong ComputeScope(PhraseContextSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var values = new[]
        {
            EncodeResolution(selection.Resolution),
            selection.Previous is null ? string.Empty : EncodePath(selection.Previous),
            selection.Following is null ? string.Empty : EncodePath(selection.Following),
        };
        return HashScope(values);
    }

    public ulong ComputeScope(PhraseFullContextReference context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return HashScope([EncodeFullContext(context)]);
    }
}
