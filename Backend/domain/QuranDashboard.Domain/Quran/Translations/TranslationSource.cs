namespace QuranDashboard.Domain.Quran.Translations;

public sealed class TranslationSource
{
    public int Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageNameEn { get; set; } = string.Empty;
    public string LanguageNameAr { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string TranslationType { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string? TranslatorKey { get; set; }
    public string? TranslatorNameEn { get; set; }
    public string? TranslatorNameAr { get; set; }
    public bool ContainsInlineFootnotes { get; set; }
    public bool ContainsHtmlMarkup { get; set; }
    public short ContentCoverageCount { get; set; }
}
