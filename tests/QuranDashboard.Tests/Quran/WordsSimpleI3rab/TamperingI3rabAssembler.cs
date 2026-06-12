using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

internal sealed class TamperingI3rabAssembler(
    II3rabAssembler inner,
    Func<IReadOnlyList<I3rabSegmentLabel>, IReadOnlyList<I3rabSegmentLabel>> tamper) : II3rabAssembler
{
    public IReadOnlyList<I3rabSegmentLabel> Assemble(IReadOnlyList<I3rabSegmentInput> segments) =>
        tamper(inner.Assemble(segments));
}
