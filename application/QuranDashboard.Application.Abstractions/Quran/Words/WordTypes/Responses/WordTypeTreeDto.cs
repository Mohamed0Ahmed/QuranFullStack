namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeTreeDto(IReadOnlyList<WordTypeTreeNodeDto> MainTypes);

public sealed record WordTypeTreeNodeDto(
    string Code,
    WordTypeLabelDto Label,
    int Count,
    WordTypeSecondaryFilterDto SecondaryFilter,
    IReadOnlyList<WordTypeChildNodeDto> Children);

public sealed record WordTypeChildNodeDto(
    string Code,
    string ChildCode,
    WordTypeLabelDto Label,
    int Count);

public sealed record WordTypeSecondaryFilterDto(
    string Kind,
    IReadOnlyList<WordTypeFilterOptionDto> Options,
    IReadOnlyList<WordTypeFilterOptionDto> VoiceOptions);

public sealed record WordTypeFilterOptionDto(string Code, WordTypeLabelDto Label);
