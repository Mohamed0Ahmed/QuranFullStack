using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using DiagnosticsProcess = System.Diagnostics.Process;

namespace QuranDashboard.Tests.TestSupport.Process;

internal static class ProcessExecution
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan TerminationBudget = TimeSpan.FromSeconds(30);

    private const int UnavailableExitCode = -1;

    internal static Task<ProcessRunResult> RunAsync(ProcessStartInfo startInfo)
    {
        return RunAsync(startInfo, DefaultTimeout);
    }

    internal static async Task<ProcessRunResult> RunAsync(ProcessStartInfo startInfo, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        using var process = DiagnosticsProcess.Start(startInfo)
            ?? throw new InvalidOperationException($"Starting '{startInfo.FileName}' produced no process.");

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var drains = Task.WhenAll(
            DrainAsync(process.StandardOutput, standardOutput),
            DrainAsync(process.StandardError, standardError));

        var timedOut = !await WaitForExitAsync(process, timeout);
        if (timedOut)
        {
            TerminateProcessTree(process);
            await WaitForExitAsync(process, TerminationBudget);
        }

        try
        {
            await drains.WaitAsync(TerminationBudget);
        }
        catch (TimeoutException)
        {
        }

        return new ProcessRunResult(
            ReadExitCode(process),
            Read(standardOutput),
            Read(standardError),
            timedOut);
    }

    private static async Task<bool> WaitForExitAsync(DiagnosticsProcess process, TimeSpan budget)
    {
        using var budgetSource = new CancellationTokenSource(budget);

        try
        {
            await process.WaitForExitAsync(budgetSource.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static void TerminateProcessTree(DiagnosticsProcess process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    private static async Task DrainAsync(StreamReader reader, StringBuilder sink)
    {
        var buffer = new char[4096];
        int read;

        while ((read = await reader.ReadAsync(buffer.AsMemory())) > 0)
        {
            lock (sink)
            {
                sink.Append(buffer, 0, read);
            }
        }
    }

    private static string Read(StringBuilder sink)
    {
        lock (sink)
        {
            return sink.ToString();
        }
    }

    private static int ReadExitCode(DiagnosticsProcess process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : UnavailableExitCode;
        }
        catch (InvalidOperationException)
        {
            return UnavailableExitCode;
        }
    }
}

internal sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    internal string Output => StandardOutput + StandardError;
}
