using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

internal static class DisplayWordsSyntheticSeed
{
    public const int ReadableWordCount = 8;
    public const int UniqueTashkeelCount = 5;
    public const int UniqueSimpleCount = 3;

    public const string TokenAlphaTashkeel = "ت-١";
    public const string TokenBetaTashkeel = "ت-٢";
    public const string TokenGammaTashkeel = "ت-٣";
    public const string TokenDeltaTashkeelA = "ت-٤َ";
    public const string TokenDeltaTashkeelB = "ت-٤ً";

    public const string TokenAlphaSimple = "س-أ";
    public const string TokenBetaSimple = "س-ب";
    public const string TokenGammaSimple = "س-ج";

    public static IReadOnlyList<Ayah> Ayahs { get; } =
    [
        new Ayah
        {
            Id = 1,
            SurahNumber = 1,
            AyahNumber = 1,
            VerseKey = "1:1",
            TextUthmani = "اختبار-١:١",
            WordsCountSource = 3,
            WordsCountReal = 3,
            PageFrom = 1,
            PageTo = 1
        },
        new Ayah
        {
            Id = 2,
            SurahNumber = 1,
            AyahNumber = 2,
            VerseKey = "1:2",
            TextUthmani = "اختبار-١:٢",
            WordsCountSource = 2,
            WordsCountReal = 2,
            PageFrom = 1,
            PageTo = 1
        },
        new Ayah
        {
            Id = 3,
            SurahNumber = 2,
            AyahNumber = 1,
            VerseKey = "2:1",
            TextUthmani = "اختبار-٢:١",
            WordsCountSource = 3,
            WordsCountReal = 3,
            PageFrom = 2,
            PageTo = 2
        }
    ];

    public const string AyahMarkerToken = "علامة-اختبار";

    public static IReadOnlyList<QuranWord> Words { get; } =
    [
        CreateWord(1, 1, 1, 1, 1, 1, 1, TokenAlphaTashkeel, TokenAlphaSimple),
        CreateWord(2, 1, 1, 1, 2, 1, 1, TokenBetaTashkeel, TokenBetaSimple),
        CreateWord(3, 1, 1, 1, 3, 1, 1, TokenAlphaTashkeel, TokenAlphaSimple),
        CreateWord(4, 2, 1, 2, 1, 1, 1, TokenAlphaTashkeel, TokenAlphaSimple),
        CreateWord(5, 2, 1, 2, 2, 1, 1, TokenGammaTashkeel, TokenGammaSimple),
        CreateWord(6, 3, 2, 1, 1, 2, 2, TokenAlphaTashkeel, TokenAlphaSimple),
        CreateWord(7, 3, 2, 1, 2, 2, 2, TokenDeltaTashkeelA, TokenAlphaSimple),
        CreateWord(8, 3, 2, 1, 3, 2, 2, TokenDeltaTashkeelB, TokenAlphaSimple)
    ];

    public static QuranWord AyahMarkerWord { get; } = CreateWord(
        id: 100,
        ayahId: 1,
        surahNumber: 1,
        ayahNumber: 1,
        wordNumber: 99,
        pageNumber: 1,
        lineNumber: 1,
        textUthmani: AyahMarkerToken,
        textUthmaniSimple: AyahMarkerToken,
        isAyahMarker: true);

    public static TokenStats AlphaTashkeelStats { get; } = new(4, 3, 2);
    public static TokenStats AlphaSimpleStats { get; } = new(6, 3, 2);

    private static QuranWord CreateWord(
        int id,
        int ayahId,
        short surahNumber,
        short ayahNumber,
        short wordNumber,
        short pageNumber,
        short lineNumber,
        string textUthmani,
        string textUthmaniSimple,
        bool isAyahMarker = false)
    {
        return new QuranWord
        {
            Id = id,
            AyahId = ayahId,
            SurahNumber = surahNumber,
            AyahNumber = ayahNumber,
            WordNumber = wordNumber,
            PageNumber = pageNumber,
            LineNumber = lineNumber,
            LineWordOrder = wordNumber,
            Location = $"{surahNumber}:{ayahNumber}:{wordNumber}",
            QpcGlyph = $"GLYPH-{id}",
            TextUthmani = textUthmani,
            TextUthmaniSimple = textUthmaniSimple,
            TextImlaeiSimple = $"إ-{textUthmaniSimple}",
            IsAyahMarker = isAyahMarker
        };
    }

    internal sealed record TokenStats(int OccurrencesCount, int AyahsCount, int SurahsCount);
}
