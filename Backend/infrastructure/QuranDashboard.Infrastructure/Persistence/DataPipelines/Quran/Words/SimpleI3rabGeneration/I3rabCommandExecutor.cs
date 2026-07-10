using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

public sealed class I3rabCommandExecutor(
    II3rabGenerationSource generationSource,
    II3rabGenerationReportWriter reportWriter) : II3rabCommandExecutor
{
    public const string AlreadyPopulatedRefusalReason =
        "I‘rab labels are already populated. Re-run with --force to overwrite them.";

    public I3rabCommandRefusal? TryRefuse(bool force, string reportOutDir)
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
        string reportOutDir)
    {
        var reportPath = reportWriter.WriteRefusal(
            new I3rabRefusalReport(reason, readiness, force),
            reportOutDir);

        return new I3rabCommandRefusal(reason, reportPath);
    }
}
