using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

public sealed class JsonFullI3rabSourceReader
{
    public async Task<ParsedFullI3rabSourceFile> ReadAsync(string sourceFilePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new FullI3rabSourceException($"Full i'rab source file was not found: {sourceFilePath}");
        }

        await using var stream = File.OpenRead(sourceFilePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new FullI3rabSourceException(
                $"Full i'rab source root must be a JSON object: {sourceFilePath}");
        }

        var entries = new Dictionary<string, ParsedFullI3rabSourceEntry>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            entries[property.Name] = ParseEntry(property.Name, property.Value, sourceFilePath);
        }

        return new ParsedFullI3rabSourceFile(sourceFilePath, entries);
    }

    private static ParsedFullI3rabSourceEntry ParseEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => ParsePointerEntry(verseKey, value, sourceFilePath),
            JsonValueKind.Object => ParseObjectEntry(verseKey, value, sourceFilePath),
            _ => throw new FullI3rabSourceException(
                $"Unsupported JSON value kind '{value.ValueKind}' for verse key '{verseKey}' in '{sourceFilePath}'.")
        };
    }

    private static ParsedFullI3rabSourceEntry ParsePointerEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        var leaderVerseKey = value.GetString();
        if (string.IsNullOrWhiteSpace(leaderVerseKey))
        {
            throw new FullI3rabSourceException(
                $"Pointer value for verse key '{verseKey}' is empty in '{sourceFilePath}'.");
        }

        return new ParsedFullI3rabSourceEntry.Pointer(verseKey, leaderVerseKey);
    }

    private static ParsedFullI3rabSourceEntry ParseObjectEntry(
        string verseKey,
        JsonElement value,
        string sourceFilePath)
    {
        if (!value.TryGetProperty("text", out var textElement)
            || textElement.ValueKind != JsonValueKind.String)
        {
            throw new FullI3rabSourceException(
                $"Object value for verse key '{verseKey}' is missing string 'text' in '{sourceFilePath}'.");
        }

        var i3rabHtml = textElement.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(i3rabHtml))
        {
            throw new FullI3rabSourceException(
                $"Full i'rab HTML for verse key '{verseKey}' is empty in '{sourceFilePath}'.");
        }

        string[]? ayahKeys = null;
        if (value.TryGetProperty("ayah_keys", out var ayahKeysElement))
        {
            if (ayahKeysElement.ValueKind != JsonValueKind.Array)
            {
                throw new FullI3rabSourceException(
                    $"Property 'ayah_keys' for verse key '{verseKey}' must be an array in '{sourceFilePath}'.");
            }

            ayahKeys = ayahKeysElement.EnumerateArray()
                .Select(item =>
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        throw new FullI3rabSourceException(
                            $"Property 'ayah_keys' for verse key '{verseKey}' must contain strings in '{sourceFilePath}'.");
                    }

                    return item.GetString()
                        ?? throw new FullI3rabSourceException(
                            $"Property 'ayah_keys' for verse key '{verseKey}' contains an empty value in '{sourceFilePath}'.");
                })
                .ToArray();
        }

        return new ParsedFullI3rabSourceEntry.TextOwning(verseKey, i3rabHtml, ayahKeys);
    }
}

public sealed record ParsedFullI3rabSourceFile(
    string SourceFilePath,
    IReadOnlyDictionary<string, ParsedFullI3rabSourceEntry> Entries);

public abstract record ParsedFullI3rabSourceEntry(string VerseKey)
{
    public sealed record TextOwning(
        string LeaderVerseKey,
        string I3rabHtml,
        string[]? AyahKeys) : ParsedFullI3rabSourceEntry(LeaderVerseKey);

    public sealed record Pointer(string MemberVerseKey, string LeaderVerseKey)
        : ParsedFullI3rabSourceEntry(MemberVerseKey);
}
