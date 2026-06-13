namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabGenerationWriter
{
    Task<I3rabGenerationResult> WriteAsync(
        IReadOnlyList<I3rabRuleSeedRow> rules,
        IReadOnlyList<I3rabSegmentLabel> labels,
        bool force,
        CancellationToken ct);
}
