using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

public sealed class SegmentArabicRenderer
{
    private static readonly HashSet<char> QuranicAnnotationMarks = ['@', ',', '.', '[', ']', '"', ':', ';', '+', '!', '%', '-'];
    private static readonly HashSet<char> ReviewChars = ['_', '#'];

    private readonly BuckwalterArabicMap map;

    public SegmentArabicRenderer(BuckwalterArabicMap map)
    {
        this.map = map;
    }

    public (string? FormArabicNormalized, string? RenderTier) Render(string formBuckwalter)
    {
        if (string.IsNullOrWhiteSpace(formBuckwalter))
        {
            return (null, null);
        }

        var (arabic, _) = map.Transliterate(formBuckwalter);

        var tier = ClassifyTier(formBuckwalter);

        return (arabic, tier);
    }

    public List<char> CollectUnmappedCharacters(string formBuckwalter)
    {
        if (string.IsNullOrEmpty(formBuckwalter))
        {
            return [];
        }

        var unmapped = new List<char>();

        foreach (var ch in formBuckwalter)
        {
            if (!map.IsMapped(ch) && !unmapped.Contains(ch))
            {
                unmapped.Add(ch);
            }
        }

        return unmapped;
    }

    public List<string> CollectCharsetWarnings(IReadOnlyList<AlignedSegmentDto> segments)
    {
        var warnings = new List<string>();

        foreach (var segment in segments)
        {
            var unmapped = CollectUnmappedCharacters(segment.FormBuckwalter);
            if (unmapped.Count > 0)
            {
                warnings.Add(
                    $"Segment {segment.Kind} #{segment.SegmentNumber} at {segment.FormBuckwalter}: " +
                    $"unmapped characters [{string.Join(", ", unmapped.Select(c => $"'{c}' (U+{((ushort)c):X4})"))}]");
            }
        }

        return warnings;
    }

    private static string ClassifyTier(string formBuckwalter)
    {
        if (formBuckwalter.Contains(' '))
        {
            return "multiword";
        }

        foreach (var ch in formBuckwalter)
        {
            if (ReviewChars.Contains(ch))
            {
                return "review";
            }
        }

        foreach (var ch in formBuckwalter)
        {
            if (QuranicAnnotationMarks.Contains(ch))
            {
                return "quranic_marks";
            }
        }

        return "clean";
    }
}
