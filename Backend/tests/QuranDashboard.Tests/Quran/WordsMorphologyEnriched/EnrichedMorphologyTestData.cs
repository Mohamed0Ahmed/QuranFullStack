using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

// Pure, source-safe synthetic builders for the enriched pathway tests. No real Quranic assertions are
// made on synthetic data; the special-word correction tests (41:44:16 etc.) read those records from the
// real artifact via a dedicated fixture, not from these synthetic builders.
internal static class EnrichedMorphologyTestData
{
    // A minimal valid STEM record. Default values mirror the artifact's shape (buckwalter + bridge Arabic
    // already merged); tests override only what they assert on.
    internal static EnrichedMorphologyRecord StemRecord(
        string location,
        int quranWordId,
        string formBuckwalter,
        string formArabic,
        string pos = "N",
        string featuresRaw = "STEM|POS:N|NOM",
        string? rootBuckwalter = null,
        string? rootArabic = null,
        string? lemmaBuckwalter = null,
        string? lemmaArabic = null,
        string textUthmani = "") => new()
        {
            Location = location,
            QuranWordId = quranWordId,
            Surah = ParseSurah(location),
            Ayah = ParseAyah(location),
            WordNumber = ParseWord(location),
            TextUthmani = textUthmani,
            CorpusPresent = true,
            QuranWordIdVerifiedAgainstDashboard = true,
            Segments =
            [
                new EnrichedMorphologySegment
                {
                    SegmentNumber = 1,
                    Kind = "STEM",
                    Pos = pos,
                    FormBuckwalter = formBuckwalter,
                    FormArabic = formArabic,
                    FeaturesRaw = featuresRaw,
                    RootBuckwalter = rootBuckwalter,
                    RootArabic = rootArabic,
                    LemmaBuckwalter = lemmaBuckwalter,
                    LemmaArabic = lemmaArabic,
                    StemBuckwalter = formBuckwalter,
                    StemArabic = formArabic,
                }
            ]
        };

    // A two-STEM word mirroring 8:6:12's shape (PREFIX + STEM + STEM + SUFFIX). Used to assert the
    // boundary ayah carries 2 real segments per the artifact.
    internal static EnrichedMorphologyRecord MultiSegmentRecord(
        string location,
        int quranWordId,
        IReadOnlyList<EnrichedMorphologySegment> segments,
        string textUthmani = "") => new()
        {
            Location = location,
            QuranWordId = quranWordId,
            Surah = ParseSurah(location),
            Ayah = ParseAyah(location),
            WordNumber = ParseWord(location),
            TextUthmani = textUthmani,
            CorpusPresent = true,
            QuranWordIdVerifiedAgainstDashboard = true,
            Segments = segments
        };

    internal static EnrichedMorphologySegment Segment(
        short number,
        string kind,
        string? pos,
        string? formBuckwalter,
        string? formArabic,
        string featuresRaw = "",
        string? rootBuckwalter = null,
        string? rootArabic = null,
        string? lemmaBuckwalter = null,
        string? lemmaArabic = null) => new()
        {
            SegmentNumber = number,
            Kind = kind,
            Pos = pos,
            FormBuckwalter = formBuckwalter,
            FormArabic = formArabic,
            FeaturesRaw = featuresRaw,
            RootBuckwalter = rootBuckwalter,
            RootArabic = rootArabic,
            LemmaBuckwalter = lemmaBuckwalter,
            LemmaArabic = lemmaArabic,
        };

    private static int ParseSurah(string location) =>
        int.Parse(location.Split(':')[0], System.Globalization.CultureInfo.InvariantCulture);

    private static int ParseAyah(string location) =>
        int.Parse(location.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);

    private static int ParseWord(string location) =>
        int.Parse(location.Split(':')[2], System.Globalization.CultureInfo.InvariantCulture);
}
