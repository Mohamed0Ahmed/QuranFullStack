using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Application.Quran.Import.ImportQuranFoundation;

public sealed class QuranFoundationAssembler
{
    public AssembledQuranData Assemble(QuranImportSourceData source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var surahs = BuildSurahs(source.Surahs);
        var ayahIdBySurahAyah = source.Ayahs.ToDictionary(
            ayah => (ayah.SurahNumber, ayah.AyahNumber),
            ayah => ayah.Id);

        var glyphByLocation = IndexWordRecords(source.Glyph, "qpc-glyph");
        var uthmaniByLocation = IndexWordRecords(source.Uthmani, "uthmani");
        var uthmaniSimpleByLocation = IndexWordRecords(source.UthmaniSimple, "uthmani-simple");
        var imlaeiSimpleByLocation = IndexWordRecords(source.ImlaeiSimple, "imlaei-simple");

        var placementByWordId = DeriveWordPlacements(source.Layout);
        var words = BuildWords(
            source.Glyph,
            ayahIdBySurahAyah,
            placementByWordId,
            glyphByLocation,
            uthmaniByLocation,
            uthmaniSimpleByLocation,
            imlaeiSimpleByLocation);

        FlagAyahMarkers(words);
        var ayahs = BuildAyahs(source.Ayahs, words);
        var pages = BuildPages(source.Layout, words);
        var lines = BuildLines(source.Layout);

        return new AssembledQuranData(surahs, ayahs, pages, words, lines);
    }

    private static List<Surah> BuildSurahs(IReadOnlyList<SurahMetaDto> surahMetadata)
    {
        return surahMetadata
            .OrderBy(surah => surah.Id)
            .Select(surah => new Surah
            {
                SurahNumber = (short)surah.Id,
                NameArabic = surah.NameArabic,
                NameSimple = surah.NameSimple,
                NameTransliteration = surah.Name,
                RevelationPlace = ParseRevelationPlace(surah.RevelationPlace),
                RevelationOrder = (short)surah.RevelationOrder,
                VersesCount = (short)surah.VersesCount,
                BismillahPre = surah.BismillahPre
            })
            .ToList();
    }

    private static List<Ayah> BuildAyahs(IReadOnlyList<AyahMetaDto> ayahMetadata, IReadOnlyList<QuranWord> words)
    {
        var wordsByAyahId = words.GroupBy(word => word.AyahId).ToDictionary(group => group.Key, group => group.ToList());

        return ayahMetadata
            .OrderBy(ayah => ayah.Id)
            .Select(ayah =>
            {
                if (!wordsByAyahId.TryGetValue(ayah.Id, out var ayahWords) || ayahWords.Count == 0)
                {
                    throw new InvalidDataException($"Ayah '{ayah.VerseKey}' has no assembled words.");
                }

                return new Ayah
                {
                    Id = ayah.Id,
                    SurahNumber = (short)ayah.SurahNumber,
                    AyahNumber = (short)ayah.AyahNumber,
                    VerseKey = ayah.VerseKey,
                    TextUthmani = ayah.Text,
                    WordsCountSource = (short)ayah.WordsCount,
                    WordsCountReal = (short)(ayahWords.Count - 1),
                    PageFrom = ayahWords.Min(word => word.PageNumber),
                    PageTo = ayahWords.Max(word => word.PageNumber)
                };
            })
            .ToList();
    }

