namespace QuranDashboard.Tests.TestSupport.Process;

public sealed class ProcessGlobalStateScopeTests
{
    [Fact]
    public void Enter_AppliesEveryBoundary_AndDisposeRestoresThem()
    {
        var callerDirectory = Directory.GetCurrentDirectory();
        var callerOut = Console.Out;
        var callerError = Console.Error;
        var presentVariable = UniqueVariableName();
        var absentVariable = UniqueVariableName();
        var scopedDirectory = CreateTemporaryDirectory();
        Environment.SetEnvironmentVariable(presentVariable, "outside");

        try
        {
            using (var scope = ProcessGlobalStateScope.Enter(
                currentDirectory: scopedDirectory.FullName,
                environmentVariables: new Dictionary<string, string?>
                {
                    [presentVariable] = "inside",
                    [absentVariable] = "inside",
                },
                captureConsole: true))
            {
                Console.Out.WriteLine("captured-standard-output");
                Console.Error.WriteLine("captured-standard-error");

                Directory.GetCurrentDirectory().Should().Be(scopedDirectory.FullName);
                Environment.GetEnvironmentVariable(presentVariable).Should().Be("inside");
                Environment.GetEnvironmentVariable(absentVariable).Should().Be("inside");
                scope.ConsoleOutput.Should()
                    .Contain("captured-standard-output")
                    .And.Contain("captured-standard-error");
                scope.RestoreFailures.Should().BeEmpty();
            }

            Directory.GetCurrentDirectory().Should().Be(callerDirectory);
            Environment.GetEnvironmentVariable(presentVariable).Should().Be("outside");
            Environment.GetEnvironmentVariable(absentVariable).Should().BeNull();
            Console.Out.Should().BeSameAs(callerOut);
            Console.Error.Should().BeSameAs(callerError);
        }
        finally
        {
            Directory.SetCurrentDirectory(callerDirectory);
            Environment.SetEnvironmentVariable(presentVariable, null);
            Environment.SetEnvironmentVariable(absentVariable, null);
            scopedDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Dispose_RestoresEveryBoundary_WhenTheScopedBodyThrows()
    {
        var callerDirectory = Directory.GetCurrentDirectory();
        var callerOut = Console.Out;
        var callerError = Console.Error;
        var variable = UniqueVariableName();
        var scopedDirectory = CreateTemporaryDirectory();
        Environment.SetEnvironmentVariable(variable, "outside");

        try
        {
            var scopedBody = () =>
            {
                using var scope = ProcessGlobalStateScope.Enter(
                    currentDirectory: scopedDirectory.FullName,
                    environmentVariables: new Dictionary<string, string?> { [variable] = "inside" },
                    captureConsole: true);

                throw new InvalidOperationException("the scoped body failed");
            };

            scopedBody.Should().Throw<InvalidOperationException>()
                .WithMessage("the scoped body failed");
            Directory.GetCurrentDirectory().Should().Be(callerDirectory);
            Environment.GetEnvironmentVariable(variable).Should().Be("outside");
            Console.Out.Should().BeSameAs(callerOut);
            Console.Error.Should().BeSameAs(callerError);
        }
        finally
        {
            Directory.SetCurrentDirectory(callerDirectory);
            Environment.SetEnvironmentVariable(variable, null);
            scopedDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Enter_RestoresAlreadyAppliedBoundaries_WhenALaterBoundaryFails()
    {
        var callerDirectory = Directory.GetCurrentDirectory();
        var callerOut = Console.Out;
        var variable = UniqueVariableName();
        var absentDirectory = Path.Combine(
            Path.GetTempPath(),
            $"process-state-absent-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable(variable, "outside");

        try
        {
            var enter = () => ProcessGlobalStateScope.Enter(
                currentDirectory: absentDirectory,
                environmentVariables: new Dictionary<string, string?> { [variable] = "inside" },
                captureConsole: true);

            enter.Should().Throw<DirectoryNotFoundException>();
            Environment.GetEnvironmentVariable(variable).Should().Be("outside");
            Directory.GetCurrentDirectory().Should().Be(callerDirectory);
            Console.Out.Should().BeSameAs(callerOut);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void Dispose_RestoresRemainingBoundaries_WhenTheCurrentDirectoryRestoreFails()
    {
        var callerDirectory = Directory.GetCurrentDirectory();
        var callerOut = Console.Out;
        var callerError = Console.Error;
        var vanishingDirectory = CreateTemporaryDirectory();
        var scopedDirectory = CreateTemporaryDirectory();
        var variable = UniqueVariableName();
        Environment.SetEnvironmentVariable(variable, "outside");

        try
        {
            Directory.SetCurrentDirectory(vanishingDirectory.FullName);
            var scope = ProcessGlobalStateScope.Enter(
                currentDirectory: scopedDirectory.FullName,
                environmentVariables: new Dictionary<string, string?> { [variable] = "inside" },
                captureConsole: true);
            vanishingDirectory.Delete();

            scope.Dispose();

            scope.RestoreFailures.Should().ContainSingle()
                .Which.Should().BeAssignableTo<IOException>();
            Environment.GetEnvironmentVariable(variable).Should().Be("outside");
            Console.Out.Should().BeSameAs(callerOut);
            Console.Error.Should().BeSameAs(callerError);
            Directory.GetCurrentDirectory().Should().Be(scopedDirectory.FullName);
        }
        finally
        {
            Directory.SetCurrentDirectory(callerDirectory);
            Environment.SetEnvironmentVariable(variable, null);
            scopedDirectory.Delete(recursive: true);
            vanishingDirectory.Refresh();
            if (vanishingDirectory.Exists)
            {
                vanishingDirectory.Delete(recursive: true);
            }
        }
    }

    [Fact]
    public void Dispose_IsIdempotent_AndLeavesLaterStateAlone()
    {
        var variable = UniqueVariableName();

        try
        {
            var scope = ProcessGlobalStateScope.Enter(
                environmentVariables: new Dictionary<string, string?> { [variable] = "inside" });
            scope.Dispose();
            Environment.SetEnvironmentVariable(variable, "set-after-disposal");

            scope.Dispose();

            Environment.GetEnvironmentVariable(variable).Should().Be("set-after-disposal");
            scope.RestoreFailures.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    private static string UniqueVariableName()
    {
        return $"QURAN_DASHBOARD_TEST_PROCESS_STATE_{Guid.NewGuid():N}";
    }

    private static DirectoryInfo CreateTemporaryDirectory()
    {
        return Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"process-state-{Guid.NewGuid():N}"));
    }
}
