using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

public sealed class TafsirImportReportBuilder : ITafsirImportReportBuilder
{
    public TafsirImportReport BuildValidationFailure(
        string sourcePath,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks,
        IReadOnlyList<string> errors)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            TafsirImportConstants.FailVerdict,
            persisted: false,
            forced,
            totals,
            checks,
            errors,
            infoNotes: ["Tafsir import failed validation; no tafsir rows were persisted."]);
    }

    public TafsirImportReport BuildRefusal(
        string sourcePath,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            TafsirImportConstants.FailVerdict,
            persisted: false,
            forced,
            source is null ? TafsirImportTotals.Empty : BuildTotals(source),
            checks: [],
            errors: [refusalMessage],
            infoNotes: ["Tafsir import refused before persistence; no tafsir rows were written."]);
    }

    public TafsirImportReport BuildCandidateSuccess(
        string sourcePath,
        TafsirSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            TafsirImportConstants.PassVerdict,
            persisted: true,
            forced,
            totals,
            checks,
            errors: [],
            infoNotes: ["Tafsir import passed validation; acceptance reports written before commit."]);
    }

    private static TafsirImportReport Build(
        string sourcePath,
        TafsirSourceData? source,
        DateTimeOffset runAtUtc,
        string verdict,
        bool persisted,
        bool forced,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> infoNotes)
    {
        var sourceSummaries = source is null
            ? Array.Empty<TafsirSourceSummary>()
            : BuildSourceSummaries(source.Sources);
        var excludedSummaries = source is null
            ? Array.Empty<TafsirExcludedSourceSummary>()
            : BuildExcludedSummaries(source.ExcludedSources);

        return new TafsirImportReport(
            runAtUtc,
            verdict,
            persisted,
            forced,
            sourcePath,
            totals,
            sourceSummaries,
            excludedSummaries,
            checks,
            BuildWarnings(source),
            errors,
            BuildInfoNotes(source, totals, infoNotes));
    }

    private static IReadOnlyList<TafsirSourceSummary> BuildSourceSummaries(
        IReadOnlyList<TafsirSourceDto> sources) =>
        sources
            .OrderBy(source => source.SourceKey, StringComparer.Ordinal)
            .Select(source => new TafsirSourceSummary(
                source.SourceKey,
                source.LanguageCode,
                source.Direction,
                source.DisplayNameEn,
                source.PackageFile,
                source.Sha256,
                source.LicenseStatus,
                source.ProvenanceStatus))
            .ToList();

    private static IReadOnlyList<TafsirExcludedSourceSummary> BuildExcludedSummaries(
        IReadOnlyList<ExcludedTafsirSourceDto> excludedSources) =>
        excludedSources
            .OrderBy(source => source.SourceKey, StringComparer.Ordinal)
            .Select(source => new TafsirExcludedSourceSummary(
                source.SourceKey,
                source.Status,
                source.ReviewReason))
            .ToList();

    private static IReadOnlyList<string> BuildWarnings(TafsirSourceData? source)
    {
        if (source is null || source.Sources.Count == 0)
        {
            return [];
        }

        var warnings = new List<string>();

        if (source.Sources.All(row =>
                string.Equals(row.LicenseStatus, "unknown", StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.ProvenanceStatus, "unknown", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add(
                $"{TafsirInvariants.WarningProvenance}: license/provenance unknown for all imported sources.");
        }

        if (source.Sources.Any(row => !string.Equals(row.LanguageCode, "ar", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add(
                $"{TafsirInvariants.WarningModernWorks}: modern/translated works may require rights review before publishing.");
        }

        return warnings;
    }

    private static IReadOnlyList<string> BuildInfoNotes(
        TafsirSourceData? source,
        TafsirImportTotals totals,
        IReadOnlyList<string> baseNotes)
    {
        var notes = new List<string>(baseNotes);

        if (source is null)
        {
            return notes;
        }

        if (source.Entries.Any(entry => entry.TafsirText.Contains('<', StringComparison.Ordinal)))
        {
            notes.Add(
                $"{TafsirInvariants.InfoInlineMarkup}: source text may include inline markup and is preserved exactly.");
        }

        notes.Add(
            $"{TafsirInvariants.InfoLanguageCoverage}: {totals.LanguageCount} languages.");

        var textBlocksBySource = source.Entries
            .GroupBy(entry => entry.SourceKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}={group.Count()}")
            .ToList();

        notes.Add(
            $"{TafsirInvariants.InfoTextBlockCount}: {string.Join(", ", textBlocksBySource)}.");

        return notes;
    }

    private static TafsirImportTotals BuildTotals(TafsirSourceData source)
    {
        var arabicSources = source.Sources.Count(row => row.LanguageCode == "ar");
        var languages = source.Sources.Select(row => row.LanguageCode).Distinct().Count();
        var distinctAyahs = source.AyahEntries.Select(row => row.AyahId).Distinct().Count();

        return new TafsirImportTotals(
            source.Sources.Count,
            source.Entries.Count,
            source.AyahEntries.Count,
            source.Sources.Count,
            source.ExcludedSources.Count,
            arabicSources,
            source.Sources.Count - arabicSources,
            languages,
            distinctAyahs);
    }
}
