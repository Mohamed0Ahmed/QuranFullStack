using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

public sealed class I3rabAssembler(II3rabRuleCatalog catalog) : II3rabAssembler
{
    public IReadOnlyList<I3rabSegmentLabel> Assemble(IReadOnlyList<I3rabSegmentInput> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var labels = new List<I3rabSegmentLabel>(segments.Count);

        foreach (var segment in segments)
        {
            var signatureKey = SegmentSignatureBuilder.Build(segment);

            if (catalog.TryGet(signatureKey, out var row))
            {
                labels.Add(new I3rabSegmentLabel(
                    segment.SegmentId,
                    row.I3rabArabic,
                    signatureKey,
                    I3rabStatusMapping.Approved,
                    null));
                continue;
            }

            labels.Add(new I3rabSegmentLabel(
                segment.SegmentId,
                null,
                signatureKey,
                I3rabStatusMapping.Unsupported,
                $"no catalogue match for signature {signatureKey}"));
        }

        return labels;
    }
}
