using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

internal sealed record PhraseIndexBuildReport(
    Guid BuildId,
    string FormatVersion,
    string BuilderVersion,
    string Status,
    string Outcome,
    bool Forced,
    bool Persisted,
    bool Active,
    bool ExactReady,
    bool SimilarityReady,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    long DurationMilliseconds,
    long PeakManagedMemoryBytes,
    long SourceRevisionBefore,
    string SourceFingerprintBefore,
    long SourceRevisionAtActivation,
    string SourceFingerprintAtActivation,
    Guid? PreviousBuildId,
    Guid? ActiveBuildId,
    PhraseIndexBuildTotals Totals,
    PhraseDiskPreflight DiskPreflight,
    IReadOnlyList<PhraseLengthBuildMetric> Metrics,
    IReadOnlyList<PhraseBuildCheck> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
