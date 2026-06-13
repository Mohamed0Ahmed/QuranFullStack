namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabGenerationSource
{
    I3rabMorphologyReadiness AssessMorphologyReadiness();

    bool I3rabAlreadyPopulated();

    IReadOnlyList<I3rabSegmentInput> LoadSegments();
}
