using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Application.Quran.DataPipelines.FullI3rab;

public sealed record ImportFullI3rabCommand(
    string SourcePath,
    bool Force,
    FullI3rabExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
