namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabGenerationWriter
{
    I3rabGenerationResult Write(
        IReadOnlyList<I3rabRuleSeedRow> rules,
        IReadOnlyList<I3rabSegmentLabel> labels,
        bool force);
}
