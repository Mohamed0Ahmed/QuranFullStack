using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.FullI3rab;

public sealed class FullI3rabImportReportBuilder : IFullI3rabImportReportBuilder
{
    public FullI3rabImportReport BuildValidationFailure(
        string sourcePath,
        FullI3rabSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> checks,
        IReadOnlyList<string> errors)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            FullI3rabImportConstants.FailVerdict,
            persisted: false,
            forced,
            totals,
            checks,
            errors,
            infoNotes: ["Full i'rab import failed validation; no full-i'rab rows were persisted."]);
    }

    public FullI3rabImportReport BuildRefusal(
        string sourcePath,
        FullI3rabSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage)
    {
        return Build(
            sourcePath,
            source,
            runAtUtc,
            FullI3rabImportConstants.FailVerdict,
            persisted: false,
            forced,
            source is null ? FullI3rabImportTotals.Empty : FullI3rabImportTotals.FromSource(source),
            checks: [],
            errors: [refusalMessage],
            infoNotes: ["Full i'rab import refused before persistence; no full-i'rab rows were written."]);
    }

    public FullI3rabImportReport BuildCandidateSuccess(
        string sourcePath,
        FullI3rabSourceData source,
        bool forced,
        bool persisted,
        DateTimeOffset runAtUtc,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> postCopyChecks,
        FullI3rabExpectedCounts expected)
    {
        var allChecks = BuildPassedLoadChecks(source, expected)
            .Concat(postCopyChecks)
            .ToList();

        return Build(
            sourcePath,
            source,
            runAtUtc,
            FullI3rabImportConstants.PassVerdict,
            persisted,
            forced,
            totals,
            allChecks,
            errors: [],
            infoNotes: persisted
                ? ["Full i'rab import committed; all hard checks passed."]
                : ["Full i'rab import passed validation; database commit is not yet proven."]);
    }

    private static FullI3rabImportReport Build(
        string sourcePath,
        FullI3rabSourceData? source,
        DateTimeOffset runAtUtc,
        string verdict,
        bool persisted,
        bool forced,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> checks,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> infoNotes)
    {
        var sourceSummaries = source is null
            ? Array.Empty<FullI3rabSourceSummary>()
            : BuildSourceSummaries(source.Sources);

        return new FullI3rabImportReport(
            runAtUtc,
            verdict,
            persisted,
            forced,
            sourcePath,
            totals,
            sourceSummaries,
            checks,
            BuildWarnings(source),
            errors,
            BuildInfoNotes(source, totals, infoNotes));
    }

    private static IReadOnlyList<FullI3rabSourceSummary> BuildSourceSummaries(
        IReadOnlyList<FullI3rabSourceDto> sources) =>
        sources
            .OrderBy(row => row.SourceKey, StringComparer.Ordinal)
            .Select(row => new FullI3rabSourceSummary(
                row.SourceKey,
                row.DisplayNameEn,
                row.PackageFile,
                row.Sha256,
                row.LicenseStatus,
                row.ProvenanceStatus,
                row.UsageScope,
                row.MarkupFormat,
                row.HasQuranQuotationMarkup))
            .ToList();

    private static IReadOnlyList<string> BuildWarnings(FullI3rabSourceData? source)
    {
        var warnings = new List<string>
        {
            $"{FullI3rabInvariants.WarningProvenance}: {FullI3rabImportConstants.ProvenanceWarning}"
        };

        if (source is not null)
        {
            warnings.AddRange(source.Warnings);
        }

        return warnings;
    }

    private static IReadOnlyList<string> BuildInfoNotes(
        FullI3rabSourceData? source,
        FullI3rabImportTotals totals,
        IReadOnlyList<string> baseNotes)
    {
        var notes = new List<string>(baseNotes);

        if (source is null)
        {
            return notes;
        }

        var blocksBySource = source.Entries
            .GroupBy(entry => entry.SourceKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}={group.Count()}")
            .ToList();

        notes.Add($"FULLI3RAB-BLOCK-COUNT: {string.Join(", ", blocksBySource)}.");
        notes.Add(
            $"FULLI3RAB-MAPPING-COUNT: sourceRows={totals.SourceRows}, " +
            $"entryRows={totals.EntryRows}, ayahMappingRows={totals.AyahMappingRows}.");

        return notes;
    }

    private static IReadOnlyList<FullI3rabCheckResult> BuildPassedLoadChecks(
        FullI3rabSourceData source,
        FullI3rabExpectedCounts expected)
    {
        var ayahsPerSource = expected.AyahsPerSource.ToString(CultureInfo.InvariantCulture);
        var sourcesText = expected.Sources.ToString(CultureInfo.InvariantCulture);

        static FullI3rabCheckResult Hard(string id, string expectedText, string observedText) =>
            new(id, FullI3rabImportConstants.HardSeverity, expectedText, observedText, Passed: true);

        return
        [
            Hard(FullI3rabInvariants.CheckPackageShape,
                "README.md, manifest.json, package-report.md, sources/", "present"),
            Hard(FullI3rabInvariants.CheckManifestFinal,
                $"manifestType={FullI3rabImportConstants.ManifestType}, isFinalImportManifest=true",
                $"manifestType={FullI3rabImportConstants.ManifestType}, isFinalImportManifest=true"),
            Hard(FullI3rabInvariants.CheckSourceCount, sourcesText, source.Sources.Count.ToString(CultureInfo.InvariantCulture)),
            Hard(FullI3rabInvariants.CheckSourceSet,
                "sources/ exactly matches manifest approved package files", "exact match"),
            Hard(FullI3rabInvariants.CheckSourceHash,
                "file sizes and sha256 match manifest", "all match"),
            Hard(FullI3rabInvariants.CheckCoverageCount,
                ayahsPerSource, $"all {sourcesText} sources = {ayahsPerSource}"),
            Hard(FullI3rabInvariants.CheckJsonShape,
                $"each source root is an object with {ayahsPerSource} ayah keys", "valid"),
            Hard(FullI3rabInvariants.CheckAyahKeysResolve,
                "every ayah key resolves to a canonical ayah", "all resolved"),
            Hard(FullI3rabInvariants.CheckPointersResolve,
                "every pointer resolves to a same-source text block", "all resolved"),
            Hard(FullI3rabInvariants.CheckAyahKeysMemberMatch,
                "every ayah_keys member maps to the declared leader", "all matched"),
            Hard(FullI3rabInvariants.CheckNoEmptyText,
                "no approved source/ayah resolves to empty i3rab html", "none"),
            Hard(FullI3rabInvariants.CheckBlockPartition,
                "blocks partition all ayahs exactly once with no gaps or overlaps", "valid partition")
        ];
    }
}
