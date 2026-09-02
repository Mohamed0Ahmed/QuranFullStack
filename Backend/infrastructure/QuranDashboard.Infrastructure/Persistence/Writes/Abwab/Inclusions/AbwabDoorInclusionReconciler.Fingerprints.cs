using System.Buffers;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class AbwabDoorInclusionReconciler
{
private static class SourceFingerprint
{
    public static byte[] Compute(SourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            writer.WriteBooleanValue(snapshot.IsGrouped);
            writer.WriteStartArray();
            foreach (var ayah in snapshot.Ayahs)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(ayah.AyahId);
                writer.WriteStartArray();
                foreach (var wordId in ayah.SelectedWordIds)
                {
                    writer.WriteNumberValue(wordId);
                }
                writer.WriteEndArray();
                writer.WriteStartArray();
                foreach (var description in ayah.Descriptions)
                {
                    writer.WriteStringValue(description);
                }
                writer.WriteEndArray();
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndArray();
        }

        return SHA256.HashData(buffer.WrittenSpan);
    }
}
}
