using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseSimilarityLinkingSelection;

public sealed record ResolvePhraseSimilarityLinkingSelectionQuery(
    string? ResolutionRef,
    int? MinimumMatchedWords,
    PhraseLinkingAyahSelection Selection);
