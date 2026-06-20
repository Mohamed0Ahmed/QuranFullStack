using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

public sealed class TranslationImportSource : ITranslationImportSource
{
    private readonly TranslationManifestReader manifestReader;
    private readonly TranslationDisplayMetadataReader displayMetadataReader;
    private readonly JsonTranslationSourceReader sourceReader;
    private readonly TranslationAssembler assembler;
    private readonly QuranDashboardDbContext dbContext;

    private TranslationFileDigests? capturedDigests;

    public TranslationImportSource(
        TranslationManifestReader manifestReader,
        TranslationDisplayMetadataReader displayMetadataReader,
        JsonTranslationSourceReader sourceReader,
        TranslationAssembler assembler,
        QuranDashboardDbContext dbContext)
    {
        this.manifestReader = manifestReader;
        this.displayMetadataReader = displayMetadataReader;
        this.sourceReader = sourceReader;
        this.assembler = assembler;
        this.dbContext = dbContext;
    }

    public async Task<TranslationSourceData> LoadAsync(
        string sourcePath,
        TranslationExpectedCounts expectedCounts,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(expectedCounts);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct, expectedCounts);
        capturedDigests = await manifestReader.CapturePackageDigestsAsync(sourcePath, ct);

        var approvedSourceKeys = manifest.ApprovedSources
            .Select(source => source.SourceKey)
            .ToHashSet(StringComparer.Ordinal);

        var displayMetadata = await displayMetadataReader.ReadAsync(
            sourcePath,
            approvedSourceKeys,
            expectedCounts,
            ct);

        var displayBySourceKey = displayMetadata.Records.ToDictionary(
            record => record.SourceKey,
            StringComparer.Ordinal);

        ValidateExcludedSources(manifest);

        var ayahRows = await dbContext.QuranAyahs
            .AsNoTracking()
            .Select(ayah => new { ayah.VerseKey, ayah.Id, ayah.TextUthmani })
            .ToListAsync(ct);

        if (ayahRows.Count == 0)
        {
            throw new InvalidOperationException(TranslationInvariants.AyahsMissing);
        }

        var ayahIdsByVerseKey = ayahRows.ToDictionary(
            row => row.VerseKey,
            row => row.Id,
            StringComparer.Ordinal);
        var ayahTextsByVerseKey = ayahRows.ToDictionary(
            row => row.VerseKey,
            row => row.TextUthmani,
            StringComparer.Ordinal);

        var sources = new List<TranslationSourceDto>(manifest.ApprovedSources.Count);
        var ayahEntries = new List<TranslationAyahEntryDto>();
        var seenSourceAyah = new HashSet<(string SourceKey, int AyahId)>();

        foreach (var manifestSource in manifest.ApprovedSources)
        {
            if (!displayBySourceKey.TryGetValue(manifestSource.SourceKey, out var displayRecord))
            {
                throw new TranslationSourceException(
                    $"Display metadata for source '{manifestSource.SourceKey}' was not found.");
            }

            var fullPath = manifestSource.FullPath
                ?? throw new TranslationSourceException(
                    $"Manifest source '{manifestSource.SourceKey}' has no resolved file path.");

            var parsedSource = await sourceReader.ReadAsync(fullPath, ct);
            var assembled = assembler.AssembleSource(
                manifestSource,
                displayRecord,
                parsedSource,
                ayahIdsByVerseKey,
                ayahTextsByVerseKey,
                seenSourceAyah,
                expectedCounts.AyahsPerSource);

            sources.Add(assembled.Source);
            ayahEntries.AddRange(assembled.AyahEntries);
        }

        ValidateFinalTypeCounts(sources, expectedCounts);

        return new TranslationSourceData(
            sources,
            ayahEntries,
            manifest.ExcludedSources);
    }

    private static void ValidateFinalTypeCounts(
        IReadOnlyList<TranslationSourceDto> sources,
        TranslationExpectedCounts expectedCounts)
    {
        TranslationTypeCountValidation.EnsureFinalTypeCounts(sources, expectedCounts);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigests is null)
        {
            throw new InvalidOperationException("Source digests were not captured. Call LoadAsync first.");
        }

        return await manifestReader.VerifyDigestsUnchangedAsync(sourcePath, capturedDigests, ct);
    }

    private static void ValidateExcludedSources(TranslationPackageManifest manifest)
    {
        var approvedKeys = manifest.ApprovedSources
            .Select(source => source.SourceKey)
            .ToHashSet(StringComparer.Ordinal);

        var excludedInApproved = manifest.ExcludedSources
            .Where(source => approvedKeys.Contains(source.SourceKey))
            .Select(source => source.SourceKey)
            .ToList();

        var check = TranslationValidationChecks.Hard(
            TranslationInvariants.CheckNoExcludedSources,
            "no excluded sources in approved set",
            excludedInApproved.Count == 0 ? "none" : string.Join(", ", excludedInApproved),
            excludedInApproved.Count == 0);

        TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
    }
}
