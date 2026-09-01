namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

// This hook is deliberately internal and has no configured production behavior. The full-canonical
// rehearsal injects it directly to observe the interval between staging and validation.
internal sealed class PhraseIndexBuildLifecycleTestHook(
    Func<Guid, CancellationToken, Task>? afterStaging = null)
{
    internal static PhraseIndexBuildLifecycleTestHook None { get; } = new();

    internal Task AfterStagingAsync(Guid buildId, CancellationToken ct) =>
        afterStaging?.Invoke(buildId, ct) ?? Task.CompletedTask;
}
