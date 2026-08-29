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
    int UniqueTextCount)
{
    public const string ApprovedSha256 =
        "c49838df2cc3e4c7a89ac5124321d6eeb324b7544b83175d62bfaa5234ba325a";
}
