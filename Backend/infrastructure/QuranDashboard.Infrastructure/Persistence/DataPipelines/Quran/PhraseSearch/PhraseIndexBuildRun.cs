using System.Diagnostics;
using System.Net.Sockets;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexBuildRun
{
    internal PhraseIndexBuildRun(Guid buildId, string reportRootDirectory)
    {
        BuildId = buildId;
        ReportDirectory = Path.Combine(reportRootDirectory, buildId.ToString("D"));
        StartedAtUtc = DateTimeOffset.UtcNow;
        Stopwatch = Stopwatch.StartNew();
    }

    internal Guid BuildId { get; }
    internal string ReportDirectory { get; }
    internal DateTimeOffset StartedAtUtc { get; }
    internal Stopwatch Stopwatch { get; }
    internal bool BuildPersisted { get; set; }
    internal bool BuilderLockHeld { get; set; }
    internal long SourceRevision { get; set; }
    internal string SourceFingerprint { get; set; } = string.Empty;
    internal long SourceRevisionAtActivation { get; set; }
    internal string SourceFingerprintAtActivation { get; set; } = string.Empty;
    internal Guid? ActiveBuildId { get; set; }
    internal PhraseIndexBuildTotals Totals { get; set; } = PhraseIndexBuildTotals.Empty;
    internal PhraseDiskPreflight DiskPreflight { get; set; } = PhraseDiskPreflight.Unavailable;
    internal List<PhraseLengthBuildMetric> Metrics { get; set; } = [];
    internal List<PhraseBuildCheck> Checks { get; set; } = [];
    internal List<string> Warnings { get; } = [];
    internal List<string> Errors { get; } = [];
    internal bool ActivationFinalizationFailed { get; private set; }
    internal PhraseIndexBuildStage CurrentStage { get; set; } = PhraseIndexBuildStage.OpenConnection;

    internal long PeakManagedMemoryBytes => Metrics
        .Where(metric => metric.PeakManagedMemoryBytes.HasValue)
        .Select(metric => metric.PeakManagedMemoryBytes!.Value)
        .DefaultIfEmpty(0)
        .Max();

    internal void RecordActivationFinalizationFailure(string code, string warning)
    {
        ActivationFinalizationFailed = true;
        if (!Errors.Contains(code, StringComparer.Ordinal))
        {
            Errors.Add(code);
        }

        if (!Warnings.Contains(warning, StringComparer.Ordinal))
        {
            Warnings.Add(warning);
        }
    }

    internal string BuildFailureDiagnostic(Exception exception)
    {
        if (exception is InvalidOperationException { InnerException: PostgresException nestedPostgres })
        {
            return $"{exception.Message} PostgreSQL {nestedPostgres.SqlState}; position={nestedPostgres.Position}.";
        }

        if (exception is PostgresException postgresException)
        {
            return $"PostgreSQL {postgresException.SqlState}; position={postgresException.Position}; "
                + $"constraint={postgresException.ConstraintName ?? "none"}";
        }

        var npgsqlException = FindNpgsqlException(exception);
        if (npgsqlException is not null)
        {
            return BuildNpgsqlFailureDiagnostic(npgsqlException);
        }

        return exception switch
        {
            InvalidOperationException => $"InvalidOperationException: {exception.Message}",
            InvalidCastException => "InvalidCastException: database value type mismatch",
            OverflowException => "OverflowException: numeric value exceeded its contract",
            _ => $"{exception.GetType().Name}: build-failed",
        };
    }

    private string BuildNpgsqlFailureDiagnostic(NpgsqlException exception)
    {
        var categories = ReadDiagnosticCategories(exception);
        var category = SelectPrimaryCategory(categories);
        var innerType = GetAllowlistedExceptionType(exception.InnerException);
        return $"NpgsqlException: category={category}; stage={GetStageToken(CurrentStage)}; "
            + $"chain={string.Join('>', categories)}; innerType={innerType}";
    }

    private static NpgsqlException? FindNpgsqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException and not PostgresException)
            {
                return (NpgsqlException)current;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadDiagnosticCategories(Exception exception)
    {
        const int maximumDepth = 8;
        var categories = new List<string>(maximumDepth);
        Exception? current = exception;
        while (current is not null && categories.Count < maximumDepth)
        {
            categories.Add(GetDiagnosticCategory(current));
            current = current.InnerException;
        }

        return categories;
    }

    private static string GetDiagnosticCategory(Exception exception) => exception switch
    {
        OperationCanceledException => "cancel",
        TimeoutException => "timeout",
        SocketException => "socket",
        IOException => "io",
        NpgsqlException => "npgsql",
        _ => "other",
    };

    private static string SelectPrimaryCategory(IReadOnlyCollection<string> categories)
    {
        foreach (var category in new[] { "timeout", "cancel", "socket", "io" })
        {
            if (categories.Contains(category, StringComparer.Ordinal))
            {
                return category;
            }
        }

        return "transport";
    }

    private static string GetAllowlistedExceptionType(Exception? exception) => exception switch
    {
        null => "none",
        OperationCanceledException => nameof(OperationCanceledException),
        TimeoutException => nameof(TimeoutException),
        SocketException => nameof(SocketException),
        IOException => nameof(IOException),
        NpgsqlException => nameof(NpgsqlException),
        _ => "other",
    };

    private static string GetStageToken(PhraseIndexBuildStage stage) => stage switch
    {
        PhraseIndexBuildStage.OpenConnection => "open-connection",
        PhraseIndexBuildStage.BootstrapSource => "bootstrap-source",
        PhraseIndexBuildStage.PrepareBuild => "prepare-build",
        PhraseIndexBuildStage.StageExactIndex => "stage-exact-index",
        PhraseIndexBuildStage.BuildSimilarityIndex => "build-similarity-index",
        PhraseIndexBuildStage.ValidateStagedIndex => "validate-staged-index",
        PhraseIndexBuildStage.PersistStagedIndex => "persist-staged-index",
        PhraseIndexBuildStage.ActivateBuild => "activate-build",
        _ => "unknown",
    };
}

internal enum PhraseIndexBuildStage
{
    OpenConnection,
    BootstrapSource,
    PrepareBuild,
    StageExactIndex,
    BuildSimilarityIndex,
    ValidateStagedIndex,
    PersistStagedIndex,
    ActivateBuild,
}
