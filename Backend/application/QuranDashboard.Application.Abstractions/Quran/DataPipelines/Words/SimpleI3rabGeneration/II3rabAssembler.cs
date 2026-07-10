namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public interface II3rabAssembler
{
    IReadOnlyList<I3rabSegmentLabel> Assemble(IReadOnlyList<I3rabSegmentInput> segments);
}
