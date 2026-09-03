using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.TestRuntime;

internal sealed class CanonicalRefreshPipeline(string backendDirectory) : ICanonicalRefreshPipeline
{
    private const string StorageProofContract = "operator-verified-database-filesystem-v1";

    private readonly string repositoryDirectory = Directory.GetParent(backendDirectory)?.FullName
        ?? throw new InvalidOperationException("Backend must have a repository parent directory.");

    private readonly string resourcesDirectory = Path.Combine(
        Directory.GetParent(backendDirectory)?.FullName
            ?? throw new InvalidOperationException("Backend must have a repository parent directory."),
        "resources");

    public async Task<CanonicalPipelinePreparation> PrepareAsync(CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        var inputs = RequiredInputs().ToArray();
        foreach (var input in inputs)
        {
            if (!Directory.Exists(input) && !File.Exists(input))
            {
                violations.Add(new ContractViolation(
                    "refresh.canonical-input.missing",
                    Path.GetRelativePath(repositoryDirectory, input)));
            }
        }

        var freeBytes = Environment.GetEnvironmentVariable("PhraseSearch__VerifiedDatabaseFreeBytes");
        if (!long.TryParse(freeBytes, out var parsedBytes) || parsedBytes <= 0)
        {
            violations.Add(new ContractViolation("refresh.phrase-search.storage-proof-bytes.missing"));
        }

        if (Environment.GetEnvironmentVariable("PhraseSearch__DatabaseStorageProofContract") != StorageProofContract)
        {
            violations.Add(new ContractViolation("refresh.phrase-search.storage-proof-contract.invalid"));
        }

        if (violations.Count != 0)
        {
            return new CanonicalPipelinePreparation(false, null, Order(violations));
        }

        var provenance = await HashInputsAsync(inputs, cancellationToken);
        return new CanonicalPipelinePreparation(true, provenance, []);
    }

    public async Task<IReadOnlyList<CapabilityRefreshStageReport>> RunAsync(
        string connectionString,
        string runId,
        long advisoryLockKey,
        CancellationToken cancellationToken)
    {
        var reports = new List<CapabilityRefreshStageReport>();
        var reportRoot = Path.Combine(resourcesDirectory, "report", "test-database-refresh", runId);
        Directory.CreateDirectory(reportRoot);

        var build = await RunProcessAsync(
            "dotnet",
            [
                "build", "QuranDashboard.sln", "--configuration", "Debug", "--no-restore",
                "--disable-build-servers", "-m:1", "-p:BuildInParallel=false", "-v", "minimal",
            ],
            connectionString: null,
            runId: null,
            advisoryLockKey: null,
            cancellationToken);
        if (build.Status != "passed")
        {
            return [build with { Stage = "build-canonical-tools" }];
        }

        var importer = Path.Combine(
            backendDirectory,
            "tools",
            "QuranDashboard.DataImporter",
            "bin",
            "Debug",
            "net10.0",
            "QuranDashboard.DataImporter.dll");
        if (!File.Exists(importer))
        {
            return [new CapabilityRefreshStageReport(
                "build-canonical-tools",
                "failed",
                build.DurationMilliseconds,
                "DataImporterAssemblyMissing")];
        }

        foreach (var stage in Commands(importer, reportRoot))
        {
            var report = await RunProcessAsync(
                "dotnet",
                stage.Arguments,
                connectionString,
                runId,
                advisoryLockKey,
                cancellationToken);
            reports.Add(report with { Stage = stage.Name });
            if (report.Status != "passed")
            {
                break;
            }
        }

        return reports;
    }

    private IEnumerable<string> RequiredInputs()
    {
        var imports = Path.Combine(resourcesDirectory, "import-sources");
        yield return Path.Combine(imports, "quran-foundation");
        yield return Path.Combine(imports, "masaq-corpus-aligned", "masaq-search-words.dashboard-ready.json");
        yield return Path.Combine(imports, "quran-enriched-morphology");
        yield return Path.Combine(imports, "mutashabihat");
        yield return Path.Combine(imports, "quran-navigation-metadata");
        yield return Path.Combine(imports, "quran-full-i3rab");
        yield return Path.Combine(imports, "quran-tafsirs-neon-10");
        yield return Path.Combine(imports, "quran-translations-neon-10");
    }

