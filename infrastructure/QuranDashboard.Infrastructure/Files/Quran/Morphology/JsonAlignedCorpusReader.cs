namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

public sealed class JsonAlignedCorpusReader
{
    public async Task<IReadOnlyList<AlignedCorpusWord>> ReadAsync(
        string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var words = new List<AlignedCorpusWord>();

        foreach (var locationEntry in document.RootElement.EnumerateObject())
        {
            var location = locationEntry.Name;
            var value = locationEntry.Value;

            var qpcUthmani = ReadRequiredString(value, "qpcUthmani");
            var segmentsArray = ReadRequiredArray(value, "segments");

            var segments = new List<AlignedCorpusSegment>();
            foreach (var segElement in segmentsArray.EnumerateArray())
            {
                segments.Add(new AlignedCorpusSegment(
                    ReadRequiredInt16(segElement, "segmentNumber"),
                    ReadRequiredString(segElement, "kind"),
                    ReadRequiredString(segElement, "pos"),
                    ReadRequiredString(segElement, "form"),
                    ReadRequiredString(segElement, "features"),
                    ReadOptionalString(segElement, "root"),
                    ReadOptionalString(segElement, "lemma")));
            }

            words.Add(new AlignedCorpusWord(location, qpcUthmani, segments));
        }

        return words;
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Property '{propertyName}' is missing or not a string.");
        }

        return property.GetString() ?? throw new InvalidDataException($"Property '{propertyName}' is empty.");
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static short ReadRequiredInt16(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidDataException($"Missing property '{propertyName}'.");
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.GetInt16()
            : short.Parse(property.GetString() ?? throw new InvalidDataException($"Property '{propertyName}' is invalid."));
    }

    private static JsonElement ReadRequiredArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Property '{propertyName}' is missing or not an array.");
        }

        return property;
    }
}

public sealed record AlignedCorpusWord(
    string QpcLocation,
    string QpcUthmani,
    IReadOnlyList<AlignedCorpusSegment> Segments);

public sealed record AlignedCorpusSegment(
    short SegmentNumber,
    string Kind,
    string Pos,
    string Form,
    string Features,
    string? Root,
    string? Lemma);
