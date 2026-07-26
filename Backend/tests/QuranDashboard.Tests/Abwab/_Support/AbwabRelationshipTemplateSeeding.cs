using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Tree;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Support;

internal static class AbwabRelationshipTemplateSeeding
{
    public static CategoryRelationship NewMutualRelationship(
        Guid lowerCategoryId,
        Guid higherCategoryId,
        RelationshipType relationshipType = RelationshipType.Similar) => new()
    {
        CategoryRelationshipId = Guid.NewGuid(),
        RelationshipType = relationshipType,
        LowerCategoryId = lowerCategoryId,
        HigherCategoryId = higherCategoryId,
    };

    public static CategoryRelationship NewDirectionalRelationship(Guid broaderCategoryId, Guid narrowerCategoryId) => new()
    {
        CategoryRelationshipId = Guid.NewGuid(),
        RelationshipType = RelationshipType.BroaderNarrower,
        SourceCategoryId = broaderCategoryId,
        TargetCategoryId = narrowerCategoryId,
    };

    public static async Task<(Category Lower, Category Higher)> TwoCategoryEndpointsAsync(
        PostgresFixture fixture, string firstName = "باب أدنى", string secondName = "باب أعلى")
    {
        var section = AbwabTreeSeeding.NewSection($"قسم علاقات {Guid.NewGuid():N}");
        var first = AbwabTreeSeeding.NewRootCategory(firstName, section.SectionId, sectionOrder: 0, globalOrder: 0);
        var second = AbwabTreeSeeding.NewRootCategory(secondName, section.SectionId, sectionOrder: 1, globalOrder: 1);
        await AbwabTreeSeeding.InsertAsync(fixture, section, first, second);

        return first.CategoryId.CompareTo(second.CategoryId) < 0 ? (first, second) : (second, first);
    }

    public static async Task<IReadOnlyList<Category>> ManyCategoryEndpointsAsync(
        PostgresFixture fixture, int count, string label = "طرف")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var section = AbwabTreeSeeding.NewSection($"قسم أطراف {Guid.NewGuid():N}");
        var endpoints = Enumerable.Range(0, count)
            .Select(i => AbwabTreeSeeding.NewRootCategory($"{label} {i} {Guid.NewGuid():N}", section.SectionId, sectionOrder: i, globalOrder: i))
            .ToList();

