using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

internal static class FullCanonicalArtifactProvisioningCommand
{
    internal static int Execute(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error)
    {
        var request = Parse(args, error);
        if (request is null)
        {
            return 2;
        }

        try
        {
            return request.Operation == FullCanonicalProvisioningOperation.Provision
                ? ProvisionAsync(request, output).GetAwaiter().GetResult()
                : VerifyAsync(request, output).GetAwaiter().GetResult();
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or InvalidOperationException
            or JsonException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception)
        {
            output.WriteLine($"full-canonical state=failed detail={exception.Message}");
            return 1;
        }
    }

    private static async Task<int> ProvisionAsync(
        FullCanonicalProvisioningCommandRequest request,
        TextWriter output)
    {
        var artifactLock = ReadLock(request.RepositoryRoot);
        FullCanonicalArtifactProvisioner.EnsureApplicableArtifacts(request.RunKind, artifactLock);
        var provisionalReceipt = new FullCanonicalProvisioningReceipt(
            "provisioning",
            request.RunKind,
            RepositoryMigrationState.Read(request.RepositoryRoot),
            []);
        if (!TryClaimReceipt(request.ReceiptPath, provisionalReceipt))
        {
            var existing = StrictJson.Read<FullCanonicalProvisioningReceipt>(
                request.ReceiptPath,
                "Full-canonical provisioning receipt");
            if (existing.Status != "provisioned" || existing.RunKind != request.RunKind)
            {
                throw new InvalidOperationException(
                    "The provisioning receipt already exists without a completed matching run; use a new run receipt path.");
            }

            await FullCanonicalArtifactProvisioner.VerifyProvisionedStateAsync(
                existing,
                artifactLock,
                request.RepositoryRoot,
                request.StagingRoot,
                new ProcessFullCanonicalArtifactDatabase(
                    request.DatabaseConnectionFile,
                    request.DatabaseContainer,
                    request.RunKind));
            output.WriteLine(
                $"full-canonical run={request.RunKind} state=already-provisioned artifacts={existing.Artifacts.Count} receipt={request.ReceiptPath}");
            return 0;
        }

        // The exclusive provisional receipt blocks retries after a partial restore. Repeating a large restore
        // is not a safe recovery operation; an operator must investigate and choose a fresh run receipt.
        try
        {
            var receipt = await FullCanonicalArtifactProvisioner.ProvisionAsync(
                request.RunKind,
                artifactLock,
                request.RepositoryRoot,
                request.StagingRoot,
                new LocalFullCanonicalArtifactFetcher(request.ArtifactRoot!),
                new ProcessFullCanonicalArtifactDatabase(
                    request.DatabaseConnectionFile,
                    request.DatabaseContainer,
                    request.RunKind));
            WriteReceipt(request.ReceiptPath, receipt);
            output.WriteLine(
                $"full-canonical run={request.RunKind} state=provisioned artifacts={receipt.Artifacts.Count} receipt={request.ReceiptPath}");
            return 0;
        }
        catch
        {
            WriteReceipt(
                request.ReceiptPath,
                new FullCanonicalProvisioningReceipt(
                    "failed",
                    request.RunKind,
                    RepositoryMigrationState.Read(request.RepositoryRoot),
                    []));
            throw;
        }
    }

    private static async Task<int> VerifyAsync(
        FullCanonicalProvisioningCommandRequest request,
        TextWriter output)
    {
        EnsureSealedExecutionEnvironment();
        var receipt = StrictJson.Read<FullCanonicalProvisioningReceipt>(
            request.ReceiptPath,
            "Full-canonical provisioning receipt");
        if (receipt.Status != "provisioned" || receipt.RunKind != request.RunKind)
        {
            throw new InvalidOperationException(
                "Sealed verification requires a completed receipt for the requested scheduled or release run.");
        }

        await FullCanonicalArtifactProvisioner.VerifyProvisionedStateAsync(
            receipt,
            ReadLock(request.RepositoryRoot),
            request.RepositoryRoot,
            request.StagingRoot,
            new ProcessFullCanonicalArtifactDatabase(
                request.DatabaseConnectionFile,
                request.DatabaseContainer,
                request.RunKind));
        output.WriteLine(
            $"full-canonical run={request.RunKind} state=verified-shared artifacts={receipt.Artifacts.Count} receipt={request.ReceiptPath}");
        return 0;
    }

    private static ArtifactTrustLock ReadLock(string repositoryRoot)
    {
        var artifactLock = ArtifactTrustLock.ReadFrom(
            Path.Combine(repositoryRoot, ArtifactTrustLock.FileName));
        var issue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (issue is not null)
        {
            throw new InvalidOperationException($"The artifact lock is invalid: {issue}");
        }

        return artifactLock;
    }

