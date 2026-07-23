using QuranDashboard.Application.Abstractions.Abwab.Core;

namespace QuranDashboard.Api.Abwab.Categories;

public sealed record AddCategoryRequest(
    string Name,
    string? Description,
    string? RepresentativeQuranExcerpt,
    Guid? ParentCategoryId,
    Guid? SectionId,
    long ExpectedTreeRevision,
    long ExpectedTimelineGeneration);

public sealed record EditCategoryRequest(
    string Name,
    string? Description,
    string? RepresentativeQuranExcerpt,
    uint ExpectedVersion,
    long ExpectedTimelineGeneration);

public sealed record CategoryMoveEntryRequest(Guid CategoryId, Guid? NewParentCategoryId, Guid? NewSectionId, uint ExpectedVersion);

public sealed record MoveCategoriesRequest(IReadOnlyList<CategoryMoveEntryRequest> Moves, long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record CategoryOrderEntryRequest(Guid CategoryId, int NewOrder, uint ExpectedVersion);

public sealed record ReorderCategoriesRequest(
    CategoryOrderScope Scope,
    Guid? ParentCategoryId,
    Guid? SectionId,
    IReadOnlyList<CategoryOrderEntryRequest> Orders,
    long ExpectedTreeRevision,
    long ExpectedTimelineGeneration);

public sealed record SubtreeDeleteRequest(uint ExpectedVersion, long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record OperationRestoreRequest(long ExpectedTreeRevision, long ExpectedTimelineGeneration);

public sealed record AddCategoryAliasRequest(string Value, long ExpectedTimelineGeneration);

public sealed record EditCategoryAliasRequest(string Value, uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record RemoveCategoryAliasRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);
