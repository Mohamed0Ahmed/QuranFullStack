namespace QuranDashboard.Application.Quran.Words.GenerateI3rab;

public sealed record GenerateI3rabCommand(bool Force, string? ReportOutDir = null);
