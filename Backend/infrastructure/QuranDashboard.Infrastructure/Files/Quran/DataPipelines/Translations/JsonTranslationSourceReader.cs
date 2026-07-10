using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

public sealed class JsonTranslationSourceReader
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);

    public async Task<ParsedTranslationSourceFile> ReadAsync(string sourceFilePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new TranslationSourceException($"Translation source file was not found: {sourceFilePath}");
        }

        using var document = await OpenDocumentAsync(sourceFilePath, ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            FailJsonShape(
                $"object root in '{sourceFilePath}'",
                $"{document.RootElement.ValueKind} root");
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            EnsureVerseKeyFormat(property.Name);
            entries[property.Name] = ParseText(property.Name, property.Value);
        }

        return new ParsedTranslationSourceFile(sourceFilePath, entries);
    }

    private static async Task<JsonDocument> OpenDocumentAsync(string sourceFilePath, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(sourceFilePath);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            FailJsonShape($"valid JSON object in '{sourceFilePath}'", ex.Message);
            throw;
        }
    }

    private static void EnsureVerseKeyFormat(string verseKey)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            FailHard(
                TranslationInvariants.CheckAyahKeysResolve,
                "canonical surah:ayah verse key",
                verseKey);
        }
    }

    private static string ParseText(string verseKey, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            FailJsonShape(
                $"{{ \"t\": string }} for verse key '{verseKey}'",
                $"{verseKey}: {value.ValueKind}");
        }

        if (!value.TryGetProperty("t", out var textElement) || textElement.ValueKind != JsonValueKind.String)
        {
            FailNoEmptyText(verseKey, "missing or non-string t");
        }

        var text = textElement.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            FailNoEmptyText(verseKey, "empty t");
        }

        return text;
    }

    [DoesNotReturn]
    private static void FailJsonShape(string expected, string observed) =>
        FailHard(TranslationInvariants.CheckJsonShape, expected, observed);

    [DoesNotReturn]
    private static void FailNoEmptyText(string verseKey, string reason) =>
        FailHard(
            TranslationInvariants.CheckNoEmptyText,
            "non-empty string t",
            $"{verseKey}: {reason}");

    [DoesNotReturn]
    private static void FailHard(string id, string expected, string observed)
    {
        var check = TranslationValidationChecks.Hard(id, expected, observed, passed: false);
        TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
        throw new InvalidOperationException("Translation validation failed.");
    }
}

public sealed record ParsedTranslationSourceFile(
    string SourceFilePath,
    IReadOnlyDictionary<string, string> Entries);
