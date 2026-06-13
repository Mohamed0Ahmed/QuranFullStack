using System.Text.Json;

namespace QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

public sealed class JsonSimilarAyahReader
{
    public async Task<IReadOnlyList<ParsedSimilarSource>> ReadAsync(
        string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("matching-ayah.json root must be a JSON object.");
        }

        var sources = new List<ParsedSimilarSource>();

        foreach (var sourceEntry in document.RootElement.EnumerateObject())
        {
            if (sourceEntry.Value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    $"Source ayah '{sourceEntry.Name}' must map to an array (MUT-JSON-SHAPE).");
            }

            var links = new List<ParsedSimilarLinkItem>();
            foreach (var item in sourceEntry.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException("Similar link item must be a JSON object (MUT-JSON-SHAPE).");
                }

                links.Add(new ParsedSimilarLinkItem(
                    ReadRequiredString(item, "matched_ayah_key"),
                    ReadRequiredInt16(item, "score"),
                    ReadRequiredInt16(item, "coverage"),
                    ReadRequiredInt16(item, "matched_words_count"),
                    ReadMatchWordsJson(item)));
            }

            sources.Add(new ParsedSimilarSource(sourceEntry.Name, links));
        }

        return sources;
    }

    private static string ReadMatchWordsJson(JsonElement item)
    {
        if (!item.TryGetProperty("match_words", out var matchWords))
        {
            throw new InvalidDataException("Similar link item is missing 'match_words' (MUT-JSON-SHAPE).");
        }

        if (matchWords.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Property 'match_words' must be an array (MUT-JSON-SHAPE).");
        }

        return matchWords.GetRawText();
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Property '{propertyName}' is missing or not a string.");
        }

        return property.GetString() ?? throw new InvalidDataException($"Property '{propertyName}' is empty.");
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
}

public sealed record ParsedSimilarSource(
    string SourceVerseKey,
    IReadOnlyList<ParsedSimilarLinkItem> Links);

public sealed record ParsedSimilarLinkItem(
    string MatchedAyahKey,
    short Score,
    short Coverage,
    short MatchedWordsCount,
    string MatchWordsJson);
