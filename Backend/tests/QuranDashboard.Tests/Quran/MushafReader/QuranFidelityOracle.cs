namespace QuranDashboard.Tests.Quran.MushafReader;

internal sealed record QuranFidelityOracle(
    int ContractVersion,
    int PageNumber,
    QuranFidelityReview Review,
    IReadOnlyList<QuranFidelitySourceIdentity> SourceIdentities,
    IReadOnlyDictionary<string, int> RowCounts,
    QuranFidelityReview RowCountsReview,
    QuranFidelityStudy Study,
    IReadOnlyList<QuranFidelityAyah> Ayahs,
    IReadOnlyList<QuranFidelityLine> Lines,
    IReadOnlyList<QuranFidelityWord> Words);

internal sealed record QuranFidelityReview(
    string ReviewedOn,
    string Authority,
    string Method);

internal sealed record QuranFidelitySourceIdentity(
    string Id,
    string Version,
    string Sha256,
    string Role,
    string Provenance);

internal sealed record QuranFidelityStudy(
    string VerseKey,
    QuranFidelityTafsir Tafsir,
    QuranFidelityTranslation Translation);

internal sealed record QuranFidelityTafsir(
    string SourceKey,
    string DisplayNameAr,
    string ShortNameAr,
    string LanguageCode,
    string Direction,
    string TafsirKind,
    string SourceValueKind,
    string SourceLeaderVerseKey,
    bool IsGroupLeader,
    int CoveredAyahCount,
    IReadOnlyList<string> CoveredAyahKeys,
    string Text);

internal sealed record QuranFidelityTranslation(
    string SourceKey,
    string DisplayNameAr,
    string DisplayNameEn,
    string LanguageCode,
    string Direction,
    string TranslationType,
    bool ContainsHtmlMarkup,
    string Text);

internal sealed record QuranFidelityAyah(
    string VerseKey,
    string TextUthmani,
    IReadOnlyList<string> WordLocations);

internal sealed record QuranFidelityLine(
    int LineNumber,
    string LineType,
    bool IsCentered,
    int? SurahNumber,
    IReadOnlyList<string> WordLocations);

internal sealed record QuranFidelityWord(
    string Location,
    string VerseKey,
    string TextUthmani,
    bool IsAyahMarker);

internal static class QuranFidelityOracleDocument
{
    private const string OracleResourceSuffix = "quran-fidelity-oracle.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static byte[] ReadOracleBytes() => ReadEmbeddedBytes(OracleResourceSuffix);

    internal static QuranFidelityOracle ReadOracle() =>
        Deserialize<QuranFidelityOracle>(ReadOracleBytes(), OracleResourceSuffix);

    private static T Deserialize<T>(byte[] bytes, string resourceName)
    {
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions)
            ?? throw new InvalidOperationException($"Embedded Quran fidelity resource '{resourceName}' was empty.");
    }

    private static byte[] ReadEmbeddedBytes(string resourceSuffix)
    {
        var assembly = typeof(QuranFidelityOracleDocument).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(resourceSuffix, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded Quran fidelity resource '{resourceName}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
