using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>> GetLinkingSelectionAsync(
        PhraseContextSelection selection,
        PhraseLinkingAyahSelection ayahSelection,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.BuildChanged();
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidReference();
        }

        var loaded = await ReadAllFilteredOccurrencesAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            cancellationToken);
        var rowsByAyah = loaded.Items
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var orderedPopulationAyahIds = rowsByAyah
            .OrderBy(pair => pair.Value[0].SurahNumber)
            .ThenBy(pair => pair.Value[0].AyahNumber)
            .Select(pair => pair.Key)
            .ToList();
        var selectedAyahIds = PhraseLinkingSelectionResolver.ResolveAyahIds(
            ayahSelection,
            orderedPopulationAyahIds);
        if (selectedAyahIds is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidSelection();
        }

        var selectedRowsByAyah = selectedAyahIds
            .Select(ayahId => rowsByAyah[ayahId])
            .ToList();
        var selectedRows = selectedRowsByAyah
            .SelectMany(rows => rows)
            .ToList();
        var occurrences = await LoadContextOccurrencesAsync(
            selectedRows,
            selection.Resolution,
            cancellationToken);
        var ayahs = selectedRowsByAyah
            .Select(rows => CreateLinkingSelectionAyah(
                rows.Select(row => occurrences[row.OccurrenceId]).ToList(),
                selection))
            .ToList();
        var response = new PhraseContextLinkingSelectionResponse(
            snapshot.ActiveBuildId,
            ayahs.Count,
            ayahs);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Success(response);
    }

    private PhraseContextLinkingSelectionAyahDto CreateLinkingSelectionAyah(
        IReadOnlyList<ContextOccurrence> occurrences,
        PhraseContextSelection selection)
    {
        var ayah = CreateContextAyah(occurrences, selection);
        var first = occurrences[0];
        var selectedWordIds = ayah.Highlights.QueryQuranWordIds
            .Concat(ayah.Highlights.PreviousQuranWordIds)
            .Concat(ayah.Highlights.FollowingQuranWordIds)
            .ToHashSet();
        var canonicalWordIds = PhraseLinkingSelectionResolver.OrderSelectedWords(
            first.Words.Select(word => word.QuranWordId).ToList(),
            selectedWordIds);

        return new PhraseContextLinkingSelectionAyahDto(
            ayah.AyahId,
            ayah.VerseKey,
            first.Row.PageFrom,
            canonicalWordIds);
    }
}
