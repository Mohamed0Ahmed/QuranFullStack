namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record QuranImportSourceData(
    IReadOnlyList<SurahMetaDto> Surahs,
    IReadOnlyList<AyahMetaDto> Ayahs,
    IReadOnlyList<WordRecordDto> Glyph,
    IReadOnlyList<WordRecordDto> Uthmani,
    IReadOnlyList<WordRecordDto> UthmaniSimple,
    IReadOnlyList<WordRecordDto> ImlaeiSimple,
    LayoutDto Layout,
    string ManifestVersion = "1");
