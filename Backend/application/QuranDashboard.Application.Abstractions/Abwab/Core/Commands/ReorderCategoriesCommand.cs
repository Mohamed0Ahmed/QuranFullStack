using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public enum CategoryOrderScope
{
    Siblings = 0,
    SectionRoots = 1,
    GlobalRoots = 2,
}

public sealed record CategoryOrderEntry(Guid CategoryId, int NewOrder, uint ExpectedVersion);

public sealed record ReorderCategoriesCommand(
    CategoryOrderScope Scope,
    Guid? ParentCategoryId,
    Guid? SectionId,
    IReadOnlyList<CategoryOrderEntry> Orders,
    long ExpectedTreeRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
