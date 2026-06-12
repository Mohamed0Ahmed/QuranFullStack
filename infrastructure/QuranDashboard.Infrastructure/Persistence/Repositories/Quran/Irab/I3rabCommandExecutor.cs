using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

public sealed class I3rabCommandExecutor(
    II3rabGenerationSource generationSource,
    II3rabGenerationReportWriter reportWriter) : II3rabCommandExecutor
{
    public const string AlreadyPopulatedRefusalReason =
        "I‘rab labels are already populated. Re-run with --force to overwrite them.";

    public I3rabCommandRefusal? TryRefuse(bool force, string? reportOutDir)
    {
        var readiness = generationSource.AssessMorphologyReadiness();
        if (!readiness.IsReady)
        {
            return Refuse(readiness.RefusalReason!, readiness, force, reportOutDir);
        }

        if (!force && generationSource.I3rabAlreadyPopulated())
        {
            return Refuse(AlreadyPopulatedRefusalReason, readiness, force, reportOutDir);
        }

        return null;
    }

    private I3rabCommandRefusal Refuse(
        string reason,
        I3rabMorphologyReadiness readiness,
        bool force,
        string? reportOutDir)
    {
        var reportDir = ResolveReportOutDir(reportOutDir);
        Directory.CreateDirectory(reportDir);
        var reportPath = reportWriter.WriteRefusal(
            new I3rabRefusalReport(reason, readiness, force),
            reportDir);

        return new I3rabCommandRefusal(reason, reportPath);
    }

    private static string ResolveReportOutDir(string? reportOutDir)
    {
        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            return reportOutDir;
        }

        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(),
            "resources",
            "report",
            "words-simple-i3rab"));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var resourcesPath = Path.Combine(directory.FullName, "resources");
            var backendPath = Path.Combine(directory.FullName, "Backend");

            if (Directory.Exists(resourcesPath) && Directory.Exists(backendPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
