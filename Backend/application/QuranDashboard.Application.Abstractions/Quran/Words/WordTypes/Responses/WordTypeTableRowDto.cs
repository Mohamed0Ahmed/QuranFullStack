using System.Text.Json.Serialization;

namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(WordTableRowDto), "word")]
[JsonDerivedType(typeof(RootTableRowDto), "root")]
[JsonDerivedType(typeof(StemTableRowDto), "stem")]
[JsonDerivedType(typeof(LemmaTableRowDto), "lemma")]
public abstract record WordTypeTableRowDto;

// Word variant — carries the existing full composite identity (TashkeelWordId + ContextCode + Case +
// Tense + Voice) directly in the payload, a superset of the preserved WordTypeRowDto (/words).
public sealed record WordTableRowDto(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice,
    string DisplayText,
    string TypeCode,
    WordTypeLabelDto TypeLabel,
    WordTypeLabelDto BroadLabel,
    string? CaseOrFeature,
    string? RootText,
    string? LemmaText,
    string? StemText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount) : WordTypeTableRowDto;

// Grouped variants carry the numeric stable identity used to open their scoped detail views.
public sealed record RootTableRowDto(
    int RootId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount) : WordTypeTableRowDto;

public sealed record StemTableRowDto(
    int StemId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount) : WordTypeTableRowDto;

public sealed record LemmaTableRowDto(
    int LemmaId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount) : WordTypeTableRowDto;
