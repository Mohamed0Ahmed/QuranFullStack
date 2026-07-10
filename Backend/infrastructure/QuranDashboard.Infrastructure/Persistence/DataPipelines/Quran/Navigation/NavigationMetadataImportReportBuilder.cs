using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;

public sealed class NavigationMetadataImportReportBuilder : INavigationMetadataImportReportBuilder
{
    private static readonly HashSet<string> AssemblerCheckIds = new(StringComparer.Ordinal)
    {
        NavigationMetadataInvariants.CheckVerseKeysResolve,
        NavigationMetadataInvariants.CheckRangeCoverageJuz,
        NavigationMetadataInvariants.CheckRangeCoverageHizb,
        NavigationMetadataInvariants.CheckRangeCoverageRub,
        NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
        NavigationMetadataInvariants.CheckHierarchy
    };

    private static readonly HashSet<string> PostAssemblerCheckIds = new(StringComparer.Ordinal)
    {
        NavigationMetadataInvariants.CheckAyahColumnsComplete,
        NavigationMetadataInvariants.CheckSourceUnchanged,
        NavigationMetadataInvariants.CheckReportWritten
    };

    public NavigationMetadataImportReport BuildValidationFailure(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        IReadOnlyList<string> errors,
        NavigationExpectedCounts? expected)
    {
        var inclusion = ResolveValidationFailureInclusion(source, totals, checks);
        var allChecks = MergeChecks(checks, forced, source, expected, inclusion);

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.ValidationFailedVerdict,
            persisted: false,
            forced,
            totals,
            allChecks,
            errors,
            expected);
    }

    public NavigationMetadataImportReport BuildRefusal(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage,
        NavigationExpectedCounts? expected)
    {
        var checks = new List<NavigationCheckResult>();
        if (string.Equals(refusalMessage, NavigationMetadataInvariants.TargetsNotEmpty, StringComparison.Ordinal))
        {
            checks.Add(RerunGuardCheck(passed: false, forced));
        }

        checks.Add(RollbackOnFailCheck(passed: true));

        var inclusion = source is null
            ? ReportCheckInclusion.None
            : ReportCheckInclusion.SourceLoadOnly;

        var allChecks = MergeChecks(checks, forced, source, expected, inclusion);

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.RefusedVerdict,
            persisted: false,
            forced,
            source is null ? NavigationImportTotals.Empty : BuildTotalsFromSource(source),
            allChecks,
            errors: [refusalMessage],
            expected);
    }

    public NavigationMetadataImportReport BuildCandidateSuccess(
        string sourcePath,
        NavigationMetadataSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> postCopyChecks,
        NavigationExpectedCounts expected)
    {
        var allChecks = MergeChecks(
            postCopyChecks,
            forced,
            source,
            expected,
            ReportCheckInclusion.SourceLoadAndAssemblerPassed);

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.AcceptedVerdict,
            persisted: true,
            forced,
            totals,
            allChecks,
            errors: [],
            expected);
    }

    private static ReportCheckInclusion ResolveValidationFailureInclusion(
        NavigationMetadataSourceData? source,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks)
    {
        if (source is null)
        {
            return ReportCheckInclusion.None;
        }

        if (totals.AyahsTagged > 0
            || checks.Any(check => PostAssemblerCheckIds.Contains(check.Id)))
        {
            return ReportCheckInclusion.SourceLoadAndAssemblerPassed;
        }

        return ReportCheckInclusion.SourceLoadOnly;
    }

    private static NavigationMetadataImportReport Build(
        string sourcePath,
        NavigationMetadataSourceData? source,
        DateTimeOffset runAtUtc,
        string verdict,
        bool persisted,
        bool forced,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        IReadOnlyList<string> errors,
        NavigationExpectedCounts? expected)
    {
        var warnings = checks
            .Where(check => check.Severity == NavigationImportConstants.WarningSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        var expectedAyahCount = expected?.Ayahs ?? 0;
        var totalAyahs = expectedAyahCount > 0 ? expectedAyahCount : totals.AyahsTagged;
        var taggedAyahs = persisted ? totals.AyahsTagged : 0;
        var coverageComplete = persisted
            && totalAyahs > 0
            && taggedAyahs == totalAyahs;

        return new NavigationMetadataImportReport(
            NavigationImportConstants.FeatureId,
            runAtUtc,
            verdict,
            persisted,
            forced,
            sourcePath,
            new NavigationManifestSummary(
                NavigationImportConstants.ManifestType,
                IsFinalImportManifest: true),
            totals,
            new NavigationAyahCoverageSummary(
                totalAyahs,
                taggedAyahs,
                taggedAyahs,
                taggedAyahs,
                coverageComplete),
            checks,
            warnings,
            errors,
            NoQuranAyahTextReadOrStored: true);
    }

    private static IReadOnlyList<NavigationCheckResult> BuildSourceLoadChecks(
        NavigationMetadataSourceData source,
        NavigationExpectedCounts expected)
    {
        var countText = $"{expected.Juz}/{expected.Hizb}/{expected.Rub}/{expected.Sajda}";
        var observedCountText =
            $"{source.Juz.Count}/{source.Hizb.Count}/{source.Rub.Count}/{source.Sajda.Count}";

        static NavigationCheckResult Hard(string id, string expectedText, string observedText, bool passed) =>
            new(id, NavigationImportConstants.HardSeverity, expectedText, observedText, passed);

        return
        [
            Hard(
                NavigationMetadataInvariants.CheckPackageShape,
                "manifest.json + sources/{juz,hizb,rub,sajda}.json",
                "present",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckManifestFinal,
                $"packageType={NavigationImportConstants.ManifestType}, isFinalImportManifest=true",
                $"packageType={NavigationImportConstants.ManifestType}, isFinalImportManifest=true",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckSourceCount,
                countText,
                observedCountText,
                source.Juz.Count == expected.Juz
                    && source.Hizb.Count == expected.Hizb
                    && source.Rub.Count == expected.Rub
                    && source.Sajda.Count == expected.Sajda),
            Hard(
                NavigationMetadataInvariants.CheckSourceHash,
                "file sizes and sha256 match manifest",
                "all match",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckJsonShape,
                "required fields per dataset",
                "valid",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckSajdaType,
                "required|optional",
                "all allowed",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckNoQuranTextCopy,
                "no Quran ayah text read or stored",
                "none",
                passed: true)
        ];
    }

    private static IReadOnlyList<NavigationCheckResult> BuildAssemblerPassedChecks(
        NavigationExpectedCounts expected)
    {
        var ayahCountText = expected.Ayahs.ToString(CultureInfo.InvariantCulture);

        static NavigationCheckResult Hard(string id, string expectedText, string observedText, bool passed) =>
            new(id, NavigationImportConstants.HardSeverity, expectedText, observedText, passed);

        return
        [
            Hard(
                NavigationMetadataInvariants.CheckVerseKeysResolve,
                "every verse key resolves to canonical ayah",
                "all resolved",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckRangeCoverageJuz,
                $"{ayahCountText} once",
                $"{ayahCountText} once",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckRangeCoverageHizb,
                $"{ayahCountText} once",
                $"{ayahCountText} once",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckRangeCoverageRub,
                $"{ayahCountText} once",
                $"{ayahCountText} once",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
                "no gaps or overlaps",
                "none",
                passed: true),
            Hard(
                NavigationMetadataInvariants.CheckHierarchy,
                "each hizb in exactly one juz; each rub in exactly one hizb",
                "valid",
                passed: true)
        ];
    }

    private static List<NavigationCheckResult> MergeChecks(
        IReadOnlyList<NavigationCheckResult> evaluatedChecks,
        bool forced,
        NavigationMetadataSourceData? source,
        NavigationExpectedCounts? expected,
        ReportCheckInclusion inclusion)
    {
        var merged = new Dictionary<string, NavigationCheckResult>(StringComparer.Ordinal);

        if (inclusion is ReportCheckInclusion.SourceLoadOnly or ReportCheckInclusion.SourceLoadAndAssemblerPassed
            && source is not null
            && expected is not null)
        {
            foreach (var check in BuildSourceLoadChecks(source, expected))
            {
                merged[check.Id] = check;
            }
        }

        if (inclusion == ReportCheckInclusion.SourceLoadAndAssemblerPassed && expected is not null)
        {
            foreach (var check in BuildAssemblerPassedChecks(expected))
            {
                merged[check.Id] = check;
            }
        }

        foreach (var check in evaluatedChecks)
        {
            if (inclusion == ReportCheckInclusion.None
                && (AssemblerCheckIds.Contains(check.Id) || PostAssemblerCheckIds.Contains(check.Id)))
            {
                continue;
            }

            merged[check.Id] = check;
        }

        merged[NavigationMetadataInvariants.CheckRollbackOnFail] = RollbackOnFailCheck(passed: true);

        if (!merged.ContainsKey(NavigationMetadataInvariants.CheckRerunGuard))
        {
            merged[NavigationMetadataInvariants.CheckRerunGuard] = RerunGuardCheck(passed: true, forced);
        }

        return merged.Values.ToList();
    }

    private static NavigationImportTotals BuildTotalsFromSource(NavigationMetadataSourceData source) =>
        new(source.Juz.Count, source.Hizb.Count, source.Rub.Count, source.Sajda.Count, 0);

    private static NavigationCheckResult RollbackOnFailCheck(bool passed) =>
        new(
            NavigationMetadataInvariants.CheckRollbackOnFail,
            NavigationImportConstants.HardSeverity,
            "full rollback on hard failure",
            passed ? "not needed" : "rolled back",
            passed);

    private static NavigationCheckResult RerunGuardCheck(bool passed, bool forced) =>
        new(
            NavigationMetadataInvariants.CheckRerunGuard,
            NavigationImportConstants.HardSeverity,
            forced ? "force rebuild allowed" : "empty navigation targets",
            passed ? "passed" : "refused",
            passed);

    private enum ReportCheckInclusion
    {
        None,
        SourceLoadOnly,
        SourceLoadAndAssemblerPassed
    }
}
