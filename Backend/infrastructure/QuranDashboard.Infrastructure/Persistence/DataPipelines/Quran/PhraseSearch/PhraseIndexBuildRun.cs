using System.Diagnostics;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexBuildRun
{
    internal PhraseIndexBuildRun(Guid buildId, bool force, string reportRootDirectory)
    {
        BuildId = buildId;
        Force = force;
        ReportDirectory = Path.Combine(reportRootDirectory, buildId.ToString("D"));
        StartedAtUtc = DateTimeOffset.UtcNow;
        Stopwatch = Stopwatch.StartNew();
    }

    internal Guid BuildId { get; }
    internal bool Force { get; }
    internal string ReportDirectory { get; }
    internal DateTimeOffset StartedAtUtc { get; }
    internal Stopwatch Stopwatch { get; }
    internal bool BuildPersisted { get; set; }
    internal bool BuilderLockHeld { get; set; }
    internal long SourceRevision { get; set; }
    internal string SourceFingerprint { get; set; } = string.Empty;
    internal long SourceRevisionAtActivation { get; set; }
    internal string SourceFingerprintAtActivation { get; set; } = string.Empty;
    internal Guid? PreviousBuildId { get; set; }
    internal Guid? ActiveBuildId { get; set; }
    internal PhraseIndexBuildTotals Totals { get; set; } = PhraseIndexBuildTotals.Empty;
    internal PhraseDiskPreflight DiskPreflight { get; set; } = PhraseDiskPreflight.Unavailable;
    internal List<PhraseLengthBuildMetric> Metrics { get; set; } = [];
    internal List<PhraseBuildCheck> Checks { get; set; } = [];
    internal List<string> Warnings { get; } = [];
    internal List<string> Errors { get; } = [];

    internal long PeakManagedMemoryBytes => Metrics
        .Where(metric => metric.PeakManagedMemoryBytes.HasValue)
        .Select(metric => metric.PeakManagedMemoryBytes!.Value)
        .DefaultIfEmpty(0)
        .Max();
}