    private static List<MushafPage> BuildPages(LayoutDto layout, IReadOnlyList<QuranWord> words)
    {
        var wordsByPage = words.GroupBy(word => word.PageNumber).ToDictionary(group => group.Key, group => group.OrderBy(word => word.Id).ToList());
        var pages = new List<MushafPage>(layout.PagesCount);

        for (var pageNumber = 1; pageNumber <= layout.PagesCount; pageNumber++)
        {
            if (!layout.Pages.TryGetValue(pageNumber, out var pageLines))
            {
                throw new InvalidDataException($"Layout is missing page {pageNumber}.");
            }

            if (!wordsByPage.TryGetValue((short)pageNumber, out var pageWords) || pageWords.Count == 0)
            {
                throw new InvalidDataException($"Page {pageNumber} has no assembled words.");
            }

            pages.Add(new MushafPage
            {
                PageNumber = (short)pageNumber,
                FirstSurahNumber = pageWords[0].SurahNumber,
                FirstAyahNumber = pageWords[0].AyahNumber,
                LastSurahNumber = pageWords[^1].SurahNumber,
                LastAyahNumber = pageWords[^1].AyahNumber,
                LinesCount = (short)pageLines.Count
            });
        }

        return pages;
    }

    private static List<MushafLine> BuildLines(LayoutDto layout)
    {
        var lines = new List<MushafLine>();

        foreach (var (pageNumber, pageLines) in layout.Pages.OrderBy(page => page.Key))
        {
            foreach (var line in pageLines.OrderBy(line => line.LineNumber))
            {
                lines.Add(new MushafLine
                {
                    PageNumber = (short)pageNumber,
                    LineNumber = (short)line.LineNumber,
                    LineType = ParseLineType(line.LineType),
                    IsCentered = line.IsCentered,
                    SurahNumber = line.SurahNumber.HasValue ? (short)line.SurahNumber.Value : null,
                    FirstWordId = line.FirstWordId,
                    LastWordId = line.LastWordId,
                    WordsCount = line.LineType == "ayah" && line.FirstWordId.HasValue && line.LastWordId.HasValue
                        ? (short)(line.LastWordId.Value - line.FirstWordId.Value + 1)
                        : (short)0
                });
            }
        }

        return lines;
    }

    private static List<QuranWord> BuildWords(
        IReadOnlyList<WordRecordDto> skeleton,
        IReadOnlyDictionary<(int SurahNumber, int AyahNumber), int> ayahIdBySurahAyah,
        IReadOnlyDictionary<int, WordPlacement> placementByWordId,
        IReadOnlyDictionary<string, WordRecordDto> glyphByLocation,
        IReadOnlyDictionary<string, WordRecordDto> uthmaniByLocation,
        IReadOnlyDictionary<string, WordRecordDto> uthmaniSimpleByLocation,
        IReadOnlyDictionary<string, WordRecordDto> imlaeiSimpleByLocation)
    {
        var words = new List<QuranWord>(skeleton.Count);

        foreach (var record in skeleton.OrderBy(word => word.Id))
        {
            if (!placementByWordId.TryGetValue(record.Id, out var placement))
            {
                throw new InvalidDataException($"Word id {record.Id} ({record.Location}) has no layout placement.");
            }

            if (!ayahIdBySurahAyah.TryGetValue((record.Surah, record.Ayah), out var ayahId))
            {
                throw new InvalidDataException($"Word '{record.Location}' references unknown ayah {record.Surah}:{record.Ayah}.");
            }

            var glyph = RequireAlignedRecord(glyphByLocation, record, "qpc-glyph");
            var uthmani = RequireAlignedRecord(uthmaniByLocation, record, "uthmani");
            var uthmaniSimple = RequireAlignedRecord(uthmaniSimpleByLocation, record, "uthmani-simple");
            var imlaeiSimple = RequireAlignedRecord(imlaeiSimpleByLocation, record, "imlaei-simple");

            words.Add(new QuranWord
            {
                Id = record.Id,
                Location = record.Location,
                AyahId = ayahId,
                SurahNumber = (short)record.Surah,
                AyahNumber = (short)record.Ayah,
                WordNumber = (short)record.Word,
                PageNumber = placement.PageNumber,
                LineNumber = placement.LineNumber,
                LineWordOrder = placement.LineWordOrder,
                QpcGlyph = glyph.Text,
                TextUthmani = uthmani.Text,
                TextUthmaniSimple = uthmaniSimple.Text,
                TextImlaeiSimple = imlaeiSimple.Text,
                IsAyahMarker = false
            });
        }

        return words;
    }

