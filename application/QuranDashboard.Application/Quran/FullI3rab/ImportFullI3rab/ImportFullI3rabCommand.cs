using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;

public sealed record ImportFullI3rabCommand(
    string SourcePath,
    bool Force,
    FullI3rabExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
