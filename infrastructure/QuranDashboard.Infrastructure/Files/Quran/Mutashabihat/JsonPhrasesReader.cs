using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

public sealed class JsonPhrasesReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PhrasesReadResult> ReadAsync(string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new MutashabihatSourceException("phrases.json root must be a JSON object.");
        }

        var groups = new List<ParsedPhraseGroup>();
        var rawOccurrenceCount = 0;

        foreach (var groupEntry in document.RootElement.EnumerateObject())
        {
            if (!int.TryParse(groupEntry.Name, out var sourceGroupId))
            {
                throw new MutashabihatSourceException(
                    $"Phrase group key '{groupEntry.Name}' is not a valid integer source_group_id.");
            }

            var value = groupEntry.Value;
            ValidateGroupShape(value, sourceGroupId);

            var source = ReadSourceBlock(value.GetProperty("source"));
            var occurrencesByVerseKey = ReadOccurrenceMap(
                value.GetProperty("ayah"),
                ref rawOccurrenceCount);

            string? rawSourceCountsJson = null;
            if (value.TryGetProperty("surahs", out _) ||
                value.TryGetProperty("ayahs", out _) ||
                value.TryGetProperty("count", out _))
            {
                rawSourceCountsJson = JsonSerializer.Serialize(new
                {
                    surahs = value.TryGetProperty("surahs", out var surahs) && surahs.ValueKind == JsonValueKind.Number
                        ? surahs.GetInt32()
                        : (int?)null,
                    ayahs = value.TryGetProperty("ayahs", out var ayahs) && ayahs.ValueKind == JsonValueKind.Number
                        ? ayahs.GetInt32()
                        : (int?)null,
                    count = value.TryGetProperty("count", out var count) && count.ValueKind == JsonValueKind.Number
                        ? count.GetInt32()
                        : (int?)null
                }, JsonOptions);
            }

            groups.Add(new ParsedPhraseGroup(
                sourceGroupId,
                source,
                rawSourceCountsJson,
                occurrencesByVerseKey));
        }

        return new PhrasesReadResult(groups, rawOccurrenceCount);
    }

    private static void ValidateGroupShape(JsonElement value, int sourceGroupId)
    {
        if (!value.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.Object)
        {
            throw new MutashabihatSourceException(
                $"Group {sourceGroupId} is missing a 'source' object (MUT-JSON-SHAPE).");
        }

        if (!value.TryGetProperty("ayah", out var ayah) || ayah.ValueKind != JsonValueKind.Object)
        {
            throw new MutashabihatSourceException(
                $"Group {sourceGroupId} is missing an 'ayah' object (MUT-JSON-SHAPE).");
        }

        _ = ReadRequiredString(source, "key");
        _ = ReadRequiredInt16(source, "from");
        _ = ReadRequiredInt16(source, "to");
    }

    private static PhraseSourceBlock ReadSourceBlock(JsonElement source) =>
        new(
            ReadRequiredString(source, "key"),
            ReadRequiredInt16(source, "from"),
            ReadRequiredInt16(source, "to"));

    private static IReadOnlyDictionary<string, IReadOnlyList<WordRange>> ReadOccurrenceMap(
        JsonElement ayahMap,
        ref int rawOccurrenceCount)
    {
        var occurrences = new Dictionary<string, IReadOnlyList<WordRange>>(StringComparer.Ordinal);

        foreach (var ayahEntry in ayahMap.EnumerateObject())
        {
            if (ayahEntry.Value.ValueKind != JsonValueKind.Array)
            {
                throw new MutashabihatSourceException(
                    $"Occurrence list for ayah '{ayahEntry.Name}' must be an array (MUT-JSON-SHAPE).");
            }

            var ranges = new List<WordRange>();
            foreach (var rangeElement in ayahEntry.Value.EnumerateArray())
            {
                ranges.Add(ReadWordRange(rangeElement));
                rawOccurrenceCount++;
            }

            occurrences[ayahEntry.Name] = ranges;
        }

        return occurrences;
    }

    private static WordRange ReadWordRange(JsonElement rangeElement)
    {
        if (rangeElement.ValueKind != JsonValueKind.Array)
        {
            throw new MutashabihatSourceException("Word range must be a JSON array (MUT-JSON-SHAPE).");
        }

        var values = rangeElement.EnumerateArray().ToList();
        if (values.Count is 1 or 2)
        {
            var from = ReadJsonInt16(values[0]);
            var to = values.Count == 2 ? ReadJsonInt16(values[1]) : from;
            return new WordRange(from, to);
        }

        throw new MutashabihatSourceException("Word range must contain one or two integers (MUT-JSON-SHAPE).");
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new MutashabihatSourceException($"Property '{propertyName}' is missing or not a string.");
        }

        return property.GetString() ?? throw new MutashabihatSourceException($"Property '{propertyName}' is empty.");
    }

    private static short ReadRequiredInt16(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new MutashabihatSourceException($"Missing property '{propertyName}'.");
        }

        return ReadJsonInt16(property);
    }

    private static short ReadJsonInt16(JsonElement property) =>
        property.ValueKind == JsonValueKind.Number
            ? property.GetInt16()
            : short.Parse(property.GetString() ?? throw new MutashabihatSourceException("Invalid short value."));
}

public sealed record PhrasesReadResult(
    IReadOnlyList<ParsedPhraseGroup> Groups,
    int RawOccurrenceCount);

public sealed record ParsedPhraseGroup(
    int SourceGroupId,
    PhraseSourceBlock Source,
    string? RawSourceCountsJson,
    IReadOnlyDictionary<string, IReadOnlyList<WordRange>> OccurrencesByVerseKey);

public sealed record PhraseSourceBlock(string Key, short From, short To);

public sealed record WordRange(short From, short To);