    internal static Dictionary<int, WordPlacement> DeriveWordPlacements(LayoutDto layout)
    {
        var placements = new Dictionary<int, WordPlacement>();

        foreach (var (pageNumber, pageLines) in layout.Pages.OrderBy(page => page.Key))
        {
            foreach (var line in pageLines.OrderBy(line => line.LineNumber))
            {
                if (!string.Equals(line.LineType, "ayah", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.FirstWordId is null || line.LastWordId is null)
                {
                    throw new InvalidDataException(
                        $"Ayah line {pageNumber}:{line.LineNumber} is missing first/last word ids.");
                }

                if (line.LastWordId.Value < line.FirstWordId.Value)
                {
                    throw new InvalidDataException(
                        $"Ayah line {pageNumber}:{line.LineNumber} has an invalid word-id range.");
                }

                var lineWordOrder = 1;
                for (var wordId = line.FirstWordId.Value; wordId <= line.LastWordId.Value; wordId++)
                {
                    if (!placements.TryAdd(
                            wordId,
                            new WordPlacement((short)pageNumber, (short)line.LineNumber, (short)lineWordOrder)))
                    {
                        throw new InvalidDataException($"Word id {wordId} is assigned to multiple layout ranges.");
                    }

                    lineWordOrder++;
                }
            }
        }

        return placements;
    }

    internal static void FlagAyahMarkers(IList<QuranWord> words)
    {
        foreach (var ayahGroup in words.GroupBy(word => (word.SurahNumber, word.AyahNumber)))
        {
            var maxWordNumber = ayahGroup.Max(word => word.WordNumber);

            foreach (var word in ayahGroup)
            {
                var isLastWord = word.WordNumber == maxWordNumber;
                var isDigitsOnly = word.TextImlaeiSimple.Length > 0 &&
                                   word.TextImlaeiSimple.All(char.IsDigit);
                word.IsAyahMarker = isLastWord && isDigitsOnly;
            }
        }
    }

    private static Dictionary<string, WordRecordDto> IndexWordRecords(
        IReadOnlyList<WordRecordDto> records,
        string sourceName)
    {
        var index = new Dictionary<string, WordRecordDto>(records.Count, StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (!index.TryAdd(record.Location, record))
            {
                throw new InvalidDataException($"Duplicate location '{record.Location}' in {sourceName}.");
            }
        }

        return index;
    }

    private static WordRecordDto RequireAlignedRecord(
        IReadOnlyDictionary<string, WordRecordDto> recordsByLocation,
        WordRecordDto skeleton,
        string sourceName)
    {
        if (!recordsByLocation.TryGetValue(skeleton.Location, out var record))
        {
            throw new InvalidDataException($"Missing location '{skeleton.Location}' in {sourceName}.");
        }

        if (record.Id != skeleton.Id ||
            record.Surah != skeleton.Surah ||
            record.Ayah != skeleton.Ayah ||
            record.Word != skeleton.Word)
        {
            throw new InvalidDataException(
                $"Location '{skeleton.Location}' is misaligned between skeleton and {sourceName}.");
        }

        return record;
    }

    private static RevelationPlace ParseRevelationPlace(string value) => value switch
    {
        "makkah" => RevelationPlace.Makkah,
        "madinah" => RevelationPlace.Madinah,
        _ => throw new InvalidDataException($"Unknown revelation place '{value}'.")
    };

    private static MushafLineType ParseLineType(string value) => value switch
    {
        "ayah" => MushafLineType.Ayah,
        "surah_name" => MushafLineType.SurahName,
        "basmallah" => MushafLineType.Basmallah,
        _ => throw new InvalidDataException($"Unknown mushaf line type '{value}'.")
    };

    internal sealed record WordPlacement(short PageNumber, short LineNumber, short LineWordOrder);
}
