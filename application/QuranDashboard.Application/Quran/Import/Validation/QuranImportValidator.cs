using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Application.Quran.Import.Validation;

public sealed class QuranImportValidator
{
    private readonly IdContiguityCheck idContiguityCheck = new();
    private readonly SourceAlignmentCheck sourceAlignmentCheck = new();
    private readonly LayoutCoverageCheck layoutCoverageCheck = new();
    private readonly DenormPlacementCheck denormPlacementCheck = new();
    private readonly PageReconstructionCheck pageReconstructionCheck = new();

    public QuranImportValidationResult Validate(
        string sourceRoot,
        string manifestVersion,
        QuranImportSourceData source,
        AssembledQuranData assembled,
        bool forced)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestVersion);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(assembled);

        var checks = new List<ImportCheckResult>();
        var warnings = new List<string>();
        var infoNotes = new List<string>();

        var surahCount = assembled.Surahs.Count;
        var verseSum = assembled.Surahs.Sum(surah => surah.VersesCount);
        var surahCountPassed = surahCount == ImportValidationExpectedCounts.Surahs &&
                               verseSum == ImportValidationExpectedCounts.Ayahs;
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.SurahCount,
            $"{ImportValidationExpectedCounts.Surahs} / sum={ImportValidationExpectedCounts.Ayahs}",
            $"{surahCount} / sum={verseSum}",
            surahCountPassed));

        var ayahCount = assembled.Ayahs.Count;
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.AyahCount,
            ImportValidationExpectedCounts.Ayahs.ToString(),
            ayahCount.ToString(),
            ayahCount == ImportValidationExpectedCounts.Ayahs));

        var pageCount = assembled.Pages.Count;
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.PageCount,
            ImportValidationExpectedCounts.Pages.ToString(),
            pageCount.ToString(),
            pageCount == ImportValidationExpectedCounts.Pages));

        var lineCount = assembled.Lines.Count;
        var linesPerPagePassed = ValidateLinesPerPage(assembled.Lines);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.LineCount,
            ImportValidationExpectedCounts.Lines.ToString(),
            linesPerPagePassed ? lineCount.ToString() : $"{lineCount} (lines-per-page mismatch)",
            lineCount == ImportValidationExpectedCounts.Lines && linesPerPagePassed));

        var wordCount = assembled.Words.Count;
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.WordCount,
            ImportValidationExpectedCounts.Words.ToString(),
            wordCount.ToString(),
            wordCount == ImportValidationExpectedCounts.Words));

        var markerCount = assembled.Words.Count(word => word.IsAyahMarker);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.MarkerCount,
            ImportValidationExpectedCounts.Markers.ToString(),
            markerCount.ToString(),
            markerCount == ImportValidationExpectedCounts.Markers));

        var readableCount = assembled.Words.Count(word => !word.IsAyahMarker);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.ReadableCount,
            ImportValidationExpectedCounts.ReadableWords.ToString(),
            readableCount.ToString(),
            readableCount == ImportValidationExpectedCounts.ReadableWords));

        var duplicateLocations = assembled.Words
            .GroupBy(word => word.Location, StringComparer.Ordinal)
            .Count(group => group.Count() > 1);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.DuplicateLocation,
            "0",
            duplicateLocations.ToString(),
            duplicateLocations == 0));

        checks.Add(idContiguityCheck.Evaluate(assembled.Words));
        checks.Add(sourceAlignmentCheck.Evaluate(source));
        checks.Add(layoutCoverageCheck.Evaluate(assembled.Lines));

        var wordsMissingPlacement = assembled.Words.Count(word => word.PageNumber <= 0 || word.LineNumber <= 0);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.WordPageLine,
            "all",
            wordsMissingPlacement == 0 ? "all" : $"{wordsMissingPlacement} missing",
            wordsMissingPlacement == 0));

        var wordsById = ImportValidationWordIndex.ById(assembled.Words);
        var validAyahLines = CountValidAyahLines(assembled.Lines, wordsById);
        var ayahLineCount = assembled.Lines.Count(line => line.LineType == MushafLineType.Ayah);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.LineWordRefs,
            $"{ImportValidationExpectedCounts.AyahLines}/{ImportValidationExpectedCounts.AyahLines}",
            $"{validAyahLines}/{ayahLineCount}",
            validAyahLines == ImportValidationExpectedCounts.AyahLines &&
            ayahLineCount == ImportValidationExpectedCounts.AyahLines));

        var bismillahSurahs = assembled.Surahs.Count(surah => surah.BismillahPre);
        var basmallahLines = assembled.Lines.Count(line => line.LineType == MushafLineType.Basmallah);
        checks.Add(ImportCheckResults.Hard(
            ImportValidationCheckIds.BismillahBasmallah,
            $"{ImportValidationExpectedCounts.BismillahSurahs}=={ImportValidationExpectedCounts.BismillahSurahs}",
            $"{bismillahSurahs}=={basmallahLines}",
            bismillahSurahs == ImportValidationExpectedCounts.BismillahSurahs &&
            basmallahLines == ImportValidationExpectedCounts.BismillahSurahs));

        checks.Add(denormPlacementCheck.Evaluate(assembled.Lines, wordsById));
        checks.Add(pageReconstructionCheck.Evaluate(assembled.Lines, wordsById));
        checks.Add(BuildAyah37130Check(assembled.Ayahs));

        infoNotes.Add(
            "Ayah-level readable text and word-level with-tashkeel text use different encodings; equality is not checked.");
        checks.Add(ImportCheckResults.Info(
            ImportValidationCheckIds.EncodingInfo,
            "not compared",
            "not compared",
            true));

        ImportValidationWarnings.CollectFromChecks(checks, warnings);

        var totals = new ImportTotals(
            surahCount,
            ayahCount,
            pageCount,
            lineCount,
            wordCount,
            markerCount,
            readableCount);

        var hardFailed = checks.Any(check =>
            check.Severity == ImportValidationSeverities.Hard && !check.Passed);
        var hasWarnings = warnings.Count > 0;
        var verdict = hardFailed
            ? ImportValidationVerdicts.Fail
            : hasWarnings
                ? ImportValidationVerdicts.PassWithWarnings
                : ImportValidationVerdicts.Pass;

        var errors = checks
            .Where(check => check.Severity == ImportValidationSeverities.Hard && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        return new QuranImportValidationResult(
            DateTimeOffset.UtcNow,
            sourceRoot,
            manifestVersion,
            verdict,
            Persisted: false,
            forced,
            totals,
            checks,
            warnings,
            errors,
            infoNotes);
    }

    private static ImportCheckResult BuildAyah37130Check(IReadOnlyList<Ayah> ayahs)
    {
        var ayah37130 = ayahs.SingleOrDefault(ayah => ayah.VerseKey == "37:130");
        var observed = ayah37130 is null
            ? "missing"
            : $"source {ayah37130.WordsCountSource} / real {ayah37130.WordsCountReal}";
        var passed = ayah37130 is not null &&
                     ayah37130.WordsCountSource == 4 &&
                     ayah37130.WordsCountReal == 3;

        return ImportCheckResults.Warning(
            ImportValidationCheckIds.Ayah37130Count,
            "source 4 / real 3",
            observed,
            passed);
    }

    private static bool ValidateLinesPerPage(IReadOnlyList<MushafLine> lines) =>
        lines
            .GroupBy(line => line.PageNumber)
            .All(group => group.Key is 1 or 2
                ? group.Count() == 8
                : group.Count() == 15);

    private static int CountValidAyahLines(
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById)
    {
        var valid = 0;

        foreach (var line in lines.Where(line => line.LineType == MushafLineType.Ayah))
        {
            if (line.FirstWordId is null || line.LastWordId is null || line.LastWordId < line.FirstWordId)
            {
                continue;
            }

            var allExist = true;
            for (var wordId = line.FirstWordId.Value; wordId <= line.LastWordId.Value; wordId++)
            {
                if (!wordsById.ContainsKey(wordId))
                {
                    allExist = false;
                    break;
                }
            }

            if (allExist)
            {
                valid++;
            }
        }

        return valid;
    }
}