    private static void WriteReceipt(string path, FullCanonicalProvisioningReceipt receipt)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.tmp-{Environment.ProcessId}";
        File.WriteAllText(temporary, SerializeReceipt(receipt));
        File.Move(temporary, path, overwrite: true);
    }

    private static bool TryClaimReceipt(string path, FullCanonicalProvisioningReceipt receipt)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(SerializeReceipt(receipt));
            writer.Flush();
            stream.Flush(flushToDisk: true);
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            return false;
        }
    }

    private static string SerializeReceipt(FullCanonicalProvisioningReceipt receipt)
    {
        return $"{JsonSerializer.Serialize(receipt, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true })}\n";
    }

    internal static FullCanonicalProvisioningCommandRequest? Parse(
        IReadOnlyList<string> args,
        TextWriter error)
    {
        var operation = args[0] == "provision-full-canonical"
            ? FullCanonicalProvisioningOperation.Provision
            : FullCanonicalProvisioningOperation.Verify;
        string? runKind = null;
        string? databaseConnectionFile = null;
        string? databaseContainer = null;
        string? stagingRoot = null;
        string? receiptPath = null;
        string? repositoryRoot = null;

        for (var index = 1; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                WriteUsage(error);
                return null;
            }

            var value = args[index + 1];
            if (string.IsNullOrWhiteSpace(value))
            {
                WriteUsage(error);
                return null;
            }

            switch (args[index])
            {
                case "--run" when runKind is null:
                    runKind = value;
                    break;
                case "--database-connection-file" when databaseConnectionFile is null:
                    databaseConnectionFile = value;
                    break;
                case "--database-container" when databaseContainer is null:
                    databaseContainer = value;
                    break;
                case "--staging-root" when stagingRoot is null:
                    stagingRoot = value;
                    break;
                case "--receipt" when receiptPath is null:
                    receiptPath = value;
                    break;
                case "--root" when repositoryRoot is null:
                    repositoryRoot = value;
                    break;
                default:
                    WriteUsage(error);
                    return null;
            }
        }

        if (runKind is not "scheduled" and not "release"
            || databaseConnectionFile is null
            || databaseContainer is null
            || stagingRoot is null
            || receiptPath is null)
        {
            WriteUsage(error);
            return null;
        }

        var artifactRoot = operation == FullCanonicalProvisioningOperation.Provision
            ? Environment.GetEnvironmentVariable("QURAN_TEST_ARTIFACT_ROOT")
            : null;
        if (operation == FullCanonicalProvisioningOperation.Provision && string.IsNullOrWhiteSpace(artifactRoot))
        {
            error.WriteLine("Full-canonical local provisioning requires QURAN_TEST_ARTIFACT_ROOT.");
            return null;
        }

        var fullRepositoryRoot = Path.GetFullPath(repositoryRoot ?? Directory.GetCurrentDirectory());
        var fullArtifactRoot = artifactRoot is null ? null : Path.GetFullPath(artifactRoot);
        var fullDatabaseConnectionFile = Path.GetFullPath(databaseConnectionFile);
        var fullStagingRoot = Path.GetFullPath(stagingRoot);
        var fullReceiptPath = Path.GetFullPath(receiptPath);
        if (IsAtOrBelow(fullStagingRoot, fullRepositoryRoot))
        {
            error.WriteLine("The full-canonical staging root must stay outside the repository worktree.");
            return null;
        }
        if (IsAtOrBelow(fullDatabaseConnectionFile, fullRepositoryRoot))
        {
            error.WriteLine("The full-canonical database connection file must stay outside the repository worktree.");
            return null;
        }
        if (IsAtOrBelow(fullReceiptPath, fullRepositoryRoot))
        {
            error.WriteLine("The full-canonical receipt must stay outside the repository worktree.");
            return null;
        }

        return new FullCanonicalProvisioningCommandRequest(
            operation,
            runKind,
            fullRepositoryRoot,
            fullDatabaseConnectionFile,
            databaseContainer,
            fullArtifactRoot,
            fullStagingRoot,
            fullReceiptPath);
    }

    internal static bool IsAtOrBelow(string path, string root)
    {
        var resolvedRoot = ResolveContainmentPath(root);
        var resolvedPath = ResolveContainmentPath(path);
        return resolvedPath == resolvedRoot
            || resolvedPath.StartsWith($"{resolvedRoot}{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string ResolveContainmentPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
            ?? throw new InvalidDataException("The full-canonical runtime path is invalid.");
        var resolvedPath = root;
        var relativePath = fullPath[root.Length..];
        foreach (var segment in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(resolvedPath, segment);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                resolvedPath = candidate;
                continue;
            }

            FileSystemInfo info = Directory.Exists(candidate)
                ? new DirectoryInfo(candidate)
                : new FileInfo(candidate);
            resolvedPath = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? candidate;
        }

        return Path.GetFullPath(resolvedPath);
    }

    internal static void EnsureSealedExecutionEnvironment()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("QURAN_DASHBOARD_FULL_CANONICAL_NETWORK"),
                "blocked",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Sealed verification requires QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked from the provider network-denied job.");
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "PATH",
            "HOME",
            "LANG",
            "LC_ALL",
            "TMP",
            "TEMP",
            "TMPDIR",
            "DOTNET_ROOT",
            "DOTNET_HOST_PATH",
            "DOTNET_CLI_HOME",
            "DOTNET_NOLOGO",
            "QURAN_DASHBOARD_TEST_RUN_ID",
            "QURAN_DASHBOARD_TEST_DB_PARALLELISM",
            "QURAN_DASHBOARD_FULL_CANONICAL_RECEIPT",
            "QURAN_DASHBOARD_FULL_CANONICAL_CONNECTION_FILE",
            "QURAN_DASHBOARD_FULL_CANONICAL_STAGING_ROOT",
            "QURAN_DASHBOARD_FULL_CANONICAL_DATABASE_CONTAINER",
            "QURAN_DASHBOARD_FULL_CANONICAL_RUN",
            "QURAN_DASHBOARD_FULL_CANONICAL_NETWORK",
            "QURAN_DASHBOARD_ARTIFACT_EXECUTION",
        };
        var names = Environment.GetEnvironmentVariables()
            .Keys
            .Cast<object>()
            .Select(key => Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty)
            .Where(name => !allowed.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (names.Length > 0)
        {
            throw new InvalidOperationException(
                $"Sealed verification requires a credential-free allowlisted environment; refusing: {string.Join(", ", names)}.");
        }
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine(
            "Usage: QURAN_TEST_ARTIFACT_ROOT=PATH test-artifacts provision-full-canonical --run scheduled|release --database-connection-file PATH --database-container NAME --staging-root PATH --receipt PATH [--root ROOT]");
        error.WriteLine(
            "       test-artifacts verify-full-canonical --run scheduled|release --database-connection-file PATH --database-container NAME --staging-root PATH --receipt PATH [--root ROOT]");
    }
}

