using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;

public sealed class JsonNavigationDatasetReader
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);
    private static readonly Regex RangePattern = new(@"^\d+-\d+$", RegexOptions.Compiled);

    public async Task<ParsedNavigationDatasets> ReadAllAsync(
        NavigationPackageManifest manifest,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var juzFile = RequireFile(manifest, "juz");
        var hizbFile = RequireFile(manifest, "hizb");
        var rubFile = RequireFile(manifest, "rub");
        var sajdaFile = RequireFile(manifest, "sajda");

        return new ParsedNavigationDatasets(
            await ReadDivisionsAsync(juzFile.FullPath!, "juz_number", ct),
            await ReadDivisionsAsync(hizbFile.FullPath!, "hizb_number", ct),
            await ReadDivisionsAsync(rubFile.FullPath!, "rub_number", ct),
            await ReadSajdaAsync(sajdaFile.FullPath!, ct));
    }

    private static NavigationManifestFileRecord RequireFile(NavigationPackageManifest manifest, string datasetKey)
    {
        return manifest.SourceFiles.Single(file =>
            string.Equals(file.DatasetKey, datasetKey, StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<ParsedNavigationDivision>> ReadDivisionsAsync(
        string filePath,
        string numberField,
        CancellationToken ct)
    {
        using var document = await OpenDocumentAsync(filePath, ct);
        var divisions = new List<ParsedNavigationDivision>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var element = property.Value;
            var number = ReadShort(element, numberField, filePath);
            var versesCount = ReadShort(element, "verses_count", filePath);
            var firstVerseKey = ReadString(element, "first_verse_key", filePath);
            var lastVerseKey = ReadString(element, "last_verse_key", filePath);
            var verseMapping = ReadVerseMapping(element, filePath);

            EnsureVerseKeyFormat(firstVerseKey, filePath);
            EnsureVerseKeyFormat(lastVerseKey, filePath);

            divisions.Add(new ParsedNavigationDivision(
                number,
                versesCount,
                firstVerseKey,
                lastVerseKey,
                verseMapping));
        }

        return divisions;
    }

    private async Task<IReadOnlyList<ParsedNavigationSajda>> ReadSajdaAsync(string filePath, CancellationToken ct)
    {
        using var document = await OpenDocumentAsync(filePath, ct);
        var sajdas = new List<ParsedNavigationSajda>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var element = property.Value;
            var sajdaNumber = ReadShort(element, "sajdah_number", filePath);
            var verseKey = ReadString(element, "verse_key", filePath);
            var sajdaType = ReadString(element, "sajdah_type", filePath);

            EnsureVerseKeyFormat(verseKey, filePath);
            ValidateSajdaType(sajdaType, filePath);

            sajdas.Add(new ParsedNavigationSajda(
                sajdaNumber,
                verseKey,
                sajdaType));
        }

        return sajdas;
    }

    private static IReadOnlyDictionary<string, string> ReadVerseMapping(JsonElement element, string filePath)
    {
        if (!element.TryGetProperty("verse_mapping", out var mappingElement)
            || mappingElement.ValueKind != JsonValueKind.Object)
        {
            FailJsonShape(filePath, "verse_mapping object", "missing");
        }

        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in mappingElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                FailJsonShape(filePath, "verse_mapping range string", property.Value.ValueKind.ToString());
            }

            var range = property.Value.GetString() ?? string.Empty;
            if (!RangePattern.IsMatch(range))
            {
                FailJsonShape(filePath, "surah:from-to range", range);
            }

            mapping[property.Name] = range;
        }

        return mapping;
    }

    private static void ValidateSajdaType(string sajdaType, string filePath)
    {
        NavigationValidationChecks.EnsureAllHardChecksPassed([
            NavigationValidationChecks.ValidateSajdaTypeAllowed(sajdaType, Path.GetFileName(filePath))
        ]);
    }

    private static async Task<JsonDocument> OpenDocumentAsync(string filePath, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                FailJsonShape(filePath, "object root", $"{document.RootElement.ValueKind} root");
            }

            return document;
        }
        catch (JsonException ex)
        {
            FailJsonShape(filePath, "valid JSON object", ex.Message);
            throw;
        }
    }

    private static short ReadShort(JsonElement element, string propertyName, string filePath)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt32(out var number))
        {
            FailJsonShape(filePath, propertyName, "missing or non-integer");
            return 0;
        }

        if (!NavigationValidationChecks.TryParsePositiveShort(number, out var parsed))
        {
            FailJsonShape(filePath, propertyName, $"out-of-range integer {number.ToString(CultureInfo.InvariantCulture)}");
            return 0;
        }

        return parsed;
    }

    private static string ReadString(JsonElement element, string propertyName, string filePath)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            FailJsonShape(filePath, propertyName, "missing or non-string");
            return string.Empty;
        }

        return value.GetString() ?? string.Empty;
    }

    private static void EnsureVerseKeyFormat(string verseKey, string filePath)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            FailHard(
                NavigationMetadataInvariants.CheckVerseKeysResolve,
                "canonical surah:ayah verse key",
                verseKey,
                filePath);
        }
    }

    [DoesNotReturn]
    private static void FailJsonShape(string filePath, string expected, string observed) =>
        FailHard(NavigationMetadataInvariants.CheckJsonShape, expected, observed, filePath);

    [DoesNotReturn]
    private static void FailHard(string id, string expected, string observed, string filePath)
    {
        var check = NavigationValidationChecks.Hard(id, expected, $"{Path.GetFileName(filePath)}:{observed}", false);
        NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
        throw new InvalidOperationException("Navigation validation failed.");
    }
}

public sealed record ParsedNavigationDatasets(
    IReadOnlyList<ParsedNavigationDivision> Juz,
    IReadOnlyList<ParsedNavigationDivision> Hizb,
    IReadOnlyList<ParsedNavigationDivision> Rub,
    IReadOnlyList<ParsedNavigationSajda> Sajda);

public sealed record ParsedNavigationDivision(
    short Number,
    short SourceVersesCount,
    string FirstVerseKey,
    string LastVerseKey,
    IReadOnlyDictionary<string, string> VerseMapping);

public sealed record ParsedNavigationSajda(
    short SajdahNumber,
    string VerseKey,
    string SajdahType);
