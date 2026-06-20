namespace QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record GenerateI3rabCommand(bool Force, string? ReportOutDir = null);
