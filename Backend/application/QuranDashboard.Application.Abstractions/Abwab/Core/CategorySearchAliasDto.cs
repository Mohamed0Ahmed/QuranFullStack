namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategorySearchAliasDto(
    Guid CategorySearchAliasId,
    string Value,
    uint Version);
