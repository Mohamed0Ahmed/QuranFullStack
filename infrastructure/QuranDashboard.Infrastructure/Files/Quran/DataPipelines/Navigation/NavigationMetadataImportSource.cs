using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;

public sealed class NavigationMetadataImportSource : INavigationMetadataImportSource
{
    private readonly NavigationManifestReader manifestReader;
    private readonly JsonNavigationDatasetReader datasetReader;

    private NavigationFileDigests? capturedDigests;

    public NavigationMetadataImportSource(
        NavigationManifestReader manifestReader,
        JsonNavigationDatasetReader datasetReader)
    {
        this.manifestReader = manifestReader;
        this.datasetReader = datasetReader;
    }

    public async Task<NavigationMetadataSourceData> LoadAsync(
        string sourcePath,
        NavigationExpectedCounts expected,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(expected);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct, expected);
        capturedDigests = await manifestReader.CapturePackageDigestsAsync(sourcePath, ct);
        var parsed = await datasetReader.ReadAllAsync(manifest, ct);

        var sourceFiles = manifest.SourceFiles
            .Select(file => new NavigationSourceFileDto(
                file.RelativePath,
                file.DatasetKey,
                file.RecordCount,
                file.Sha256,
                file.SizeBytes))
            .ToList();

        return new NavigationMetadataSourceData(
            ToDivisionDtos(parsed.Juz),
            ToDivisionDtos(parsed.Hizb),
            ToDivisionDtos(parsed.Rub),
            parsed.Sajda
                .Select(sajda => new NavigationSajdaDto(
                    sajda.SajdahNumber,
                    sajda.VerseKey,
                    sajda.SajdahType))
                .ToList(),
            sourceFiles);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigests is null)
        {
            throw new InvalidOperationException("Source digests were not captured. Call LoadAsync first.");
        }

        return await manifestReader.VerifyDigestsUnchangedAsync(sourcePath, capturedDigests, ct);
    }

    private static IReadOnlyList<NavigationDivisionDto> ToDivisionDtos(IReadOnlyList<ParsedNavigationDivision> divisions) =>
        divisions
            .Select(division => new NavigationDivisionDto(
                division.Number,
                division.SourceVersesCount,
                division.FirstVerseKey,
                division.LastVerseKey,
                division.VerseMapping))
            .ToList();
}
