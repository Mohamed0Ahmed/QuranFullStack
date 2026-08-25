namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record QuranImportSourceData(
    IReadOnlyList<SurahMetaDto> Surahs,
    IReadOnlyList<AyahMetaDto> Ayahs,
    IReadOnlyList<WordRecordDto> Glyph,
    IReadOnlyList<WordRecordDto> Uthmani,
    IReadOnlyList<WordRecordDto> UthmaniSimple,
    IReadOnlyList<WordRecordDto> ImlaeiSimple,
    MasaqSearchWordsSourceSummary MasaqSearchWords,
    LayoutDto Layout,
    string ManifestVersion = "1");

public sealed record MasaqSearchWordsSourceSummary(
    string FilePath,
    string Schema,
    string Sha256,
    int WordCount,
    int UniqueTextCount);
