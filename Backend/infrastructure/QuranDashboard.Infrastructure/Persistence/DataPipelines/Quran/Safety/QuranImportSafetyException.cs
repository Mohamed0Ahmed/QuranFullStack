namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;

public sealed class QuranImportSafetyException : Exception
{
    public QuranImportSafetyException(string message)
        : base(message)
    {
    }
}
