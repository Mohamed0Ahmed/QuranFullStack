using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class ExclusivePostgreSqlLease : IAsyncDisposable
{
    private readonly System.Diagnostics.Process server;
    private readonly string dataDirectory;
    private readonly CrossProcessPostgreSqlLock crossProcessLock;
    private readonly Action release;

    private int disposed;

    private ExclusivePostgreSqlLease(
        System.Diagnostics.Process server,
        string dataDirectory,
        string connectionString,
        CrossProcessPostgreSqlLock crossProcessLock,
        Action release)
    {
        this.server = server;
        this.dataDirectory = dataDirectory;
        ConnectionString = connectionString;
        this.crossProcessLock = crossProcessLock;
        this.release = release;
    }

    internal Guid ServerInstanceId { get; } = Guid.NewGuid();

    internal string ConnectionString { get; }

    internal static async Task<ExclusivePostgreSqlLease> AcquireAsync(
        string owner,
        Action release,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        var binaries = PostgresLocalBinaries.Resolve();
        var credential = string.IsNullOrWhiteSpace(password) ? Guid.NewGuid().ToString("N") : password;
        var dataDirectory = Path.Combine(
            Path.GetTempPath(),
            $"qdb-exclusive-pg-{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDirectory);

        var crossProcessLock = await CrossProcessPostgreSqlLock.AcquireProjectLockAsync(
            $"pid {Environment.ProcessId} exclusive local postgres for {owner}",
            cancellationToken);
        try
        {
            var port = GetFreeTcpPort();
            InitializeCluster(binaries.Initdb, dataDirectory, credential);
            ConfigureCluster(dataDirectory, port);
            var process = StartServer(binaries.Postgres, dataDirectory, port);
            try
            {
                var connectionString = new NpgsqlConnectionStringBuilder
                {
                    Host = "127.0.0.1",
                    Port = port,
                    Username = "postgres",
                    Password = credential,
                    Database = "postgres",
                    Pooling = false,
                    Timeout = 5,
                }.ConnectionString;
                await WaitUntilReadyAsync(connectionString, cancellationToken);
                return new ExclusivePostgreSqlLease(
                    process,
                    dataDirectory,
                    connectionString,
                    crossProcessLock,
                    release);
            }
            catch
            {
                StopServer(process);
                throw;
            }
        }
        catch
        {
            TryDeleteDirectory(dataDirectory);
            crossProcessLock.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        try
        {
            NpgsqlConnection.ClearAllPools();
            StopServer(server);
        }
        finally
        {
            TryDeleteDirectory(dataDirectory);
            crossProcessLock.Dispose();
            release();
            await Task.CompletedTask;
        }
    }

    private static void InitializeCluster(string initdb, string dataDirectory, string password)
    {
        var pwFile = Path.Combine(dataDirectory, "pwfile");
        File.WriteAllText(pwFile, password + Environment.NewLine, Encoding.UTF8);
        Run(
            initdb,
            [
                "-D", dataDirectory,
                "--username=postgres",
                $"--pwfile={pwFile}",
                "--auth=scram-sha-256",
                "--encoding=UTF8",
                "--locale=C",
            ]);
        File.Delete(pwFile);
    }

    private static void ConfigureCluster(string dataDirectory, int port)
    {
        var configuration = Path.Combine(dataDirectory, "postgresql.conf");
        File.AppendAllText(
            configuration,
            $"""

            listen_addresses = '127.0.0.1'
            port = {port}
            unix_socket_directories = '{dataDirectory.Replace("'", "''")}'
            fsync = off
            synchronous_commit = off
            full_page_writes = off
            """);
    }

    private static System.Diagnostics.Process StartServer(string postgres, string dataDirectory, int port)
    {
        var start = new ProcessStartInfo
        {
            FileName = postgres,
            ArgumentList =
            {
                "-D",
                dataDirectory,
                "-p",
                port.ToString(),
                "-k",
                dataDirectory,
                "-h",
                "127.0.0.1",
            },
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start {postgres}.");
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        return process;
    }

    private static async Task WaitUntilReadyAsync(string connectionString, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (exception is NpgsqlException or SocketException or TimeoutException)
            {
                last = exception;
                await Task.Delay(200, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Local PostgreSQL 18 exclusive server did not become ready: {last?.Message}");
    }

    private static void StopServer(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(10));
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private static void Run(string fileName, IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stderr = process.StandardError.ReadToEnd();
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} failed with exit {process.ExitCode}: {stderr}{stdout}");
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}

internal static class PostgresLocalBinaries
{
    internal static (string Initdb, string Postgres) Resolve()
    {
        var candidates = new[]
        {
            "/usr/lib/postgresql/18/bin",
            "/usr/pgsql-18/bin",
            "/usr/local/pgsql/bin",
        };
        foreach (var directory in candidates)
        {
            var initdb = Path.Combine(directory, "initdb");
            var postgres = Path.Combine(directory, "postgres");
            if (File.Exists(initdb) && File.Exists(postgres))
            {
                return (initdb, postgres);
            }
        }

        var pathInitdb = FindOnPath("initdb");
        var pathPostgres = FindOnPath("postgres");
        if (pathInitdb is not null && pathPostgres is not null)
        {
            return (pathInitdb, pathPostgres);
        }

        throw new InvalidOperationException(
            "TestRuntime exclusive isolation requires local PostgreSQL 18 binaries (initdb and postgres). "
            + "Install PostgreSQL 18 on this machine; container-backed servers are not a supported test path.");
    }

    private static string? FindOnPath(string fileName)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var directory in paths)
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
