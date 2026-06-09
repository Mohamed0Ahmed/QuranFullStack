
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Infrastructure.Files.Quran.Import;

public sealed class JsonWordSourceReader
{
    public async Task<IReadOnlyList<WordRecordDto>> ReadAsync(string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var words = new List<WordRecordDto>(document.RootElement.GetRawText().Length / 40);
        foreach (var entry in document.RootElement.EnumerateObject())
        {
            var value = entry.Value;
            var word = new WordRecordDto(
                ReadRequiredInt(value, "id"),
                ReadRequiredIntFromString(value, "surah"),
                ReadRequiredIntFromString(value, "ayah"),
                ReadRequiredIntFromString(value, "word"),
                ReadRequiredString(value, "location"),
                ReadRequiredString(value, "text"));

            if (!string.Equals(entry.Name, word.Location, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Word file '{filePath}' has mismatched key '{entry.Name}' and location '{word.Location}'.");
            }

            words.Add(word);
        }

        return words.OrderBy(word => word.Id).ToArray();
    }

    private static int ReadRequiredInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidDataException($"Missing property '{propertyName}'.");
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetInt32(),
            JsonValueKind.String when int.TryParse(property.GetString(), out var value) => value,
            _ => throw new InvalidDataException($"Property '{propertyName}' is not a valid integer.")
        };
    }

    private static int ReadRequiredIntFromString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidDataException($"Missing property '{propertyName}'.");
        }

        var text = property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
        if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text.Trim('"'), out var value))
        {
            throw new InvalidDataException($"Property '{propertyName}' is not a valid integer string.");
        }

        return value;
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Property '{propertyName}' is missing or not a string.");
        }

        return property.GetString() ?? throw new InvalidDataException($"Property '{propertyName}' is empty.");
    }
}
