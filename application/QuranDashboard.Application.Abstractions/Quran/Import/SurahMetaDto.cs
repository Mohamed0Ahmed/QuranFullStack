namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record SurahMetaDto(
    int Id,
    string Name,
    string NameSimple,
    string NameArabic,
    int RevelationOrder,
    string RevelationPlace,
    int VersesCount,
    bool BismillahPre);
