namespace QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;

public static class ImportValidationCheckIds
{
    public const string SurahCount = "surah-count";
    public const string AyahCount = "ayah-count";
    public const string PageCount = "page-count";
    public const string LineCount = "line-count";
    public const string WordCount = "word-count";
    public const string MarkerCount = "marker-count";
    public const string ReadableCount = "readable-count";
    public const string DuplicateLocation = "duplicate-location";
    public const string IdContiguous = "id-contiguous";
    public const string SourceAlignment = "source-alignment";
    public const string ImlaeiCleanKey = "imlaei-clean-key";
    public const string MasaqSearchWords = "masaq-search-words";
    public const string LayoutCoverage = "layout-coverage";
    public const string WordPageLine = "word-page-line";
    public const string LineWordRefs = "line-word-refs";
    public const string BismillahBasmallah = "bismillah-basmallah";
    public const string DenormPageLine = "denorm-page-line";
    public const string PageReconstruct = "page-reconstruct";
    public const string Ayah37130Count = "ayah-37-130-count";
    public const string EncodingInfo = "encoding-info";

    public static readonly IReadOnlyDictionary<string, string> Severities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SurahCount] = ImportValidationSeverities.Hard,
            [AyahCount] = ImportValidationSeverities.Hard,
            [PageCount] = ImportValidationSeverities.Hard,
            [LineCount] = ImportValidationSeverities.Hard,
            [WordCount] = ImportValidationSeverities.Hard,
            [MarkerCount] = ImportValidationSeverities.Hard,
            [ReadableCount] = ImportValidationSeverities.Hard,
            [DuplicateLocation] = ImportValidationSeverities.Hard,
            [IdContiguous] = ImportValidationSeverities.Hard,
            [SourceAlignment] = ImportValidationSeverities.Hard,
            [ImlaeiCleanKey] = ImportValidationSeverities.Hard,
            [MasaqSearchWords] = ImportValidationSeverities.Hard,
            [LayoutCoverage] = ImportValidationSeverities.Hard,
            [WordPageLine] = ImportValidationSeverities.Hard,
            [LineWordRefs] = ImportValidationSeverities.Hard,
            [BismillahBasmallah] = ImportValidationSeverities.Hard,
            [DenormPageLine] = ImportValidationSeverities.Hard,
            [PageReconstruct] = ImportValidationSeverities.Hard,
            [Ayah37130Count] = ImportValidationSeverities.Warning,
            [EncodingInfo] = ImportValidationSeverities.Info
        };

    public static readonly IReadOnlyList<string> All =
    [
        SurahCount,
        AyahCount,
        PageCount,
        LineCount,
        WordCount,
        MarkerCount,
        ReadableCount,
        DuplicateLocation,
        IdContiguous,
        SourceAlignment,
        ImlaeiCleanKey,
        MasaqSearchWords,
        LayoutCoverage,
        WordPageLine,
        LineWordRefs,
        BismillahBasmallah,
        DenormPageLine,
        PageReconstruct,
        Ayah37130Count,
        EncodingInfo
    ];
}
