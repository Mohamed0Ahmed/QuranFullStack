using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Translations;

public sealed class TranslationImportReportBuilder : ITranslationImportReportBuilder
{
    public TranslationImportReport BuildValidationFailure(
        string sourcePath,
        string profile,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors)
    {
        var allChecks = checks
            .Append(RollbackOnFailCheck(passed: true))
            .ToList();

        return Build(
            sourcePath,
            profile,
            source,
            runAtUtc,
            TranslationImportConstants.FailVerdict,
            persisted: false,
            forced,
            totals,
            allChecks,
            errors,
            BuildWarnings(source),
            infoNotes: ["Translation import failed validation; no translation rows were persisted."]);
    }

    public TranslationImportReport BuildRefusal(
        string sourcePath,
        string profile,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage)
    {
        var checks = new List<TranslationCheckResult>();
        if (string.Equals(refusalMessage, TranslationInvariants.TargetsNotEmpty, StringComparison.Ordinal))
        {
            checks.Add(RerunGuardCheck(passed: false, forced));
        }

        checks.Add(RollbackOnFailCheck(passed: true));

        return Build(
            sourcePath,
            profile,
            source,
            runAtUtc,
            TranslationImportConstants.FailVerdict,
            persisted: false,
            forced,
            source is null ? TranslationImportTotals.Empty : TranslationImportTotals.FromSource(source),
            checks,
            errors: [refusalMessage],
            BuildWarnings(source),
            infoNotes: ["Translation import refused before persistence; no translation rows were written."]);
    }

    public TranslationImportReport BuildCandidateSuccess(
        string sourcePath,
        string profile,
        TranslationSourceData source,
        bool forced,
        bool persisted,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> postCopyChecks,
        TranslationExpectedCounts expected)
    {
        var postCopyIds = postCopyChecks
            .Select(check => check.Id)
            .ToHashSet(StringComparer.Ordinal);

        var allChecks = BuildPassedLoadChecks(source, expected)
            .Where(check => !postCopyIds.Contains(check.Id))
            .Concat(postCopyChecks)
            .Append(RollbackOnFailCheck(passed: true))
            .Append(RerunGuardCheck(passed: true, forced))
            .ToList();

        return Build(
            sourcePath,
            profile,
            source,
            runAtUtc,
            TranslationImportConstants.PassVerdict,
            persisted,
            forced,
            totals,
            allChecks,
            errors: [],
            BuildWarnings(source),
            BuildSuccessInfoNotes(source, totals, persisted));
    }

    private static TranslationImportReport Build(
        string sourcePath,
        string profile,
        TranslationSourceData? source,
        DateTimeOffset runAtUtc,
        string verdict,
        bool persisted,
        bool forced,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
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
            profile,
            sourcePath,
            totals,
            sourceSummaries,
            excludedSummaries,
            checks,
            warnings,
            errors,
            infoNotes);
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
                source.ContainsHtmlMarkup,
                source.ReclassifiedFromSimpleByContent))
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

    private static IReadOnlyList<string> BuildSuccessInfoNotes(
        TranslationSourceData source,
        TranslationImportTotals totals,
        bool persisted)
    {
        var notes = new List<string>
        {
            persisted
                ? "Translation import committed; all hard checks passed."
                : "Translation import passed validation; database commit is not yet proven."
        };

        if (source.Sources.Any(row => row.ContainsInlineFootnotes || row.ContainsHtmlMarkup))
        {
            notes.Add(
                $"{TranslationInvariants.InfoInlineMarkup}: source text may include inline footnotes or embedded HTML and is preserved exactly.");
        }

        notes.Add($"{TranslationInvariants.InfoLanguageCoverage}: {totals.LanguageCount} languages.");

        var reclassified = source.Sources
            .Where(row => row.ReclassifiedFromSimpleByContent)
            .Select(row => row.SourceKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        if (reclassified.Count > 0)
        {
            notes.Add(
                $"{TranslationInvariants.InfoReclassified}: {reclassified.Count.ToString(CultureInfo.InvariantCulture)} source(s) reclassified from simple to with_footnotes by content ({string.Join(", ", reclassified)}).");
        }

        return notes;
    }

    private static IReadOnlyList<TranslationCheckResult> BuildPassedLoadChecks(
        TranslationSourceData source,
        TranslationExpectedCounts expected)
    {
        var approved = source.Sources.Count;
        var simple = source.Sources.Count(row => row.TranslationType == "simple");
        var withFootnotes = source.Sources.Count(row => row.TranslationType == "with_footnotes");
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
            Hard(TranslationInvariants.CheckTypeCounts,
                $"simple={expected.SimpleSources}, with_footnotes={expected.WithFootnotesSources}",
                $"simple={simple}, with_footnotes={withFootnotes}",
                simple == expected.SimpleSources && withFootnotes == expected.WithFootnotesSources),
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

    private static TranslationCheckResult RollbackOnFailCheck(bool passed) =>
        new(
            TranslationInvariants.CheckRollbackOnFail,
            TranslationImportConstants.HardSeverity,
            "failed hard checks leave no accepted partial import",
            passed ? "no partial import persisted" : "partial import persisted",
            passed);

    private static TranslationCheckResult RerunGuardCheck(bool passed, bool forced) =>
        new(
            TranslationInvariants.CheckRerunGuard,
            TranslationImportConstants.HardSeverity,
            "normal re-run refuses non-empty targets; forced replacement revalidates before replacing",
            passed
                ? forced
                    ? "forced replacement with pre-validation"
                    : "no existing translation data"
                : "translation tables not empty",
            passed);
}
