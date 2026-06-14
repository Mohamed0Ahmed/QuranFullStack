using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

public sealed class TafsirImportSource : ITafsirImportSource
{
    private readonly TafsirManifestReader manifestReader;
    private readonly JsonTafsirSourceReader sourceReader;
    private readonly TafsirAssembler assembler;
    private readonly QuranDashboardDbContext dbContext;

    private TafsirFileDigests? capturedDigests;

    public TafsirImportSource(
        TafsirManifestReader manifestReader,
        JsonTafsirSourceReader sourceReader,
        TafsirAssembler assembler,
        QuranDashboardDbContext dbContext)
    {
        this.manifestReader = manifestReader;
        this.sourceReader = sourceReader;
        this.assembler = assembler;
        this.dbContext = dbContext;
    }

    public async Task<TafsirSourceData> LoadAsync(string sourcePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct);
        capturedDigests = await manifestReader.CaptureDigestsAsync(sourcePath, ct);

        var ayahRows = await dbContext.QuranAyahs
            .AsNoTracking()
            .Select(ayah => new { ayah.VerseKey, ayah.Id, ayah.TextUthmani })
            .ToListAsync(ct);

        if (ayahRows.Count == 0)
        {
            throw new InvalidOperationException(TafsirInvariants.AyahsMissing);
        }

        var ayahIdsByVerseKey = ayahRows.ToDictionary(
            row => row.VerseKey,
            row => row.Id,
            StringComparer.Ordinal);
        var ayahTextsByVerseKey = ayahRows.ToDictionary(
            row => row.VerseKey,
            row => row.TextUthmani,
            StringComparer.Ordinal);

        var sources = new List<TafsirSourceDto>(manifest.ApprovedSources.Count);
        var entries = new List<TafsirEntryDto>();
        var ayahEntries = new List<TafsirAyahEntryDto>();
        var seenSourceAyah = new HashSet<(string SourceKey, int AyahId)>();

        foreach (var manifestSource in manifest.ApprovedSources)
        {
            var fullPath = manifestSource.FullPath
                ?? throw new TafsirSourceException(
                    $"Manifest source '{manifestSource.SourceKey}' has no resolved file path.");

            var parsedSource = await sourceReader.ReadAsync(fullPath, ct);
            var assembled = assembler.AssembleSource(
                manifestSource,
                parsedSource,
                manifest.ManifestJson,
                ayahIdsByVerseKey,
                ayahTextsByVerseKey,
                seenSourceAyah);

            sources.Add(assembled.Source);
            entries.AddRange(assembled.Entries);
            ayahEntries.AddRange(assembled.AyahEntries);
        }

        return new TafsirSourceData(
            sources,
            entries,
            ayahEntries,
            manifest.ExcludedSources);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigests is null)
        {
            throw new InvalidOperationException("Source digests were not captured. Call LoadAsync first.");
        }

        return await manifestReader.VerifyDigestsUnchangedAsync(sourcePath, capturedDigests, ct);
    }
}
