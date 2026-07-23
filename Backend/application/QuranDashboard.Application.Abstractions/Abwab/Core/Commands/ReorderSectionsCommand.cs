using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record SectionOrderEntry(Guid SectionId, int SortOrder, uint ExpectedVersion);

public sealed record ReorderSectionsCommand(
    IReadOnlyList<SectionOrderEntry> Orders,
    long ExpectedTreeRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
