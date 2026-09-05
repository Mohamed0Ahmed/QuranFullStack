using System.Diagnostics;
using System.Text.Json;

namespace QuranDashboard.TestRuntime;

internal static class RunEvidenceTelemetry
{
    internal const string PathEnvironmentVariable = "QURAN_DASHBOARD_RUN_EVIDENCE_PATH";
    internal const string CommandIdEnvironmentVariable = "QURAN_DASHBOARD_TEST_COMMAND_ID";

    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static void RecordFingerprint(string kind, long durationMilliseconds) =>
        Record(new
        {
            Event = "fingerprint",
            Kind = kind,
            DurationMilliseconds = durationMilliseconds,
            CommandId = CommandId(),
        });

    internal static void RecordLease(string kind, long waitMilliseconds, string command) =>
        Record(new
        {
            Event = "lease",
            Kind = kind,
            WaitMilliseconds = waitMilliseconds,
            Command = command,
            CommandId = CommandId(),
        });

    internal static void RecordSubPhase(string name, long durationMilliseconds) =>
        Record(new
        {
            Event = "subPhase",
            Name = name,
            DurationMilliseconds = durationMilliseconds,
            CommandId = CommandId(),
        });

    internal static async Task MeasureSubPhaseAsync(string name, Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action();
        }
        finally
        {
            stopwatch.Stop();
            RecordSubPhase(name, stopwatch.ElapsedMilliseconds);
        }
    }

    internal static async Task<T> MeasureSubPhaseAsync<T>(string name, Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            stopwatch.Stop();
            RecordSubPhase(name, stopwatch.ElapsedMilliseconds);
        }
    }

    private static string? CommandId() => Environment.GetEnvironmentVariable(CommandIdEnvironmentVariable);

    private static void Record(object payload)
    {
        var path = Environment.GetEnvironmentVariable(PathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var line = JsonSerializer.Serialize(payload, JsonOptions) + Environment.NewLine;
            lock (Gate)
            {
                File.AppendAllText(path, line);
            }
        }
        catch (Exception)
        {
            // Telemetry must never change the command or test verdict.
        }
    }
}
