using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

// Resolves the staged Dashboard-ready enriched artifact. The artifact is ~96 MB and lives under the
// gitignored resources/ tree; it is staged locally (Feature 020 §1) but not guaranteed in every CI clone.
// Tests that depend on it skip cleanly when it is absent rather than failing the build.
public sealed class EnrichedMorphologyArtifactFixture
{
    private const string ArtifactRelativePath =
        "resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json";

    private const string SourceRootRelativePath =
        "resources/import-sources/quran-enriched-morphology";

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var resources = Path.Combine(directory.FullName, "resources");
            var backend = Path.Combine(directory.FullName, "Backend");
            if (Directory.Exists(resources) && Directory.Exists(backend))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }

    internal static string SourceRoot => Path.Combine(ResolveRepositoryRoot(), SourceRootRelativePath);

    internal static string ArtifactPath => Path.Combine(ResolveRepositoryRoot(), ArtifactRelativePath);

internal static bool IsArtifactPresent => File.Exists(ArtifactPath);

    internal static async Task<MorphologySourceData> LoadAsync()
    {
        var manifestReader = new EnrichedMorphologyManifestReader();
        var sourceReader = new EnrichedMorphologyReader();
        var dimensionBuilder = new EnrichedDimensionBuilder();
        var source = new EnrichedMorphologyImportSource(manifestReader, sourceReader, dimensionBuilder);
        return await source.LoadAsync(SourceRoot, CancellationToken.None);
    }
}

internal sealed class EnrichedArtifactFactAttribute : FactAttribute
{
    public EnrichedArtifactFactAttribute()
    {
        if (!EnrichedMorphologyArtifactFixture.IsArtifactPresent)
        {
            Skip = "Requires local gitignored enriched morphology artifact under resources/import-sources/quran-enriched-morphology.";
        }
    }
}

internal sealed class EnrichedArtifactTheoryAttribute : TheoryAttribute
{
    public EnrichedArtifactTheoryAttribute()
    {
        if (!EnrichedMorphologyArtifactFixture.IsArtifactPresent)
        {
            Skip = "Requires local gitignored enriched morphology artifact under resources/import-sources/quran-enriched-morphology.";
        }
    }
}
