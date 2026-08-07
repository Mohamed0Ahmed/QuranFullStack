using System.Diagnostics;
using System.Globalization;

namespace QuranDashboard.Tests.TestSupport.Process;

public sealed class ProcessExecutionTests
{
    private const int PipeBufferExceedingLength = 200_000;

    [Fact]
    public void DefaultTimeout_IsTwoMinutes()
    {
        ProcessExecution.DefaultTimeout.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task RunAsync_ReturnsTheExitCodeAndBothRedirectedStreams()
    {
        var result = await ProcessExecution.RunAsync(
            Bash("printf 'standard-output'; printf 'standard-error' >&2; exit 7"));

        result.ExitCode.Should().Be(7);
        result.StandardOutput.Should().Be("standard-output");
        result.StandardError.Should().Be("standard-error");
        result.Output.Should().Be("standard-outputstandard-error");
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_DrainsBothStreamsConcurrently_WhenEachOneFillsThePipeBuffer()
    {
        var result = await ProcessExecution.RunAsync(
            Bash(
                $"head -c {PipeBufferExceedingLength} /dev/zero | tr '\\0' 'e' >&2; "
                + $"head -c {PipeBufferExceedingLength} /dev/zero | tr '\\0' 'o'"),
            TimeSpan.FromSeconds(60));

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(0);
        result.StandardError.Length.Should().Be(PipeBufferExceedingLength);
        result.StandardOutput.Length.Should().Be(PipeBufferExceedingLength);
    }

    [Fact]
    public async Task RunAsync_TimesOut_TerminatesTheWholeProcessTree_AndKeepsWhatWasDrained()
    {
        var timeout = TimeSpan.FromSeconds(2);
        var elapsed = Stopwatch.StartNew();

        var result = await ProcessExecution.RunAsync(
            Bash("sleep 300 & echo \"grandchild=$!\"; wait"),
            timeout);

        elapsed.Stop();
        result.TimedOut.Should().BeTrue();
        elapsed.Elapsed.Should().BeGreaterThanOrEqualTo(timeout);
        result.StandardOutput.Should().StartWith("grandchild=");
        await WaitUntilTerminatedAsync(ReadGrandchildProcessId(result.StandardOutput));
    }

    private static ProcessStartInfo Bash(string script)
    {
        var startInfo = new ProcessStartInfo("bash");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(script);
        return startInfo;
    }

    private static int ReadGrandchildProcessId(string standardOutput)
    {
        return int.Parse(
            standardOutput.Trim()["grandchild=".Length..],
            CultureInfo.InvariantCulture);
    }

    private static async Task WaitUntilTerminatedAsync(int processId)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (IsRunning(processId) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        IsRunning(processId).Should().BeFalse(
            "the timed-out process tree must be terminated, not only its root");
    }

    private static bool IsRunning(int processId)
    {
        var statusPath = $"/proc/{processId}/stat";

        try
        {
            var status = File.ReadAllText(statusPath);
            return status[status.LastIndexOf(')') + 2] != 'Z';
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
