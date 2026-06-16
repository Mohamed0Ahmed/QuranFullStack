namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public sealed record NavigationMetadataSourceData(
    IReadOnlyList<NavigationDivisionDto> Juz,
    IReadOnlyList<NavigationDivisionDto> Hizb,
    IReadOnlyList<NavigationDivisionDto> Rub,
    IReadOnlyList<NavigationSajdaDto> Sajda,
    IReadOnlyList<NavigationSourceFileDto> SourceFiles);

public sealed record NavigationDivisionDto(
    short Number,
    short VersesCount,
    string FirstVerseKey,
    string LastVerseKey,
    IReadOnlyDictionary<string, string> VerseMapping);

public sealed record NavigationSajdaDto(
    short SajdahNumber,
    string VerseKey,
    string SajdahType);

public sealed record NavigationSourceFileDto(
    string RelativePath,
    string DatasetKey,
    int RecordCount,
    string Sha256,
    long SizeBytes);
