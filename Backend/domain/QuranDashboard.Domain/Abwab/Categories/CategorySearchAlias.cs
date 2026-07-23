using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Categories;

public sealed class CategorySearchAlias : IAbwabAuditable
{
    public Guid CategorySearchAliasId { get; set; }

    public Guid CategoryId { get; set; }

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }
}
