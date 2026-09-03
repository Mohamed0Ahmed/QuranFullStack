namespace QuranDashboard.TestRuntime;

internal static class Program
{
    internal static Task<int> Main(string[] args)
    {
        return TestRuntimeCommand.ExecuteAsync(args, Console.Out, Console.Error);
    }
}
