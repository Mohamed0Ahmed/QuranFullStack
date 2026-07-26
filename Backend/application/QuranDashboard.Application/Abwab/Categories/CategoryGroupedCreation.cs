using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Categories;

public sealed record CategoryCreationInput(string Name, string? RepresentativeQuranExcerpt, string? Description);

// The `029` in-transaction creation core — normalization, tree/name guards, protection gate, order
// allocation — extracted so the single-add path and template application share ONE writer instead of
// forking it. One instance per audited operation: the in-memory claims below are what let a grouped
// creation allocate sibling orders and reject in-group name collisions before anything is saved,
// which a per-row database query cannot see.
internal sealed class CategoryGroupedCreation(
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ProtectionResolver protectionResolver)
{
    private readonly Dictionary<Guid, Category> _createdById = [];
    private readonly Dictionary<Guid, int> _nextSiblingOrderByParent = [];
    private readonly HashSet<(Guid ParentCategoryId, string NormalizedName)> _claimedChildNames = [];
    private readonly HashSet<string> _claimedRootNames = new(StringComparer.Ordinal);

    public async Task<Category> AddChildAsync(Guid parentCategoryId, CategoryCreationInput input, CancellationToken cancellationToken)
    {
        var parent = await RequireParentAsync(parentCategoryId, cancellationToken);
        await EnsureParentAcceptsStructureChangesAsync(parentCategoryId, cancellationToken);

        var normalized = ArabicNameNormalizer.Normalize(input.Name);
        await GuardChildNameAsync(parentCategoryId, normalized, cancellationToken);

        var category = new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = input.Name,
            NormalizedName = normalized,
            Description = input.Description,
            RepresentativeQuranExcerpt = input.RepresentativeQuranExcerpt,
            ParentCategoryId = parentCategoryId,
            SectionId = null,
            SiblingOrder = await NextSiblingOrderAsync(parentCategoryId, cancellationToken),
            SectionOrder = null,
            GlobalOrder = null,
            AncestorIds = [.. parent.AncestorIds, parent.CategoryId],
            Depth = parent.AncestorIds.Count + 1,
        };

        Track(category);
        categories.Add(category);
        return category;
    }

    public async Task<Category> AddRootAsync(Guid? sectionId, CategoryCreationInput input, CancellationToken cancellationToken)
    {
        var effectiveSectionId = sectionId ?? await sections.GetPermanentDefaultSectionIdAsync(cancellationToken);
        if (!await sections.ExistsActiveAsync(effectiveSectionId, cancellationToken))
        {
            throw Unavailable();
        }

        var normalized = ArabicNameNormalizer.Normalize(input.Name);
        await GuardRootNameAsync(normalized, cancellationToken);

        var maxSectionOrder = await categories.GetMaxSectionOrderAsync(effectiveSectionId, null, cancellationToken) ?? -1;
        var maxGlobalOrder = await categories.GetMaxGlobalOrderAsync(null, cancellationToken) ?? -1;

        var category = new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = input.Name,
            NormalizedName = normalized,
            Description = input.Description,
            RepresentativeQuranExcerpt = input.RepresentativeQuranExcerpt,
            ParentCategoryId = null,
            SectionId = effectiveSectionId,
            SiblingOrder = null,
            SectionOrder = maxSectionOrder + 1,
            GlobalOrder = maxGlobalOrder + 1,
            AncestorIds = [],
            Depth = 0,
        };

        Track(category);
        categories.Add(category);
        return category;
    }

    private async Task<Category> RequireParentAsync(Guid parentCategoryId, CancellationToken cancellationToken) =>
        _createdById.TryGetValue(parentCategoryId, out var created)
            ? created
            : await categories.FindTrackedAsync(parentCategoryId, includeDeleted: false, cancellationToken) ?? throw Unavailable();

    // A parent created inside this very operation carries no manual protection and inherits only from
    // ancestors already checked when it was created, so re-resolving it would query a row that does
    // not exist yet and fail as unavailable.
    private async Task EnsureParentAcceptsStructureChangesAsync(Guid parentCategoryId, CancellationToken cancellationToken)
    {
        if (_createdById.ContainsKey(parentCategoryId))
        {
            return;
        }

        await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
            protectionResolver, parentCategoryId, ManualProtectionType.InternalStructure, cancellationToken);
    }

    private async Task GuardChildNameAsync(Guid parentCategoryId, string normalizedName, CancellationToken cancellationToken)
    {
        if (!_claimedChildNames.Add((parentCategoryId, normalizedName)))
        {
            throw NameConflict();
        }

        await CategoryTreeGuards.GuardNameConflictAsync(categories, normalizedName, parentCategoryId, null, cancellationToken);
    }

    private async Task GuardRootNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        if (!_claimedRootNames.Add(normalizedName))
        {
            throw NameConflict();
        }

        await CategoryTreeGuards.GuardNameConflictAsync(categories, normalizedName, null, null, cancellationToken);
    }

    private async Task<int> NextSiblingOrderAsync(Guid parentCategoryId, CancellationToken cancellationToken)
    {
        if (!_nextSiblingOrderByParent.TryGetValue(parentCategoryId, out var next))
        {
            next = (await categories.GetMaxSiblingOrderAsync(parentCategoryId, null, cancellationToken) ?? -1) + 1;
        }

        _nextSiblingOrderByParent[parentCategoryId] = next + 1;
        return next;
    }

    private void Track(Category category) => _createdById[category.CategoryId] = category;

    private static AbwabWriteConflictException Unavailable() =>
        new(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

    private static AbwabWriteConflictException NameConflict() =>
        new(AbwabConflictCodes.CategoryNameConflict, "A category with this name already exists in this scope.");
}
