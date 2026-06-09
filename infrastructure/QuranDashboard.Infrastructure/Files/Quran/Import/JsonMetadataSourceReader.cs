
namespace QuranDashboard.Infrastructure.Files.Quran.Import;

public sealed class JsonMetadataSourceReader
{
    public async Task<IReadOnlyList<SurahMetaDto>> ReadSurahsAsync(string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var rows = new List<SurahMetaDto>();
        foreach (var row in document.RootElement.EnumerateObject())
        {
            rows.Add(new SurahMetaDto(
                ReadRequiredInt(row.Value, "id"),
                ReadRequiredString(row.Value, "name"),
                ReadRequiredString(row.Value, "name_simple"),
                ReadRequiredString(row.Value, "name_arabic"),
                ReadRequiredInt(row.Value, "revelation_order"),
                ReadRequiredString(row.Value, "revelation_place"),
                ReadRequiredInt(row.Value, "verses_count"),
                ReadRequiredBool(row.Value, "bismillah_pre")));
        }

        return rows.OrderBy(row => row.Id).ToArray();
    }

    public async Task<IReadOnlyList<AyahMetaDto>> ReadAyahsAsync(string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var rows = new List<AyahMetaDto>();
        foreach (var row in document.RootElement.EnumerateObject())
        {
            rows.Add(new AyahMetaDto(
                ReadRequiredInt(row.Value, "id"),
                ReadRequiredInt(row.Value, "surah_number"),
                ReadRequiredInt(row.Value, "ayah_number"),
                ReadRequiredString(row.Value, "verse_key"),
                ReadRequiredInt(row.Value, "words_count"),
                ReadRequiredString(row.Value, "text")));
        }

        return rows.OrderBy(row => row.Id).ToArray();
    }

    private static int ReadRequiredInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : throw new InvalidDataException($"Property '{propertyName}' is missing or not an integer.");
    }

    private static bool ReadRequiredBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False))
        {
            throw new InvalidDataException($"Property '{propertyName}' is missing or not a boolean.");
        }

        return property.GetBoolean();
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
