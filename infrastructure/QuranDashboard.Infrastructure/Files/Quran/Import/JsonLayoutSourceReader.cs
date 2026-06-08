using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Infrastructure.Files.Quran.Import;

public sealed class JsonLayoutSourceReader
{
    public async Task<LayoutDto> ReadAsync(string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = document.RootElement;
        var pagesCount = root.GetProperty("pagesCount").GetInt32();
        var linesPerPage = root.GetProperty("linesPerPage").GetInt32();
        var pagesElement = root.GetProperty("pages");

        var pages = new Dictionary<int, IReadOnlyList<LineDto>>();
        foreach (var pageEntry in pagesElement.EnumerateObject())
        {
            var pageNumber = int.Parse(pageEntry.Name, System.Globalization.CultureInfo.InvariantCulture);
            var lines = new List<LineDto>();

            foreach (var lineElement in pageEntry.Value.EnumerateArray())
            {
                lines.Add(new LineDto(
                    ReadRequiredInt(lineElement, "pageNumber"),
                    ReadRequiredInt(lineElement, "lineNumber"),
                    ReadRequiredString(lineElement, "lineType"),
                    ReadRequiredBool(lineElement, "isCentered"),
                    ReadNullableInt(lineElement, "surahNumber"),
                    ReadNullableInt(lineElement, "firstWordId"),
                    ReadNullableInt(lineElement, "lastWordId")));
            }

            pages.Add(pageNumber, lines);
        }

        return new LayoutDto(pagesCount, linesPerPage, pages);
    }

    private static int ReadRequiredInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : throw new InvalidDataException($"Property '{propertyName}' is missing or not an integer.");
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : int.Parse(property.GetRawText(), System.Globalization.CultureInfo.InvariantCulture);
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
