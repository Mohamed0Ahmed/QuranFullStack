using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Support;

internal static class AbwabRelationshipTemplateSeeding
{
    public static async Task<(Category Lower, Category Higher)> TwoCategoryEndpointsAsync(
        PostgresFixture fixture, string firstName = "باب أدنى", string secondName = "باب أعلى")
    {
        var section = AbwabTreeSeeding.NewSection($"قسم علاقات {Guid.NewGuid():N}");
        var first = AbwabTreeSeeding.NewRootCategory(firstName, section.SectionId, sectionOrder: 0, globalOrder: 0);
        var second = AbwabTreeSeeding.NewRootCategory(secondName, section.SectionId, sectionOrder: 1, globalOrder: 1);
        await AbwabTreeSeeding.InsertAsync(fixture, section, first, second);

        return first.CategoryId.CompareTo(second.CategoryId) < 0 ? (first, second) : (second, first);
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
