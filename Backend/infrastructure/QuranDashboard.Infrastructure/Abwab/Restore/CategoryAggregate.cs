using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record CategoryAggregate(Category Category, IReadOnlyList<CategorySearchAlias> Aliases);
