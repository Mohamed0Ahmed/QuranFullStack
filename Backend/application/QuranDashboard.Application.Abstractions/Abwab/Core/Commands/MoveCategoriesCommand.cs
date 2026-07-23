using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategoryMoveEntry(Guid CategoryId, Guid? NewParentCategoryId, Guid? NewSectionId, uint ExpectedVersion);

public sealed record MoveCategoriesCommand(
    IReadOnlyList<CategoryMoveEntry> Moves,
    long ExpectedTreeRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
