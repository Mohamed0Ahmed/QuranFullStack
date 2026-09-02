using System.Text.Json;

namespace QuranDashboard.TestArtifacts;

internal static class ArtifactTrustCommand
{
    internal static int Execute(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error)
    {
        if (args.Count > 0 && args[0] is "provision-full-canonical" or "verify-full-canonical")
        {
            return FullCanonicalArtifactProvisioningCommand.Execute(args, output, error);
        }

        if (args.Count > 0 && args[0] == "previous-release-upgrade")
        {
            return PreviousReleaseMigrationUpgradeCommand.Execute(args, output, error);
        }

        if (args.Count > 0 && args[0] == "rehearse-full-canonical-recovery")
        {
            return FullCanonicalRecoveryRehearsalCommand.Execute(args, output, error);
        }

        if (args.Count > 0 && args[0] == "verify-content-addressed")
        {
            return VerifyContentAddressed(args, output, error);
        }

        var request = Parse(args, error);
        if (request is null)
        {
            return 2;
        }

        var lockPath = Path.Combine(request.RepositoryRoot, ArtifactTrustLock.FileName);
        ArtifactTrustLock artifactLock;
        try
        {
            artifactLock = ArtifactTrustLock.ReadFrom(lockPath);
        }
        catch (Exception exception) when (IsContractReadException(exception))
        {
            WriteLockMismatch(output, exception.Message);
            return 1;
        }

        var lockIssue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (lockIssue is not null)
        {
            WriteLockMismatch(output, lockIssue);
            return 1;
        }

        var selectedArtifacts = SelectArtifacts(artifactLock, request, error);
        if (selectedArtifacts is null)
        {
            output.WriteLine(
                "summary required=1 present=0 missing=1 stale=0 mismatched=0");
            return 1;
        }

        var results = selectedArtifacts
            .Select(artifact => Evaluate(request, artifactLock, artifact))
            .ToArray();
        foreach (var result in results)
        {
            output.WriteLine(
                $"artifact={result.ArtifactId} required=true state={ReportValue(result.Trust.State)} detail={result.Trust.Detail}");
        }

        output.WriteLine(
            $"summary required={results.Length} " +
            $"present={results.Count(result => result.Trust.State == ArtifactTrustState.Present)} " +
            $"missing={results.Count(result => result.Trust.State == ArtifactTrustState.Missing)} " +
            $"stale={results.Count(result => result.Trust.State == ArtifactTrustState.Stale)} " +
            $"mismatched={results.Count(result => result.Trust.State == ArtifactTrustState.Mismatched)}");
        return results.All(result => result.Trust.State == ArtifactTrustState.Present) ? 0 : 1;
    }

    private static int VerifyContentAddressed(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error)
    {
        string? artifactId = null;
        string? contentRoot = null;
        string? repositoryRoot = null;
        for (var index = 1; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count)
            {
                WriteContentAddressedUsage(error);
                return 2;
            }

            switch (args[index])
            {
                case "--artifact" when artifactId is null:
                    artifactId = args[index + 1];
                    break;
                case "--content-root" when contentRoot is null:
                    contentRoot = args[index + 1];
                    break;
                case "--root" when repositoryRoot is null:
                    repositoryRoot = args[index + 1];
                    break;
                default:
                    WriteContentAddressedUsage(error);
                    return 2;
            }
        }

        if (string.IsNullOrWhiteSpace(artifactId)
            || string.IsNullOrWhiteSpace(contentRoot)
            || string.IsNullOrWhiteSpace(repositoryRoot))
        {
            WriteContentAddressedUsage(error);
            return 2;
        }

        var root = Path.GetFullPath(repositoryRoot);
        var source = Path.GetFullPath(contentRoot);
        if (FullCanonicalArtifactProvisioningCommand.IsAtOrBelow(source, root))
        {
            output.WriteLine("artifact=content-addressed required=true state=mismatched detail=content root must stay outside the repository");
            output.WriteLine("summary required=1 present=0 missing=0 stale=0 mismatched=1");
            return 1;
        }

