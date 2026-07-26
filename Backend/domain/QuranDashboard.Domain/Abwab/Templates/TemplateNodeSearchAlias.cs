using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Templates;

public sealed class TemplateNodeSearchAlias : IAbwabAuditable
{
    public Guid TemplateNodeSearchAliasId { get; set; }

    public Guid TemplateNodeId { get; set; }

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public uint Version { get; set; }
}