internal enum FullCanonicalProvisioningOperation
{
    Provision,
    Verify,
}

internal sealed record FullCanonicalProvisioningCommandRequest(
    FullCanonicalProvisioningOperation Operation,
    string RunKind,
    string RepositoryRoot,
    string DatabaseConnectionFile,
    string DatabaseContainer,
    string? ArtifactRoot,
    string StagingRoot,
    string ReceiptPath);

internal sealed class LocalFullCanonicalArtifactFetcher(string artifactRoot) : IFullCanonicalArtifactFetcher
{
    public async Task FetchAsync(
        LockedArtifact artifact,
        string stagingRoot,
        CancellationToken cancellationToken = default)
    {
        var payload = artifact.StagedFiles.Single(file => file.Role == "payload");
        var storageIdentity = $"@sha256:{payload.Sha256}";
        if (!artifact.ImmutableStorageId.StartsWith("local://", StringComparison.Ordinal)
            || !artifact.ImmutableStorageId.EndsWith(storageIdentity, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Local full-canonical provisioning requires a local immutable storage identity matching the payload SHA-256.");
        }

        var contentRoot = Path.Combine(Path.GetFullPath(artifactRoot), "sha256", payload.Sha256);
        if (!Directory.Exists(contentRoot))
        {
            throw new FileNotFoundException(
                $"The content-addressed artifact is missing beneath QURAN_TEST_ARTIFACT_ROOT: sha256/{payload.Sha256}.");
        }

        foreach (var file in artifact.StagedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = Path.Combine(contentRoot, Path.GetFileName(file.Path));
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    $"The content-addressed artifact is missing beneath QURAN_TEST_ARTIFACT_ROOT: sha256/{payload.Sha256}/{Path.GetFileName(file.Path)}.");
            }

            var destination = Path.Combine(stagingRoot, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }

        await Task.CompletedTask;
    }
}

internal sealed class ProcessFullCanonicalArtifactDatabase : IFullCanonicalArtifactDatabase
{
    private readonly string container;
    private readonly string database;
    private readonly IPAddress hostAddress;
    private readonly string? password;
    private readonly int port;
    private readonly string runKind;
    private readonly string username;

