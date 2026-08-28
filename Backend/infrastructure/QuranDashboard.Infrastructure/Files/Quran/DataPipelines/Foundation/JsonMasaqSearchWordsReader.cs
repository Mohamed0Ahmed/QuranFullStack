using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

public sealed class JsonMasaqSearchWordsReader
{
    public const string FileName = "masaq-search-words.dashboard-ready.json";
    public const string ExpectedSchema = "masaq-search-words-dashboard-ready-v1";
    public const int ExpectedWordCount = 77_432;
    public const int ExpectedUniqueTextCount = 14_910;

    public async Task<MasaqSearchWordsSource> ReadAsync(string sourceRoot, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var filePath = Path.Combine(Path.GetFullPath(sourceRoot), FileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("MASAQ search words source was not found.", filePath);
        }

        var sourceBytes = await File.ReadAllBytesAsync(filePath, ct);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(sourceBytes));
        if (!string.Equals(
                sha256,
                MasaqSearchWordsSourceSummary.ApprovedSha256,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"MASAQ search words source digest is not approved: sha256={sha256}.");
        }

        await using var stream = new MemoryStream(sourceBytes, writable: false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = document.RootElement;
        var schema = ReadRequiredString(root, "schema");
        if (!string.Equals(schema, ExpectedSchema, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unexpected MASAQ search words schema '{schema}'.");
        }

        ValidateDeclaredResult(root);

        if (!root.TryGetProperty("words", out var wordsElement)
            || wordsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("MASAQ search words array is missing.");
        }

        var words = new List<WordRecordDto>(ExpectedWordCount);
        foreach (var element in wordsElement.EnumerateArray())
        {
            var text = ReadRequiredString(element, "textMasaqWithoutDiacritics");
            if (string.IsNullOrWhiteSpace(text) || text.Contains('\0'))
            {
                throw new InvalidDataException("MASAQ search words contain an empty or invalid text value.");
            }

            words.Add(new WordRecordDto(
                ReadRequiredInt(element, "quranWordId"),
                ReadRequiredInt(element, "surah"),
                ReadRequiredInt(element, "ayah"),
                ReadRequiredInt(element, "wordNumber"),
                ReadRequiredString(element, "location"),
                text,
                text));
        }

        ValidateRows(words);

        var summary = new MasaqSearchWordsSourceSummary(
            filePath,
            schema,
            sha256,
            words.Count,
            words.Select(word => word.Text).Distinct(StringComparer.Ordinal).Count());

        return new MasaqSearchWordsSource(words, summary);
    }

    private static void ValidateDeclaredResult(JsonElement root)
    {
        if (!root.TryGetProperty("validation", out var validation)
            || validation.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("MASAQ search words validation block is missing.");
        }

        var verdict = ReadRequiredString(validation, "verdict");
        var expectedCount = ReadRequiredInt(validation, "expectedWordCount");
        var actualCount = ReadRequiredInt(validation, "actualWordCount");
        var uniqueIds = ReadRequiredInt(validation, "uniqueQuranWordIdCount");
        var uniqueLocations = ReadRequiredInt(validation, "uniqueLocationCount");

        if (!string.Equals(verdict, "PASS", StringComparison.Ordinal)
            || expectedCount != ExpectedWordCount
            || actualCount != ExpectedWordCount
            || uniqueIds != ExpectedWordCount
            || uniqueLocations != ExpectedWordCount)
        {
            throw new InvalidDataException(
                $"MASAQ search words declared validation is invalid: verdict={verdict}, expected={expectedCount}, actual={actualCount}, uniqueIds={uniqueIds}, uniqueLocations={uniqueLocations}.");
        }
    }

    private static void ValidateRows(IReadOnlyList<WordRecordDto> words)
    {
        var uniqueIds = words.Select(word => word.Id).Distinct().Count();
        var uniqueLocations = words.Select(word => word.Location).Distinct(StringComparer.Ordinal).Count();
        var uniqueTexts = words.Select(word => word.Text).Distinct(StringComparer.Ordinal).Count();
        var invalidLocations = words.Count(word =>
            !string.Equals(
                word.Location,
                $"{word.Surah}:{word.Ayah}:{word.Word}",
                StringComparison.Ordinal));

        if (words.Count != ExpectedWordCount
            || uniqueIds != ExpectedWordCount
            || uniqueLocations != ExpectedWordCount
            || uniqueTexts != ExpectedUniqueTextCount
            || invalidLocations != 0)
        {
            throw new InvalidDataException(
                $"MASAQ search words rows are invalid: rows={words.Count}, uniqueIds={uniqueIds}, uniqueLocations={uniqueLocations}, uniqueTexts={uniqueTexts}, invalidLocations={invalidLocations}.");
        }
    }

    private static int ReadRequiredInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value))
        {
            throw new InvalidDataException($"MASAQ property '{propertyName}' is missing or not an integer.");
        }

        return value;
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidDataException($"MASAQ property '{propertyName}' is missing or empty.");
        }

        return property.GetString()!;
    }

}

public sealed record MasaqSearchWordsSource(
    IReadOnlyList<WordRecordDto> Words,
    MasaqSearchWordsSourceSummary Summary);
