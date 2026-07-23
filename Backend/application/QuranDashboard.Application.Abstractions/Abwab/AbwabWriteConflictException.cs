namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabWriteConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