    internal string ConnectionString { get; }

    internal ProcessFullCanonicalArtifactDatabase(
        string connectionFile,
        string container,
        string runKind)
    {
        if (!File.Exists(connectionFile))
        {
            throw new FileNotFoundException("The private full-canonical database connection file is missing.", connectionFile);
        }

        DbConnectionStringBuilder builder;
        try
        {
            builder = new DbConnectionStringBuilder
            {
                ConnectionString = File.ReadAllText(connectionFile).Trim(),
            };
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The private full-canonical database connection file is invalid.",
                exception);
        }
        var host = ReadConnectionValue(builder, "Host") ?? ReadConnectionValue(builder, "Server");
        if (!IPAddress.TryParse(host, out var parsedHost) || !IPAddress.IsLoopback(parsedHost))
        {
            throw new InvalidOperationException(
                "Full-canonical provisioning requires a disposable PostgreSQL target with a literal loopback IP address.");
        }

        var configuredPort = ReadConnectionValue(builder, "Port");
        if (!int.TryParse(configuredPort ?? "5432", NumberStyles.None, CultureInfo.InvariantCulture, out port)
            || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("The private full-canonical database connection file has an invalid PostgreSQL port.");
        }

        database = ReadConnectionValue(builder, "Database") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("The private full-canonical database connection file must name the disposable database.");
        }

        username = ReadConnectionValue(builder, "Username")
            ?? ReadConnectionValue(builder, "User ID")
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("The private full-canonical database connection file must name the PostgreSQL user.");
        }

        password = ReadConnectionValue(builder, "Password") ?? ReadConnectionValue(builder, "Pwd");
        ConnectionString = builder.ConnectionString;
        this.container = container;
        hostAddress = parsedHost;
        this.runKind = runKind;
    }

    public async Task AssertPostgreSqlCompatibilityAsync(
        LockedPostgreSqlState expected,
        CancellationToken cancellationToken = default)
    {
        var expectedMajor = MajorVersion(expected.ProducerVersion, "locked PostgreSQL producer version");
        var targetMajor = MajorVersion(
            await QueryAsync("SHOW server_version;", cancellationToken),
            "target PostgreSQL version");
        if (targetMajor != expectedMajor)
        {
            throw new InvalidOperationException(
                $"The disposable PostgreSQL target major {targetMajor} does not match locked producer major {expectedMajor}.");
        }

        var restoreMajor = MajorVersion(
            await RunForOutputAsync(CreateProcessStartInfo("pg_restore"), ["--version"], cancellationToken),
            "pg_restore version");
        if (restoreMajor < expectedMajor)
        {
            throw new InvalidOperationException(
                $"pg_restore major {restoreMajor} cannot restore locked producer major {expectedMajor}.");
        }

        var containerInspection = await RunForOutputAsync(
            CreateProcessStartInfo("docker"),
            ["inspect", container],
            cancellationToken);
        if (!IsExpectedContainer(containerInspection, expected.ContainerDigest))
        {
            throw new InvalidOperationException(
                "The PostgreSQL connection is not bound to the provisioner-owned digest-pinned container for this run.");
        }
    }

    public async Task AssertMigrationAsync(
        ArtifactMigrationState expected,
        CancellationToken cancellationToken = default)
    {
        var output = await QueryAsync(
            "SELECT count(*)::text || '|' || max(\"MigrationId\") FROM public.\"__EFMigrationsHistory\";",
            cancellationToken);
        var parts = output.Split('|');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            || count != expected.Count
            || !string.Equals(parts[1], expected.Head, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The PostgreSQL target migration state does not match {expected.Head} (count {expected.Count}).");
        }
    }

    public async Task AssertRestoreTargetIsEmptyAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default)
    {
        var counts = await CountRowsAsync(tables, cancellationToken);
        var populated = counts.FirstOrDefault(entry => entry.Value != 0);
        if (!string.IsNullOrEmpty(populated.Key))
        {
            throw new InvalidOperationException(
                $"The provisioner-owned PostgreSQL target is not empty at '{populated.Key}'.");
        }
    }

    public Task RestoreAsync(
        LockedArtifact artifact,
        string payloadPath,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateProcessStartInfo("pg_restore");
        AddDatabaseConnectionArguments(startInfo);
        startInfo.ArgumentList.Add("--data-only");
        startInfo.ArgumentList.Add("--disable-triggers");
        startInfo.ArgumentList.Add("--exit-on-error");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--no-privileges");
        foreach (var table in artifact.TableScope.Tables)
        {
            startInfo.ArgumentList.Add($"--table=public.{table}");
        }
        startInfo.ArgumentList.Add(payloadPath);
        return RunWithoutLoggingAsync(startInfo, cancellationToken, $"restore full-canonical artifact '{artifact.Id}'");
    }

    public async Task<IReadOnlyDictionary<string, long>> CountRowsAsync(
        IReadOnlyList<string> tables,
        CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            if (!ArtifactTrustLockValidator.IsValidTableIdentifier(table))
            {
                throw new InvalidOperationException($"Invalid table identifier '{table}'.");
            }

            var output = await QueryAsync(
                $"SELECT count(*) FROM public.\"{table}\";",
                cancellationToken);
            if (!long.TryParse(output, NumberStyles.None, CultureInfo.InvariantCulture, out var count))
            {
                throw new InvalidOperationException($"PostgreSQL did not return one row count for '{table}'.");
            }

            counts[table] = count;
        }

        return counts;
    }

    private async Task<string> QueryAsync(string sql, CancellationToken cancellationToken)
    {
        var startInfo = CreateProcessStartInfo("psql");
        AddDatabaseConnectionArguments(startInfo);
        startInfo.ArgumentList.Add("--no-align");
        startInfo.ArgumentList.Add("--tuples-only");
        startInfo.ArgumentList.Add("--quiet");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add("ON_ERROR_STOP=1");
        startInfo.ArgumentList.Add("--command");
        startInfo.ArgumentList.Add(sql);

        return await RunForOutputAsync(startInfo, [], cancellationToken);
    }

    private void AddDatabaseConnectionArguments(ProcessStartInfo startInfo)
    {
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(hostAddress.ToString());
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--username");
        startInfo.ArgumentList.Add(username);
        startInfo.ArgumentList.Add("--dbname");
        startInfo.ArgumentList.Add(database);
        startInfo.ArgumentList.Add("--no-password");
    }

    private ProcessStartInfo CreateProcessStartInfo(string fileName)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        if (!string.IsNullOrEmpty(password))
        {
            startInfo.Environment["PGPASSWORD"] = password;
        }

        return startInfo;
    }

    private static async Task RunWithoutLoggingAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        string operation)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start PostgreSQL to {operation}.");
        var discardOutput = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
        var discardError = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(discardOutput, discardError);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"PostgreSQL could not {operation}.");
        }
    }

    private static async Task<string> RunForOutputAsync(
        ProcessStartInfo startInfo,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PostgreSQL compatibility verification.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var discardError = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await discardError;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("PostgreSQL compatibility verification failed.");
        }

        return output.Trim();
    }

    private static int MajorVersion(string value, string name)
    {
        var firstDigit = value.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
        if (firstDigit < 0)
        {
            throw new InvalidOperationException($"The {name} is unreadable.");
        }

        var digits = new string(value.Skip(firstDigit).TakeWhile(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            ? major
            : throw new InvalidOperationException($"The {name} is unreadable.");
    }

    private bool IsExpectedContainer(string inspection, string expectedDigest)
    {
        using var document = JsonDocument.Parse(inspection);
        if (document.RootElement.ValueKind != JsonValueKind.Array
            || document.RootElement.GetArrayLength() != 1)
        {
            return false;
        }

        var containerInfo = document.RootElement[0];
        if (!containerInfo.TryGetProperty("Config", out var config)
            || !config.TryGetProperty("Image", out var image)
            || !string.Equals(image.GetString(), $"postgres@{expectedDigest}", StringComparison.Ordinal)
            || !config.TryGetProperty("Labels", out var labels)
            || !labels.TryGetProperty("com.qurandashboard.full-canonical.run", out var run)
            || !string.Equals(run.GetString(), runKind, StringComparison.Ordinal))
        {
            return false;
        }

        if (!containerInfo.TryGetProperty("NetworkSettings", out var networkSettings)
            || !networkSettings.TryGetProperty("Ports", out var ports)
            || !ports.TryGetProperty("5432/tcp", out var bindings)
            || bindings.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var bindingsForPostgres = bindings.EnumerateArray().ToArray();
        if (bindingsForPostgres.Length != 1)
        {
            return false;
        }

        var expectedPort = port.ToString(CultureInfo.InvariantCulture);
        return bindingsForPostgres[0].TryGetProperty("HostIp", out var hostIp)
            && IPAddress.TryParse(hostIp.GetString(), out var boundAddress)
            && hostAddress.Equals(boundAddress)
            && bindingsForPostgres[0].TryGetProperty("HostPort", out var hostPort)
            && string.Equals(hostPort.GetString(), expectedPort, StringComparison.Ordinal);
    }

    private static string? ReadConnectionValue(DbConnectionStringBuilder builder, string name)
    {
        return builder.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

}
