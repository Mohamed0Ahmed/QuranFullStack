using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

public sealed class MutashabihatImportSource : IMutashabihatImportSource
{
    private readonly MutashabihatManifestReader manifestReader;
    private readonly JsonPhrasesReader phrasesReader;
    private readonly JsonSimilarAyahReader similarAyahReader;
    private readonly MutashabihatAssembler assembler;
    private readonly QuranDashboardDbContext dbContext;
    private readonly MutashabihatImportSession importSession;

    private MutashabihatFileDigests? capturedDigests;

    public MutashabihatImportSource(
        MutashabihatManifestReader manifestReader,
        JsonPhrasesReader phrasesReader,
        JsonSimilarAyahReader similarAyahReader,
        MutashabihatAssembler assembler,
        QuranDashboardDbContext dbContext,
        MutashabihatImportSession importSession)
    {
        this.manifestReader = manifestReader;
        this.phrasesReader = phrasesReader;
        this.similarAyahReader = similarAyahReader;
        this.assembler = assembler;
        this.dbContext = dbContext;
        this.importSession = importSession;
    }

    public async Task<MutashabihatSourceData> LoadAsync(string sourcePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct);
        capturedDigests = await manifestReader.CaptureDigestsAsync(sourcePath, ct);

        var ayahIdsByVerseKey = await ReadAyahIdsByVerseKeyAsync(ct);
        if (ayahIdsByVerseKey.Count == 0)
        {
            throw new InvalidOperationException(MutashabihatInvariants.AyahsMissing);
        }

        var phrasesPath = GetManifestPath(manifest, "mutashabihat-ul-quran/phrases.json");
        var phrases = await phrasesReader.ReadAsync(phrasesPath, ct);
        importSession.RawOccurrenceCount = phrases.RawOccurrenceCount;

        var similarSources = await similarAyahReader.ReadAsync(
            GetManifestPath(manifest, "similar-ayahs/matching-ayah.json"), ct);

        var groupsData = assembler.AssembleGroups(phrases, ayahIdsByVerseKey);
        var links = assembler.AssembleLinks(similarSources, ayahIdsByVerseKey);

        return new MutashabihatSourceData(groupsData.Groups, links);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigests is null)
        {
            throw new InvalidOperationException("Source digests were not captured. Call LoadAsync first.");
        }

        return await manifestReader.VerifyDigestsUnchangedAsync(sourcePath, capturedDigests, ct);
    }

    private async Task<Dictionary<string, int>> ReadAyahIdsByVerseKeyAsync(CancellationToken ct)
    {
        var rows = await dbContext.QuranAyahs
            .AsNoTracking()
            .Select(ayah => new { ayah.VerseKey, ayah.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.VerseKey, row => row.Id, StringComparer.Ordinal);
    }

    private static string GetManifestPath(MutashabihatManifest manifest, string key)
    {
        if (!manifest.Files.TryGetValue(key, out var file) || string.IsNullOrWhiteSpace(file.FullPath))
        {
            throw new MutashabihatSourceException($"Manifest file '{key}' was not resolved.");
        }

        return file.FullPath;
    }
}
