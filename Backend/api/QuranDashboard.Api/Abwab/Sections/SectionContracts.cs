namespace QuranDashboard.Api.Abwab.Sections;

public sealed record AddSectionRequest(string Name, long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record EditSectionRequest(string Name, uint ExpectedVersion, long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record SectionOrderEntryRequest(Guid SectionId, int SortOrder, uint ExpectedVersion);

public sealed record ReorderSectionsRequest(IReadOnlyList<SectionOrderEntryRequest> Orders, long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record DeleteSectionRequest(uint ExpectedVersion, long ExpectedTreeRevision, long ExpectedTimelineGeneration);
