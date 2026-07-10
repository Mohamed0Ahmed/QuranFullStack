using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;

public sealed class JsonTafsirSourceReader
{
    public async Task<ParsedTafsirSourceFile> ReadAsync(string sourceFilePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new TafsirSourceException($"Tafsir source file was not found: {sourceFilePath}");
        }

        await using var stream = File.OpenRead(sourceFilePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new TafsirSourceException(
                $"Tafsir source root must be a JSON object: {sourceFilePath}");
        }

        var entries = new Dictionary<string, ParsedTafsirSourceEntry>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            entries[property.Name] = ParseEntry(property.Name, property.Value, sourceFilePath);
        }

        return new ParsedTafsirSourceFile(sourceFilePath, entries);
    }

    private static ParsedTafsirSourceEntry ParseEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => ParsePointerEntry(verseKey, value, sourceFilePath),
            JsonValueKind.Object => ParseObjectEntry(verseKey, value, sourceFilePath),
            _ => throw new TafsirSourceException(
                $"Unsupported JSON value kind '{value.ValueKind}' for verse key '{verseKey}' in '{sourceFilePath}'.")
        };
    }

    private static ParsedTafsirSourceEntry ParsePointerEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        var leaderVerseKey = value.GetString();
        if (string.IsNullOrWhiteSpace(leaderVerseKey))
        {
            throw new TafsirSourceException(
                $"Pointer value for verse key '{verseKey}' is empty in '{sourceFilePath}'.");
        }

        return new ParsedTafsirSourceEntry.Pointer(verseKey, leaderVerseKey);
    }

    private static ParsedTafsirSourceEntry ParseObjectEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        if (!value.TryGetProperty("text", out var textElement)
            || textElement.ValueKind != JsonValueKind.String)
        {
            throw new TafsirSourceException(
                $"Object value for verse key '{verseKey}' is missing string 'text' in '{sourceFilePath}'.");
        }

        var text = textElement.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            throw new TafsirSourceException(
                $"Tafsir text for verse key '{verseKey}' is empty in '{sourceFilePath}'.");
        }

        string[]? ayahKeys = null;
        if (value.TryGetProperty("ayah_keys", out var ayahKeysElement))
        {
            if (ayahKeysElement.ValueKind != JsonValueKind.Array)
            {
                throw new TafsirSourceException(
                    $"Property 'ayah_keys' for verse key '{verseKey}' must be an array in '{sourceFilePath}'.");
            }

            ayahKeys = ayahKeysElement.EnumerateArray()
                .Select(item =>
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        throw new TafsirSourceException(
                            $"Property 'ayah_keys' for verse key '{verseKey}' must contain strings in '{sourceFilePath}'.");
                    }

                    return item.GetString()
                        ?? throw new TafsirSourceException(
                            $"Property 'ayah_keys' for verse key '{verseKey}' contains an empty value in '{sourceFilePath}'.");
                })
                .ToArray();
        }

        return new ParsedTafsirSourceEntry.TextOwning(verseKey, text, ayahKeys);
    }
}

public sealed record ParsedTafsirSourceFile(
    string SourceFilePath,
    IReadOnlyDictionary<string, ParsedTafsirSourceEntry> Entries);

public abstract record ParsedTafsirSourceEntry(string VerseKey)
{
    public sealed record TextOwning(
        string LeaderVerseKey,
        string Text,
        string[]? AyahKeys) : ParsedTafsirSourceEntry(LeaderVerseKey);

    public sealed record Pointer(string MemberVerseKey, string LeaderVerseKey)
        : ParsedTafsirSourceEntry(MemberVerseKey);
}
