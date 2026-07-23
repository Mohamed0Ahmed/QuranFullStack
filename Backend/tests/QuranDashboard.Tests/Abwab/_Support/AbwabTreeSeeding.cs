using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Sections;
using QuranDashboard.Domain.Abwab.Tree;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab._Support;

internal static class AbwabTreeSeeding
{
    private static long _changeSetSequence;

    public static Section NewSection(string name, int sortOrder = 0, bool isPermanentDefault = false) => new()
    {
        SectionId = Guid.NewGuid(),
        Name = name,
        NormalizedName = ArabicNameNormalizer.Normalize(name),
        SortOrder = sortOrder,
        IsPermanentDefault = isPermanentDefault,
    };

    public static Category NewRootCategory(string name, Guid sectionId, int sectionOrder, int globalOrder) => new()
    {
        CategoryId = Guid.NewGuid(),
        Name = name,
        NormalizedName = ArabicNameNormalizer.Normalize(name),
        ParentCategoryId = null,
        SectionId = sectionId,
        SiblingOrder = null,
        SectionOrder = sectionOrder,
        GlobalOrder = globalOrder,
        AncestorIds = [],
        Depth = 0,
    };

    public static Category NewChildCategory(string name, Category parent, int siblingOrder) => new()
    {
        CategoryId = Guid.NewGuid(),
        Name = name,
        NormalizedName = ArabicNameNormalizer.Normalize(name),
        ParentCategoryId = parent.CategoryId,
        SectionId = null,
        SiblingOrder = siblingOrder,
        SectionOrder = null,
        GlobalOrder = null,
        AncestorIds = [.. parent.AncestorIds, parent.CategoryId],
        Depth = parent.AncestorIds.Count + 1,
    };

    public static CategorySearchAlias NewAlias(Guid categoryId, string value) => new()
    {
        CategorySearchAliasId = Guid.NewGuid(),
        CategoryId = categoryId,
        Value = value,
        NormalizedValue = ArabicNameNormalizer.Normalize(value),
    };

    public static ManualProtection NewManualProtection(
        Guid categoryId,
        ManualProtectionType type,
        ManualProtectionScope scope,
        string appliedBy = "tester",
        DateTimeOffset? appliedAtUtc = null) => new()
    {
        ManualProtectionId = Guid.NewGuid(),
        CategoryId = categoryId,
        ProtectionType = type,
        ProtectionScope = scope,
        AppliedByActorSubject = appliedBy,
        AppliedAtUtc = appliedAtUtc ?? DateTimeOffset.UnixEpoch,
    };

    public static async Task InsertAsync(PostgresFixture fixture, params object[] entities)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(
            fixture,
            new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));

        foreach (var entity in entities)
        {
            context.Add(entity);
        }

        context.AbwabChangeSets.Add(NewChangeSet());
        await context.SaveChangesAsync();
    }

    public static ChangeSet NewChangeSet() => new()
    {
        Id = Guid.NewGuid(),
        TimelineGeneration = 0,
        ChangeSetSequence = Interlocked.Increment(ref _changeSetSequence),
        ActorSubject = "tester",
        CreatedAtUtc = DateTimeOffset.UnixEpoch,
    };
}
