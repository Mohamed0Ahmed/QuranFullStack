namespace QuranDashboard.Tests.TestSupport.Process;

internal sealed class ProcessGlobalStateScope : IDisposable
{
    private readonly Dictionary<string, string?> restorableEnvironmentVariables = new(StringComparer.Ordinal);
    private readonly List<Exception> restoreFailures = [];

    private StringWriter? capturedConsole;
    private TextWriter? restorableOut;
    private TextWriter? restorableError;
    private string? restorableCurrentDirectory;
    private int disposed;

    private ProcessGlobalStateScope()
    {
    }

    internal IReadOnlyList<Exception> RestoreFailures => [.. restoreFailures];

    internal string ConsoleOutput => capturedConsole?.ToString() ?? string.Empty;

    internal static ProcessGlobalStateScope Enter(
        string? currentDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        bool captureConsole = false)
    {
        var scope = new ProcessGlobalStateScope();

        try
        {
            scope.ApplyEnvironmentVariables(environmentVariables);
            scope.ApplyCurrentDirectory(currentDirectory);
            scope.ApplyConsoleCapture(captureConsole);
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        RestoreConsole();
        RestoreCurrentDirectory();
        RestoreEnvironmentVariables();
    }

    private void ApplyEnvironmentVariables(IReadOnlyDictionary<string, string?>? environmentVariables)
    {
        if (environmentVariables is null)
        {
            return;
        }

        foreach (var variable in environmentVariables)
        {
            restorableEnvironmentVariables[variable.Key] = Environment.GetEnvironmentVariable(variable.Key);
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    private void ApplyCurrentDirectory(string? currentDirectory)
    {
        if (currentDirectory is null)
        {
            return;
        }

        restorableCurrentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(currentDirectory);
    }

    private void ApplyConsoleCapture(bool captureConsole)
    {
        if (!captureConsole)
        {
            return;
        }

        capturedConsole = new StringWriter();
        restorableOut = Console.Out;
        restorableError = Console.Error;
        Console.SetOut(capturedConsole);
        Console.SetError(capturedConsole);
    }

    private void RestoreConsole()
    {
        var originalOut = restorableOut;
        if (originalOut is not null)
        {
            Restore(() => Console.SetOut(originalOut), "Console.Out");
        }

        var originalError = restorableError;
        if (originalError is not null)
        {
            Restore(() => Console.SetError(originalError), "Console.Error");
        }
    }

    private void RestoreCurrentDirectory()
    {
        var originalCurrentDirectory = restorableCurrentDirectory;
        if (originalCurrentDirectory is null)
        {
            return;
        }

        Restore(
            () => Directory.SetCurrentDirectory(originalCurrentDirectory),
            $"the current directory '{originalCurrentDirectory}'");
    }

    private void RestoreEnvironmentVariables()
    {
        foreach (var variable in restorableEnvironmentVariables)
        {
            Restore(
                () => Environment.SetEnvironmentVariable(variable.Key, variable.Value),
                $"the environment variable '{variable.Key}'");
        }
    }

    private void Restore(Action restore, string boundary)
    {
        try
        {
            restore();
        }
        catch (Exception exception)
        {
            restoreFailures.Add(exception);
            Console.Error.WriteLine(
                $"[process-state] restoring {boundary} failed: {exception.Message}");
        }
    }
}
