using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Application.Abstractions.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

// Template application creates ORDINARY Category aggregate rows, so its inversion is performed by the
// single 029 CategoryRestoreAdapter (§8: "it is not a second 'template-created category' adapter").
// The adapter arrives through its interface so the delegation is observable in a test rather than
// only asserted in prose.
public sealed class TemplateApplicationEventInterpreter(
    IAbwabRestoreAdapter<CategoryAggregate, CategoryRestoreSnapshot> categoryAdapter) : IAbwabRestoreEventInterpreter
{
    public const string Kind = TemplateAuditActions.Applied;

    public const int Schema = 1;

    public string EventKind => Kind;

    public int InterpreterSchemaVersion => Schema;

    public IReadOnlyList<CategoryRestoreSnapshot> CaptureCreatedCategories(IReadOnlyList<CategoryAggregate> createdCategories)
    {
        ArgumentNullException.ThrowIfNull(createdCategories);
        return [.. createdCategories.Select(categoryAdapter.Capture)];
    }

    public IReadOnlyList<CategoryAggregate> ReconstructCreatedCategories(IReadOnlyList<CategoryRestoreSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        return [.. snapshots.Select(categoryAdapter.Reconstruct)];
    }
}
