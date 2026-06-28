using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class MorphologyImportSource : IMorphologyImportSource
{
    private readonly IWordLemmaNormalizationReader normalizationReader;
    private readonly MorphologyManifestReader manifestReader;
    private readonly JsonAlignedCorpusReader corpusReader;
    private readonly JsonQulRootReader rootReader;
    private readonly JsonQulLemmaReader lemmaReader;
    private readonly JsonQulStemReader stemReader;
    private readonly MorphologyAssembler assembler;
    private readonly QuranDashboardDbContext dbContext;

    private MorphologyFileDigests? capturedDigests;

    public MorphologyImportSource(
        IWordLemmaNormalizationReader normalizationReader,
        MorphologyManifestReader manifestReader,
        JsonAlignedCorpusReader corpusReader,
        JsonQulRootReader rootReader,
        JsonQulLemmaReader lemmaReader,
        JsonQulStemReader stemReader,
        MorphologyAssembler assembler,
        QuranDashboardDbContext dbContext)
    {
        this.normalizationReader = normalizationReader;
        this.manifestReader = manifestReader;
        this.corpusReader = corpusReader;
        this.rootReader = rootReader;
        this.lemmaReader = lemmaReader;
        this.stemReader = stemReader;
        this.assembler = assembler;
        this.dbContext = dbContext;
    }

    public async Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct);
        capturedDigests = await manifestReader.CaptureDigestsAsync(sourcePath, ct);

        var readableWordIdsByLocation = await ReadReadableWordIdsAsync(ct);
        if (readableWordIdsByLocation.Count == 0)
        {
            throw new InvalidOperationException(MorphologyInvariants.FoundationNotLoaded);
        }

        var corpusPath = GetManifestPath(manifest, "corpus/quranic-corpus-morphology-qpc-aligned.json");
        var corpusWords = await corpusReader.ReadAsync(corpusPath, ct);

        var roots = await rootReader.ReadAsync(
            GetManifestPath(manifest, "qul/word-root.json"), ct);
        var lemmas = await lemmaReader.ReadAsync(
            GetManifestPath(manifest, "qul/word-lemma.json"), ct);
        var stems = await stemReader.ReadAsync(
            GetManifestPath(manifest, "qul/word-stem-corrected-arabic.json"), ct);

        var normalized = normalizationReader.Apply(
            lemmas,
            normalizationReader.Load(),
            new HashSet<string>(readableWordIdsByLocation.Keys, StringComparer.Ordinal));

        var source = assembler.Assemble(
            corpusWords,
            readableWordIdsByLocation,
            roots,
            normalized.CorrectedLemmas,
            stems);

        return source with { CorrectionSummary = normalized.Summary };
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigests is null)
        {
            throw new InvalidOperationException("Source digests were not captured. Call LoadAsync first.");
        }

        return await manifestReader.VerifyDigestsUnchangedAsync(sourcePath, capturedDigests, ct);
    }

    private async Task<Dictionary<string, int>> ReadReadableWordIdsAsync(CancellationToken ct)
    {
        var rows = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker)
            .Select(word => new { word.Location, word.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.Location, row => row.Id, StringComparer.Ordinal);
    }

    private static string GetManifestPath(MorphologyManifest manifest, string key)
    {
        if (!manifest.Files.TryGetValue(key, out var file) || string.IsNullOrWhiteSpace(file.FullPath))
        {
            throw new InvalidDataException($"Manifest file '{key}' was not resolved.");
        }

        return file.FullPath;
    }
}