    private IEnumerable<PipelineCommand> Commands(string importer, string reportRoot)
    {
        var imports = Path.Combine(resourcesDirectory, "import-sources");
        string Report(string name) => Path.Combine(reportRoot, name);
        yield return new PipelineCommand(
            "import-foundation",
            [importer, "import-foundation", "--source", Path.Combine(imports, "quran-foundation"), "--report-out", Report("foundation")]);
        yield return new PipelineCommand("rebuild-words", [importer, "rebuild-words", "--report-out", Report("words")]);
        yield return new PipelineCommand("build-phrase-index", [importer, "build-phrase-index", "--report-out", Report("phrase-search")]);
        yield return new PipelineCommand(
            "import-morphology-enriched",
            [importer, "import-morphology", "--enriched", "--source", Path.Combine(imports, "quran-enriched-morphology"), "--report-out", Report("morphology")]);
        yield return new PipelineCommand("generate-i3rab", [importer, "generate-i3rab", "--report-out", Report("i3rab-simple")]);
        yield return new PipelineCommand(
            "import-mutashabihat",
            [importer, "import-mutashabihat", "--source", Path.Combine(imports, "mutashabihat"), "--report-out", Report("mutashabihat")]);
        yield return new PipelineCommand(
            "import-navigation-metadata",
            [importer, "import-navigation-metadata", "--source", Path.Combine(imports, "quran-navigation-metadata"), "--report-out", Report("navigation")]);
        yield return new PipelineCommand(
            "import-full-i3rab",
            [importer, "import-full-i3rab", "--source", Path.Combine(imports, "quran-full-i3rab"), "--report-out", Report("i3rab-full")]);
        yield return new PipelineCommand(
            "import-tafsirs-curated-10",
            [importer, "import-tafsirs", "--profile", "curated-10", "--source", Path.Combine(imports, "quran-tafsirs-neon-10"), "--report-out", Report("tafsirs")]);
        yield return new PipelineCommand(
            "import-translations-curated-10",
            [importer, "import-translations", "--profile", "curated-10", "--source", Path.Combine(imports, "quran-translations-neon-10"), "--report-out", Report("translations")]);
    }

    private async Task<CapabilityRefreshStageReport> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? connectionString,
        string? runId,
        long? advisoryLockKey,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = backendDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (connectionString is not null)
        {
            startInfo.Environment["ConnectionStrings__QuranDashboardDb"] = connectionString;
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
            startInfo.Environment["QURAN_DASHBOARD_TEST_RUNTIME_GUARD"] = "exclusive-v1";
            startInfo.Environment["QURAN_DASHBOARD_TEST_RUN_ID"] = runId!;
            startInfo.Environment["QURAN_DASHBOARD_TEST_LOCK_COMMAND"] = "capability-refresh";
            startInfo.Environment["QURAN_DASHBOARD_TEST_LOCK_KEY"] = advisoryLockKey!.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            startInfo.Environment.Remove("ConnectionStrings__QuranDashboardDb");
            startInfo.Environment.Remove("ConnectionStrings__QuranDashboardTest");
        }

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) when (process.HasExited)
                {
                    // The child completed between the state check and termination request.
                }
            }

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdout, stderr);
            throw;
        }

        await Task.WhenAll(stdout, stderr);
        return new CapabilityRefreshStageReport(
            "process",
            process.ExitCode == 0 ? "passed" : "failed",
            stopwatch.ElapsedMilliseconds,
            process.ExitCode == 0 ? null : $"ProcessExitCode{process.ExitCode}");
    }

    private async Task<string> HashInputsAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken)
    {
        using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in inputs.SelectMany(EnumerateFiles)
                     .OrderBy(path => Path.GetRelativePath(repositoryDirectory, path), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(repositoryDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
            incremental.AppendData(Encoding.UTF8.GetBytes(relative));
            incremental.AppendData([0]);
            await using var stream = File.OpenRead(file);
            var buffer = new byte[1024 * 128];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) != 0)
            {
                incremental.AppendData(buffer.AsSpan(0, read));
            }

            incremental.AppendData([0]);
        }

        return Convert.ToHexStringLower(incremental.GetHashAndReset());
    }

    private static IEnumerable<string> EnumerateFiles(string input)
    {
        if (File.Exists(input))
        {
            yield return input;
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Canonical source inputs cannot contain symbolic links.");
            }

            yield return file;
        }
    }

    private static IReadOnlyList<ContractViolation> Order(IEnumerable<ContractViolation> violations) =>
        violations.OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();

    private sealed record PipelineCommand(string Name, IReadOnlyList<string> Arguments);
}
