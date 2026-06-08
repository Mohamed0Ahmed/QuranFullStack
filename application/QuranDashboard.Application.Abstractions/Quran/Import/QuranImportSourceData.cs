namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record QuranImportSourceData(
    IReadOnlyList<SurahMetaDto> Surahs,
    IReadOnlyList<AyahMetaDto> Ayahs,
    IReadOnlyList<WordRecordDto> Glyph,
    IReadOnlyList<WordRecordDto> Uthmani,
    IReadOnlyList<WordRecordDto> UthmaniSimple,
    IReadOnlyList<WordRecordDto> ImlaeiSimple,
    LayoutDto Layout);
