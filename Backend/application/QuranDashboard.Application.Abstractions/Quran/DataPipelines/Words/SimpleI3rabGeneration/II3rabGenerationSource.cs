namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public interface II3rabGenerationSource
{
    I3rabMorphologyReadiness AssessMorphologyReadiness();

    bool I3rabAlreadyPopulated();

    IReadOnlyList<I3rabSegmentInput> LoadSegments();
}
