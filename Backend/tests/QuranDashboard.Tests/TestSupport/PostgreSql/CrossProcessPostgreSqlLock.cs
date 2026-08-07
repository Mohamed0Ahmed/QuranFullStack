using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class CrossProcessPostgreSqlLock : IDisposable
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(5);

    private const string TestProjectFileName = "QuranDashboard.Tests.csproj";
    private const string HolderFileSuffix = ".holder";

    private static readonly Lazy<string> ProjectLockFilePathLoader = new(ResolveProjectLockFilePath);

    private readonly FileStream handle;

    private CrossProcessPostgreSqlLock(FileStream handle, string filePath)
    {
        this.handle = handle;
        FilePath = filePath;
    }

    internal static string ProjectLockFilePath => ProjectLockFilePathLoader.Value;

    internal string FilePath { get; }

    internal static async Task<CrossProcessPostgreSqlLock> AcquireAsync(
        string owner,
        string filePath,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var startedUtc = DateTimeOffset.UtcNow;
        var deadlineUtc = startedUtc + timeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handle = TryOpenExclusively(filePath);
            if (handle is not null)
            {
                RecordHolder(filePath, owner);
                return new CrossProcessPostgreSqlLock(handle, filePath);
            }

            if (DateTimeOffset.UtcNow >= deadlineUtc)
            {
                throw new TimeoutException(
                    $"Waited {timeout.TotalMinutes:F0} minutes for the project PostgreSQL lock at {filePath}. "
                    + $"{DescribeRecordedHolder(filePath)} "
                    + "Only one project-owned PostgreSQL test runtime may exist at a time.");
            }

            var waited = DateTimeOffset.UtcNow - startedUtc;
            Console.WriteLine(
                $"[postgres-lock] {owner} waiting {waited.TotalSeconds:F0}s for {filePath}. "
                + DescribeRecordedHolder(filePath));

            await Task.Delay(retryInterval, cancellationToken);
        }
    }

    internal static Task<CrossProcessPostgreSqlLock> AcquireProjectLockAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        return AcquireAsync(
            owner,
            ProjectLockFilePath,
            DefaultTimeout,
            DefaultRetryInterval,
            cancellationToken);
    }

    public void Dispose()
    {
        handle.Dispose();
    }

    private static FileStream? TryOpenExclusively(string filePath)
    {
        try
        {
            return new FileStream(
                filePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void RecordHolder(string filePath, string owner)
    {
        var startedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        File.WriteAllText(
            filePath + HolderFileSuffix,
            $"pid={Environment.ProcessId} owner={owner} startedUtc={startedUtc}{Environment.NewLine}");
    }

    private static string DescribeRecordedHolder(string filePath)
    {
        try
        {
            var holderFile = filePath + HolderFileSuffix;
            return File.Exists(holderFile)
                ? $"Last recorded holder: {File.ReadAllText(holderFile).Trim()}."
                : "No holder was recorded.";
        }
        catch (IOException)
        {
            return "The recorded holder could not be read.";
        }
    }

    private static string ResolveProjectLockFilePath()
    {
        var temporaryRoot = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(temporaryRoot))
        {
            temporaryRoot = "/tmp";
        }

        var identity = $"{ResolveTestProjectPath()}\n{GetUserId()}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        var hash = Convert.ToHexString(digest)[..16].ToLowerInvariant();

        return Path.Combine(temporaryRoot, "quran-dashboard-tests", $"{hash}-postgres.lock");
    }

    private static string ResolveTestProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, TestProjectFileName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"{TestProjectFileName} was not found above {AppContext.BaseDirectory}; "
            + "the cross-process lock identity cannot be derived.");
    }

    [DllImport("libc", EntryPoint = "getuid")]
    private static extern uint GetUserId();
}
