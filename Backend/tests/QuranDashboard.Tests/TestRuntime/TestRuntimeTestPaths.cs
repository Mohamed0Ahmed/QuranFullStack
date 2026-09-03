using FluentAssertions;

namespace QuranDashboard.Tests.TestRuntime;

internal static class TestRuntimeTestPaths
{
    internal static string ContractPath
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "QuranDashboard.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull("the tests must run beneath the Backend solution");
            return Path.Combine(directory!.FullName, "testing", "test-database-contract.json");
        }
    }

    internal static string AssemblyPath
    {
        get
        {
            var backendDirectory = Directory.GetParent(Path.GetDirectoryName(ContractPath)!)!.FullName;
            var targetFrameworkDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            return Path.Combine(
                backendDirectory,
                "tools",
                "QuranDashboard.TestRuntime",
                "bin",
                targetFrameworkDirectory.Parent!.Name,
                targetFrameworkDirectory.Name,
                "QuranDashboard.TestRuntime.dll");
        }
    }

    internal static string RefreshOraclePath
    {
        get
        {
            var backendDirectory = Directory.GetParent(Path.GetDirectoryName(ContractPath)!)!.FullName;
            return Path.Combine(
                Directory.GetParent(backendDirectory)!.FullName,
                "test-oracles",
                "test-database-refresh.json");
        }
    }
}
