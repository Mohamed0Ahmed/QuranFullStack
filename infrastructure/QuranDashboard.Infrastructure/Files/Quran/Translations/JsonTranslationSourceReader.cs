using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.Translations;

public sealed class JsonTranslationSourceReader
{
    public async Task<ParsedTranslationSourceFile> ReadAsync(string sourceFilePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new TranslationSourceException($"Translation source file was not found: {sourceFilePath}");
        }

        await using var stream = File.OpenRead(sourceFilePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new TranslationSourceException(
                $"Translation source root must be a JSON object: {sourceFilePath}");
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            entries[property.Name] = ParseText(property.Name, property.Value, sourceFilePath);
        }

        return new ParsedTranslationSourceFile(sourceFilePath, entries);
    }

    private static string ParseText(string verseKey, JsonElement value, string sourceFilePath)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new TranslationSourceException(
                $"Translation value for verse key '{verseKey}' must be an object in '{sourceFilePath}'.");
        }

        if (!value.TryGetProperty("t", out var textElement) || textElement.ValueKind != JsonValueKind.String)
        {
            throw new TranslationSourceException(
                $"Translation value for verse key '{verseKey}' is missing string 't' in '{sourceFilePath}'.");
        }

        var text = textElement.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            throw new TranslationSourceException(
                $"Translation text for verse key '{verseKey}' is empty in '{sourceFilePath}'.");
        }

        return text;
    }
}

public sealed record ParsedTranslationSourceFile(
    string SourceFilePath,
    IReadOnlyDictionary<string, string> Entries);
