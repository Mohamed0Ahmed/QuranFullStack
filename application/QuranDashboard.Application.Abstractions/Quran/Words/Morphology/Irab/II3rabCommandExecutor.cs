namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabCommandRefusal(string Message, string ReportPath);

public interface II3rabCommandExecutor
{
    /// <summary>
    /// Returns a refusal when morphology is missing/stale or i‘rab is already populated without force.
    /// Writes a refusal report to <paramref name="reportOutDir"/> and performs no database writes.
    /// </summary>
    I3rabCommandRefusal? TryRefuse(bool force, string reportOutDir);
}