        try
        {
            var artifactLock = ArtifactTrustLock.ReadFrom(Path.Combine(root, ArtifactTrustLock.FileName));
            var issue = ArtifactTrustLockValidator.Validate(artifactLock);
            if (issue is not null)
            {
                WriteLockMismatch(output, issue);
                return 1;
            }

            var artifact = artifactLock.Artifacts.SingleOrDefault(candidate => candidate.Id == artifactId);
            if (artifact is null)
            {
                output.WriteLine("artifact=content-addressed required=true state=missing detail=artifact is not locked");
                output.WriteLine("summary required=1 present=0 missing=1 stale=0 mismatched=0");
                return 1;
            }

            var trust = ArtifactTrustVerifier.VerifyContentAddressed(artifactLock, artifact, root, source);
            output.WriteLine($"artifact={artifact.Id} required=true state={ReportValue(trust.State)} detail={trust.Detail}");
            output.WriteLine($"summary required=1 present={(trust.State == ArtifactTrustState.Present ? 1 : 0)} missing={(trust.State == ArtifactTrustState.Missing ? 1 : 0)} stale={(trust.State == ArtifactTrustState.Stale ? 1 : 0)} mismatched={(trust.State == ArtifactTrustState.Mismatched ? 1 : 0)}");
            return trust.State == ArtifactTrustState.Present ? 0 : 1;
        }
        catch (Exception exception) when (IsContractReadException(exception))
        {
            WriteLockMismatch(output, "content-addressed verification could not read the locked contract");
            return 1;
        }
    }

    private static ArtifactCommandResult Evaluate(
        ArtifactCommandRequest request,
        ArtifactTrustLock artifactLock,
        LockedArtifact artifact)
    {
        try
        {
            var result = request.Operation == ArtifactOperation.Status
                ? ArtifactTrustVerifier.Status(artifact, request.RepositoryRoot)
                : ArtifactTrustVerifier.Verify(artifactLock, artifact, request.RepositoryRoot);
            return new ArtifactCommandResult(artifact.Id, result);
        }
        catch (Exception exception) when (IsContractReadException(exception))
        {
            return new ArtifactCommandResult(
                artifact.Id,
                new ArtifactTrustResult(ArtifactTrustState.Mismatched, exception.Message));
        }
    }

    private static IReadOnlyList<LockedArtifact>? SelectArtifacts(
        ArtifactTrustLock artifactLock,
        ArtifactCommandRequest request,
        TextWriter error)
    {
        if (request.ArtifactId is not null)
        {
            var artifact = artifactLock.Artifacts.SingleOrDefault(
                candidate => string.Equals(
                    candidate.Id,
                    request.ArtifactId,
                    StringComparison.Ordinal));
            if (artifact is null)
            {
                error.WriteLine(
                    $"Artifact '{request.ArtifactId}' is not locked in {ArtifactTrustLock.FileName}.");
                return null;
            }

            return [artifact];
        }

        if (request.Lane is not null)
        {
            var artifacts = artifactLock.Artifacts
                .Where(artifact => artifact.RequiredLanes.Contains(
                    request.Lane,
                    StringComparer.Ordinal))
                .OrderBy(artifact => artifact.Id, StringComparer.Ordinal)
                .ToArray();
            if (artifacts.Length == 0)
            {
                error.WriteLine(
                    $"Lane '{request.Lane}' has no locked artifact in {ArtifactTrustLock.FileName}.");
                return null;
            }

            return artifacts;
        }

        return artifactLock.Artifacts
            .OrderBy(artifact => artifact.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ArtifactCommandRequest? Parse(
        IReadOnlyList<string> args,
        TextWriter error)
    {
        if (args.Count == 0 || args[0] is not "status" and not "verify")
        {
            WriteUsage(error);
            return null;
        }

        string? artifactId = null;
        string? lane = null;
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
                case "--artifact" when artifactId is null:
                    artifactId = value;
                    break;
                case "--lane" when lane is null:
                    lane = value;
                    break;
                case "--root" when repositoryRoot is null:
                    repositoryRoot = value;
                    break;
                default:
                    WriteUsage(error);
                    return null;
            }
        }

        if (artifactId is not null && lane is not null)
        {
            WriteUsage(error);
            return null;
        }

        return new ArtifactCommandRequest(
            args[0] == "status" ? ArtifactOperation.Status : ArtifactOperation.Verify,
            Path.GetFullPath(repositoryRoot ?? Directory.GetCurrentDirectory()),
            artifactId,
            lane);
    }

    private static void WriteLockMismatch(TextWriter output, string detail)
    {
        output.WriteLine($"artifact=lock required=true state=mismatched detail={detail}");
        output.WriteLine("summary required=1 present=0 missing=0 stale=0 mismatched=1");
    }

    private static string ReportValue(ArtifactTrustState state)
    {
        return state switch
        {
            ArtifactTrustState.Present => "present",
            ArtifactTrustState.Missing => "missing",
            ArtifactTrustState.Stale => "stale",
            ArtifactTrustState.Mismatched => "mismatched",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown trust state."),
        };
    }

    private static bool IsContractReadException(Exception exception)
    {
        return exception is JsonException
            or IOException
            or UnauthorizedAccessException
            or InvalidOperationException;
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine(
            "Usage: test-artifacts status|verify [--artifact ARTIFACT_ID | --lane LANE] [--root REPOSITORY_ROOT]");
    }

    private static void WriteContentAddressedUsage(TextWriter error)
    {
        error.WriteLine("Usage: test-artifacts verify-content-addressed --artifact ARTIFACT_ID --content-root ROOT --root REPOSITORY_ROOT");
    }
}

internal enum ArtifactOperation
{
    Status,
    Verify,
}

internal sealed record ArtifactCommandRequest(
    ArtifactOperation Operation,
    string RepositoryRoot,
    string? ArtifactId,
    string? Lane);

internal sealed record ArtifactCommandResult(
    string ArtifactId,
    ArtifactTrustResult Trust);
