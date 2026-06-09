
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Infrastructure.Files.Quran.Import;

public sealed class QuranImportSource : IQuranImportSource
{
    private readonly ManifestReader manifestReader;
    private readonly JsonWordSourceReader wordSourceReader;
    private readonly JsonLayoutSourceReader layoutSourceReader;
    private readonly JsonMetadataSourceReader metadataSourceReader;

    public QuranImportSource(
        ManifestReader manifestReader,
        JsonWordSourceReader wordSourceReader,
        JsonLayoutSourceReader layoutSourceReader,
        JsonMetadataSourceReader metadataSourceReader)
    {
        this.manifestReader = manifestReader;
        this.wordSourceReader = wordSourceReader;
        this.layoutSourceReader = layoutSourceReader;
        this.metadataSourceReader = metadataSourceReader;
    }

    public async Task<QuranImportSourceData> LoadAsync(string sourceRoot, CancellationToken ct)
    {
        var manifest = await manifestReader.ReadAsync(sourceRoot, ct);

        var glyph = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "qpc-glyph"), ct);
        var uthmani = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "uthmani"), ct);
        var uthmaniSimple = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "uthmani-simple"), ct);
        var imlaeiSimple = await wordSourceReader.ReadAsync(GetSourcePath(manifest, "imlaei-simple"), ct);
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
            layout,
            manifest.Version);
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
