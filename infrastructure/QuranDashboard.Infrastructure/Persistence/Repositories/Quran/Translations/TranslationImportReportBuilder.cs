using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Translations;

public sealed class TranslationImportReportBuilder : ITranslationImportReportBuilder
{
    public TranslationImportReport BuildValidationFailure(
        string sourcePath,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            TranslationImportConstants.FailVerdict,
            persisted: false,
            forced,
            totals,
            checks,
            errors,
            infoNotes: ["Translation import failed validation; no translation rows were persisted."]);
    }

    public TranslationImportReport BuildRefusal(
        string sourcePath,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            TranslationImportConstants.FailVerdict,
            persisted: false,
            forced,
            source is null ? TranslationImportTotals.Empty : TranslationImportTotals.FromSource(source),
            checks: [],
            errors: [refusalMessage],
            infoNotes: ["Translation import refused before persistence; no translation rows were written."]);
    }

    public TranslationImportReport BuildCandidateSuccess(
        string sourcePath,
        TranslationSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> postCopyChecks,
        TranslationExpectedCounts expected)
    {
        var allChecks = BuildPassedLoadChecks(source, expected)
            .Concat(postCopyChecks)
            .ToList();

        return Build(
            sourcePath,
            source,
            runAtUtc,
            TranslationImportConstants.PassVerdict,
            persisted: true,
            forced,
            totals,
            allChecks,
            errors: [],
            infoNotes: ["Translation import passed validation; acceptance reports written before commit."]);
    }

    private static TranslationImportReport Build(
        string sourcePath,
        TranslationSourceData? source,
        DateTimeOffset runAtUtc,
        string verdict,
        bool persisted,
        bool forced,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> infoNotes)
    {
        var sourceSummaries = source is null
            ? Array.Empty<TranslationSourceSummary>()
            : BuildSourceSummaries(source.Sources);
        var excludedSummaries = source is null
            ? Array.Empty<TranslationExcludedSourceSummary>()
            : BuildExcludedSummaries(source.ExcludedSources);

        return new TranslationImportReport(
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

    private static IReadOnlyList<TranslationSourceSummary> BuildSourceSummaries(
        IReadOnlyList<TranslationSourceDto> sources) =>
        sources
            .OrderBy(source => source.SourceKey, StringComparer.Ordinal)
            .Select(source => new TranslationSourceSummary(
                source.SourceKey,
                source.LanguageCode,
                source.Direction,
                source.TranslationType,
                source.DisplayNameEn,
                source.DisplayNameAr,
                source.PackageFile,
                source.Sha256,
                source.FileSizeBytes,
                source.ContainsInlineFootnotes,
                source.ContainsHtmlMarkup))
            .ToList();

    private static IReadOnlyList<TranslationExcludedSourceSummary> BuildExcludedSummaries(
        IReadOnlyList<ExcludedTranslationSourceDto> excludedSources) =>
        excludedSources
            .OrderBy(source => source.SourceKey, StringComparer.Ordinal)
            .Select(source => new TranslationExcludedSourceSummary(
                source.SourceKey,
                source.Status,
                source.Reason))
            .ToList();

    private static IReadOnlyList<string> BuildWarnings(TranslationSourceData? source)
    {
        if (source is null || source.Sources.Count == 0)
        {
            return [];
        }

        return
        [
            $"{TranslationInvariants.WarningProvenance}: license/provenance unknown for all imported sources; internal use only."
        ];
    }

    private static IReadOnlyList<string> BuildInfoNotes(
        TranslationSourceData? source,
        TranslationImportTotals totals,
        IReadOnlyList<string> baseNotes)
    {
        var notes = new List<string>(baseNotes);

        if (source is null)
        {
            return notes;
        }

        if (source.Sources.Any(row => row.ContainsInlineFootnotes || row.ContainsHtmlMarkup))
        {
            notes.Add(
                $"{TranslationInvariants.InfoInlineMarkup}: source text may include inline footnotes or embedded HTML and is preserved exactly.");
        }

        notes.Add($"{TranslationInvariants.InfoLanguageCoverage}: {totals.LanguageCount} languages.");

        return notes;
    }

    private static IReadOnlyList<TranslationCheckResult> BuildPassedLoadChecks(
        TranslationSourceData source,
        TranslationExpectedCounts expected)
    {
        var approved = source.Sources.Count;
        var excluded = source.ExcludedSources.Count;
        var ayahsPerSource = expected.AyahsPerSource.ToString(CultureInfo.InvariantCulture);

        static TranslationCheckResult Hard(
            string id,
            string expectedText,
            string observedText,
            bool passed) =>
            new(id, TranslationImportConstants.HardSeverity, expectedText, observedText, passed);

        return
        [
            Hard(TranslationInvariants.CheckPackageShape,
                "README.md, manifest.json, package-report.md, source-display-metadata.json, sources/",
                "present",
                passed: true),
            Hard(TranslationInvariants.CheckManifestFinal,
                $"manifestType={TranslationImportConstants.ManifestType}, isFinalImportManifest=true",
                $"manifestType={TranslationImportConstants.ManifestType}, isFinalImportManifest=true",
                passed: true),
            Hard(TranslationInvariants.CheckDisplayMetadataFinal,
                $"metadataType={TranslationImportConstants.DisplayMetadataType}, status={TranslationImportConstants.DisplayMetadataFinalStatus}, sourceCount={expected.ApprovedSources}",
                $"metadataType={TranslationImportConstants.DisplayMetadataType}, status={TranslationImportConstants.DisplayMetadataFinalStatus}, sourceCount={approved}",
                approved == expected.ApprovedSources),
            Hard(TranslationInvariants.CheckDisplayMetadataSet,
                "display metadata sourceKey set exactly matches manifest approved source set",
                "exact match",
                passed: true),
            Hard(TranslationInvariants.CheckDisplayMetadataRequiredFields,
                "required display metadata fields present and non-empty",
                "all present and non-empty",
                passed: true),
            Hard(TranslationInvariants.CheckSourceCount,
                expected.ApprovedSources.ToString(CultureInfo.InvariantCulture),
                approved.ToString(CultureInfo.InvariantCulture),
                approved == expected.ApprovedSources),
            Hard(TranslationInvariants.CheckExcludedCount,
                expected.ExcludedSources.ToString(CultureInfo.InvariantCulture),
                excluded.ToString(CultureInfo.InvariantCulture),
                excluded == expected.ExcludedSources),
            Hard(TranslationInvariants.CheckSourceSet,
                "sources/ exactly matches manifest approved package files", "exact match", passed: true),
            Hard(TranslationInvariants.CheckSourceHash,
                "file sizes and sha256 match manifest", "all match", passed: true),
            Hard(TranslationInvariants.CheckNoExcludedSources,
                "no excluded sources in approved set", "none", passed: true),
            Hard(TranslationInvariants.CheckJsonShape,
                $"object root with {ayahsPerSource} verse keys", "valid", passed: true),
            Hard(TranslationInvariants.CheckCoverageCount,
                ayahsPerSource, $"all {approved.ToString(CultureInfo.InvariantCulture)} sources = {ayahsPerSource}", passed: true),
            Hard(TranslationInvariants.CheckNoEmptyText,
                "no approved source has empty, null, missing, or non-string t", "none", passed: true),
            Hard(TranslationInvariants.CheckAyahKeysResolve,
                "every verse key resolves to canonical ayah", "all resolved", passed: true),
            Hard(TranslationInvariants.CheckNoDuplicateAyahEntry,
                "no duplicate source/ayah mapping", "none", passed: true)
        ];
    }
}
