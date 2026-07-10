namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record I3rabCommandRefusal(string Message, string ReportPath);

public interface II3rabCommandExecutor
{

    I3rabCommandRefusal? TryRefuse(bool force, string reportOutDir);
}
