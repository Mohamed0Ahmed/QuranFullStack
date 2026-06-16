using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

public sealed class NavigationMetadataImportReportBuilder : INavigationMetadataImportReportBuilder
{
    public NavigationMetadataImportReport BuildValidationFailure(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        IReadOnlyList<string> errors)
    {
        var allChecks = checks
            .Append(RollbackOnFailCheck(passed: true))
            .ToList();

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.ValidationFailedVerdict,
            persisted: false,
            forced,
            totals,
            allChecks,
            errors);
    }

    public NavigationMetadataImportReport BuildRefusal(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage)
    {
        var checks = new List<NavigationCheckResult>();
        if (string.Equals(refusalMessage, NavigationMetadataInvariants.TargetsNotEmpty, StringComparison.Ordinal))
        {
            checks.Add(RerunGuardCheck(passed: false, forced));
        }

        checks.Add(RollbackOnFailCheck(passed: true));

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.RefusedVerdict,
            persisted: false,
            forced,
            source is null ? NavigationImportTotals.Empty : BuildTotalsFromSource(source),
            checks,
            errors: [refusalMessage]);
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
        var allChecks = postCopyChecks
            .Append(RollbackOnFailCheck(passed: true))
            .Append(RerunGuardCheck(passed: true, forced))
            .ToList();

        return Build(
            sourcePath,
            source,
            runAtUtc,
            NavigationImportConstants.AcceptedVerdict,
            persisted: true,
            forced,
            totals,
            allChecks,
            errors: []);
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
        IReadOnlyList<string> errors)
    {
        var warnings = checks
            .Where(check => check.Severity == NavigationImportConstants.WarningSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

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
                totals.AyahsTagged,
                totals.AyahsTagged,
                totals.AyahsTagged,
                totals.AyahsTagged,
                totals.AyahsTagged > 0),
            checks,
            warnings,
            errors,
            NoQuranAyahTextReadOrStored: true);
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
}
