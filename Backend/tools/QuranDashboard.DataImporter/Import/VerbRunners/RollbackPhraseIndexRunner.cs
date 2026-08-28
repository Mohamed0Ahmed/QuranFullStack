using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class RollbackPhraseIndexRunner
{
    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> createHost,
        Action printUsage)
    {
        if (args.Length != 0)
        {
            Console.Error.WriteLine("rollback-phrase-index does not accept arguments.");
            printUsage();
            return RollbackPhraseIndexResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RollbackPhraseIndexHandler>();
        var result = await handler.HandleAsync(new RollbackPhraseIndexCommand(), CancellationToken.None);
        var output = result.Succeeded ? Console.Out : Console.Error;
        output.WriteLine(result.Message);
        output.WriteLine($"outcome={result.Outcome}");
        output.WriteLine($"retry_directive={FormatRetryDirective(result.RetryDirective)}");
        output.WriteLine($"active_build_id={result.ActiveBuildId?.ToString() ?? "none"}");
        output.WriteLine($"previous_build_id={result.PreviousBuildId?.ToString() ?? "none"}");
        output.WriteLine($"source_revision={result.SourceRevision}");
        output.WriteLine($"source_fingerprint={result.SourceFingerprint}");
        return result.ExitCode;
    }

    private static string FormatRetryDirective(
        PhraseIndexRollbackRetryDirective directive) =>
        directive switch
        {
            PhraseIndexRollbackRetryDirective.SafeToRetry => "safe-to-retry",
            PhraseIndexRollbackRetryDirective.DoNotRetry => "do-not-retry",
            _ => "not-applicable",
        };
}
