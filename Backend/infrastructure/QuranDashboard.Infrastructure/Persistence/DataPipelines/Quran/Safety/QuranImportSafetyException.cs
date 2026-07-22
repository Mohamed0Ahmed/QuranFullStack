namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;

// Raised when a destructive Quran import step is refused fail-closed: either an out-of-scope FK
// dependent would be reached by a TRUNCATE ... CASCADE, or the destructive statement could not be
// parsed into a target set to preflight. Feature 028 US2 — the exception is the "fail closed" signal
// itself.
public sealed class QuranImportSafetyException : Exception
{
    public QuranImportSafetyException(string message)
        : base(message)
    {
    }
}
