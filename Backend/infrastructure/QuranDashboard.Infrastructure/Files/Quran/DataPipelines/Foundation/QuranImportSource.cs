
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

public sealed class QuranImportSource : IQuranImportSource
{
    private readonly ManifestReader manifestReader;
    private readonly JsonWordSourceReader wordSourceReader;
    private readonly JsonMasaqSearchWordsReader masaqSearchWordsReader;
    private readonly JsonLayoutSourceReader layoutSourceReader;
    private readonly JsonMetadataSourceReader metadataSourceReader;

    public QuranImportSource(
        ManifestReader manifestReader,
        JsonWordSourceReader wordSourceReader,
        JsonMasaqSearchWordsReader masaqSearchWordsReader,
        JsonLayoutSourceReader layoutSourceReader,
        JsonMetadataSourceReader metadataSourceReader)
    {
        this.manifestReader = manifestReader;
        this.wordSourceReader = wordSourceReader;
        this.masaqSearchWordsReader = masaqSearchWordsReader;
        this.layoutSourceReader = layoutSourceReader;
        this.metadataSourceReader = metadataSourceReader;
    }

    public async Task<QuranImportSourceData> LoadAsync(string sourceRoot, CancellationToken ct)
    {
        var manifest = await manifestReader.ReadAsync(sourceRoot, ct);

        var glyph = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "qpc-glyph"), ct);
        var uthmani = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "uthmani"), ct);
        var uthmaniSimple = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "uthmani-simple"), ct);
        var legacyImlaeiSimple = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "imlaei-simple"), ct);
        var masaqSearchWords = await masaqSearchWordsReader.ReadAsync(
            ResolveMasaqSourceRoot(sourceRoot),
            ct);
        var imlaeiSimple = MergeImlaeiSources(legacyImlaeiSimple, masaqSearchWords.Words);
        var layout = await layoutSourceReader.ReadAsync(GetSourcePath(manifest, "layout"), ct);
        var surahs = await metadataSourceReader.ReadSurahsAsync(GetSourcePath(manifest, "surah-meta"), ct);
        var ayahs = await metadataSourceReader.ReadAyahsAsync(GetSourcePath(manifest, "ayah-meta"), ct);

        return new QuranImportSourceData(
            surahs,
            ayahs,
            glyph,
            uthmani,
            uthmaniSimple,
            imlaeiSimple,
            masaqSearchWords.Summary,
            layout,
            manifest.Version);
    }

    private static IReadOnlyList<WordRecordDto> MergeImlaeiSources(
        IReadOnlyList<WordRecordDto> legacyWords,
        IReadOnlyList<WordRecordDto> masaqWords)
    {
        var masaqByLocation = masaqWords.ToDictionary(
            word => word.Location,
            StringComparer.Ordinal);
        var merged = new List<WordRecordDto>(legacyWords.Count);
        var matchedMasaqWords = 0;
        var retainedMarkers = 0;

        foreach (var legacyWord in legacyWords.OrderBy(word => word.Id))
        {
            if (masaqByLocation.TryGetValue(legacyWord.Location, out var masaqWord))
            {
                if (legacyWord.Id != masaqWord.Id
                    || legacyWord.Surah != masaqWord.Surah
                    || legacyWord.Ayah != masaqWord.Ayah
                    || legacyWord.Word != masaqWord.Word)
                {
                    throw new InvalidDataException(
                        $"MASAQ word '{masaqWord.Location}' is misaligned with the foundation source.");
                }

                merged.Add(masaqWord);
                matchedMasaqWords++;
                continue;
            }

            if (legacyWord.Text.Length == 0 || !legacyWord.Text.All(char.IsDigit))
            {
                throw new InvalidDataException(
                    $"Foundation word '{legacyWord.Location}' is missing from MASAQ and is not an ayah marker.");
            }

            merged.Add(legacyWord);
            retainedMarkers++;
        }

        if (matchedMasaqWords != JsonMasaqSearchWordsReader.ExpectedWordCount
            || retainedMarkers != 6_236
            || merged.Count != legacyWords.Count)
        {
            throw new InvalidDataException(
                $"MASAQ/foundation merge is incomplete: matched={matchedMasaqWords}, markers={retainedMarkers}, total={merged.Count}.");
        }

        return merged;
    }

    private static string ResolveMasaqSourceRoot(string foundationSourceRoot)
    {
        var fullFoundationPath = Path.GetFullPath(foundationSourceRoot);
        var importSourcesRoot = Directory.GetParent(fullFoundationPath)?.FullName
            ?? throw new InvalidDataException("Could not resolve the import-sources directory.");

        return Path.Combine(importSourcesRoot, "masaq-corpus-aligned");
    }

    private static string GetSourcePath(ImportManifest manifest, string key)
    {
        if (!manifest.Sources.TryGetValue(key, out var source) || string.IsNullOrWhiteSpace(source.FullPath))
        {
            throw new InvalidDataException($"Manifest source '{key}' was not resolved.");
        }

        return source.FullPath;
    }
}