        await AbwabTreeSeeding.InsertAsync(fixture, [section, .. endpoints]);
        return endpoints;
    }

    public static async Task<(Category Category, ManualProtection Protection)> ProtectedCategoryAsync(
        PostgresFixture fixture,
        ManualProtectionType type,
        ManualProtectionScope scope = ManualProtectionScope.CategoryOnly,
        string name = "باب محمي")
    {
        var section = AbwabTreeSeeding.NewSection($"قسم حماية {Guid.NewGuid():N}");
        var category = AbwabTreeSeeding.NewRootCategory(name, section.SectionId, sectionOrder: 0, globalOrder: 0);
        var protection = AbwabTreeSeeding.NewManualProtection(category.CategoryId, type, scope);
        await AbwabTreeSeeding.InsertAsync(fixture, section, category, protection);

        return (category, protection);
    }

    public static async Task<IReadOnlyList<Category>> DeepCategoryChainAsync(
        PostgresFixture fixture, int depth, string label = "سلسلة")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        var section = AbwabTreeSeeding.NewSection($"قسم {label} {Guid.NewGuid():N}");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory($"{label} 0", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var chain = new List<Category> { root };
        var current = root;
        for (var level = 1; level <= depth; level++)
        {
            var child = AbwabTreeSeeding.NewChildCategory($"{label} {level}", current, siblingOrder: 0);
            await AbwabTreeSeeding.InsertAsync(fixture, child);
            chain.Add(child);
            current = child;
        }

        return chain;
    }

    public static DoorTemplate NewDoorTemplate(string name = "قالب باب", string? description = null) => new()
    {
        DoorTemplateId = Guid.NewGuid(),
        Name = name,
        NormalizedName = ArabicNameNormalizer.Normalize(name),
        Description = description,
    };

    public static TemplateNode NewTemplateNode(
        Guid doorTemplateId,
        string name,
        int siblingOrder,
        Guid? parentTemplateNodeId = null,
        string? representativeQuranExcerpt = null,
        string? description = null) => new()
    {
        TemplateNodeId = Guid.NewGuid(),
        DoorTemplateId = doorTemplateId,
        ParentTemplateNodeId = parentTemplateNodeId,
        Name = name,
        NormalizedName = ArabicNameNormalizer.Normalize(name),
        RepresentativeQuranExcerpt = representativeQuranExcerpt,
        Description = description,
        SiblingOrder = siblingOrder,
    };

    public static TemplateNodeSearchAlias NewTemplateNodeAlias(Guid templateNodeId, string value) => new()
    {
        TemplateNodeSearchAliasId = Guid.NewGuid(),
        TemplateNodeId = templateNodeId,
        Value = value,
        NormalizedValue = ArabicNameNormalizer.Normalize(value),
    };

    // A template whose roots each carry `depth` levels of single children, so reparent/cycle and
    // application tests share one deep-tree shape instead of hand-building it per file.
    public static async Task<(DoorTemplate Template, IReadOnlyList<TemplateNode> Nodes)> DeepTemplateAsync(
        PostgresFixture fixture,
        int rootCount,
        int depth,
        string label = "عقدة")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rootCount);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        var template = NewDoorTemplate($"قالب {label} {Guid.NewGuid():N}");
        var nodes = new List<TemplateNode>();

        for (var rootIndex = 0; rootIndex < rootCount; rootIndex++)
        {
            var root = NewTemplateNode(template.DoorTemplateId, $"{label} {rootIndex}", rootIndex);
            nodes.Add(root);

            var parent = root;
            for (var level = 1; level <= depth; level++)
            {
                var child = NewTemplateNode(template.DoorTemplateId, $"{label} {rootIndex}-{level}", 0, parent.TemplateNodeId);
                nodes.Add(child);
                parent = child;
            }
        }

        await AbwabTreeSeeding.InsertAsync(fixture, template);
        await AbwabTreeSeeding.InsertAsync(fixture, [.. nodes.Cast<object>()]);

        return (template, nodes);
    }

    // A parent ring cannot be inserted in one pass — EF orders inserts by foreign key and a
    // self-referencing cycle has no valid order — so the ring is closed by a second write, which is
    // also how a corrupted row would reach the database in the first place.
    public static async Task<(DoorTemplate Template, IReadOnlyList<TemplateNode> Nodes)> CyclicTemplateAsync(
        PostgresFixture fixture)
    {
        var template = NewDoorTemplate($"قالب دوري {Guid.NewGuid():N}");
        var first = NewTemplateNode(template.DoorTemplateId, $"عقدة أولى {Guid.NewGuid():N}", 0);
        var second = NewTemplateNode(template.DoorTemplateId, $"عقدة ثانية {Guid.NewGuid():N}", 0, first.TemplateNodeId);

        await AbwabTreeSeeding.InsertAsync(fixture, template);
        await AbwabTreeSeeding.InsertAsync(fixture, first, second);

        await using (var db = SecurityTestHarness.CreateContext(fixture))
        {
            var tracked = await db.Set<TemplateNode>().SingleAsync(n => n.TemplateNodeId == first.TemplateNodeId);
            tracked.ParentTemplateNodeId = second.TemplateNodeId;
            await db.SaveChangesAsync();
        }

        first.ParentTemplateNodeId = second.TemplateNodeId;
        return (template, [first, second]);
    }

    public static async Task<DoorTemplateAggregate> LoadTemplateAggregateAsync(PostgresFixture fixture, Guid doorTemplateId)
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);

        var template = await db.Set<DoorTemplate>().AsNoTracking().SingleAsync(t => t.DoorTemplateId == doorTemplateId);
        var nodes = await db.Set<TemplateNode>().AsNoTracking()
            .Where(n => n.DoorTemplateId == doorTemplateId)
            .OrderBy(n => n.SiblingOrder)
            .ToListAsync();
        var nodeIds = nodes.Select(n => n.TemplateNodeId).ToList();
        var aliases = await db.Set<TemplateNodeSearchAlias>().AsNoTracking()
            .Where(a => nodeIds.Contains(a.TemplateNodeId))
            .ToListAsync();

        return new DoorTemplateAggregate(template, nodes, aliases);
    }

    public static async Task<(Category Root, IReadOnlyList<Category> Children)> CategorySubtreeAsync(
        PostgresFixture fixture, int childCount, string label = "فرع")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(childCount);

        var section = AbwabTreeSeeding.NewSection($"قسم {label} {Guid.NewGuid():N}");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory($"{label} جذر", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var children = Enumerable.Range(0, childCount)
            .Select(i => AbwabTreeSeeding.NewChildCategory($"{label} {i}", root, siblingOrder: i))
            .ToList();

        if (children.Count > 0)
        {
            await AbwabTreeSeeding.InsertAsync(fixture, children.ToArray());
        }

        return (root, children);
    }
}
