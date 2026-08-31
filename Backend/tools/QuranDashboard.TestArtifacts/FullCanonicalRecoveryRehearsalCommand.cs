namespace QuranDashboard.TestArtifacts;

// This command deliberately stops before opening a database until a reviewed full-canonical artifact exists.
// The repository has no such adoption yet, so accepting ambient connection strings would create false evidence.
internal static class FullCanonicalRecoveryRehearsalCommand
{
    internal static int Execute(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if ((args.Count != 2 && args.Count != 4)
            || args[1] != "--confirm-backup"
            || (args.Count == 4 && args[2] != "--root"))
        {
            error.WriteLine("Usage: test-artifacts rehearse-full-canonical-recovery --confirm-backup [--root REPOSITORY_ROOT]");
            return 2;
        }

        ArtifactTrustLock artifactLock;
        try
        {
            var root = Path.GetFullPath(args.Count == 4 ? args[3] : Directory.GetCurrentDirectory());
            artifactLock = ArtifactTrustLock.ReadFrom(Path.Combine(root, ArtifactTrustLock.FileName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            output.WriteLine("full-canonical-recovery state=blocked classification=data-recovery reason=artifact-lock-unavailable");
            return 1;
        }
        var lockIssue = ArtifactTrustLockValidator.Validate(artifactLock);
        if (lockIssue is not null)
        {
            output.WriteLine($"full-canonical-recovery state=blocked classification=data-recovery reason=invalid-artifact-lock");
            return 1;
        }

        if (!artifactLock.Artifacts.Any(artifact => artifact.Restore?.Kind == "full-canonical"))
        {
            output.WriteLine(
                "full-canonical-recovery state=blocked classification=data-recovery "
                + "reason=no-reviewed-full-canonical-artifact-with-immutable-storage-identity");
            return 1;
        }

        output.WriteLine(
            "full-canonical-recovery state=blocked classification=data-recovery "
            + "reason=operator-must-supply-disposable-source-target-and-private-backup-paths");
        return 1;
    }
}
