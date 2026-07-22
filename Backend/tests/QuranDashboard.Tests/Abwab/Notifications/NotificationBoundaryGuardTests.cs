namespace QuranDashboard.Tests.Abwab.Notifications;

public sealed class NotificationBoundaryGuardTests
{
    private static readonly string[] ForbiddenSurfaceRoots =
    [
        Path.Combine("application", "QuranDashboard.Application"),
        Path.Combine("application", "QuranDashboard.Application.Abstractions"),
        Path.Combine("api", "QuranDashboard.Api"),
    ];

    private static readonly string[] ForbiddenSurfaceTokens =
    [
        "Controller",
        "Endpoint",
        "Http",
        "Mock",
        "Port",
    ];

    private static readonly string StorageFolder =
        Path.Combine("infrastructure", "QuranDashboard.Infrastructure", "Abwab", "Notifications");

    [Fact]
    public void NoPublicNotificationSurface_InApplicationAbstractionsOrApi()
    {
        var offenders = ForbiddenSurfaceRoots
            .SelectMany(NotificationNamedFiles)
            .ToList();

        offenders.Should().BeEmpty(
            "028 is notification STORAGE only — no public port/endpoint/HTTP adapter (032 owns surfaces, 033 "
            + "the restore events); offending file(s): " + string.Join("; ", offenders));
    }

    [Fact]
    public void NoNotificationMockOrAdapter_InTheTestTree_OutsideTheStorageTests()
    {
        var testsRoot = Path.Combine(BackendRoot(), "tests");
        var allowed = Path.Combine("Abwab", "Notifications");

        var offenders = EnumerateCs(testsRoot)
            .Where(file => Path.GetFileName(file).Contains("Notification", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains(allowed, StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(BackendRoot(), file))
            .ToList();

        offenders.Should().BeEmpty(
            "no notification mock/adapter may exist (there is no port to mock in 028); offending file(s): "
            + string.Join("; ", offenders));
    }

    [Fact]
    public void InfrastructureNotificationCode_IsStorageOnly_NoSurfaceTokens()
    {
        var storageDirectory = Path.Combine(BackendRoot(), StorageFolder);

        var offenders = EnumerateCs(storageDirectory)
            .Where(file => ForbiddenSurfaceTokens.Any(token =>
                Path.GetFileName(file).Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Select(file => Path.GetRelativePath(BackendRoot(), file))
            .ToList();

        offenders.Should().BeEmpty(
            "the Infrastructure notification folder must hold storage only — no HTTP adapter/controller/port/"
            + "mock; offending file(s): " + string.Join("; ", offenders));
    }

    [Fact]
    public void NoNotificationFrontendFeature()
    {
        var featureDirectory = Path.Combine(
            RepoRoot(), "Frontend", "quran-dashboard-ui", "src", "app", "features", "notifications");

        Directory.Exists(featureDirectory).Should().BeFalse(
            "032 owns the notification UI; 028 introduces no frontend notification feature");
    }

    [Fact]
    public void NotificationStorage_Exists_SoTheGuardIsNotVacuous()
    {
        var storageDirectory = Path.Combine(BackendRoot(), StorageFolder);
        Directory.Exists(storageDirectory).Should().BeTrue("028 must ship the notification storage writer/repository");

        var storageFiles = EnumerateCs(storageDirectory).Select(Path.GetFileName).ToList();
        storageFiles.Should().Contain(name => name!.Contains("Writer", StringComparison.Ordinal),
            "the transaction-joining storage writer must exist");
        storageFiles.Should().Contain(name => name!.Contains("Repository", StringComparison.Ordinal),
            "the low-level recipient/read-state repository must exist");
    }

    [Fact]
    public void Scanner_DetectsASurfaceName()
    {
        const string surfaceFile = "NotificationsController.cs";
        surfaceFile.Contains("Notification", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        ForbiddenSurfaceTokens.Any(token => surfaceFile.Contains(token, StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    private static IEnumerable<string> NotificationNamedFiles(string root)
    {
        var directory = Path.Combine(BackendRoot(), root);
        return EnumerateCs(directory)
            .Where(file => Path.GetFileName(file).Contains("Notification", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(BackendRoot(), file));
    }

    private static IEnumerable<string> EnumerateCs(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            : [];

    private static string BackendRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "Backend"));

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
}
