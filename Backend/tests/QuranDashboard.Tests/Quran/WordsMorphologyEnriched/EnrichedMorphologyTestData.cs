using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

internal static class EnrichedMorphologyTestData
{
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
