using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

public sealed class FullI3rabImportSource : IFullI3rabImportSource
{
    private readonly FullI3rabManifestReader manifestReader;
    private readonly JsonFullI3rabSourceReader sourceReader;
    private readonly FullI3rabAssembler assembler;
    private readonly QuranDashboardDbContext dbContext;

    private FullI3rabFileDigests? capturedDigests;

    public FullI3rabImportSource(
        FullI3rabManifestReader manifestReader,
        JsonFullI3rabSourceReader sourceReader,
        FullI3rabAssembler assembler,
        QuranDashboardDbContext dbContext)
    {
        this.manifestReader = manifestReader;
        this.sourceReader = sourceReader;
        this.assembler = assembler;
        this.dbContext = dbContext;
    }

    public async Task<FullI3rabSourceData> LoadAsync(
        string sourcePath,
        FullI3rabExpectedCounts expectedCounts,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(expectedCounts);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct, expectedCounts);
        capturedDigests = await manifestReader.CaptureDigestsAsync(sourcePath, ct);

        var ayahIdsByVerseKey = await dbContext.QuranAyahs
            .AsNoTracking()
            .ToDictionaryAsync(
                ayah => ayah.VerseKey,
                ayah => ayah.Id,
                ct);

        if (ayahIdsByVerseKey.Count == 0)
        {
            throw new InvalidOperationException(FullI3rabInvariants.AyahsMissing);
        }

        var sources = new List<FullI3rabSourceDto>(manifest.ApprovedSources.Count);
        var entries = new List<FullI3rabEntryDto>();
        var ayahEntries = new List<FullI3rabAyahEntryDto>();
        var warnings = new List<string>();
        var seenSourceAyah = new HashSet<(string SourceKey, int AyahId)>();

        foreach (var manifestSource in manifest.ApprovedSources)
        {
            var fullPath = manifestSource.FullPath
                ?? throw new FullI3rabSourceException(
                    $"Manifest source '{manifestSource.SourceKey}' has no resolved file path.");

            var parsedSource = await sourceReader.ReadAsync(fullPath, ct);
            var assembled = assembler.AssembleSource(
                manifestSource,
                manifest,
                parsedSource,
                ayahIdsByVerseKey,
                seenSourceAyah,
                expectedCounts.AyahsPerSource);

            sources.Add(assembled.Source);
            entries.AddRange(assembled.Entries);
            ayahEntries.AddRange(assembled.AyahEntries);

            foreach (var warning in assembled.Warnings)
            {
                warnings.Add(
                    $"[{manifestSource.SourceKey}] {warning.Id}: expected {warning.Expected}, observed {warning.Observed}");
            }
        }

        return new FullI3rabSourceData(sources, entries, ayahEntries, warnings);
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
