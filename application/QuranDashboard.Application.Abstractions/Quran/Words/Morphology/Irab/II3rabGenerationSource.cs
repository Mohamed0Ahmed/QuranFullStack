namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabGenerationSource
{
    bool MorphologyIsReady(out int segmentCount);

    bool I3rabAlreadyPopulated();

    IReadOnlyList<I3rabSegmentInput> LoadSegments();
}
